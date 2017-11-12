using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;

using Com.AugustCellars.CoAP.Log;
using Com.AugustCellars.CoAP.Net;
using Com.AugustCellars.CoAP.Stack;
using Com.AugustCellars.COSE;
using PeterO.Cbor;
using Attributes = Com.AugustCellars.COSE.Attributes;

namespace Com.AugustCellars.CoAP.OSCOAP
{
#if INCLUDE_OSCOAP
    public class OscoapLayer : AbstractLayer
    {
        static readonly ILogger _Log = LogManager.GetLogger(typeof(OscoapLayer));
        static readonly byte[] _FixedHeader = new byte[] {0x40, 0x01, 0xff, 0xff};
        Boolean _replayWindow = true;
        readonly ConcurrentDictionary<Exchange.KeyUri, BlockHolder> _ongoingExchanges = new ConcurrentDictionary<Exchange.KeyUri, BlockHolder>();

        class BlockHolder
        {
            readonly BlockwiseStatus _requestStatus;
            readonly BlockwiseStatus _responseStatus;
            readonly Response _response;

            public BlockHolder(Exchange exchange)
            {
                _requestStatus = exchange.OSCOAP_RequestBlockStatus;
                _responseStatus = exchange.OSCOAP_ResponseBlockStatus;
                _response = exchange.Response;
            }

            public void RestoreTo(Exchange exchange)
            {
                exchange.OSCOAP_RequestBlockStatus = _requestStatus;
                exchange.OSCOAP_ResponseBlockStatus = _responseStatus;
                exchange.Response = _response;
            }
        }


        /// <summary>
        /// Constructs a new OSCAP layer.
        /// </summary>
        public OscoapLayer(ICoapConfig config)
        {
            _replayWindow = config.OSCOAP_ReplayWindow;
            if (_Log.IsInfoEnabled) {
                _Log.Info("OscoapLayer - replay=" + _replayWindow.ToString()); // Print out config if any
            }

            config.PropertyChanged += ConfigChanged;

            // _replayWindow = false;
        }

        void ConfigChanged(Object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            ICoapConfig config = (ICoapConfig) sender;
            if (String.Equals(e.PropertyName, "OSCOAP_ReplayWindow")) _replayWindow = config.OSCOAP_ReplayWindow;
        }

        /// <inheritdoc />
        public override void SendRequest(INextLayer nextLayer, Exchange exchange, Request request)
        {
            if ((request.OscoapContext != null) || (exchange.OscoapContext != null)) {
                SecurityContext ctx = exchange.OscoapContext;
                if (request.OscoapContext != null) {
                    ctx = request.OscoapContext;
                    exchange.OscoapContext = ctx;
                }

                Codec.IMessageEncoder me = Spec.NewMessageEncoder();
                Request encryptedRequest = new Request(request.Method);

                if (request.Payload != null) {
                    encryptedRequest.Payload = request.Payload;
                }

                MoveRequestHeaders(request, encryptedRequest);

                _Log.Info(m => m("New inner response message\n{0}", encryptedRequest.ToString()));

                ctx.Sender.IncrementSequenceNumber();

                Encrypt0Message enc = new Encrypt0Message(false);
                byte[] msg = me.Encode(encryptedRequest);
                int tokenSize = msg[0] & 0xf;
                byte[] msg2 = new byte[msg.Length - (3 + tokenSize)];
                Array.Copy(msg, 4 + tokenSize, msg2, 1, msg2.Length-1);
                msg2[0] = msg[1];
                enc.SetContent(msg2);

                // Build AAD
                CBORObject aad = CBORObject.NewArray();
                aad.Add(CBORObject.FromObject(1)); // version
                aad.Add(CBORObject.FromObject(ctx.Sender.Algorithm));
                aad.Add(CBORObject.FromObject(ctx.Sender.Id));
                aad.Add(CBORObject.FromObject(ctx.Sender.PartialIV));
                aad.Add(CBORObject.FromObject(new byte[0]));
                if (ctx.GroupId != null) {
                    aad.Add(CBORObject.FromObject(ctx.GroupId));
                }

#if DEBUG
                switch (SecurityContext.FutzError) {
                    case 1:
                        aad[0] = CBORObject.FromObject(2);
                        break; // Change version #
                    case 2:
                        aad[1] = CBORObject.FromObject(request.Code + 1);
                        break; // Change request code
                    case 3:
                        aad[2] = CBORObject.FromObject(ctx.Sender.Algorithm.AsInt32() + 1);
                        break; // Change algorithm number
                }
#endif

                _Log.Info(m => m("SendRequest: AAD = {0}", BitConverter.ToString(aad.EncodeToBytes())));

                enc.SetExternalData(aad.EncodeToBytes());
#if DEBUG
                {
                    byte[] fooX = ctx.Sender.PartialIV;
                    if (SecurityContext.FutzError == 8) fooX[fooX.Length - 1] += 1;
                    enc.AddAttribute(HeaderKeys.IV, ctx.Sender.GetIV(fooX), Attributes.DO_NOT_SEND);
                }
#else
                enc.AddAttribute(HeaderKeys.IV, ctx.Sender.GetIV(ctx.Sender.PartialIV), Attributes.DO_NOT_SEND);
#endif
                enc.AddAttribute(HeaderKeys.PartialIV, CBORObject.FromObject(ctx.Sender.PartialIV), /* Attributes.PROTECTED */ Attributes.DO_NOT_SEND);
                enc.AddAttribute(HeaderKeys.Algorithm, ctx.Sender.Algorithm, Attributes.DO_NOT_SEND);
                enc.AddAttribute(HeaderKeys.KeyId, CBORObject.FromObject(ctx.Sender.Id), /*Attributes.PROTECTED*/ Attributes.DO_NOT_SEND);
                if (ctx.GroupId != null) {
                    enc.AddAttribute(CBORObject.FromObject("gid"), CBORObject.FromObject(ctx.GroupId), Attributes.DO_NOT_SEND);
                }

                if (_Log.IsInfoEnabled) {
                    _Log.Info("SendRequest: AAD = " + BitConverter.ToString(aad.EncodeToBytes()));
                    _Log.Info("SendRequest: IV = " + BitConverter.ToString(ctx.Sender.GetIV(ctx.Sender.PartialIV).GetByteString()));
                    _Log.Info("SendRequest: Key = " + BitConverter.ToString(ctx.Sender.Key));
                }

                enc.Encrypt(ctx.Sender.Key);

                if (ctx.Sender.SigningKey != null) {
                    CounterSignature sig = new CounterSignature(ctx.Sender.SigningKey);
                    sig.AddAttribute(HeaderKeys.Algorithm, ctx.Sender.SigningKey[CoseKeyKeys.Algorithm], Attributes.DO_NOT_SEND);
                    sig.SetObject(enc);
                    CBORObject aad2 = ctx.Sender.SigningKey[CoseKeyKeys.Algorithm];
                    sig.SetExternalData(aad2.EncodeToBytes());
                    CBORObject signatureBytes = sig.EncodeToCBORObject();
                    enc.AddAttribute(HeaderKeys.CounterSignature, signatureBytes, Attributes.DO_NOT_SEND);
                }

                byte[] optionValue = DoCompression(enc);

                OscoapOption o = new OscoapOption();
                o.Set(optionValue);
                request.AddOption(o);
                request.Payload = enc.GetEncryptedContent();

                if (request.HasOption(OptionType.Observe)) request.Method = Method.FETCH;
                else request.Method = Method.POST;
            }
            base.SendRequest(nextLayer, exchange, request);
        }

        /// <inheritdoc />
        public override void ReceiveRequest(INextLayer nextLayer, Exchange exchange, Request request)
        {
            if (!request.HasOption(OptionType.Oscoap)) {
                base.ReceiveRequest(nextLayer, exchange, request);
                return;
            }
            Response response;
            try {
                Option op = request.GetFirstOption(OptionType.Oscoap);
                request.RemoveOptions(OptionType.Oscoap);

                _Log.Info(m => m("Incoming Request: {0}", Util.Utils.ToString(request)));

                Encrypt0Message msg = Uncompress(op.RawValue);
                if (msg == null) {
                    //  Only bother to reply to CON messages
                    if (request.Type == MessageType.CON) {
                        response = new Response(StatusCode.BadOption) {
                            PayloadString = "Unable to decompress"
                        };
                        exchange.SendResponse(response);
                    }
                    return;
                }
                msg.SetEncryptedContent(request.Payload);

                List<SecurityContext> contexts = new List<SecurityContext>();
                SecurityContext ctx = null;
                CBORObject kid;

                //  We may know the context because it is a follow up on a conversation - 
                //  In which case we can just use the same one.
                //  M00BUG - Multicast problem of recipient ID?

                CBORObject gid = null;
                if (exchange.OscoapContext != null) {
                    contexts.Add(exchange.OscoapContext);
                    if (exchange.OscoapContext.GroupId != null) {
                        gid = CBORObject.FromObject(exchange.OscoapContext.GroupId);
                    }
                    kid = CBORObject.FromObject(exchange.OscoapSenderId);
                }
                else {
                    gid = msg.FindAttribute(CBORObject.FromObject("gid"));
                    kid = msg.FindAttribute(HeaderKeys.KeyId);

                    if (kid == null) {
                        exchange.SendResponse(new Response(StatusCode.BadRequest));
                        return;
                    }

                    if (gid != null) {
                        contexts = SecurityContextSet.AllContexts.FindByGroupId(gid.GetByteString());
                    }
                    else {
                        contexts = SecurityContextSet.AllContexts.FindByKid(kid.GetByteString());
                    }

                    if (contexts.Count == 0) {
                        response = new Response(StatusCode.Unauthorized)
                        {
                            PayloadString = "No Context Found - 1"
                        };
                        exchange.SendResponse(response);
                        return; // Ignore messages that have no known security context.
                    }
                }

                byte[] partialIV = msg.FindAttribute(HeaderKeys.PartialIV).GetByteString();

                //  Build AAD
                CBORObject aad = CBORObject.NewArray();
                aad.Add(CBORObject.FromObject(1)); // M00BUG
                aad.Add(CBORObject.FromObject(0)); // Place holder for algorithm
                aad.Add(CBORObject.FromObject(kid));
                aad.Add(CBORObject.FromObject(partialIV));
                aad.Add(CBORObject.FromObject(new byte[0])); // encoded I options
                if (gid != null) {
                    aad.Add(gid);
                }

                byte[] payload = null;

                byte[] seqNoArray = new byte[8];
                Array.Copy(partialIV, 0, seqNoArray, 8 - partialIV.Length, partialIV.Length);
                if (BitConverter.IsLittleEndian) Array.Reverse(seqNoArray);
                Int64 seqNo = BitConverter.ToInt64(seqNoArray, 0);

                String responseString = "General decrypt failure";

                foreach (SecurityContext context in contexts) {
                    SecurityContext.EntityContext recip = context.Recipient;
                    if (recip == null) {
                        if (kid == null) continue;
                        recip = context.Recipients[kid.GetByteString()];
                        if (recip == null) continue;
                    }

                    if (_replayWindow && recip.ReplayWindow.HitTest(seqNo)) {
                        _Log.Info(m => m("Hit test on {0} failed", seqNo));
                        responseString = "Hit test - duplicate";
                        continue;
                    }

                    aad[1] = recip.Algorithm;

                    if (_Log.IsInfoEnabled) {
                        _Log.Info("AAD = " + BitConverter.ToString(aad.EncodeToBytes()));
                        _Log.Info("IV = " + BitConverter.ToString(recip.GetIV(partialIV).GetByteString()));
                        _Log.Info("Key = " + BitConverter.ToString(recip.Key));
                    }


                    msg.SetExternalData(aad.EncodeToBytes());

                    msg.AddAttribute(HeaderKeys.Algorithm, recip.Algorithm, Attributes.DO_NOT_SEND);
                    msg.AddAttribute(HeaderKeys.IV, recip.GetIV(partialIV), Attributes.DO_NOT_SEND);

                    try {
                        ctx = context;
                        payload = msg.Decrypt(recip.Key);
                        recip.ReplayWindow.SetHit(seqNo);
                    }
                    catch (Exception e) {
                        if (_Log.IsInfoEnabled) _Log.Info("--- ",  e);
                        ctx = null;
                    }

                    if (ctx != null) {
                        break;
                    }
                }

                if (ctx == null) {
                    if (request.Type == MessageType.CON) {
                        response = new Response(StatusCode.BadRequest) {
                            PayloadString = responseString
                        };
                        exchange.SendResponse(response);
                    }
                    return;
                }

                exchange.OscoapContext = ctx; // So we know it on the way back.
                request.OscoapContext = ctx;
                exchange.OscoapSequenceNumber = partialIV;
                exchange.OscoapSenderId = kid.GetByteString();

                byte[] newRequestData = new byte[payload.Length + _FixedHeader.Length-1];
                Array.Copy(_FixedHeader, newRequestData, _FixedHeader.Length);
                Array.Copy(payload, 1, newRequestData, _FixedHeader.Length, payload.Length-1);
                newRequestData[1] = payload[0];

                Codec.IMessageDecoder me = Spec.NewMessageDecoder(newRequestData);
                Request newRequest = me.DecodeRequest();

                //  Update headers is a pain

                RestoreOptions(request, newRequest);
                request.Method = newRequest.Method;

                if (_Log.IsInfoEnabled) {
                    // log.Info(String.Format("Secure message post = " + Util.Utils.ToString(request)));
                }

                //  We may want a new exchange at this point if it relates to a new message for blockwise.

                if (request.HasOption(OptionType.Block2)) {
                    Exchange.KeyUri keyUri = new Exchange.KeyUri(request.URI, null, request.Source);
                    BlockHolder block;
                    _ongoingExchanges.TryGetValue(keyUri, out block);

                    if (block != null) {
                        block.RestoreTo(exchange);
                    }
                }

                request.Payload = newRequest.Payload;
            }
            catch (Exception e) {
                _Log.Error("OSCOAP Layer: reject message because ", e);
                exchange.OscoapContext = null;

                if (request.Type == MessageType.CON) {
                    response = new Response(StatusCode.Unauthorized) {
                        Payload = Encoding.UTF8.GetBytes("Error is " + e.Message)
                    };
                    exchange.SendResponse(response);
                }
                //  Ignore messages that we cannot decrypt.
                return;
            }

            base.ReceiveRequest(nextLayer, exchange, request);
        }

        public override void SendResponse(INextLayer nextLayer, Exchange exchange, Response response)
        {
            if (exchange.OscoapContext != null) {
                SecurityContext ctx = exchange.OscoapContext;

                Codec.IMessageEncoder me = Spec.NewMessageEncoder();
                Response encryptedResponse = new Response((StatusCode) response.Code);

                if (response.Payload != null) {
                    encryptedResponse.Payload = response.Payload;
                }

                MoveResponseHeaders(response, encryptedResponse);

                if (_Log.IsInfoEnabled) {
                    _Log.Info("SendResponse: New inner response message");
                    _Log.Info(encryptedResponse.ToString());
                }

                //  Build AAD
                CBORObject aad = CBORObject.NewArray();
                aad.Add(1);
                aad.Add(ctx.Sender.Algorithm);
                aad.Add(exchange.OscoapSenderId);
                aad.Add(exchange.OscoapSequenceNumber);
                aad.Add(CBORObject.FromObject(new byte[0])); // Options
                if (ctx.GroupId != null) {
                    aad.Add(ctx.GroupId);
                }

                if (_Log.IsInfoEnabled) {
                    _Log.Info("SendResponse: AAD = " + BitConverter.ToString(aad.EncodeToBytes()));
                }

                Encrypt0Message enc = new Encrypt0Message(false);
                byte[] msg = me.Encode(encryptedResponse);
                int tokenSize = msg[0] & 0xf;
                byte[] msg2 = new byte[msg.Length - (3 + tokenSize)];
                Array.Copy(msg, 4 + tokenSize, msg2, 1, msg2.Length-1);
                msg2[0] = msg[1];
                enc.SetContent(msg2);
                enc.SetExternalData(aad.EncodeToBytes());

                if (response.HasOption(OptionType.Observe) || ctx.GroupId != null) {
                    enc.AddAttribute(HeaderKeys.PartialIV, CBORObject.FromObject(ctx.Sender.PartialIV), Attributes.UNPROTECTED);
                    enc.AddAttribute(HeaderKeys.IV, ctx.Sender.GetIV(ctx.Sender.PartialIV), Attributes.DO_NOT_SEND);
                    ctx.Sender.IncrementSequenceNumber();
                    if (ctx.GroupId != null) {
                        enc.AddAttribute(HeaderKeys.KeyId, CBORObject.FromObject(ctx.Sender.Id), Attributes.UNPROTECTED);
                    }
                }
                else {
                    CBORObject iv = ctx.Recipient.GetIV(exchange.OscoapSequenceNumber);

                    enc.AddAttribute(HeaderKeys.IV, iv, Attributes.DO_NOT_SEND);

                }

                _Log.Info(m => m($"SendResponse: IV = {BitConverter.ToString(enc.FindAttribute(HeaderKeys.IV, Attributes.DO_NOT_SEND).GetByteString())}"));
                _Log.Info(m =>m($"SendResponse: Key = {BitConverter.ToString(ctx.Sender.Key)}"));

                enc.AddAttribute(HeaderKeys.Algorithm, ctx.Sender.Algorithm, Attributes.DO_NOT_SEND);
                enc.Encrypt(ctx.Sender.Key);

                byte[] finalBody = DoCompression(enc);

                    OscoapOption o = new OscoapOption(OptionType.Oscoap);
                    o.Set(finalBody);
                    response.AddOption(o);
                    response.StatusCode = StatusCode.Content;
                response.Payload = enc.GetEncryptedContent();

                //  Need to be able to retrieve this again undersome cirumstances.

                if (encryptedResponse.HasOption(OptionType.Block2)) {
                    Request request = exchange.CurrentRequest;
                    Exchange.KeyUri keyUri = new Exchange.KeyUri(request.URI, null, response.Destination);

                    //  Observe notification only send the first block, hence do not store them as ongoing
                    if (exchange.OSCOAP_ResponseBlockStatus != null && !encryptedResponse.HasOption(OptionType.Observe)) {
                        //  Remember ongoing blockwise GET requests
                        BlockHolder blockInfo = new BlockHolder(exchange);
                        if (Util.Utils.Put(_ongoingExchanges, keyUri, blockInfo) == null) {
                            if (_Log.IsInfoEnabled) _Log.Info("Ongoing Block2 started late, storing " + keyUri + " for " + request);

                        }
                        else {
                            if (_Log.IsInfoEnabled) _Log.Info("Ongoing Block2 continued, storing " + keyUri + " for " + request);
                        }
                    }
                    else {
                        if (_Log.IsInfoEnabled) _Log.Info("Ongoing Block2 completed, cleaning up " + keyUri + " for " + request);
                        BlockHolder exc;
                        _ongoingExchanges.TryRemove(keyUri, out exc);
                    }
                }

            }

            base.SendResponse(nextLayer, exchange, response);
        }

        public override void ReceiveResponse(INextLayer nextLayer, Exchange exchange, Response response)
        {
            if (response.HasOption(OptionType.Oscoap)) {
                Option op = response.GetFirstOption(OptionType.Oscoap);

                if (exchange.OscoapContext == null) {
                    return;
                }

                SecurityContext ctx = exchange.OscoapContext;

                bool fServerIv = true;

                Encrypt0Message msg = Uncompress(op.RawValue);
                if (msg == null) return;
                msg.SetEncryptedContent(response.Payload);

                SecurityContext.EntityContext recip = ctx.Recipient;
                if (recip == null) {
                    if (ctx.GroupId == null) {
                        //  This is not currently a valid state to be in
                        return;
                    }
                    CBORObject kid = msg.FindAttribute(HeaderKeys.KeyId);
                    if (kid == null) {
                        //  this is not currently a valid state to be in
                        return;
                    }
                    recip = ctx.Recipients[kid.GetByteString()];
                    if (recip == null) {
                        // M00TODO - deal with asking the user for a recipient structure at this point.
                        return;
                    }
                }

                if (msg.FindAttribute(HeaderKeys.PartialIV) == null) {
                    msg.AddAttribute(HeaderKeys.PartialIV, CBORObject.FromObject(ctx.Sender.PartialIV), Attributes.DO_NOT_SEND);
                    fServerIv = false;
                }


                byte[] partialIV = msg.FindAttribute(HeaderKeys.PartialIV).GetByteString();
                byte[] seqNoArray = new byte[8];
                Array.Copy(partialIV, 0, seqNoArray, 8 - partialIV.Length, partialIV.Length);
                if (BitConverter.IsLittleEndian) Array.Reverse(seqNoArray);
                Int64 seqNo = BitConverter.ToInt64(seqNoArray, 0);

                if (fServerIv) if (_replayWindow && recip.ReplayWindow.HitTest(seqNo)) return;

                msg.AddAttribute(HeaderKeys.Algorithm, recip.Algorithm, Attributes.DO_NOT_SEND);

                CBORObject fullIV;
                if (fServerIv) fullIV = recip.GetIV(partialIV);
                else fullIV = ctx.Sender.GetIV(partialIV);
                msg.AddAttribute(HeaderKeys.IV, fullIV, Attributes.DO_NOT_SEND);

                //  build aad
                CBORObject aad = CBORObject.NewArray();
                aad.Add(1);
                aad.Add(recip.Algorithm);
                aad.Add(ctx.Sender.Id);
                aad.Add(ctx.Sender.PartialIV);
                aad.Add(CBORObject.FromObject(new byte[0])); // OPTIONS
                if (ctx.GroupId != null) aad.Add(ctx.GroupId);

                msg.SetExternalData(aad.EncodeToBytes());

                _Log.Info(m => m($"fServerIv = {fServerIv}"));
                _Log.Info(m => m("ReceiveResponse: AAD = " + BitConverter.ToString(aad.EncodeToBytes())));
                _Log.Info(m => m($"ReceiveResponse: IV = {BitConverter.ToString(fullIV.GetByteString())}"));
                _Log.Info(m => m($"ReceiveResponse: Key = {BitConverter.ToString(recip.Key)}"));

                byte[] payload = msg.Decrypt(recip.Key);

                recip.ReplayWindow.SetHit(seqNo);

                byte[] rgb = new byte[payload.Length + _FixedHeader.Length - 1];
                Array.Copy(_FixedHeader, rgb, _FixedHeader.Length);
                Array.Copy(payload, 1, rgb, _FixedHeader.Length, payload.Length-1);
                rgb[1] = payload[0];
                Codec.IMessageDecoder me = Spec.NewMessageDecoder(rgb);
                Response decryptedReq = me.DecodeResponse();

                response.Payload = decryptedReq.Payload;
                response.StatusCode = decryptedReq.StatusCode;

                RestoreOptions(response, decryptedReq);
            }
            base.ReceiveResponse(nextLayer, exchange, response);
        }

        private static void MoveRequestHeaders(Request unprotected, Request encrypted)
        {
            //
            //  Rules for dealing with Proxy-URI are as follows:
            //
            //  1. Decompose it into the pieces.  
            //  2. Remove all the pieces that we need to protect
            //  3. Put the partial set of fields back together again
            //

            //  Deal with Proxy-Uri
            if (unprotected.ProxyUri != null) {
                int port;
                if (!unprotected.ProxyUri.IsAbsoluteUri) throw new Exception("Must be an absolute URI");
                if (!string.IsNullOrEmpty(unprotected.ProxyUri.Fragment)) throw new Exception("Fragments not allowed in ProxyUri");
                switch (unprotected.ProxyUri.Scheme) {
                    case "coap":
                        port = 5683;
                        break;

                    case "coaps":
                        port = 5684;
                        break;

                    default:
                        throw new Exception("Unsupported schema");
                }

                string strUri = unprotected.ProxyUri.Scheme + ":";

                if (unprotected.ProxyUri.Host[0] != '[') {
                    encrypted.UriHost = unprotected.ProxyUri.Host;
                }
                strUri += "//" + unprotected.ProxyUri.Host;

                if ((unprotected.ProxyUri.Port != 0) && (unprotected.ProxyUri.Port != port)) {
                    encrypted.UriPort = unprotected.ProxyUri.Port;
                    strUri += ":" + port.ToString();
                }

                string p = unprotected.ProxyUri.AbsolutePath;
                if (p != null) {
                    encrypted.UriPath = p;
                }

                if (unprotected.ProxyUri.Query != null) {
                    encrypted.AddUriQuery(unprotected.ProxyUri.Query);
                    unprotected.ClearUriQuery();
                }

                unprotected.URI = new Uri(strUri + "/");
            }

            List<Option> toDelete = new List<Option>();
            foreach (Option op in unprotected.GetOptions()) {
                switch (op.Type) {
                    case OptionType.UriHost:
                    case OptionType.UriPort:
                    case OptionType.ProxyUri:
                    case OptionType.ProxyScheme:
                    case OptionType.Observe:
                        break;

                    default:
                        encrypted.AddOption(op);
                        toDelete.Add(op);
                        break;
                }
            }

            foreach (Option op in toDelete) unprotected.RemoveOptions(op.Type);
            unprotected.URI = null;
        }

        private static void MoveResponseHeaders(Response unprotected, Response encrypted)
        {
            //  Deal with Proxy-Uri
            if (unprotected.ProxyUri != null) {
                throw new Exception("Should not see Proxy-Uri on a response.");
            }

            List<Option> toDelete = new List<Option>();
            foreach (Option op in unprotected.GetOptions()) {
                switch (op.Type) {
                    case OptionType.UriHost:
                    case OptionType.UriPort:
                    case OptionType.ProxyUri:
                    case OptionType.ProxyScheme:
                    case OptionType.Observe:
                        break;

                    default:
                        encrypted.AddOption(op);
                        toDelete.Add(op);
                        break;
                }
            }

            foreach (Option op in toDelete) unprotected.RemoveOptions(op.Type);
        }

        private static void RestoreOptions(Message response, Message decryptedReq)
        {

            foreach (Option op in response.GetOptions()) {
                switch (op.Type) {
                    case OptionType.Block1:
                    case OptionType.Block2:
                    case OptionType.Oscoap:
                    case OptionType.MaxAge:
                        response.RemoveOptions(op.Type);
                        break;

                    case OptionType.UriHost:
                    case OptionType.UriPort:
                    case OptionType.ProxyUri:
                    case OptionType.ProxyScheme:
                        break;

                    default:
                        response.RemoveOptions(op.Type);
                        break;
                }
            }

            foreach (Option op in decryptedReq.GetOptions()) {
                switch (op.Type) {
                    default:
                        response.AddOption(op);
                        break;
                }
            }
        }

        private static byte[] DoCompression(Encrypt0Message msg)
        {
            // Start with 0abc deee
            //  a, b, c and d are presence flags
            //  eee is the length of the iv field.

            int cbSize = 1;

            //

            CBORObject partialIV = msg.FindAttribute(HeaderKeys.PartialIV);
            byte[] iv = new byte[0];
            if (partialIV != null) {
                iv = partialIV.GetByteString();
            }
            cbSize += iv.Length;
            byte head = (byte) iv.Length;

            // Context Hint/Group ID

            CBORObject gid = msg.FindAttribute(CBORObject.FromObject("gid"));
            if (gid != null) {
                cbSize += (1 + gid.GetByteString().Length);
                head |= 0x10;
            }

            CBORObject sig = msg.FindAttribute(HeaderKeys.CounterSignature);
            byte[] sigBytes = null;
            if (sig != null) {
                sigBytes = sig.EncodeToBytes();
                cbSize += (1 + sigBytes.Length);
                head |= 0x20;
            }

            CBORObject kid = msg.FindAttribute(HeaderKeys.KeyId);
            if (kid != null) {
                cbSize += (0 + kid.GetByteString().Length);
                head |= 0x08;
            }

            //  Additional items to flag

            byte[] encBody = new byte[cbSize];
            encBody[0] = head;
            cbSize = 1;
            if (iv.Length > 0) {
                Array.Copy(iv, 0, encBody, cbSize, iv.Length);
                cbSize += iv.Length;
            }

            if (gid != null) {
                if (gid.GetByteString().Length > 255) throw new Exception("GID too large");
                encBody[cbSize] = (byte) gid.GetByteString().Length;
                Array.Copy(gid.GetByteString(), 0, encBody, cbSize + 1, gid.GetByteString().Length);
                cbSize += gid.GetByteString().Length + 1;
            }

            if (sig != null) {
                if (sigBytes.Length > 255) throw new Exception("GID too large");
                encBody[cbSize] = (byte) sigBytes.Length;
                Array.Copy(sigBytes, 0, encBody, cbSize + 1, sig.GetByteString().Length);
                cbSize += sigBytes.Length + 1;
            }

            if (kid != null) {
                if (kid.GetByteString().Length > 255) throw new Exception("KID too large");
                Array.Copy(kid.GetByteString(), 0, encBody, cbSize, kid.GetByteString().Length);
            }

#if DEBUG
            {
                CBORObject xxx = msg.EncodeToCBORObject();
                Console.WriteLine("Protected Attributes = " + BitConverter.ToString(xxx[0].GetByteString()));
            }
#endif

            return encBody;
        }

        private static Encrypt0Message Uncompress(byte[] raw)
        {
            CBORObject map = CBORObject.NewMap();

            if (raw.Length == 0) raw = new byte[1];

            //  Decode the wierd body
            //  First byte is of the form 0abcdeee where abcd are flags and eee is the the IV size.
            if (0 != (raw[0] & 0x80)) return null; // This is not legal
            if (0 != (raw[0] & 0x40)) return null; // These are not currently supported.

            int iX = 1;
            if (0 != (raw[0] & 0x07)) {
                byte[] ivX = new byte[raw[0] & 0x7];
                Array.Copy(raw, iX, ivX, 0, ivX.Length);
                map.Add(HeaderKeys.PartialIV, ivX);
                iX += ivX.Length;
            }

            if (0 != (raw[0] & 0x10)) {
                byte[] gidX = new byte[raw[iX]];
                Array.Copy(raw, iX + 1, gidX, 0, gidX.Length);
                iX += (gidX.Length + 1);
                map.Add(CBORObject.FromObject("gid"), gidX);
            }

            if (0 != (raw[0] & 0x20)) {
                byte[] counter = new byte[raw[iX]];
                Array.Copy(raw, iX + 1, counter, 0, counter.Length);
                iX += (counter.Length + 1);
                map.Add(HeaderKeys.CounterSignature, counter);
            }

            if (0 != (raw[0] & 0x08)) {
                byte[] kidX = new byte[raw.Length - iX];
                Array.Copy(raw, iX, kidX, 0, kidX.Length);
                map.Add(HeaderKeys.KeyId, kidX);
            }

            CBORObject msgX = CBORObject.NewArray();
            msgX.Add(new byte[0]);
            msgX.Add(map);
            msgX.Add(CBORObject.Null);

            Encrypt0Message msg = (Encrypt0Message) COSE.Message.DecodeFromBytes(msgX.EncodeToBytes(), Tags.Encrypt0);

            return msg;
        }
    }
#endif
}
