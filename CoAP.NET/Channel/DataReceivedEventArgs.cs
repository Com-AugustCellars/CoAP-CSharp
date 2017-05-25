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

namespace Com.AugustCellars.CoAP.Channel
{
    /// <summary>
    /// Provides data for <see cref="IChannel.DataReceived"/> event.
    /// </summary>
    public class DataReceivedEventArgs : EventArgs
    {
        readonly Byte[] _data;
        readonly System.Net.EndPoint _endPoint;
        private readonly ISession _session;

        /// <summary>
        /// </summary>
        public DataReceivedEventArgs(Byte[] data, System.Net.EndPoint endPoint, ISession session)
        {
            _data = data;
            _endPoint = endPoint;
            _session = session;
        }

        /// <summary>
        /// Gets the received bytes.
        /// </summary>
        public Byte[] Data { get => _data;  }

        /// <summary>
        /// Gets the <see cref="System.Net.EndPoint"/> where the data is received from.
        /// </summary>
        public System.Net.EndPoint EndPoint { get => _endPoint;  }

        /// <summary>
        /// Gets the communication session for the message.
        /// </summary>
        public ISession Session
        {
            get => _session;
        }
    }
}
