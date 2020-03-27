/*
 * Copyright (c) 2011-2013, Longxiang He <helongxiang@smeshlink.com>,
 * SmeshLink Technology Co.
 *
 * Copyright (c) 2019-2020, Jim Schaad <ietf@augustcellars.com>
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY.
 * 
 * This file is part of the CoAP.NET, a CoAP framework in C#.
 * Please see README for more information.
 */

using System;
using System.Collections.Generic;
using Com.AugustCellars.CoAP.OSCOAP;

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
        /// <summary>
        /// <remarks>draft-ietf-core-oscoap</remarks>
        /// </summary>
        Oscoap = 9,
        /// <summary>
        /// <remarks>RFC 7967</remarks>
        /// </summary>
        NoResponse = 258,

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
        /// Maximum message size option - only for SignalCode.CMS
        /// </summary>
        Signal_MaxMessageSize = 2,
        /// <summary>
        /// Block Transfer Support - only for SignalCode.CMS
        /// </summary>
        Signal_BlockTransfer = 4,
        /// <summary>
        /// Custody option - only for SignalCode.Ping and SignalCode.Pong
        /// </summary>
        Signal_Custody = 2,
        /// <summary>
        /// Use Alternate Address option - only for SignalCode.Release
        /// </summary>
        Signal_AltAddress = 2,
        /// <summary>
        /// Dont reconnect for option - only for SignalCode.Release
        /// </summary>
        Signal_HoldOff = 4,
        /// <summary>
        /// BAD CSM option - only for SignalCode.Abort
        /// </summary>
        Signal_BadCSMOption = 2,
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

    public class OptionData
    {
        public OptionType OptionNumber { get; }
        public OptionFormat OptionFormat { get; }
        public string OptionName { get; }
        public Type OptionType { get; }

        public OptionData(OptionType option, string name, OptionFormat format, Type type)
        {
            OptionNumber = option;
            OptionFormat = format;
            OptionName = name;
            OptionType = type;
        }

        public static Dictionary<OptionType, OptionData> OptionInfoDictionary { get; } = new Dictionary<OptionType, OptionData>() {
            {CoAP.OptionType.Accept, new OptionData(CoAP.OptionType.Accept, "Accept", OptionFormat.Integer, null) },
            {CoAP.OptionType.ContentType, new OptionData(CoAP.OptionType.ContentType, "ContentType", OptionFormat.Integer, null) },
            {CoAP.OptionType.Block1, new OptionData(CoAP.OptionType.Block1, "Block1", OptionFormat.Integer, typeof(BlockOption)) },
            {CoAP.OptionType.Block2, new OptionData(CoAP.OptionType.Block2, "Block2", OptionFormat.Integer, typeof(BlockOption)) },
            {CoAP.OptionType.ETag, new OptionData(CoAP.OptionType.ETag, "ETag", OptionFormat.Opaque, null) },
            {CoAP.OptionType.IfMatch, new OptionData(CoAP.OptionType.IfMatch, "If-Match", OptionFormat.Opaque, null)},
            {CoAP.OptionType.IfNoneMatch, new OptionData(CoAP.OptionType.IfNoneMatch, "If-None-Match", OptionFormat.Opaque, null) },
            {CoAP.OptionType.LocationPath, new OptionData(CoAP.OptionType.LocationPath, "Location-Path", OptionFormat.String, null) },
            {CoAP.OptionType.LocationQuery, new OptionData(CoAP.OptionType.LocationQuery, "Location-Query", OptionFormat.String, null) },
            {CoAP.OptionType.MaxAge, new OptionData(CoAP.OptionType.MaxAge, "Max-Age", OptionFormat.Integer, null) },
            {CoAP.OptionType.NoResponse, new OptionData(CoAP.OptionType.NoResponse, "No-Response", OptionFormat.Unknown, null)  },
            {CoAP.OptionType.Observe, new OptionData(CoAP.OptionType.Observe, "Observe", OptionFormat.Integer, null)},
            {CoAP.OptionType.Oscoap, new OptionData(CoAP.OptionType.Oscoap, "OSCORE", OptionFormat.Opaque, typeof(OscoapOption))},
            {CoAP.OptionType.ProxyScheme, new OptionData(CoAP.OptionType.ProxyScheme, "ProxyScheme", OptionFormat.String, null) },
            {CoAP.OptionType.ProxyUri, new OptionData(CoAP.OptionType.ProxyUri, "Proxy-Uri", OptionFormat.String, null) },
            {CoAP.OptionType.Size1, new OptionData(CoAP.OptionType.Size1, "Size1", OptionFormat.Integer, null) },
            {CoAP.OptionType.Size2, new OptionData(CoAP.OptionType.Size2, "Size2", OptionFormat.Integer, null) },
            {CoAP.OptionType.UriHost, new OptionData(CoAP.OptionType.UriHost, "UriHost", OptionFormat.String, null)},
            {CoAP.OptionType.UriPath, new OptionData(CoAP.OptionType.UriPath, "UriPath", OptionFormat.String, null) },
            {CoAP.OptionType.UriPort, new OptionData(CoAP.OptionType.UriPort, "UriPort", OptionFormat.Integer, null) },
            {CoAP.OptionType.UriQuery, new OptionData(CoAP.OptionType.UriQuery, "UriQuery", OptionFormat.String, null) },
        };
    }
}
