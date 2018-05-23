﻿using System;
using System.Text;
using PeterO.Cbor;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
using NUnit.Framework;
using Com.AugustCellars.COSE;

namespace Com.AugustCellars.CoAP.OSCOAP
{
    [TestClass]
    public class SecurityContext_Test
    {
        private static readonly byte[] _SenderId = Encoding.UTF8.GetBytes("client");
        private static readonly byte[] _RecipientId = Encoding.UTF8.GetBytes("server");
        private static readonly byte[] _Secret = new byte[] {01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20, 0x21, 0x22, 0x23};
        private static readonly CBORObject _KeyAgreeAlg = AlgorithmValues.ECDH_SS_HKDF_256;
        private static readonly byte[] _Salt = Encoding.UTF8.GetBytes("Salt String");

        [TestMethod]
        public void Derive_CCM()
        {
            SecurityContext ctx = SecurityContext.DeriveContext(_Secret, _SenderId, _RecipientId, null, AlgorithmValues.AES_CCM_16_64_128, _KeyAgreeAlg);

            Assert.AreEqual(ctx.Sender.BaseIV, new byte[]{ 0x6D, 0xD5, 0x8C, 0x18, 0xFD, 0x22, 0xFB, 0xA8, 0xB4, 0xA7, 0xA2, 0xD0, 0x6B });
            Assert.AreEqual(ctx.Sender.Key, new byte[] { 0x8D, 0x41, 0x3A, 0xD6, 0x59, 0xFA, 0x1C, 0xF0, 0xB0, 0x7C, 0x2F, 0xD9, 0x6A, 0x53, 0x75, 0xC3 });
            Assert.AreEqual(ctx.Recipient.BaseIV, new byte[]{ 0x6D, 0xD5, 0x9C, 0x11, 0xE6, 0x31, 0xF0, 0xAE, 0xB4, 0xA7, 0xA2, 0xD0, 0x6B });
            Assert.AreEqual(ctx.Recipient.Key, new byte[]{ 0x4E, 0x48, 0xF7, 0xCB, 0xDC, 0x2E, 0x71, 0x89, 0x9A, 0x6B, 0x3C, 0x82, 0x13, 0x4F, 0xE5, 0x09 });

            SecurityContext ctx2 = SecurityContext.DeriveContext(_Secret, _RecipientId, _SenderId, null, AlgorithmValues.AES_CCM_16_64_128, _KeyAgreeAlg);
            Assert.AreEqual(ctx.Sender.BaseIV, ctx2.Recipient.BaseIV);
            Assert.AreEqual(ctx.Sender.Key, ctx2.Recipient.Key);
        }

        [TestMethod]
        public void Derive_Salt()
        {
            SecurityContext ctx = SecurityContext.DeriveContext(_Secret, _SenderId, _RecipientId, _Salt, AlgorithmValues.AES_CCM_16_64_128, _KeyAgreeAlg);

            Assert.AreEqual(ctx.Sender.BaseIV, new byte[] { 0x84, 0x2C, 0xB2, 0x77, 0xDC, 0xA6, 0x1A, 0xE3, 0x8C, 0x2F, 0x3E, 0x25, 0x18 });
            Assert.AreEqual(ctx.Sender.Key, new byte[] { 0x9E, 0x54, 0x18, 0x11, 0xD2, 0xFA, 0x52, 0xEB, 0x05, 0x86, 0xD3, 0x88, 0xA0, 0x92, 0xAC, 0x93 });
            Assert.AreEqual(ctx.Recipient.BaseIV, new byte[] { 0x84, 0x2C, 0xA2, 0x7E, 0xC7, 0xB5, 0x11, 0xE5, 0x8C, 0x2F, 0x3E, 0x25, 0x18 });
            Assert.AreEqual(ctx.Recipient.Key, new byte[] { 0xAD, 0x35, 0xAF, 0xD2, 0xA8, 0x86, 0x08, 0x8A, 0x13, 0xD6, 0x94, 0x04, 0x34, 0x0A, 0xC0, 0x1E});
        }

        [TestMethod]
        public void Derive_Hash512()
        {
            SecurityContext ctx = SecurityContext.DeriveContext(_Secret, _SenderId, _RecipientId, _Salt, AlgorithmValues.AES_CCM_16_64_128, AlgorithmValues.ECDH_SS_HKDF_512);

            Assert.AreEqual(ctx.Sender.BaseIV, new byte[] { 0xAB, 0xCE, 0x1E, 0x0A, 0x26, 0x80, 0x66, 0x76, 0xC4, 0x3A, 0x38, 0x94, 0x55 });
            Assert.AreEqual(ctx.Sender.Key, new byte[] { 0xB6, 0xC4, 0x06, 0xD0, 0x2C, 0x3B, 0xDC, 0x8D, 0xFE, 0x21, 0x03, 0x93, 0xC8, 0x3F, 0xFC, 0xFA });
            Assert.AreEqual(ctx.Recipient.BaseIV, new byte[] { 0xAB, 0xCE, 0x0E, 0x03, 0x3D, 0x93, 0x6D, 0x70, 0xC4, 0x3A, 0x38, 0x94, 0x55 });
            Assert.AreEqual(ctx.Recipient.Key, new byte[] { 0xA6, 0x60, 0x66, 0xBB, 0x88, 0xCE, 0x9B, 0x24, 0xCC, 0xE3, 0x00, 0xE7, 0x23, 0xB1, 0x5C, 0x7F });
        }

        [TestMethod]
        public void Derive_GCM()
        {
            SecurityContext ctx = SecurityContext.DeriveContext(_Secret, _SenderId, _RecipientId, null, AlgorithmValues.AES_GCM_128, _KeyAgreeAlg);

            Assert.AreEqual(ctx.Sender.BaseIV, new byte[] { 0x08, 0x74, 0x9E, 0xBB, 0x1F, 0x60, 0x70, 0xA3, 0x29, 0xE4, 0x48, 0xAC });
            Assert.AreEqual(ctx.Sender.Key, new byte[] { 0xAA, 0x43, 0x2E, 0xA7, 0xF4, 0xC0, 0xAF, 0x8E, 0x1B, 0x0D, 0x82, 0xD0, 0x13, 0x50, 0xC1, 0xCB });
            Assert.AreEqual(ctx.Recipient.BaseIV, new byte[] { 0x08, 0x64, 0x97, 0xA0, 0x0C, 0x6B, 0x76, 0xA3, 0x29, 0xE4, 0x48, 0xAC });
            Assert.AreEqual(ctx.Recipient.Key, new byte[] { 0x04, 0xCF, 0xD6, 0xF1, 0xE2, 0x64, 0xF4, 0x95, 0x7D, 0xC3, 0xE1, 0x6F, 0x32, 0x09, 0x11, 0x4E });
        }
    }
}
