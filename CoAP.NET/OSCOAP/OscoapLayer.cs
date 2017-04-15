using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;

using Com.AugustCellars.CoAP.Log;
using Com.AugustCellars.CoAP.Net;
using Com.AugustCellars.CoAP.Stack;
using Com.AugustCellars.COSE;
using PeterO.Cbor;

namespace Com.AugustCellars.CoAP.OSCOAP
{
#if INCLUDE_OSCOAP
    public class OscoapLayer : AbstractLayer
    {
        static readonly ILogger log = LogManager.GetLogger(typeof(OscoapLayer));
        static byte[] fixedHeader = new byte[] { 0x40, 0x01, 0xff, 0xff };
        Boolean _replayWindow = true;
        readonly ConcurrentDictionary<Exchange.KeyUri, BlockHolder> _ongoingExchanges = new ConcurrentDictionary<Exchange.KeyUri, BlockHolder>();

        class BlockHolder
        {
            BlockwiseStatus _requestStatus;
            BlockwiseStatus _responseStatus;
            Response _response;

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
            /*
            _maxMessageSize = config.MaxMessageSize;
            _defaultBlockSize = config.DefaultBlockSize;
            _blockTimeout = config.BlockwiseStatusLifetime;
            */
            _replayWindow = config.OSCOAP_ReplayWindow;
            if (log.IsInfoEnabled)
                log.Info("OscoapLayer - replay=" + _replayWindow.ToString());  // Print out config if any

            config.PropertyChanged += ConfigChanged;
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

                OSCOAP.SecurityContext ctx = exchange.OscoapContext;
                if (request.OscoapContext != null) {
                    ctx = request.OscoapContext;
                    exchange.OscoapContext = ctx;
                }

                Codec.IMessageEncoder me = Spec.NewMessageEncoder();
                Request encryptedRequest = new Request(CoAP.Method.GET);

                if (request.Payload != null) {
                    hasPayload = true;
                    encryptedRequest.Payload = request.Payload;
                }

                MoveRequestHeaders(request, encryptedRequest);

                if (log.IsInfoEnabled) {
                    log.Info("New inner response message");
                    log.Info(encryptedRequest.ToString());
                }

                ctx.Sender.IncrementSequenceNumber();

                Encrypt0Message enc = new Encrypt0Message(false);
                byte[] msg = me.Encode(encryptedRequest);
                int tokenSize = msg[0] & 0xf;
                byte[] msg2 = new byte[msg.Length - (4 + tokenSize)];
                Array.Copy(msg, 4 + tokenSize, msg2, 0, msg2.Length);
                enc.SetContent(msg2);

                // Build the partial URI
                string partialURI = request.URI.AbsoluteUri; // M00BUG?

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

                if (log.IsInfoEnabled) {
                    log.Info("AAD = " + BitConverter.ToString(aad.EncodeToBytes()));
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

                enc.Encrypt(ctx.Sender.Key);

                byte[] encBody;
#if OSCOAP_COMPRESS
                encBody = DoCompression(enc);


#else
                encBody= enc.EncodeToBytes();
#endif

                if (hasPayload) {
                    request.Payload = encBody;
                    request.AddOption(new OSCOAP.OscoapOption());
                }
                else {
                    OSCOAP.OscoapOption o = new OSCOAP.OscoapOption();
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
            Response response;

            if (request.HasOption(OptionType.Oscoap)) {
                try {
                    CoAP.Option op = request.GetFirstOption(OptionType.Oscoap);
                    request.RemoveOptions(OptionType.Oscoap);

                    Encrypt0Message msg;

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
                        response = new Response((StatusCode)0x70);
                        response.PayloadString = "Unable to decompress";
                        exchange.SendResponse(response);
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
                        contexts = OSCOAP.SecurityContextSet.AllContexts.FindByKid(kid.GetByteString());
                        if (contexts.Count == 0) {
                            response = new Response((StatusCode) 0x70);
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
                            if (log.IsInfoEnabled) {
                                log.Info(String.Format("Hit test on {0} failed", seqNo));
                            }
                            responseString = "Hit test - duplicate";
                            continue;
                        }

                        aad[3] = context.Recipient.Algorithm;

                        if (log.IsInfoEnabled) {
                            log.Info("AAD = " + BitConverter.ToString(aad.EncodeToBytes()));
                            log.Info("IV = " + BitConverter.ToString(context.Recipient.GetIV(partialIV).GetByteString()));
                            log.Info("Key = " + BitConverter.ToString(context.Recipient.Key));
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
                            if (log.IsInfoEnabled) log.Info("--- " + e.ToString());
                            responseString = "Decryption Failure";
                            ctx = null;
                        }

                        if (ctx != null) {
                            break;
                        }
                    }

                    if (ctx == null) {
                        response = new Response((StatusCode)0x70);
                        response.PayloadString = responseString;
                        exchange.SendResponse(response);
                        return;
                    }

                    exchange.OscoapContext = ctx;  // So we know it on the way back.
                    request.OscoapContext = ctx;
                    exchange.OscoapSequenceNumber = partialIV;

                    byte[] newRequestData = new byte[payload.Length + fixedHeader.Length];
                    Array.Copy(fixedHeader, newRequestData, fixedHeader.Length);
                    Array.Copy(payload, 0, newRequestData, fixedHeader.Length, payload.Length);

                    CoAP.Codec.IMessageDecoder me = CoAP.Spec.NewMessageDecoder(newRequestData);
                    CoAP.Request newRequest = me.DecodeRequest();

                    //  Update headers is a pain

                    RestoreOptions(request, newRequest);

                    if (log.IsInfoEnabled) {
                       // log.Info(String.Format("Secure message post = " + Util.Utils.ToString(request)));
                    }

                    //  We may want a new exchange at this point if it relates to a new message for blockwise.

                    if (request.HasOption(OptionType.Block2)) {
                        Exchange.KeyUri keyUri = new Exchange.KeyUri(request.URI, null, request.Source);
                        BlockHolder block = null;
                        _ongoingExchanges.TryGetValue(keyUri, out block);

                        if (block != null) {
                            block.RestoreTo(exchange);
                        }
                    }

                    request.Payload = newRequest.Payload;
                }
                catch (Exception e) {
                    log.Error("OSCOAP Layer: reject message because " + e.ToString());
                    exchange.OscoapContext = null;

                    response = new Response((StatusCode)0x70);
                    response.Payload = UTF8Encoding.UTF8.GetBytes( "Error is " + e.Message);
                    exchange.SendResponse(response);
                    //  Ignore messages that we cannot decrypt.
                    return;
                }
            }

            base.ReceiveRequest(nextLayer, exchange, request);
        }

        public override void SendResponse(INextLayer nextLayer, Exchange exchange, Response response)
        {
            if (exchange.OscoapContext != null) {
                OSCOAP.SecurityContext ctx = exchange.OscoapContext;

                Codec.IMessageEncoder me = Spec.NewMessageEncoder();
                Response encryptedResponse = new Response((CoAP.StatusCode)response.Code);

                bool hasPayload = false;
                if (response.Payload != null) {
                    hasPayload = true;
                    encryptedResponse.Payload = response.Payload;
                }

                MoveResponseHeaders(response, encryptedResponse);

                if (log.IsInfoEnabled) {
                    log.Info("New inner response message");
                    log.Info(encryptedResponse.ToString());
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
                if (response.HasOption(OptionType.Observe)) {
                    finalBody = DoCompression(enc);
                }
                else {
                    CBORObject msgX = enc.EncodeToCBORObject();
                    finalBody = msgX[2].GetByteString();
                }
#else
                finalBody = enc.EncodeToBytes();
#endif

                if (hasPayload) {
                    response.Payload = finalBody;
                    response.AddOption(new OSCOAP.OscoapOption());
                }
                else {
                    OSCOAP.OscoapOption o = new OSCOAP.OscoapOption();
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
                            if (log.IsInfoEnabled) log.Info("Ongoing Block2 started late, storing " + keyUri + " for " + request);

                        }
                        else {
                            if (log.IsInfoEnabled) log.Info("Ongoing Block2 continued, storing " + keyUri + " for " + request);
                        }
                    }
                    else {
                        if (log.IsInfoEnabled) log.Info("Ongoing Block2 completed, cleaning up " + keyUri + " for " + request);
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
                OSCOAP.SecurityContext ctx;
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
                    CBORObject protectedMap = CBORObject.NewMap();
                    // protectedMap.Add(HeaderKeys.PartialIV, CBORObject.FromObject( ctx.Sender.PartialIV));

                    CBORObject msgX = CBORObject.NewArray();
                    msgX.Add(new byte[0] /*protectedMap.EncodeToBytes()*/);
                    msgX.Add(CBORObject.NewMap());
                    msgX.Add(raw);

                    msg = (Encrypt0Message)Com.AugustCellars.COSE.Message.DecodeFromBytes(msgX.EncodeToBytes(), Tags.Encrypt0);
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

                if (log.IsInfoEnabled) {
                    log.Info("AAD = " + BitConverter.ToString(aad.EncodeToBytes()));
                }

                byte[] payload = msg.Decrypt(ctx.Recipient.Key);

                ctx.Recipient.ReplayWindow.SetHit(seqNo);

                byte[] rgb = new byte[payload.Length + fixedHeader.Length];
                Array.Copy(fixedHeader, rgb, fixedHeader.Length);
                Array.Copy(payload, 0, rgb, fixedHeader.Length, payload.Length);
                rgb[1] = 0x45;
                Codec.IMessageDecoder me = CoAP.Spec.NewMessageDecoder(rgb);
                Response decryptedReq = me.DecodeResponse();

                response.Payload = decryptedReq.Payload;

                RestoreOptions(response, decryptedReq);
            }
            base.ReceiveResponse(nextLayer, exchange, response);
        }

        void MoveRequestHeaders(Request unprotected, Request encrypted)
        {
            List<Option> deleteMe = new List<Option>();
            int port;

            //
            //  Rules for dealing with Proxy-URI are as follows:
            //
            //  1. Decompose it into the pieces.  
            //  2. Remove all the pieces that we need to protect
            //  3. Put the partial set of fields back together again
            //

            //  Deal with Proxy-Uri
            if (unprotected.ProxyUri != null) {
                String strUri = "";
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


                String strPort = "";
                if ((unprotected.ProxyUri.Port != 0) && (unprotected.ProxyUri.Port != port)) {
                    encrypted.UriPort = unprotected.ProxyUri.Port;
                    strPort = ":" + port.ToString();
                    strUri += strPort;
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

        void MoveResponseHeaders(Response unprotected, Response encrypted)
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

        void RestoreOptions(Message response, Message decryptedReq)
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

        byte[] DoCompression(Encrypt0Message msg)
        {
            CBORObject body;
            byte[] encBody;

            body = msg.EncodeToCBORObject();

            // 10000abc 01000def -> 00abcdef
            // abc is going to be 2 or 3 - count of items in the array
            // def is part of the partial IV field
            // Fake a CBOR array and then screw it up big time
            //  [ partial IV, ?kid, encrypted content (w/o cbor headers) ]
            byte[] iv = msg.FindAttribute(HeaderKeys.PartialIV).GetByteString();
            byte[] kid = new byte[0];
            CBORObject kidO = msg.FindAttribute(HeaderKeys.KeyId);
            if (kidO != null) kid = kidO.EncodeToBytes();
            byte[] foo = body[2].GetByteString();

            encBody = new byte[iv.Length + 1 + kid.Length + foo.Length];
            if (kid != null) encBody[0] = (byte)(0x18 | (iv.Length));
            else encBody[0] = (byte)(0x10 | (iv[0] & 0x7));
            Array.Copy(iv, 0, encBody, 1, iv.Length);
            if (kid != null) {
                Array.Copy(kid, 0, encBody, iv.Length+1, kid.Length);
            }
            Array.Copy(foo, 0, encBody, iv.Length +1 + kid.Length, foo.Length);

            Console.WriteLine("Protected Attributes = " + BitConverter.ToString(body[0].GetByteString()));

            return encBody;
        }

        Encrypt0Message Uncompress(byte[] raw)
        {
            bool fHasKid = true;
            CBORObject map = CBORObject.NewMap();

            //  Decode the wierd body
            //  First byte is of the form 00aabbbb where aa is the number of items and bbbb is the bottom of the IV size.
            if ((raw[0] & 0xf8) == 0x18) {
                fHasKid = true;
            }
            else if ((raw[0] & 0xf8) == 0x10) {
                fHasKid = false;
            }
            else { 
                return null;
            }

            byte[] ivX = new byte[raw[0] & 0x7];
            if (ivX.Length > 22) return null;            // We don't currently support this case.
            int iX = 1;
            Array.Copy(raw, iX, ivX, 0, ivX.Length);
            map.Add(HeaderKeys.PartialIV, ivX);


            iX += ivX.Length;
            if (fHasKid) {
                byte[] kidX = new byte[raw[ivX.Length + 1] & 0xf];
                if (kidX.Length > 22) return null;           // We dont' currently support this case.
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

           Encrypt0Message msg = (Encrypt0Message)Com.AugustCellars.COSE.Message.DecodeFromBytes(msgX.EncodeToBytes(), Tags.Encrypt0);

            return msg;

        }
    }
#endif
            }
