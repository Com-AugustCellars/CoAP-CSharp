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
using System.Net;
using Com.AugustCellars.CoAP.Log;

namespace Com.AugustCellars.CoAP.Proxy.Resources
{
    /// <summary>
    /// This class will provide a resource for doing mapping from a CoAP
    /// to an HTTP request and pass it on
    /// </summary>
    public class ProxyHttpClientResource : ForwardingResource
    {
        private static readonly ILogger _Log = LogManager.GetLogger(typeof(ProxyHttpClientResource));

        /// <summary>
        /// Create the resource with the name "/proxy/coapClient"
        /// </summary>
        public ProxyHttpClientResource()
            : this("proxy/coapClient")
        {
        }

        /// <summary>
        /// Create the resource with the provided resource name
        /// </summary>
        /// <param name="name"></param>
        public ProxyHttpClientResource(String name)
            : base(name)
        {
            Attributes.Title = "Forward the requests to a HTTP client.";
        }

        /// <summary>
        /// Code to do the mapping and send out the request to an http server
        /// </summary>
        /// <param name="incomingCoapRequest">request to be mapped</param>
        /// <returns>response to return</returns>
        protected override Response ForwardRequest(Request incomingCoapRequest)
        {
            // check the invariant: the request must have the proxy-uri set
            if (!incomingCoapRequest.HasOption(OptionType.ProxyUri)) {
                _Log.Warn("Proxy-uri option not set.");
                return new Response(StatusCode.BadOption);
            }

            // remove the fake uri-path
            incomingCoapRequest.RemoveOptions(OptionType.UriPath); // HACK

            // get the proxy-uri set in the incoming coap request
            Uri proxyUri;
            try {
                proxyUri = incomingCoapRequest.ProxyUri;
            }
            catch (UriFormatException e) {
                _Log.Warn(m => m("Proxy-uri option malformed: {0}", e.Message));
                return new Response(StatusCode.BadOption);
            }

            WebRequest httpRequest;
            try {
                httpRequest = HttpTranslator.GetHttpRequest(incomingCoapRequest);
            }
            catch (TranslationException e) {
                _Log.Warn(m => m("Problems during the http/coap translation: {0}", e.Message));
                return new Response(StatusCode.BadGateway);
            }

            HttpWebResponse httpResponse = (HttpWebResponse) httpRequest.GetResponse();

            DateTime timestamp = DateTime.Now;
            try {
                Response coapResponse = HttpTranslator.GetCoapResponse(httpResponse, incomingCoapRequest);
                coapResponse.Timestamp = timestamp;
                return coapResponse;
            }
            catch (TranslationException e) {
                _Log.Warn(m => m("Problems during the http/coap translation: {0}", e.Message));
                return new Response(StatusCode.BadGateway);
            }
        }
    }
}
