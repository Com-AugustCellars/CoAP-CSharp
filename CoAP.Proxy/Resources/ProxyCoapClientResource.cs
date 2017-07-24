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
using Com.AugustCellars.CoAP.Server.Resources;

namespace Com.AugustCellars.CoAP.Proxy.Resources
{
    /// <summary>
    /// Resource that implements the ability to proxy a CoAP request to a different CoAP server
    /// </summary>
    public class ProxyCoapClientResource : ForwardingResource
    {
        private static readonly ILogger _Log = LogManager.GetLogger(typeof(ProxyCoapClientResource));

        /// <summary>
        /// Create a proxy resource with the name "/proxy/coapClient"
        /// </summary>
        public ProxyCoapClientResource()
            : this("proxy/coapClient")
        {
        }

        /// <summary>
        /// Create a proxy resource with a provided name.
        /// </summary>
        /// <param name="name"></param>
        public ProxyCoapClientResource(string name)
            : base(name)
        {
            Attributes.Title = "Forward the requests to a CoAP server.";
        }

        /// <summary>
        /// Given a request - forward it
        /// </summary>
        /// <param name="incomingRequest">request to proxy</param>
        /// <returns>response back to requester</returns>
        protected override Response ForwardRequest(Request incomingRequest)
        {
            // check the invariant: the request must have the proxy-uri set
            if (!incomingRequest.HasOption(OptionType.ProxyUri)) {
                _Log.Warn("Proxy-uri option not set.");
                return new Response(StatusCode.BadOption);
            }

            // create a new request to forward to the requested coap server
            Request outgoingRequest;

            try {
                outgoingRequest = CoapTranslator.GetRequest(incomingRequest);

                outgoingRequest.Send();
            }
            catch (TranslationException ex) {
                _Log.Warn(m => m("Proxy-uri option malformed: {0}", ex.Message));
                return new Response(StatusCode.BadOption);
            }
            catch (System.IO.IOException ex) {
                _Log.Warn(m => m("Failed to execute request: {0}", ex.Message));
                return new Response(StatusCode.InternalServerError);
            }

            // receive the response
            Response receivedResponse;

            try {
                // M00BUG - Should time out on this and we don't right now.
                receivedResponse = outgoingRequest.WaitForResponse();
            }
            catch (System.Threading.ThreadInterruptedException ex) {
                _Log.Warn(m => m("Receiving of response interrupted: {0}", ex.Message));
                return new Response(StatusCode.InternalServerError);
            }

            if (receivedResponse == null) {
                return new Response(StatusCode.GatewayTimeout);
            }

            // create the real response for the original request
            Response outgoingResponse = CoapTranslator.GetResponse(receivedResponse);
            return outgoingResponse;
        }
    }
}
