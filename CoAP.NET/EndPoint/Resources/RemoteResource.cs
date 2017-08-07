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


namespace Com.AugustCellars.CoAP.EndPoint.Resources
{
    public partial class  RemoteResource : IComparable<RemoteResource>, IResource
    {
        public static RemoteResource NewRoot(String linkFormat)
        {
            return LinkFormat.Deserialize(linkFormat);
        }

        /// <summary>
        /// Creates a resouce instance with proper subtype.
        /// </summary>
        /// <returns></returns>
        protected  RemoteResource CreateInstance(String name)
        {
            return new RemoteResource(name);
        }

        protected  void DoCreateSubResource(Request request, String newIdentifier)
        { 
        }
    }
}
