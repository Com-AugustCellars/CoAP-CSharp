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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using System.Runtime.Remoting.Messaging;

namespace Com.AugustCellars.CoAP.Proxy.Http
{
    /// <summary>
    /// Create a simple web server to act as a proxy.
    /// </summary>
    internal partial class WebServer
    {
        private readonly string _name;
        private readonly List<IServiceProvider> _serviceProviders = new List<IServiceProvider>();
        private readonly IChannel _channel;

        /// <summary>
        /// Create a simple web server object
        /// </summary>
        /// <param name="name">name for the web server channel</param>
        /// <param name="port">port to operate on</param>
        public WebServer(string name, int port)
        {
            _name = name;
            _channel = new HttpServerChannel(name, port, new WebServerFormatterSinkProvider(this));
        }

        /// <summary>
        /// Add the provider to the list of providers supported by the server
        /// </summary>
        /// <param name="provider"></param>
        public void AddProvider(IServiceProvider provider)
        {
            _serviceProviders.Add(provider);
        }

        /// <summary>
        /// Start the HTTP channel
        /// </summary>
        public void Start()
        {
            ChannelServices.RegisterChannel(_channel, false);
        }

        /// <summary>
        /// Stop the HTTP channel we are using
        /// </summary>
        public void Stop()
        {
            try {
                ChannelServices.UnregisterChannel(_channel);
            }
            catch {
                // ignored
            }
        }

        /// <summary>
        /// Implements the HTTP channel sink interface
        /// </summary>
        private class WebServerChannelSink : IServerChannelSink
        {
            private readonly WebServer _webServer;

            /// <summary>
            /// Create the sink
            /// </summary>
            /// <param name="next">next sink in the chain</param>
            /// <param name="channel">channel receiver</param>
            /// <param name="webServer">What web server</param>
            public WebServerChannelSink(IServerChannelSink next, IChannelReceiver channel, WebServer webServer)
            {
                _webServer = webServer;
                NextChannelSink = next;
               // if (channel != null) /throw new Exception("We don't use this");
            }

            /// <summary>
            /// Link of channel sinks.
            /// </summary>
            public IServerChannelSink NextChannelSink { get; private set; }

            public IDictionary Properties
            {
                get => throw new NotImplementedException();
            }

            public void AsyncProcessResponse(IServerResponseChannelSinkStack sinkStack, Object state, IMessage msg, ITransportHeaders headers, Stream stream)
            {
                throw new NotImplementedException();
            }

            public Stream GetResponseStream(IServerResponseChannelSinkStack sinkStack, Object state, IMessage msg, ITransportHeaders headers)
            {
                throw new NotImplementedException();
            }

            public ServerProcessing ProcessMessage(IServerChannelSinkStack sinkStack, IMessage requestMsg, ITransportHeaders requestHeaders, Stream requestStream, out IMessage responseMsg, out ITransportHeaders responseHeaders, out Stream responseStream)
            {
                if (requestMsg != null) {
                    return NextChannelSink.ProcessMessage(sinkStack, requestMsg, requestHeaders, requestStream,
                        out responseMsg, out responseHeaders, out responseStream);
                }

                IHttpRequest request = GetRequest(requestHeaders, requestStream);
                IHttpResponse response = GetResponse(request);

                foreach (IServiceProvider provider in _webServer._serviceProviders) {
                    if (provider.Accept(request)) {
                        provider.Process(request, response);
                        break;
                    }
                }

                response.AppendHeader("Server", _webServer._name);

                responseHeaders = (response as RemotingHttpResponse).Headers;
                responseStream = response.OutputStream;
                responseMsg = null;

                return ServerProcessing.Complete;
            }

            private static IHttpResponse GetResponse(IHttpRequest request)
            {
                RemotingHttpResponse response = new RemotingHttpResponse();
                return response;
            }

            private static IHttpRequest GetRequest(ITransportHeaders requestHeaders, Stream requestStream)
            {
                return new RemotingHttpRequest(requestHeaders, requestStream);
            }
        }
    }
}
