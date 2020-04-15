/*
 * Copyright (c) 2011-2014, Longxiang He <helongxiang@smeshlink.com>,
 * SmeshLink Technology Co.
 *
 * Copyright (c) 2020, Jim Schaad <ietf@augustcellars.com>
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY.
 * 
 * This file is part of the CoAP.NET, a CoAP framework in C#.
 * Please see README for more information.
 */

using System.Net;
using System.Net.Sockets;

namespace Com.AugustCellars.CoAP.Channel
{
    /// <summary>
    /// Extension methods for <see cref="IPAddress"/>.
    /// </summary>
    public static class IPAddressExtensions
    {
        /// <summary>
        /// Checks whether the IP address is an IPv4-mapped IPv6 address.
        /// </summary>
        /// <param name="address">the <see cref="IPAddress"/> object to check</param>
        /// <returns>true if the IP address is an IPv4-mapped IPv6 address; otherwise, false.</returns>
        public static bool IsIPv4MappedToIPv6(IPAddress address)
        {
            if (address.AddressFamily != AddressFamily.InterNetworkV6) {
                return false;
            }

            byte[] bytes = address.GetAddressBytes();
            for (int i = 0; i < 10; i++) {
                if (bytes[i] != 0) {
                    return false;
                }
            }
            return bytes[10] == 0xff && bytes[11] == 0xff;
        }

        /// <summary>
        /// Maps the <see cref="IPAddress"/> object to an IPv4 address.
        /// </summary>
        /// <param name="address">the <see cref="IPAddress"/> object</param>
        /// <returns>An IPv4 address.</returns>
        public static IPAddress MapToIPv4(IPAddress address)
        {
            if (address.AddressFamily == AddressFamily.InterNetwork) {
                return address;
            }

            byte[] bytes = address.GetAddressBytes();
            long newAddress = (uint)(bytes[12] & 0xff) | (uint)(bytes[13] & 0xff) << 8 | (uint)(bytes[14] & 0xff) << 16 | (uint)(bytes[15] & 0xff) << 24;
            return new IPAddress(newAddress);
        }

        /// <summary>
        /// Maps the <see cref="IPAddress"/> object to an IPv6 address.
        /// </summary>
        /// <param name="address">the <see cref="IPAddress"/> object</param>
        /// <returns>An IPv6 address.</returns>
        public static IPAddress MapToIPv6(IPAddress address)
        {
            if (address.AddressFamily == AddressFamily.InterNetworkV6) {
                return address;
            }

            byte[] bytes = address.GetAddressBytes();
            byte[] newAddress = new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0xff, 0xff, bytes[0], bytes[1], bytes[2], bytes[3] };
            return new IPAddress(newAddress);
        }

        public static bool IsMulticastAddress(IPAddress address)
        {
            return address.IsIPv6Multicast || (address.AddressFamily == AddressFamily.InterNetwork && ((address.GetAddressBytes()[0] & 0xf0) == 0xe0));
        }
    }
}
