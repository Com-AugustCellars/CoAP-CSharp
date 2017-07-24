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

using Com.AugustCellars.CoAP.Net;
using Com.AugustCellars.CoAP.Proxy.Resources;

namespace Com.AugustCellars.CoAP.Proxy
{
    /// <summary>
    /// ????????
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class DirectProxyCoAPResolver : IProxyCoAPResolver
    {
        /// <summary>
        /// Create a ????
        /// </summary>
        public DirectProxyCoAPResolver()
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="proxyCoapClientResource"></param>
        public DirectProxyCoAPResolver(ForwardingResource proxyCoapClientResource)
        {
            ProxyCoapClientResource = proxyCoapClientResource;
        }

        /// <summary>
        /// Get/Set the Proxy Coap resource code used by the resolver.
        /// </summary>
        public ForwardingResource ProxyCoapClientResource { get; set; }

        /// <summary>
        /// On a request - call the set client resource 
        /// </summary>
        /// <param name="exchange"></param>
        public void ForwardRequest(Exchange exchange)
        {
            ProxyCoapClientResource.HandleRequest(exchange);
        }
    }
}
