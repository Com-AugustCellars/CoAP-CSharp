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

namespace Com.AugustCellars.CoAP.Deduplication
{
    /// <summary>
    /// Provides methods to detect duplicates.
    /// Notice that CONs and NONs can be duplicates.
    /// </summary>
    public interface IDeduplicator
    {
        /// <summary>
        /// Starts.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops.
        /// </summary>
        void Stop();

        /// <summary>
        /// Clears the state of this deduplicator.
        /// </summary>
        void Clear();

        /// <summary>
        /// Checks if the specified key is already associated with a previous
        /// exchange and otherwise associates the key with the exchange specified.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="exchange"></param>
        /// <returns>the previous exchange associated with the specified key,
        /// or <code>null</code> if there was no mapping for the key</returns>
        Exchange FindPrevious(Exchange.KeyID key, Exchange exchange);

        /// <summary>
        /// Find an exchange based on the key id
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <returns>Exchange if one exists</returns>
        Exchange Find(Exchange.KeyID key);
    }
}
