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
using System.Text;

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
            Request req = exchange.Request;

            if (req.HasOption(OptionType.Accept)) {
                byte[] payload;

                switch (req.Accept) {
                    case MediaType.ApplicationLinkFormat:
                        payload = Encoding.UTF8.GetBytes(LinkFormat.Serialize(_root, req.UriQueries));
                        break;

                    case MediaType.ApplicationCbor:
                        payload = LinkFormat.SerializeCbor(_root, req.UriQueries);
                        break;

                    case MediaType.ApplicationJson:
                        payload = Encoding.UTF8.GetBytes(LinkFormat.SerializeJson(_root, req.UriQueries));
                        break;

                    default:
                        exchange.Respond(StatusCode.BadOption);
                        return;
                }

                exchange.Respond(StatusCode.Content, payload, req.Accept);
            }
            else {
                exchange.Respond(StatusCode.Content,
                    LinkFormat.Serialize(_root, exchange.Request.UriQueries),
                    MediaType.ApplicationLinkFormat);
            }
        }
    }
}
