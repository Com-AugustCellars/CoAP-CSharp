using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;

using Com.AugustCellars.CoAP.Log;
using Com.AugustCellars.CoAP.Net;
using Com.AugustCellars.CoAP.Stack;
using Com.AugustCellars.COSE;
using PeterO.Cbor;
// ReSharper disable All

namespace Com.AugustCellars.CoAP.OSCOAP
{
#if INCLUDE_OSCOAP
    public class OscoapLayer : AbstractLayer
    {
        static readonly ILogger _Log = LogManager.GetLogger(typeof(OscoapLayer));
        static readonly byte[] _FixedHeader = new byte[] { 0x40, 0x01, 0xff, 0xff };
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
            ICoapConfig config = (ICoapConfig)sender;
            if (String.Equals(e.PropertyName, "OSCOAP_ReplayWindow")) _replayWindow = config.OSCOAP_ReplayWindow;
        }

        public override void SendRequest(INextLayer nextLayer, Exchange exchange, Request request)
        {
            if ((request.OscoapContext != null) || (exchange.OscoapContext != null)) {
                bool hasPayload = false;

                SecurityContext ctx = exchange.OscoapContext;
                if (request.OscoapContext != null) {
                    ctx = request.OscoapContext;
                    exchange.OscoapContext = ctx;
                }

                Codec.IMessageEncoder me = Spec.NewMessageEncoder();
                Request encryptedRequest = new Request(Method.GET);

                if (request.Payload != null) {
                    hasPayload = true;
                    encryptedRequest.Payload = request.Payload;
                }

                MoveRequestHeaders(request, encryptedRequest);

                if (_Log.IsInfoEnabled) {
                    _Log.Info("New inner response message");
                    _Log.Info(encryptedRequest.ToString());
                }

                ctx.Sender.IncrementSequenceNumber();

                Encrypt0Message enc = new Encrypt0Message(false);
                byte[] msg = me.Encode(encryptedRequest);
                int tokenSize = msg[0] & 0xf;
                byte[] msg2 = new byte[msg.Length - (4 + tokenSize)];
                Array.Copy(msg, 4 + tokenSize, msg2, 0, msg2.Length);
                enc.SetContent(msg2);

                // Build AAD
                CBORObject aad = CBORObject.NewArray();
                aad.Add(CBORObject.FromObject(1));                      // version
                aad.Add(CBORObject.FromObject(request.Code));           // code
                aad.Add(CBORObject.FromObject(new byte[0]));            // options
                aad.Add(CBORObject.FromObject(ctx.Sender.Algorithm));
                aad.Add(CBORObject.FromObject(ctx.Sender.Id));
                aad.Add(CBORObject.FromObject(ctx.Sender.PartialIV));

#if DEBUG
                switch (SecurityContext.FutzError) {
                    case 1: aad[0] = CBORObject.FromObject(2); break; // Change version #
                    case 2: aad[1] = CBORObject.FromObject(request.Code + 1); break; // Change request code
                    case 3: aad[2] = CBORObject.FromObject(ctx.Sender.Algorithm.AsInt32() + 1); break; // Change algorithm number
                }
#endif

                if (_Log.IsInfoEnabled) {
                    _Log.Info("AAD = " + BitConverter.ToString(aad.EncodeToBytes()));
                }

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

                if (_Log.IsInfoEnabled) {
                    _Log.Info("AAD = " + BitConverter.ToString(aad.EncodeToBytes()));
                    _Log.Info("IV = " + BitConverter.ToString(ctx.Sender.GetIV(ctx.Sender.PartialIV).GetByteString()));
                    _Log.Info("Key = " + BitConverter.ToString(ctx.Sender.Key));
                }

                enc.Encrypt(ctx.Sender.Key);

                byte[] encBody;
#if OSCOAP_COMPRESS
                encBody = DoCompression(enc);
#else
                encBody= enc.EncodeToBytes();
#endif

                if (hasPayload) {
                    request.Payload = encBody;
                    request.AddOption(new OscoapOption());
                }
                else {
                    OscoapOption o = new OscoapOption();
                    o.Set(encBody);
                    request.AddOption(o);
                }

#if DEBUG
                if (SecurityContext.FutzError == 9) {
                    request.AddUriQuery("?first=1");
                }
#endif



            }
            base.SendRequest(nextLayer, exchange, request);
        }

        public override void ReceiveRequest(INextLayer nextLayer, Exchange exchange, Request request)
        {
            if (request.HasOption(OptionType.Oscoap)) {
                Response response;
                try {
                    Option op = request.GetFirstOption(OptionType.Oscoap);
                    request.RemoveOptions(OptionType.Oscoap);

                    Encrypt0Message msg;

                    if (_Log.IsInfoEnabled) {
                        _Log.Info("Incoming Request: " + Util.Utils.ToString(request));
                    }

#if OSCOAP_COMPRESS
                    byte[] raw;
                    if (op.RawValue.Length== 0) {
                        raw = request.Payload;
                    }
                    else {
                        raw = op.RawValue;
                    }

                    msg = Uncompress(raw);
                    if (msg == null) {
                        if (request.Type == MessageType.CON) {
                            response = new Response(StatusCode.BadOption);
                            response.PayloadString = "Unable to decompress";
                            exchange.SendResponse(response);
                        }
                        return;  // Ignore messages that have no known security context.
                    }

#else
                    if (op.RawValue.Length == 0) {
                        msg = (Encrypt0Message)Com.AugustCellars.COSE.Message.DecodeFromBytes(request.Payload, Tags.Encrypt0);
                    }
                    else {
                        msg = (Encrypt0Message)Com.AugustCellars.COSE.Message.DecodeFromBytes(op.RawValue, Tags.Encrypt0);
                    }
#endif

                    List<SecurityContext> contexts = new List<SecurityContext>();
                    SecurityContext ctx = null;

                    if (exchange.OscoapContext != null) {
                        contexts.Add(exchange.OscoapContext);
                    }
                    else {
                        CBORObject kid = msg.FindAttribute(HeaderKeys.KeyId);
                        contexts = SecurityContextSet.AllContexts.FindByKid(kid.GetByteString());
                        if (contexts.Count == 0) {
                            response = new Response(StatusCode.Unauthorized);
                            response.PayloadString = "No Context Found - 1";
                            exchange.SendResponse(response);
                            return;  // Ignore messages that have no known security context.
                        }
                    }

                    String partialURI = request.URI.AbsoluteUri; // M00BUG?

                    //  Build AAD
                    CBORObject aad = CBORObject.NewArray();
                    aad.Add(CBORObject.FromObject(1)); // M00BUG
                    aad.Add(CBORObject.FromObject(request.Code));
                    aad.Add(CBORObject.FromObject(new byte[0])); // encoded I options
                    aad.Add(CBORObject.FromObject(0));  // Place holder for algorithm
                    aad.Add(CBORObject.FromObject(msg.FindAttribute(HeaderKeys.KeyId)));

                    byte[] payload = null;
                    byte[] partialIV = msg.FindAttribute(HeaderKeys.PartialIV).GetByteString();
                    aad.Add(CBORObject.FromObject(partialIV));

                    byte[] seqNoArray = new byte[8];
                    Array.Copy(partialIV, 0, seqNoArray, 8 - partialIV.Length, partialIV.Length);
                    if (BitConverter.IsLittleEndian) Array.Reverse(seqNoArray);
                    Int64 seqNo = BitConverter.ToInt64(seqNoArray, 0);

                    String responseString = "General decrypt failure";

                    foreach (SecurityContext context in contexts) {
                        if (_replayWindow && context.Recipient.ReplayWindow.HitTest(seqNo)) {
                            if (_Log.IsInfoEnabled) {
                                _Log.Info(String.Format("Hit test on {0} failed", seqNo));
                            }
                            responseString = "Hit test - duplicate";
                            continue;
                        }

                        aad[3] = context.Recipient.Algorithm;

                        if (_Log.IsInfoEnabled) {
                            _Log.Info("AAD = " + BitConverter.ToString(aad.EncodeToBytes()));
                            _Log.Info("IV = " + BitConverter.ToString(context.Recipient.GetIV(partialIV).GetByteString()));
                            _Log.Info("Key = " + BitConverter.ToString(context.Recipient.Key));
                        }
                        

                        msg.SetExternalData(aad.EncodeToBytes());

                        msg.AddAttribute(HeaderKeys.Algorithm, context.Recipient.Algorithm, Attributes.DO_NOT_SEND);
                        msg.AddAttribute(HeaderKeys.IV, context.Recipient.GetIV(partialIV), Attributes.DO_NOT_SEND);

                        try {
                            ctx = context;
                            payload = msg.Decrypt(context.Recipient.Key);
                            context.Recipient.ReplayWindow.SetHit(seqNo);
                        }
                        catch (Exception e) {
                            if (_Log.IsInfoEnabled) _Log.Info("--- " + e.ToString());
                            responseString = "Decryption Failure";
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

                    exchange.OscoapContext = ctx;  // So we know it on the way back.
                    request.OscoapContext = ctx;
                    exchange.OscoapSequenceNumber = partialIV;

                    byte[] newRequestData = new byte[payload.Length + _FixedHeader.Length];
                    Array.Copy(_FixedHeader, newRequestData, _FixedHeader.Length);
                    Array.Copy(payload, 0, newRequestData, _FixedHeader.Length, payload.Length);

                    Codec.IMessageDecoder me = Spec.NewMessageDecoder(newRequestData);
                    Request newRequest = me.DecodeRequest();

                    //  Update headers is a pain

                    RestoreOptions(request, newRequest);

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
                    _Log.Error("OSCOAP Layer: reject message because " + e.ToString());
                    exchange.OscoapContext = null;

                    if (request.Type == MessageType.CON) {
                        response = new Response(StatusCode.Unauthorized);
                        response.Payload = Encoding.UTF8.GetBytes("Error is " + e.Message);
                        exchange.SendResponse(response);
                    }
                    //  Ignore messages that we cannot decrypt.
                    return;
                }
            }

            base.ReceiveRequest(nextLayer, exchange, request);
        }

        public override void SendResponse(INextLayer nextLayer, Exchange exchange, Response response)
        {
            if (exchange.OscoapContext != null) {
                SecurityContext ctx = exchange.OscoapContext;

                Codec.IMessageEncoder me = Spec.NewMessageEncoder();
                Response encryptedResponse = new Response((StatusCode)response.Code);

                bool hasPayload = false;
                if (response.Payload != null) {
                    hasPayload = true;
                    encryptedResponse.Payload = response.Payload;
                }

                MoveResponseHeaders(response, encryptedResponse);

                if (_Log.IsInfoEnabled) {
                    _Log.Info("New inner response message");
                    _Log.Info(encryptedResponse.ToString());
                }

                //  Build AAD
                CBORObject aad = CBORObject.NewArray();
                aad.Add(1);
                aad.Add(response.Code);
                aad.Add(CBORObject.FromObject(new byte[0]));        // This is a mess for observe.
                aad.Add(ctx.Sender.Algorithm);
                aad.Add(ctx.Recipient.Id);
                aad.Add(exchange.OscoapSequenceNumber);

                Console.WriteLine(BitConverter.ToString(aad.EncodeToBytes()));

                Encrypt0Message enc = new Encrypt0Message(false);
                byte[] msg = me.Encode(encryptedResponse);
                int tokenSize = msg[0] & 0xf;
                byte[] msg2 = new byte[msg.Length - (4 + tokenSize)];
                Array.Copy(msg, 4 + tokenSize, msg2, 0, msg2.Length);
                enc.SetContent(msg2);
                enc.SetExternalData(aad.EncodeToBytes());

#if OSCOAP_COMPRESS
                if (response.HasOption(OptionType.Observe)) {
                    enc.AddAttribute(HeaderKeys.PartialIV, CBORObject.FromObject(ctx.Sender.PartialIV), Attributes.PROTECTED);
                    enc.AddAttribute(HeaderKeys.IV, ctx.Sender.GetIV(ctx.Sender.PartialIV), Attributes.DO_NOT_SEND);
                    ctx.Sender.IncrementSequenceNumber();
                }
                else {
                    CBORObject iv = ctx.Sender.GetIV(exchange.OscoapSequenceNumber);
                    byte[] ivX = iv.GetByteString();
                    ivX[0] = (byte)( ivX[0] + 0x80);

                    enc.AddAttribute(HeaderKeys.IV, CBORObject.FromObject(ivX), Attributes.DO_NOT_SEND);

                }
#else
                enc.AddAttribute(HeaderKeys.PartialIV, CBORObject.FromObject(ctx.Sender.PartialIV), Attributes.PROTECTED);
                enc.AddAttribute(HeaderKeys.IV, ctx.Sender.GetIV(ctx.Sender.PartialIV), Attributes.DO_NOT_SEND);
                ctx.Sender.IncrementSequenceNumber();
#endif

                enc.AddAttribute(HeaderKeys.Algorithm, ctx.Sender.Algorithm, Attributes.DO_NOT_SEND);
                enc.Encrypt(ctx.Sender.Key);

                byte[] finalBody;
#if OSCOAP_COMPRESS
#if false

                if (response.HasOption(OptionType.Observe)) {
                    finalBody = DoCompression(enc);
                }
                else {
                    CBORObject msgX = enc.EncodeToCBORObject();
                    finalBody = msgX[2].GetByteString();
                }
#else
                finalBody = DoCompression(enc);
#endif
#else
                finalBody = enc.EncodeToBytes();
#endif

                if (hasPayload) {
                    response.Payload = finalBody;
                    response.AddOption(new OscoapOption());
                }
                else {
                    OscoapOption o = new OscoapOption();
                    o.Set(finalBody);
                    response.AddOption(o);
                }


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
                Encrypt0Message msg;
                SecurityContext ctx;
                Option op = response.GetFirstOption(OptionType.Oscoap);

                 if (exchange.OscoapContext == null) {
                    return;
                }
                else ctx = exchange.OscoapContext;

#if OSCOAP_COMPRESS
                byte[] raw;
                if (op.RawValue.Length > 0) raw = op.RawValue;
                else raw = response.Payload;

                bool fHasObserve = response.HasOption(OptionType.Observe);

                if (fHasObserve) {
                    msg = Uncompress(raw);
                    if (msg == null) return;
                }
                else {
                    msg = Uncompress(raw);
                    if (msg == null)
                        return;

                    msg.AddAttribute(HeaderKeys.PartialIV, CBORObject.FromObject(ctx.Sender.PartialIV), Attributes.DO_NOT_SEND);
                }
#else
                if (op.RawValue.Length > 0) {
                    msg = (Encrypt0Message)Com.AugustCellars.COSE.Message.DecodeFromBytes(op.RawValue, Tags.Encrypt0);
                }
                else {
                    msg = (Encrypt0Message)Com.AugustCellars.COSE.Message.DecodeFromBytes(response.Payload, Tags.Encrypt0);
                }
#endif



                byte[] partialIV = msg.FindAttribute(HeaderKeys.PartialIV).GetByteString();
                byte[] seqNoArray = new byte[8];
                Array.Copy(partialIV, 0, seqNoArray, 8 - partialIV.Length, partialIV.Length);
                if (BitConverter.IsLittleEndian) Array.Reverse(seqNoArray);
                Int64 seqNo = BitConverter.ToInt64(seqNoArray, 0);

                if (fHasObserve) if (_replayWindow && ctx.Recipient.ReplayWindow.HitTest(seqNo)) return;

                msg.AddAttribute(HeaderKeys.Algorithm, ctx.Recipient.Algorithm, Attributes.DO_NOT_SEND);

               CBORObject fullIV = ctx.Recipient.GetIV(partialIV);
                if (!fHasObserve) fullIV.GetByteString()[0] += 0x80;
                msg.AddAttribute(HeaderKeys.IV, fullIV, Attributes.DO_NOT_SEND);

                //  build aad
                CBORObject aad = CBORObject.NewArray();
                aad.Add(CBORObject.FromObject(1));
                aad.Add(CBORObject.FromObject(response.Code));
                aad.Add(CBORObject.FromObject(new byte[0]));        // M00BUG
                aad.Add(ctx.Recipient.Algorithm);
                aad.Add(ctx.Sender.Id);
                aad.Add(ctx.Sender.PartialIV);

                msg.SetExternalData(aad.EncodeToBytes());

                if (_Log.IsInfoEnabled) {
                    _Log.Info("AAD = " + BitConverter.ToString(aad.EncodeToBytes()));
                }

                byte[] payload = msg.Decrypt(ctx.Recipient.Key);

                ctx.Recipient.ReplayWindow.SetHit(seqNo);

                byte[] rgb = new byte[payload.Length + _FixedHeader.Length];
                Array.Copy(_FixedHeader, rgb, _FixedHeader.Length);
                Array.Copy(payload, 0, rgb, _FixedHeader.Length, payload.Length);
                rgb[1] = 0x45;
                Codec.IMessageDecoder me = Spec.NewMessageDecoder(rgb);
                Response decryptedReq = me.DecodeResponse();

                response.Payload = decryptedReq.Payload;

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
                String strUri;
                if (!unprotected.ProxyUri.IsAbsoluteUri) throw new Exception("Must be an absolute URI");
                if (unprotected.ProxyUri.Fragment != null) throw new Exception("Fragments not allowed in ProxyUri");
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
                strUri = unprotected.ProxyUri.Scheme + ":";

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
                        break;

                    case OptionType.Observe:
                        encrypted.AddOption(op);
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
            List<Option> deleteMe = new List<Option>();

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
                        break;

                    case OptionType.Observe:
                        encrypted.AddOption(op);
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
            CBORObject encMsg = msg.EncodeToCBORObject();
            byte[] body;
            byte[] encBody;

            body = encMsg[2].GetByteString();
            

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

            //

            CBORObject kid = msg.FindAttribute(HeaderKeys.KeyId);
            if (kid != null) {
                cbSize += (1 + kid.GetByteString().Length);
                head |= 0x08;
            }

            //  Additional items to flag

            encBody = new byte[cbSize + body.Length];
            encBody[0] = head;
            cbSize = 1;
            if (iv.Length > 0) {
                Array.Copy(iv, 0, encBody, cbSize, iv.Length);
                cbSize += iv.Length;
            }

            if (kid != null) {
                if (kid.GetByteString().Length > 255) throw new Exception("KID too large");
                encBody[cbSize] = (byte) kid.GetByteString().Length;
                Array.Copy(kid.GetByteString(), 0, encBody, cbSize+1, kid.GetByteString().Length);
                cbSize += kid.GetByteString().Length + 1;
            }

            Array.Copy(body, 0, encBody, cbSize, body.Length);

#if DEBUG
            {
                CBORObject xxx = msg.EncodeToCBORObject();
                Console.WriteLine("Protected Attributes = " + BitConverter.ToString(xxx[0].GetByteString()));
            }
#endif

            return encBody;
        }

        Encrypt0Message Uncompress(byte[] raw)
        {
            CBORObject map = CBORObject.NewMap();

            //  Decode the wierd body
            //  First byte is of the form 0abcdeee where abcd are flags and eee is the the IV size.
            if (0 != (raw[0] & 0x80)) return null;  // This is not legal
            if (0 != (raw[0] & 0x70)) return null;  // These are not currently supported.

            int iX = 1;
            if (0 != (raw[0] & 0x07)) {
                byte[] ivX = new byte[raw[0] & 0x7];
                Array.Copy(raw, iX, ivX, 0, ivX.Length);
                map.Add(HeaderKeys.PartialIV, ivX);
                iX += ivX.Length;
            }

            if (0 != (raw[0] & 0x08)) {
                byte[] kidX = new byte[raw[iX]];
                Array.Copy(raw, iX + 1, kidX, 0, kidX.Length);
                iX += (kidX.Length + 1);
                map.Add(HeaderKeys.KeyId, kidX);
            }

            byte[] encBody = new byte[raw.Length - iX];
            Array.Copy(raw, iX, encBody, 0, encBody.Length);


            CBORObject msgX = CBORObject.NewArray();
            msgX.Add(new byte[0]);
            msgX.Add(map);
            msgX.Add(encBody);

           Encrypt0Message msg = (Encrypt0Message)COSE.Message.DecodeFromBytes(msgX.EncodeToBytes(), Tags.Encrypt0);

            return msg;

        }
    }
#endif
            }
