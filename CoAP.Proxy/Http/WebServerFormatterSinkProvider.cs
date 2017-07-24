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

using System.Runtime.Remoting.Channels;

namespace Com.AugustCellars.CoAP.Proxy.Http
{
    partial class WebServer
    {
        class WebServerFormatterSinkProvider : IServerFormatterSinkProvider
        {
            private readonly WebServer _webServer;

            public WebServerFormatterSinkProvider(WebServer webServer)
            {
                _webServer = webServer;
            }

            /// <summary>
            /// Get/Set next provider in the chain
            /// </summary>
            public IServerChannelSinkProvider Next { get; set; }

            public IServerChannelSink CreateSink(IChannelReceiver channel)
            {
                IServerChannelSink sink = null;
                if (Next != null) {
                    sink = Next.CreateSink(channel);
                }

                return new WebServerChannelSink(sink, channel, _webServer);
            }

            public void GetChannelData(IChannelDataStore channelData)
            { }
        }
    }
}
