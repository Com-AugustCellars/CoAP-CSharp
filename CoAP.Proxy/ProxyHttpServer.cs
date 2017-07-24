/*
 * Copyright (c) 2011-2015, Longxiang He <helongxiang@smeshlink.com>,
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

namespace Com.AugustCellars.CoAP.Proxy
{
    /// <summary>
    /// Implementation of a class which acts as an HTTP server for the purpose of
    /// doing proxying from HTTP to CoAP.
    /// </summary>
    public class ProxyHttpServer
    {
        static readonly ILogger _Log = LogManager.GetLogger(typeof(ProxyHttpServer));
        static readonly string ProxyCoapClient = "proxy/coapClient";
        static readonly string ProxyHttpClient = "proxy/httpClient";
        readonly ICacheResource _cacheResource = new NoCache(); // TODO cache implementation
        private readonly HttpStack _httpStack;

        /// <summary>
        /// Create the proxy server on the default HTTP port
        /// </summary>
        public ProxyHttpServer()
            : this(CoapConfig.Default.HttpPort)
        {
        }

        /// <summary>
        /// Create the proxy server on a specific port
        /// </summary>
        /// <param name="httpPort">http port</param>
        public ProxyHttpServer(Int32 httpPort)
        {
            _httpStack = new HttpStack(httpPort) {
                RequestHandler = HandleRequest
            };
        }

        /// <summary>
        /// Get/Set the CoAP resolver to use
        /// </summary>
        public IProxyCoAPResolver ProxyCoapResolver { get; set; }


        private void HandleRequest(Request request)
        {
            Exchange exchange = new ProxyExchange(this, request);
            exchange.Request = request;

            // ignore the request if it is reset or acknowledge
            // check if the proxy-uri is defined
            if ((request.Type != MessageType.RST) && (request.Type != MessageType.ACK) &&
                request.HasOption(OptionType.ProxyUri)) {

                // get the response from the cache
                Response response = _cacheResource.GetResponse(request);

                // TODO update statistics
                //_statsResource.updateStatistics(request, response != null);

                // check if the response is present in the cache
                if (response != null) {
                    // link the retrieved response with the request to set the
                    // parameters request-specific (i.e., token, id, etc)
                    exchange.SendResponse(response);
                    return;
                }
            }

            // edit the request to be correctly forwarded if the proxy-uri is set

            if (request.HasOption(OptionType.ProxyUri)) {
                try {
                    ManageProxyUriRequest(request);
                }
                catch (Exception) {
                    _Log.Warn(m => m("Proxy-uri malformed: {0}", request.GetFirstOption(OptionType.ProxyUri).StringValue));

                    exchange.SendResponse(new Response(StatusCode.BadOption));
                }
            }

            // handle the request as usual
            if (ProxyCoapResolver != null) ProxyCoapResolver.ForwardRequest(exchange);

        }

        /// <summary>
        /// Cache the response if it was for a proxy request
        /// </summary>
        /// <param name="request">request</param>
        /// <param name="response">matching response</param>
        protected void ResponseProduced(Request request, Response response)
        {
            // check if the proxy-uri is defined
            if (request.HasOption(OptionType.ProxyUri)) {
                // insert the response in the cache
                _cacheResource.CacheResponse(request, response);
            }
        }

        private void ManageProxyUriRequest(Request request)
        {
            // check which schema is requested
            Uri proxyUri = request.ProxyUri;

            // the local resource that will abstract the client part of the
            // proxy
            String clientPath;

            // switch between the schema requested
            if (proxyUri.Scheme != null && proxyUri.Scheme.StartsWith("http")) {
                // the local resource related to the http client
                clientPath = ProxyHttpClient;
            }
            else {
                // the local resource related to the http client
                clientPath = ProxyCoapClient;
            }

            // set the path in the request to be forwarded correctly
            request.UriPath = clientPath;
        }

        private class ProxyExchange : Exchange
        {
            readonly ProxyHttpServer _server;
            readonly Request _request;

            public ProxyExchange(ProxyHttpServer server, Request request)
                : base(request, Origin.Remote)
            {
                _server = server;
                _request = request;
            }

            public override void SendAccept()
            {
                // has no meaning for HTTP: do nothing
            }

            public override void SendReject()
            {
                // TODO: close the HTTP connection to signal rejection
            }

            public override void SendResponse(Response response)
            {
                // Redirect the response to the HttpStack instead of a normal
                // CoAP endpoint.
                // TODO: When we change endpoint to be an interface, we can
                // redirect the responses a little more elegantly.
                try {
                    _request.Response = response;
                    _server.ResponseProduced(_request, response);
                    _server._httpStack.DoSendResponse(_request, response);
                }
                catch (Exception e) {
                    if (_Log.IsWarnEnabled) _Log.Warn("Exception while responding to Http request", e);
                }
            }
        }

        private class NoCache : ICacheResource
        {
            public void CacheResponse(Request request, Response response)
            {
            }

            public Response GetResponse(Request request)
            {
                return null;
            }

            public void InvalidateRequest(Request request)
            {
            }
        }
    }
}
