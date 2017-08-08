/*
 * Copyright (c) 2011-2012, Longxiang He <helongxiang@smeshlink.com>,
 * SmeshLink Technology Co.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY.
 * 
 * This file is part of the CoAP.NET, a CoAP framework in C#.
 * Please see README for more information.
 */

using Com.AugustCellars.CoAP.Server.Resources;
using System;
using System.Text;

namespace Com.AugustCellars.CoAP.EndPoint.Resources
{
    public partial class  RemoteResource : IComparable<RemoteResource>, IResource
    {
        public static RemoteResource NewRoot(String linkFormat, int mediaType = MediaType.ApplicationLinkFormat)
        {
            switch (mediaType) {
                case MediaType.ApplicationLinkFormat:
                    return LinkFormat.Deserialize(linkFormat);

                case MediaType.ApplicationJson:
                    return LinkFormat.DeserializeJson(linkFormat);

                default:
                    throw new ArgumentException("Unrecognized media type");
            }
        }

        public static RemoteResource NewRoot(byte[] linkFormat, int mediaType = MediaType.ApplicationLinkFormat)
        {
            switch (mediaType) {
                case MediaType.ApplicationLinkFormat:
                    return LinkFormat.Deserialize(Encoding.UTF8.GetString(linkFormat));

                case MediaType.ApplicationCbor:
                    return LinkFormat.DeserializeCbor(linkFormat);

                case MediaType.ApplicationJson:
                    return LinkFormat.DeserializeJson(Encoding.UTF8.GetString(linkFormat));

                default:
                    throw new ArgumentException("Unrecognized media type");
            }
        }

        /// <summary>
        /// Creates a resouce instance with proper subtype.
        /// </summary>
        /// <returns></returns>
        protected RemoteResource CreateInstance(String name)
        {
            return new RemoteResource(name);
        }

        protected  void DoCreateSubResource(Request request, String newIdentifier)
        { 
        }
    }
}
