/*
 * Copyright (c) 2011-2014, Longxiang He <helongxiang@smeshlink.com>,
 * SmeshLink Technology Co.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY.
 * 
 * This file is part of the CoAP.NET, a CoAP framework in C#.
 * Please see README for more information.
 */

using System;
using Com.AugustCellars.CoAP.Log;
using Com.AugustCellars.CoAP.Net;

namespace Com.AugustCellars.CoAP.Stack
{
    /// <summary>
    /// Doesn't do much yet except for setting a simple token. Notice that empty
    /// tokens must be represented as byte array of length 0 (not null).
    /// </summary>
    public class TokenLayer : AbstractLayer
    {
#if false
        private Int32 _counter;
#endif
        private static ILogger _Log = LogManager.GetLogger("TokenLayer");

        /// <summary>
        /// Constructs a new token layer.
        /// </summary>
        public TokenLayer(ICoapConfig config)
        {
#if false
            if (config.UseRandomTokenStart) {
                _counter = new Random().Next();
            }
#endif
        }

        /// <inheritdoc/>
        public override void SendRequest(INextLayer nextLayer, Exchange exchange, Request request)
        {
#if false
            //  We now do this at the matcher layer so it can be random
            if (request.Token == null) {
                request.Token = NewToken();
            }
#endif
            base.SendRequest(nextLayer, exchange, request);
        }

        /// <inheritdoc/>
        public override void SendResponse(INextLayer nextLayer, Exchange exchange, Response response)
        {
            // A response must have the same token as the request it belongs to. If
            // the token is empty, we must use a byte array of length 0.
            if (response.Token == null) {
                response.Token = exchange.CurrentRequest.Token;
            }
            base.SendResponse(nextLayer, exchange, response);
        }

        /// <inheritdoc/>
        public override void ReceiveRequest(INextLayer nextLayer, Exchange exchange, Request request)
        {
            if (exchange.CurrentRequest.Token == null) {
                _Log.Info("ReceiveRequest: Received request token cannot be null");
                throw new InvalidOperationException("Received requests's token cannot be null, use byte[0] for empty tokens");
            }
            base.ReceiveRequest(nextLayer, exchange, request);
        }

        /// <inheritdoc/>
        public override void ReceiveResponse(INextLayer nextLayer, Exchange exchange, Response response)
        {
            if (response.Token == null) {
                _Log.Info("ReceiveResponse: Received response token cannot be null");
                throw new InvalidOperationException("Received response's token cannot be null, use byte[0] for empty tokens");
            }
            base.ReceiveResponse(nextLayer, exchange, response);
        }

#if false
        private Byte[] NewToken()
        {
            UInt32 token = (UInt32)System.Threading.Interlocked.Increment(ref _counter);
            return new Byte[]
            { 
                (Byte)(token >> 24), (Byte)(token >> 16),
                (Byte)(token >> 8), (Byte)token
            };
        }
#endif
    }
}
