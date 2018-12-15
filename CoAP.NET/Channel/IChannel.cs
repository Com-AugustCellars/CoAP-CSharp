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
using System.Net;

namespace Com.AugustCellars.CoAP.Channel
{
    /// <summary>
    /// Represents a channel where bytes data can flow through.
    /// </summary>
    public interface IChannel : IDisposable
    {
        /// <summary>
        /// Gets the local endpoint of this channel.
        /// </summary>
        System.Net.EndPoint LocalEndPoint { get; }
        /// <summary>
        /// Occurs when some bytes are received in this channel.
        /// </summary>
        event EventHandler<DataReceivedEventArgs> DataReceived;
        /// <summary>
        /// Add a multicast address to the channel
        /// </summary>
        /// <param name="ep">address to add</param>
        /// <returns>true if added</returns>
        bool AddMulticastAddress(IPEndPoint ep);
        /// <summary>
        /// Starts this channel.
        /// </summary>
        void Start();
        /// <summary>
        /// Stops this channel.
        /// </summary>
        void Stop();
        /// <summary>
        /// Abort the session - may not be clean.
        /// </summary>
        /// <param name="session">Session to abort</param>
        void Abort(ISession session);
        /// <summary>
        /// Clean shutdown for the session
        /// </summary>
        /// <param name="session">Session to shutdown</param>
        void Release(ISession session);
        /// <summary>
        /// Sends data through this channel. This method should be non-blocking.
        /// </summary>
        /// <param name="data">the bytes to send</param>
        /// <param name="session">what session to send this on</param>
        /// <param name="ep">the target endpoint</param>
        void Send(Byte[] data, ISession session, System.Net.EndPoint ep);
        /// <summary>
        /// Get the session that is going to be used to send to this endpoint.
        /// </summary>
        /// <param name="ep">the target endpoint</param>
        /// <returns>The session object</returns>
        ISession GetSession(System.Net.EndPoint ep);
    }
}
