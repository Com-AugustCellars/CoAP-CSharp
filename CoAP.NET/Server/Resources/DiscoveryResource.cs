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

namespace Com.AugustCellars.CoAP.Server.Resources
{
    /// <summary>
    /// Represents the CoAP .well-known/core resource.
    /// </summary>
    public class DiscoveryResource : Resource
    {
        /// <summary>
        /// String that represents the name of this resource.
        /// </summary>
        /// Why is this public?
        public static readonly String Core = "core";
        private readonly IResource _root;

        /// <summary>
        /// Instantiates a new discovery resource.
        /// </summary>
        public DiscoveryResource(IResource root)
            : this(Core, root)
        { }

        /// <summary>
        /// Instantiates a new discovery resource with the specified name.
        /// </summary>
        public DiscoveryResource(String name, IResource root)
            : base(name)
        {
            _root = root;
        }

        /// <inheritdoc/>
        protected override void DoGet(CoapExchange exchange)
        {
            exchange.Respond(StatusCode.Content,
                LinkFormat.Serialize(_root, exchange.Request.UriQueries),
                MediaType.ApplicationLinkFormat);
        }
    }
}
