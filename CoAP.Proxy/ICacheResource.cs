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

namespace Com.AugustCellars.CoAP.Proxy
{
    /// <summary>
    /// Interface to a response cache for the system.
    /// This interface is subject to change
    /// </summary>
    public interface ICacheResource
    {
        /// <summary>
        /// Put a request/response pair into the cache
        /// </summary>
        /// <param name="request">request that generated the response</param>
        /// <param name="response">response to cache</param>
        void CacheResponse(Request request, Response response);

        /// <summary>
        /// Retrieve a response from the cache
        /// </summary>
        /// <param name="request">request to be satisfied</param>
        /// <returns>response to be returned</returns>
        Response GetResponse(Request request);

        /// <summary>
        /// Invalidated a cache entry for a request
        /// </summary>
        /// <param name="request">request to match</param>
        void InvalidateRequest(Request request);
    }
}
