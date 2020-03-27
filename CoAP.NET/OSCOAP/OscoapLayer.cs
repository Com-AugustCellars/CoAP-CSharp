/*
 * Copyright (c) 2019-2020, Jim Schaad <ietf@augustcellars.com>
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY.
 * 
 * This file is part of the CoAP.NET, a CoAP framework in C#.
 * Please see README for more information.
 */

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;

using Com.AugustCellars.CoAP.Log;
using Com.AugustCellars.CoAP.Net;
using Com.AugustCellars.CoAP.Stack;
using Com.AugustCellars.CoAP.Util;
using Com.AugustCellars.COSE;
using Org.BouncyCastle.Utilities.Encoders;
using PeterO.Cbor;
using Attributes = Com.AugustCellars.COSE.Attributes;

namespace Com.AugustCellars.CoAP.OSCOAP
{
    public class OscoapLayer : AbstractLayer
    {
        static readonly ILogger _Log = LogManager.GetLogger(typeof(OscoapLayer));
        static readonly byte[] fixedHeader = new byte[] {0x40, 0x01, 0xff, 0xff};
        bool _replayWindow;
        readonly ConcurrentDictionary<Exchange.KeyUri, BlockHolder> _ongoingExchanges = new ConcurrentDictionary<Exchange.KeyUri, BlockHolder>();

        class BlockHolder
        {
            readonly BlockwiseStatus _requestStatus;
            readonly BlockwiseStatus _responseStatus;
            readonly Response _response;

            public BlockHolder(Exchange exchange)
            {
                _requestStatus = exchange.OscoreRequestBlockStatus;
                _responseStatus = exchange.OSCOAP_ResponseBlockStatus;
                _response = exchange.Response;
            }

            public void RestoreTo(Exchange exchange)
            {
                exchange.OscoreRequestBlockStatus = _requestStatus;
                exchange.OSCOAP_ResponseBlockStatus = _responseStatus;
                exchange.Response = _response;
            }
        }


        /// <summary>
        /// Constructs a new OSCORE layer.
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
            if ((request.OscoreContext != null) || (exchange.OscoreContext != null)) {
                SecurityContext ctx = exchange.OscoreContext;
                if (request.OscoreContext != null) {
                    ctx = request.OscoreContext;
                    exchange.OscoreContext = ctx;
                }

                if (ctx.Sender.SequenceNumberExhausted) {
                    OscoreEvent e = new OscoreEvent(OscoreEvent.EventCode.PivExhaustion, null, null, ctx, ctx.Sender);
                    _Log.Info(m => m($"Partial IV exhaustion occured for {Base64.ToBase64String(ctx.Sender.Key)}"));

                    ctx.OnEvent(e);
                    if (e.SecurityContext == ctx) {
                        throw new CoAPException("Kill message from IV exhaustion");
                    }

                    ctx = e.SecurityContext;
                    exchange.OscoreContext = ctx;
                    request.OscoreContext = ctx;
                }

                Codec.IMessageEncoder me = Spec.NewMessageEncoder();
                Request encryptedRequest = new Request(request.Method);

                if (request.Payload != null) {
                    encryptedRequest.Payload = request.Payload;
                }

                MoveRequestHeaders(request, encryptedRequest);

                _Log.Info(m => m("New inner response message\n{0}", encryptedRequest.ToString()));

                ctx.Sender.IncrementSequenceNumber();
                if (ctx.Sender.SendSequenceNumberUpdate) {
                    OscoreEvent e = new OscoreEvent(OscoreEvent.EventCode.SenderIvSave, null, null, ctx, ctx.Sender);
                    ctx.OnEvent(e);
                }

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
                aad.Add(CBORObject.NewArray());  // Algorithms
                aad[1].Add(CBORObject.FromObject(ctx.Sender.Algorithm));
                if (ctx.Sender.SigningAlgorithm != null) {
                    aad[1].Add(ctx.Sender.SigningAlgorithm);
                    if (ctx.CountersignParams != null) {
                        aad[1].Add(ctx.CountersignParams);
                    }
                    if (ctx.CountersignKeyParams != null) {
                        aad[1].Add(ctx.CountersignKeyParams);
                    }
                }
                aad.Add(CBORObject.FromObject(ctx.Sender.Id));
                aad.Add(CBORObject.FromObject(ctx.Sender.PartialIV));
                aad.Add(CBORObject.FromObject(new byte[0]));  // I options go here

                _Log.Info(m => m($"SendRequest: AAD = {BitConverter.ToString(aad.EncodeToBytes())}"));

                enc.SetExternalData(aad.EncodeToBytes());
                enc.AddAttribute(HeaderKeys.IV, ctx.Sender.GetIV(ctx.Sender.PartialIV), Attributes.DO_NOT_SEND);
                enc.AddAttribute(HeaderKeys.PartialIV, CBORObject.FromObject(ctx.Sender.PartialIV), /* Attributes.PROTECTED */ Attributes.DO_NOT_SEND);
                enc.AddAttribute(HeaderKeys.Algorithm, ctx.Sender.Algorithm, Attributes.DO_NOT_SEND);
                enc.AddAttribute(HeaderKeys.KeyId, CBORObject.FromObject(ctx.Sender.Id), /*Attributes.PROTECTED*/ Attributes.DO_NOT_SEND);
                if (ctx.GroupId != null) {
                    enc.AddAttribute(HeaderKeys.KidContext, CBORObject.FromObject(ctx.GroupId), Attributes.DO_NOT_SEND);
                }

                _Log.Info(m => m("SendRequest: AAD = {0}\nSendRequest: IV = {1}\nSendRequest: Key = {2}",
                                 BitConverter.ToString(aad.EncodeToBytes()),
                                                       ctx.Sender.GetIV(ctx.Sender.PartialIV).GetByteString(),
                                                       BitConverter.ToString(ctx.Sender.Key)));

                byte[] optionValue = BuildOscoreOption(enc);

                CounterSignature1 cs1 = null;
                if (ctx.Sender.SigningKey != null) {
                    cs1 = new CounterSignature1(ctx.Sender.SigningKey);
                    cs1.AddAttribute(HeaderKeys.Algorithm, ctx.Sender.SigningAlgorithm, Attributes.DO_NOT_SEND);
                    aad.Add(optionValue);
                    _Log.Info(m => m("SendRequest: AAD for Signature = {0}", BitConverter.ToString(aad.EncodeToBytes())));
                    cs1.SetExternalData(aad.EncodeToBytes());
                    cs1.SetObject(enc);
                    enc.CounterSigner1 = cs1;
                }

                enc.Encrypt(ctx.Sender.Key);

                OscoapOption o = new OscoapOption();
                o.Set(optionValue);
                request.AddOption(o);
                request.Payload = enc.GetEncryptedContent();
                if (cs1 != null) {
                    int cbOrig = request.Payload.Length;
                    byte[] rgbOrig = request.Payload;
                    byte[] signatureBytes = cs1.EncodeToCBORObject().GetByteString();
                    Array.Resize(ref rgbOrig, cbOrig + signatureBytes.Length);
                    Array.Copy(signatureBytes, 0, rgbOrig, cbOrig, signatureBytes.Length);
                    request.Payload = rgbOrig;
                }

                request.Method = request.HasOption(OptionType.Observe) ? Method.FETCH : Method.POST;
                request.Code = (int) request.Method;
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

                _Log.Info(m => m("Incoming Request: {0}", Utils.ToString(request)));

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
                if (exchange.OscoreContext != null) {
                    contexts.Add(exchange.OscoreContext);
                    if (exchange.OscoreContext.GroupId != null) {
                        gid = CBORObject.FromObject(exchange.OscoreContext.GroupId);
                    }
                    kid = CBORObject.FromObject(exchange.OscoapSenderId);
                }
                else {
                    gid = msg.FindAttribute(HeaderKeys.KidContext);
                    kid = msg.FindAttribute(HeaderKeys.KeyId);

                    if (kid == null) {
                        exchange.SendResponse(new Response(StatusCode.BadRequest));
                        return;
                    }

                    if (gid != null) {
                        contexts =  exchange.EndPoint.SecurityContexts.FindByGroupId(gid.GetByteString());
                        if (contexts.Count == 0) {
                            OscoreEvent e = new OscoreEvent(OscoreEvent.EventCode.UnknownGroupIdentifier, gid.GetByteString(), kid.GetByteString(), null, null);

                            exchange.EndPoint.SecurityContexts.OnEvent(e);

                            if (e.SecurityContext != null) {
                                contexts.Add(e.SecurityContext);
                            }
                        }
                    }
                    else {
                        contexts = exchange.EndPoint.SecurityContexts.FindByKid(kid.GetByteString());
                    }

                    if (contexts.Count == 0) {
                        response = new Response(StatusCode.Unauthorized) {
                            PayloadString = "No Context Found - 1"
                        };
                        exchange.SendResponse(response);
                        return; // Ignore messages that have no known security context.
                    }
                }

                byte[] partialIV = msg.FindAttribute(HeaderKeys.PartialIV).GetByteString();

                //  Build AAD
                CBORObject aad = CBORObject.NewArray();
                aad.Add(CBORObject.FromObject(1)); // Version #
                aad.Add(CBORObject.NewArray());  // Array place holder
                aad[1].Add(CBORObject.FromObject(0)); // Place holder for algorithm
                aad.Add(CBORObject.FromObject(kid));
                aad.Add(CBORObject.FromObject(partialIV));
                aad.Add(CBORObject.FromObject(new byte[0])); // encoded I options

                byte[] payload = null;

                byte[] seqNoArray = new byte[8];
                Array.Copy(partialIV, 0, seqNoArray, 8 - partialIV.Length, partialIV.Length);
                if (BitConverter.IsLittleEndian) Array.Reverse(seqNoArray);
                Int64 seqNo = BitConverter.ToInt64(seqNoArray, 0);

                string responseString = "General decrypt failure";

                for (int pass = 0; pass < 2; pass++) {
                    //  Don't try and get things fixed the first time around if more than one context exists.'
                    responseString = "General decrypt failure";
                    if (contexts.Count == 1) {
                        pass = 1;
                    }

                    foreach (SecurityContext context in contexts) {
                        SecurityContext.EntityContext recip = context.Recipient;
                        if (gid != null) {
                            if (recip != null) {
                                if (!SecurityContext.ByteArrayComparer.AreEqual(recip.Id, kid.GetByteString())) {
                                    continue;
                                }
                            }
                            else {
                                if (context.Recipients.ContainsKey(kid.GetByteString())) {
                                    recip = context.Recipients[kid.GetByteString()];
                                }
                                else if (pass == 1) {
                                    OscoreEvent e = new OscoreEvent(OscoreEvent.EventCode.UnknownKeyIdentifier, gid.GetByteString(), kid.GetByteString(), context, null);
                                    context.OnEvent(e);
                                    if (e.RecipientContext == null) { 
                                        continue;
                                    }

                                    if (e.SecurityContext != context) {
                                        continue;
                                    }
                                    recip = e.RecipientContext;
                                }
                                else {
                                    continue;
                                }
                            }
                        }

                        if (_replayWindow && recip.ReplayWindow.HitTest(seqNo)) {
                            _Log.Info(m => m("Hit test on {0} failed", seqNo));
                            responseString = "Hit test - duplicate";
                            continue;
                        }
                        else {
                            _Log.Info(m => m("Hit test disabled"));
                        }

                        aad[1] = CBORObject.NewArray();
                        aad[1].Add(recip.Algorithm);
                        if (context.Sender.SigningKey != null) {
                            aad[1].Add(context.Sender.SigningKey[CoseKeyKeys.Algorithm]);
                            if (context.CountersignParams != null) {
                                aad[1].Add(context.CountersignParams);
                            }

                            if (context.CountersignKeyParams != null) {
                                aad[1].Add(context.CountersignKeyParams);
                            }

                            int cbSignature = context.SignatureSize;
                            byte[] rgbSignature = new byte[cbSignature];
                            byte[] rgbPayload = new byte[request.Payload.Length - cbSignature];

                            Array.Copy(request.Payload, rgbPayload, rgbPayload.Length);
                            Array.Copy(request.Payload, rgbPayload.Length, rgbSignature, 0, cbSignature);

                            CounterSignature1 cs1 = new CounterSignature1(rgbSignature);
                            cs1.AddAttribute(HeaderKeys.Algorithm, context.Sender.SigningAlgorithm, Attributes.DO_NOT_SEND);
                            cs1.SetObject(msg);
                            cs1.SetKey(recip.SigningKey);

                            aad.Add(op.RawValue);
                            byte[] aadData = aad.EncodeToBytes();
                            cs1.SetExternalData(aadData);
                            msg.SetEncryptedContent(rgbPayload);

                            try {
                                if (!msg.Validate(cs1)) {
                                    continue;
                                }

                            }
                            catch (CoseException) {
                                // try the next possible one
                                continue;
                            }

                        }

                        if (aad.Count == 6) {
                            aad.Remove(aad[5]);
                        }

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
                            if (recip.ReplayWindow.SetHit(seqNo)) {
                                OscoreEvent e = new OscoreEvent(OscoreEvent.EventCode.HitZoneMoved, null, null, ctx, recip);
                                context.OnEvent(e);
                            }

                        }
                        catch (Exception e) {
                            if (_Log.IsInfoEnabled) _Log.Info("--- ", e);
                            ctx = null;
                        }

                        if (ctx != null) {
                            break;
                        }
                    }
                }

                if (ctx == null) {
                    if (request.Type == MessageType.CON || request.Type == MessageType.NON) {
                        response = new Response(StatusCode.BadRequest) {
                            PayloadString = responseString
                        };
                        exchange.SendResponse(response);
                    }
                    return;
                }

                exchange.OscoreContext = ctx; // So we know it on the way back.
                request.OscoreContext = ctx;
                exchange.OscoapSequenceNumber = partialIV;
                exchange.OscoapSenderId = kid.GetByteString();

                byte[] newRequestData = new byte[payload.Length + fixedHeader.Length-1];
                Array.Copy(fixedHeader, newRequestData, fixedHeader.Length);
                Array.Copy(payload, 1, newRequestData, fixedHeader.Length, payload.Length-1);
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
                    Exchange.KeyUri keyUri = new Exchange.KeyUri(request, request.Source);
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
                exchange.OscoreContext = null;

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
            if (exchange.OscoreContext != null) {
                SecurityContext ctx = exchange.OscoreContext;

                if (ctx.ReplaceWithSecurityContext != null) {
                    while (ctx.ReplaceWithSecurityContext != null) {
                        ctx = ctx.ReplaceWithSecurityContext;
                    }

                    exchange.OscoreContext = ctx;
                }

                if (ctx.Sender.SequenceNumberExhausted && response.HasOption(OptionType.Observe)) {
                    OscoreEvent e = new OscoreEvent(OscoreEvent.EventCode.PivExhaustion, null, null, ctx, ctx.Sender);
                    _Log.Info(m => m($"Partial IV exhaustion occured for {Base64.ToBase64String(ctx.Sender.Key)}"));

                    ctx.OnEvent(e);
                    ctx = e.SecurityContext;
                    if (ctx.Sender.SequenceNumberExhausted) {
                        throw new CoAPException("Kill message from IV exhaustion");
                    }

                    ctx = e.SecurityContext;
                    exchange.OscoreContext = ctx;
                }

                Codec.IMessageEncoder me = Spec.NewMessageEncoder();
                Response encryptedResponse = new Response((StatusCode) response.Code) {
                    Type = MessageType.CON // It does not matter what this is as it will get ignored later.
                };

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
                aad.Add(CBORObject.NewArray());
                aad[1].Add(ctx.Sender.Algorithm);
                aad.Add(exchange.OscoapSenderId);
                aad.Add(exchange.OscoapSequenceNumber);
                aad.Add(CBORObject.FromObject(new byte[0])); // I Options

                if (ctx.Sender.SigningAlgorithm != null) {
                    aad[1].Add(ctx.Sender.SigningAlgorithm);
                    if (ctx.CountersignParams != null) {
                        aad[1].Add(ctx.CountersignParams);
                    }

                    if (ctx.CountersignKeyParams != null) {
                        aad[1].Add(ctx.CountersignKeyParams);
                    }
                }
                if (_Log.IsInfoEnabled) {
                    _Log.Info("SendResponse: AAD = " + BitConverter.ToString(aad.EncodeToBytes()));
                }

                Encrypt0Message enc = new Encrypt0Message(false);
                byte[] msg = me.Encode(encryptedResponse);
                int tokenSize = msg[0] & 0xf;
                byte[] msg2 = new byte[msg.Length - (3 + tokenSize)];
                Array.Copy(msg, 4 + tokenSize, msg2, 1, msg2.Length - 1);
                msg2[0] = msg[1];
                enc.SetContent(msg2);
                enc.SetExternalData(aad.EncodeToBytes());

                if (response.HasOption(OptionType.Observe)) {
                    enc.AddAttribute(HeaderKeys.PartialIV, CBORObject.FromObject(ctx.Sender.PartialIV),
                                     Attributes.UNPROTECTED);
                    enc.AddAttribute(HeaderKeys.IV, ctx.Sender.GetIV(ctx.Sender.PartialIV), Attributes.DO_NOT_SEND);
                    ctx.Sender.IncrementSequenceNumber();
                    if (ctx.Sender.SendSequenceNumberUpdate) {
                        OscoreEvent e = new OscoreEvent(OscoreEvent.EventCode.SenderIvSave, null, null, ctx, ctx.Sender);
                        ctx.OnEvent(e);
                    }
                    if (ctx.IsGroupContext) {
                        enc.AddAttribute(HeaderKeys.KeyId, CBORObject.FromObject(ctx.Sender.Id),
                                         Attributes.UNPROTECTED);
                    }
                }
                else if (ctx.IsGroupContext) {
                    if (!ctx.Recipients.ContainsKey(exchange.OscoapSenderId)) {
                        return;
                    }
                    CBORObject iv = ctx.Recipients[exchange.OscoapSenderId].GetIV(exchange.OscoapSequenceNumber);

                    enc.AddAttribute(HeaderKeys.IV, iv, Attributes.DO_NOT_SEND);
                    enc.AddAttribute(HeaderKeys.KeyId, CBORObject.FromObject(ctx.Sender.Id),
                        Attributes.UNPROTECTED);
                }
                else { 
                    CBORObject iv = ctx.Recipient.GetIV(exchange.OscoapSequenceNumber);

                    enc.AddAttribute(HeaderKeys.IV, iv, Attributes.DO_NOT_SEND);
                }

                _Log.Info(m => m($"SendResponse: IV = {BitConverter.ToString(enc.FindAttribute(HeaderKeys.IV, Attributes.DO_NOT_SEND).GetByteString())}"));
                _Log.Info(m => m($"SendResponse: Key = {BitConverter.ToString(ctx.Sender.Key)}"));

                byte[] optionValue = BuildOscoreOption(enc);

                CounterSignature1 cs1 = null;
                if (ctx.IsGroupContext) {
                    aad.Add(optionValue);
                    cs1 = new CounterSignature1(ctx.Sender.SigningKey);
                    cs1.AddAttribute(HeaderKeys.Algorithm, ctx.Sender.SigningAlgorithm, Attributes.DO_NOT_SEND);
                    cs1.SetExternalData(aad.EncodeToBytes());
                    cs1.SetObject(enc);
                    enc.CounterSigner1 = cs1;
                }

                enc.AddAttribute(HeaderKeys.Algorithm, ctx.Sender.Algorithm, Attributes.DO_NOT_SEND);
                enc.Encrypt(ctx.Sender.Key);

                OscoapOption o = new OscoapOption(OptionType.Oscoap);
                o.Set(optionValue);
                response.AddOption(o);
                response.Code = (int) StatusCode.Changed;
                response.Payload = enc.GetEncryptedContent();

                if (cs1 != null) {
                    int cbOrig = response.Payload.Length;
                    byte[] rgbOrig = response.Payload;
                    byte[] signatureBytes = cs1.EncodeToCBORObject().GetByteString();
                    Array.Resize(ref rgbOrig, cbOrig + signatureBytes.Length);
                    Array.Copy(signatureBytes, 0, rgbOrig, cbOrig, signatureBytes.Length);
                    response.Payload = rgbOrig;
                }

                //  Need to be able to retrieve this again under some circumstances.

                if (encryptedResponse.HasOption(OptionType.Block2)) {
                    Request request = exchange.CurrentRequest;
                    Exchange.KeyUri keyUri = new Exchange.KeyUri(request, response.Destination);

                    //  Observe notification only send the first block, hence do not store them as ongoing
                    if (exchange.OSCOAP_ResponseBlockStatus != null &&
                        !encryptedResponse.HasOption(OptionType.Observe)) {
                        //  Remember ongoing blockwise GET requests
                        BlockHolder blockInfo = new BlockHolder(exchange);
                        if (Utils.Put(_ongoingExchanges, keyUri, blockInfo) == null) {
                            _Log.Info($"Ongoing Block2 started late, storing {keyUri} for {request}");
                        }
                        else { 
                            _Log.Info($"Ongoing Block2 continued, storing {keyUri} for {request}");
                        }
                    }
                    else { 
                        _Log.Info($"Ongoing Block2 completed, cleaning up {keyUri} for {request}");
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

                if (exchange.OscoreContext == null) {
                    return;
                }

                _Log.Info($"Incoming message for OSCORE\n{Utils.ToString(response)}");

                SecurityContext ctx = exchange.OscoreContext;

                bool fServerIv = true;

                Encrypt0Message msg = Uncompress(op.RawValue);
                if (msg == null) return;
                msg.SetEncryptedContent(response.Payload);

                SecurityContext.EntityContext recip = ctx.Recipient;
                if (ctx.IsGroupContext) {
                    if (ctx.GroupId == null) {
                        //  This is not currently a valid state to be in
                        return;
                    }

                    CBORObject kid = msg.FindAttribute(HeaderKeys.KeyId);
                    if (kid == null)
                    {
                        //  this is not currently a valid state to be in
                        return;
                    }

                    CBORObject gid = msg.FindAttribute(HeaderKeys.KidContext);
                    if (gid != null && !SecurityContext.ByteArrayComparer.AreEqual(ctx.GroupId, gid.GetByteString())) {
                        OscoreEvent e = new OscoreEvent(OscoreEvent.EventCode.UnknownGroupIdentifier, gid.GetByteString(), kid.GetByteString(), null, null);
                        ctx.OnEvent(e);

                        if (e.SecurityContext == null) {
                            return;
                        }

                        ctx = e.SecurityContext;
                    }

                    if (gid == null) {
                        gid = CBORObject.FromObject(ctx.GroupId);
                    }

                    ctx.Recipients.TryGetValue(kid.GetByteString(), out recip);
                    if (recip == null) {
                        OscoreEvent e = new OscoreEvent(OscoreEvent.EventCode.UnknownKeyIdentifier, gid.GetByteString(), kid.GetByteString(), ctx, null);
                        ctx.OnEvent(e);

                        if (e.RecipientContext == null) {
                            return;
                        }

                        recip = e.RecipientContext;
                    }

                    if (msg.FindAttribute(HeaderKeys.PartialIV) == null) {
                        msg.AddAttribute(HeaderKeys.PartialIV, CBORObject.FromObject(ctx.Sender.PartialIV),
                            Attributes.DO_NOT_SEND);
                        fServerIv = false;
                    }
                }
                else {
                    if (msg.FindAttribute(HeaderKeys.PartialIV) == null) {
                        msg.AddAttribute(HeaderKeys.PartialIV, CBORObject.FromObject(ctx.Sender.PartialIV),
                                         Attributes.DO_NOT_SEND);
                        fServerIv = false;
                    }
                }

                byte[] partialIV = msg.FindAttribute(HeaderKeys.PartialIV).GetByteString();
                byte[] seqNoArray = new byte[8];
                Array.Copy(partialIV, 0, seqNoArray, 8 - partialIV.Length, partialIV.Length);
                if (BitConverter.IsLittleEndian) Array.Reverse(seqNoArray);
                Int64 seqNo = BitConverter.ToInt64(seqNoArray, 0);

                if (fServerIv) if (_replayWindow && recip.ReplayWindow.HitTest(seqNo)) return;

                msg.AddAttribute(HeaderKeys.Algorithm, recip.Algorithm, Attributes.DO_NOT_SEND);

                CBORObject fullIV;
                if (fServerIv) {
                    fullIV = recip.GetIV(partialIV);
                }
                else {
                    fullIV = ctx.Sender.GetIV(partialIV);
                }

                msg.AddAttribute(HeaderKeys.IV, fullIV, Attributes.DO_NOT_SEND);

                //  build aad
                CBORObject aad = CBORObject.NewArray();
                aad.Add(1); // Version #
                aad.Add(CBORObject.NewArray());
                aad[1].Add(recip.Algorithm);
                aad.Add(ctx.Sender.Id);
                aad.Add(ctx.Sender.PartialIV);
                aad.Add(CBORObject.FromObject(new byte[0])); // OPTIONS

                if (ctx.Sender.SigningAlgorithm != null) {
                    aad[1].Add(ctx.Sender.SigningAlgorithm);
                    if (ctx.CountersignParams != null) {
                        aad[1].Add(ctx.CountersignParams);
                    }

                    if (ctx.CountersignKeyParams != null) {
                        aad[1].Add(ctx.CountersignKeyParams);
                    }
                }

                msg.SetExternalData(aad.EncodeToBytes());

                _Log.Info(m => m($"fServerIv = {fServerIv}"));
                _Log.Info(m => m("ReceiveResponse: AAD = " + BitConverter.ToString(aad.EncodeToBytes())));
                _Log.Info(m => m($"ReceiveResponse: IV = {BitConverter.ToString(fullIV.GetByteString())}"));
                _Log.Info(m => m($"ReceiveResponse: Key = {BitConverter.ToString(recip.Key)}"));

                if (ctx.IsGroupContext) {
                    aad.Add(op.RawValue);

                    int cbSignature = 64; // M00TODO   Need to figure out the size of the signature from the context.
                    byte[] rgbSignature = new byte[cbSignature];
                    byte[] rgbPayload = new byte[response.Payload.Length - cbSignature];

                    Array.Copy(response.Payload, rgbPayload, rgbPayload.Length);
                    Array.Copy(response.Payload, rgbPayload.Length, rgbSignature, 0, cbSignature);

                    CounterSignature1 cs1 = new CounterSignature1(rgbSignature);
                    cs1.AddAttribute(HeaderKeys.Algorithm, ctx.Sender.SigningAlgorithm, Attributes.DO_NOT_SEND);
                    cs1.SetObject(msg);
                    cs1.SetKey(recip.SigningKey);

                    byte[] aadData = aad.EncodeToBytes();
                    cs1.SetExternalData(aadData);
                    msg.SetEncryptedContent(rgbPayload);

                    try {
                        if (!msg.Validate(cs1)) {
                            return;
                        }

                    }
                    catch (CoseException) {
                        // try the next possible one
                        return;
                    }
                }

                byte[] payload = msg.Decrypt(recip.Key);

                if (recip.ReplayWindow.SetHit(seqNo)) {
                    OscoreEvent e = new OscoreEvent(OscoreEvent.EventCode.HitZoneMoved, null, null, ctx, recip);
                    ctx.OnEvent(e);
                }

                byte[] rgb = new byte[payload.Length + fixedHeader.Length - 1];
                Array.Copy(fixedHeader, rgb, fixedHeader.Length);
                Array.Copy(payload, 1, rgb, fixedHeader.Length, payload.Length-1);
                rgb[1] = payload[0];
                Codec.IMessageDecoder me = Spec.NewMessageDecoder(rgb);
                Response decryptedReq = me.DecodeResponse();

                _Log.Info($"Inner message for OSCORE{Utils.ToString(decryptedReq)}");

                response.Payload = decryptedReq.Payload;
                response.Code = (int) decryptedReq.StatusCode;

                RestoreOptions(response, decryptedReq);
                if (decryptedReq.HasOption(OptionType.Observe)) {
                    if (partialIV.Length > 3) {
                        byte[] x = new byte[3];
                        Array.Copy(partialIV, partialIV.Length-3, x, 0, 3);
                        partialIV = x;
                    }
                    response.AddOption(Option.Create(OptionType.Observe, partialIV));
                }

                _Log.Info($"Outgoing message for OSCORE{Utils.ToString(response)}");
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
                if (!unprotected.ProxyUri.IsAbsoluteUri) throw new CoAPException("Must be an absolute URI");
                if (!string.IsNullOrEmpty(unprotected.ProxyUri.Fragment)) throw new CoAPException("Fragments not allowed in ProxyUri");
                switch (unprotected.ProxyUri.Scheme) {
                    case "coap":
                        port = 5683;
                        break;

                    case "coaps":
                        port = 5684;
                        break;

                    default:
                        throw new CoAPException("Unsupported schema");
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
                encrypted.UriPath = p;

                encrypted.AddUriQuery(unprotected.ProxyUri.Query);
                unprotected.ClearUriQuery();

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

                    // Put these options on both the inner and outer messages.
                case OptionType.NoResponse:
                case OptionType.Observe:
                    encrypted.AddOption(op);
                    break;

                // Section 4.1.3.1 - MAY set outer to zero for OSCORE error responses.  -- we will not do anything.
                case OptionType.MaxAge:

                default:
                    encrypted.AddOption(op);
                    toDelete.Add(op);
                    break;
                }
            }

            foreach (Option op in toDelete) {
                unprotected.RemoveOptions(op.Type);
            }

            unprotected.URI = null;
        }

        private static void MoveResponseHeaders(Response unprotected, Response encrypted)
        {
            //  Deal with Proxy-Uri
            if (unprotected.ProxyUri != null) {
                throw new CoAPException("Should not see Proxy-Uri on a response.");
            }

            List<Option> toDelete = new List<Option>();
            foreach (Option op in unprotected.GetOptions()) {
                switch (op.Type) {
                case OptionType.UriHost:
                case OptionType.UriPort:
                case OptionType.ProxyUri:
                case OptionType.ProxyScheme:
                    break;

                    //  Always supposed to be set to 0 for minimal size.
                case OptionType.Observe:
                    encrypted.AddOption(Option.Create(OptionType.Observe));
                    break;

                // Section 4.1.3.1 - MAY set outer to zero for OSCORE error responses.  -- we will not do anything.
                case OptionType.MaxAge:

                default:
                    encrypted.AddOption(op);
                    toDelete.Add(op);
                    break;
                }
            }

            foreach (Option op in toDelete) {
                unprotected.RemoveOptions(op.Type);
            }
        }

        private static void RestoreOptions(Message response, Message decryptedReq)
        {
            List<Option> toDelete = new List<Option>();
            foreach (Option op in response.GetOptions()) {
                switch (op.Type) {
                    case OptionType.Block1:
                    case OptionType.Block2:
                    case OptionType.Oscoap:
                    case OptionType.MaxAge:
                    case OptionType.NoResponse:
                    case OptionType.Observe:
                        toDelete.Add(op);
                        break;

                    case OptionType.UriHost:
                    case OptionType.UriPort:
                    case OptionType.ProxyUri:
                    case OptionType.ProxyScheme:
                        break;

                    default:
                        toDelete.Add(op);
                        break;
                }
            }

            foreach (Option op in toDelete) {
                response.RemoveOptions(op.Type);
            }

            foreach (Option op in decryptedReq.GetOptions()) {
                switch (op.Type) {
                    default:
                        response.AddOption(op);
                        break;
                }
            }
        }

        private static byte[] BuildOscoreOption(Encrypt0Message msg)
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

            CBORObject gid = msg.FindAttribute(HeaderKeys.KidContext);
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

            byte[] optionValue = new byte[cbSize];
            optionValue[0] = head;
            cbSize = 1;
            if (iv.Length > 0) {
                Array.Copy(iv, 0, optionValue, cbSize, iv.Length);
                cbSize += iv.Length;
            }

            if (gid != null) {
                if (gid.GetByteString().Length > 255) throw new CoAPException("GID too large");
                optionValue[cbSize] = (byte) gid.GetByteString().Length;
                Array.Copy(gid.GetByteString(), 0, optionValue, cbSize + 1, gid.GetByteString().Length);
                cbSize += gid.GetByteString().Length + 1;
            }

            if (sig != null) {
                if (sigBytes.Length > 255) throw new CoAPException("SIG too large");
                optionValue[cbSize] = (byte) sigBytes.Length;
                Array.Copy(sigBytes, 0, optionValue, cbSize + 1, sig.GetByteString().Length);
                cbSize += sigBytes.Length + 1;
            }

            if (kid != null) {
                if (kid.GetByteString().Length > 255) throw new CoAPException("KID too large");
                Array.Copy(kid.GetByteString(), 0, optionValue, cbSize, kid.GetByteString().Length);
            }

            return optionValue;
        }

        private static Encrypt0Message Uncompress(byte[] raw)
        {
            CBORObject map = CBORObject.NewMap();

            if (raw.Length == 0) raw = new byte[1];

            //  Decode the weird body
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
                map.Add(HeaderKeys.KidContext, gidX);
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
            }
