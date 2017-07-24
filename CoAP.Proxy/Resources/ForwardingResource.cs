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
using Com.AugustCellars.CoAP.Net;
using Com.AugustCellars.CoAP.Server.Resources;

namespace Com.AugustCellars.CoAP.Proxy.Resources
{
    /// <summary>
    /// Create an abstrack interface for doing a forwarder resource so that it gets everything 
    /// from one API.
    /// </summary>
    public abstract class ForwardingResource : Resource
    {
        /// <summary>
        /// Create a forwarding resource object
        /// </summary>
        /// <param name="resourceIdentifier">name of resource</param>
        /// <param name="hidden">is this a hidden resource? default to false</param>
        protected ForwardingResource(String resourceIdentifier, Boolean hidden = false)
            : base(resourceIdentifier, hidden)
        { }

        /// <summary>
        /// Deal with a request
        /// Default is to accept it and then pass on to get the request process and then send it back.
        /// </summary>
        /// <param name="exchange"></param>
        public override void HandleRequest(Exchange exchange)
        {
            exchange.SendAccept();
            Response response = ForwardRequest(exchange.Request);
            exchange.SendResponse(response);
        }

        /// <summary>
        /// Function to do the request forwarding
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected abstract Response ForwardRequest(Request request);
    }
}
