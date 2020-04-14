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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Com.AugustCellars.CoAP.Channel
{
    [TestClass]
    public class IPAddressTest
    {
        [TestMethod]
        public void TestMapBetweenIPv4AndIPv6()
        {
            for (byte i = 0; i < byte.MaxValue; i++) {
                IPAddress ipv4 = new IPAddress(new byte[] { 10, 0, 0, i });
                IPAddress ipv6 = IPAddressExtensions.MapToIPv6(ipv4);
                Assert.IsTrue(IPAddressExtensions.IsIPv4MappedToIPv6(ipv6));

                IPAddress ipv4Mapped = IPAddressExtensions.MapToIPv4(ipv6);
                Assert.AreEqual(ipv4, ipv4Mapped);
            }
        }

        [TestMethod]
        public void NotIPv4Address()
        {
            IPAddress ipv6 = IPAddress.Parse("[2001:0db8:ac10:fe01::]");
            Assert.IsFalse((IPAddressExtensions.IsIPv4MappedToIPv6(ipv6)));
        }

        [TestMethod]
        public void TestNoMapping()
        {
            IPAddress ipv4 = new IPAddress(new byte[] { 10, 0, 0, 5 });
            IPAddress ipv4X = IPAddressExtensions.MapToIPv4(ipv4);
            Assert.AreEqual(ipv4, ipv4X);

            IPAddress ipv6 = IPAddress.Parse("[2001:0db8:ac10:fe01::]");
            IPAddress ipv6X = IPAddressExtensions.MapToIPv6(ipv6);
            Assert.AreEqual(ipv6, ipv6X);

        }
    }
}
