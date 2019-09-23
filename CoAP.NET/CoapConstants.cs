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

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Com.AugustCellars.CoAP
{
    /// <summary>
    /// Constants defined for CoAP protocol
    /// </summary>
    public static class CoapConstants
    {
        /// <summary>
        /// RFC 7252 CoAP version.
        /// </summary>
        public const int Version = 0x01;

        /// <summary>
        /// The CoAP URI scheme.
        /// </summary>
        public const string UriScheme = "coap";

        /// <summary>
        /// The CoAPS URI scheme.
        /// </summary>
        public const string SecureUriScheme = "coaps";

        /// <summary>
        /// The default CoAP port for normal CoAP communication (not secure).
        /// </summary>
        public const int DefaultPort = 5683;

        /// <summary>
        /// The default CoAP port for secure CoAP communication (coaps).
        /// </summary>
        public const int DefaultSecurePort = 5684;

        /// <summary>
        /// The initial time (ms) for a CoAP message
        /// </summary>
        public const int AckTimeout = 2000;

        /// <summary>
        /// The initial timeout is set
        /// to a random number between RESPONSE_TIMEOUT and (RESPONSE_TIMEOUT *
        /// RESPONSE_RANDOM_FACTOR)
        /// </summary>
        public const double AckRandomFactor = 1.5D;

        /// <summary>
        /// The max time that a message would be retransmitted
        /// </summary>
        public const int MaxRetransmit = 4;

        /// <summary>
        /// Default block size used for block-wise transfers
        /// </summary>
        public const int DefaultBlockSize = 512;
        // public const Int32 MessageCacheSize = 32;
        // public const Int32 ReceiveBufferSize = 4096;
        // public const Int32 DefaultOverallTimeout = 100000;

        /// <summary>
        /// Default URI for wellknown resource
        /// </summary>
        public const string DefaultWellKnownURI = "/.well-known/core";

        //        public const Int32 TokenLength = 8;

        /// <summary>
        /// Max Age value to use if not on message
        /// </summary>
        public const int DefaultMaxAge = 60;

        /// <summary>
        /// The number of notifications until a CON notification will be used.
        /// </summary>
        public const int ObservingRefreshInterval = 10;

        /// <summary>
        /// EmptyToken value to use if no token provided.
        /// </summary>
        public static readonly byte[] EmptyToken = new byte[0];

        /// <summary>
        /// The lowest value of a request code.
        /// </summary>
        public const int RequestCodeLowerBound = 1;

        /// <summary>
        /// The highest value of a request code.
        /// </summary>
        public const int RequestCodeUpperBound = 31;

        /// <summary>
        /// The lowest value of a response code.
        /// </summary>
        public const int ResponseCodeLowerBound = 64;

        /// <summary>
        /// The highest value of a response code.
        /// </summary>
        public const int ResponseCodeUpperBound = 191;

        /// <summary>
        /// The lowest value of a signal code.
        /// </summary>
        public const int SignalCodeLowerBound = 224;

        /// <summary>
        /// The highest value of a signal code.
        /// </summary>
        public const int SignalCodeUpperBound = 255;
    }

    public class UriInformation
    {
        public string Name { get; }
        public int DefaultPort { get; }
        public enum TransportType { IP = 1 }

        private TransportType Transport { get; }

        public UriInformation(string name, int defaultPort, TransportType transportType)
        {
            Name = name;
            DefaultPort = defaultPort;
            Transport = transportType;
        }

        public static ReadOnlyDictionary<string, UriInformation> UriDefaults { get; } = new ReadOnlyDictionary<string, UriInformation>(
            new Dictionary<string, UriInformation>() {
                {"coap", new UriInformation("coap", 5683, TransportType.IP)},
                {"coaps", new UriInformation("coaps", 5684, TransportType.IP)},
                {"coap+udp", new UriInformation("coap+udp", 5683, TransportType.IP)},
                {"coaps+udp", new UriInformation("coaps+udp", 5684, TransportType.IP)},
                {"coap+tcp", new UriInformation("coap+tcp", 5683, TransportType.IP)},
                {"coaps+tcp", new UriInformation("coaps+tcp", 5684, TransportType.IP)}
            }
        );
    }
}
