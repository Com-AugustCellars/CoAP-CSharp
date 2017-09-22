/*
 * Copyright (c) 2011-2013, Longxiang He <helongxiang@smeshlink.com>,
 * SmeshLink Technology Co.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY.
 * 
 * This file is part of the CoAP.NET, a CoAP framework in C#.
 * Please see README for more information.
 */

namespace Com.AugustCellars.CoAP
{
    /// <summary>
    /// CoAP option types as defined in
    /// RFC 7252, Section 12.2 and other CoAP extensions.
    /// </summary>
    public enum OptionType
    {
        Unknown = -1,

        /// <summary>
        /// <remarks>RFC 7252</remarks>
        /// </summary>
        Reserved = 0,
        /// <summary>
        /// C, opaque, 0-8 B, -
        /// <remarks>RFC 7252</remarks>
        /// </summary>
        IfMatch = 1,
        /// <summary>
        /// C, String, 1-270 B, ""
        /// <remarks>RFC 7252</remarks>
        /// </summary>
        UriHost = 3,
        /// <summary>
        /// E, sequence of bytes, 1-4 B, -
        /// <remarks>RFC 7252</remarks>
        /// </summary>
        ETag = 4,
        /// <summary>
        /// <remarks>RFC 7252</remarks>
        /// </summary>
        IfNoneMatch = 5,
        /// <summary>
        /// E, Duration, 1 B, 0
        /// <remarks>RFC 7641</remarks>
        /// </summary>
        Observe = 6,
        /// <summary>
        /// C, uint, 0-2 B
        /// <remarks>RFC 7252</remarks>
        /// </summary>
        UriPort = 7,
        /// <summary>
        /// E, String, 1-270 B, -
        /// <remarks>RFC 7252</remarks>
        /// </summary>
        LocationPath = 8,
        /// <summary>
        /// C, String, 1-270 B, ""
        /// <remarks>RFC 7252</remarks>
        /// </summary>
        UriPath = 11,
        /// <summary>
        /// C, 8-bit uint, 1 B, 0 (text/plain)
        /// <seealso cref="ContentFormat"/>
        /// <remarks>RFC 7252</remarks>
        /// </summary>
        ContentType = 12,
        /// <summary>
        /// C, 8-bit uint, 1 B, 0 (text/plain)
        /// <remarks>RFC 7252</remarks>
        /// </summary>
        ContentFormat = 12,
        /// <summary>
        /// E, variable length, 1--4 B, 60 Seconds
        /// <remarks>RFC 7252</remarks>
        /// </summary>
        MaxAge = 14,
        /// <summary>
        /// C, String, 1-270 B, ""
        /// <remarks>RFC 7252</remarks>
        /// </summary>
        UriQuery = 15,
        /// <summary>
        /// C, Sequence of Bytes, 1-n B, -
        /// <remarks>RFC 7252</remarks>
        /// </summary>
        Accept = 17,
        /// <summary>
        /// E, String, 1-270 B, -
        /// <remarks>RFC 7252</remarks>
        /// </summary>
        LocationQuery = 20,
        /// <summary>
        /// C, U, uint, 0-3
        /// Block transfer from server to client
        /// <remarks>RFC 7959</remarks>
        /// </summary>
        Block2 = 23,
        /// <summary>
        /// C, U, uint, 0-3
        /// Block transfer from client to server
        /// <remarks>RFC 7959</remarks>
        /// </summary>
        Block1 = 27,
        /// <summary>
        /// uint, 0-4
        /// <remarks>RFC 7959</remarks>
        /// </summary>
        Size2 = 28,
        /// <summary>
        /// C, String, 1-270 B, "coap"
        /// <remarks>RFC 7252</remarks>
        /// </summary>
        ProxyUri = 35,
        /// <summary>
        /// <remarks>RFC 7252</remarks>
        /// </summary>
        ProxyScheme = 39,

        /// <summary>
        /// <remarks>RFC 7252</remarks>
        /// </summary>
        Size1 = 60,
        /// <summary>
        /// <remarks>RFC 7252</remarks>
        /// </summary>
        Reserved1 = 128,
        /// <summary>
        /// <remarks>RFC 7252</remarks>
        /// </summary>
        Reserved2 = 132,
        /// <summary>
        /// <remarks>RFC 7252</remarks>
        /// </summary>
        Reserved3 = 136,
        /// <summary>
        /// <remarks>RFC 7252</remarks>
        /// </summary>
        Reserved4 = 140,

#if INCLUDE_OSCOAP
        /// <summary>
        /// <remarks>draft-ietf-core-oscoap</remarks>
        /// </summary>
        Oscoap = 65025,
        Oscoap2 = 21,
#endif

#if FRESHNESS
        /// <summary>
        /// Resend request for freshness purposes
        /// <remarks>draft-amsuess-core-repeat-request-tag</remarks>
        /// </summary>
        ResendRequest = 65026,

        /// <summary>
        /// Request defined ETAG
        /// <remarks>draft-amsuess-core-repeat-request-tag</remarks>
        /// </summary>
        Request_ETag = 65027,
#endif

        /// <summary>
        /// no-op for fenceposting
        /// <remarks>draft-bormann-coap-misc-04</remarks>
        /// </summary>
        [System.Obsolete]
        FencepostDivisor = 114,
    }

    /// <summary>
    /// CoAP option formats
    /// </summary>
    public enum OptionFormat
    {
        Integer,
        String,
        Opaque,
        Unknown
    }
}
