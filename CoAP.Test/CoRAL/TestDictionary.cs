using System;
using Com.AugustCellars.CoAP.Coral;
using Com.AugustCellars.CoAP.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.BouncyCastle.Utilities.Encoders;
using PeterO.Cbor;

namespace CoAP.Test.Std10.CoRAL
{
    [TestClass]
    public class TestDictionary
    {
        private CoralDictionary _dictionary;

        [TestInitialize]
        public void SetupDictionary()
        {
            _dictionary = new CoralDictionary() {
                {0, "http://www.w3.org/1999/02/22-rdf-syntax-ns#type"},
                {1, "http://www.iana.org/assignments/relation/item>"},
                {2, "http://www.iana.org/assignments/relation/collection"},
                {3, CBORObject.FromObject(93.56) },
                {4, new Cori("coap://host:99/path1/path2/path3") },
                {5, CBORObject.FromObjectAndTag(0, 1) },
                {6, CBORObject.FromObject(new byte[] {1,3,5,7, 9, 11, 13, 15})},
            };

        }

        [TestMethod]
        public void AddKey()
        {
            CBORObject result = _dictionary.Lookup("missing", false);
            Assert.AreEqual("missing", result.AsString());

            _dictionary.Add(7, "missing");

            result = _dictionary.Lookup("missing", false);
            Assert.AreEqual(7, result.AsInt32());

            Cori cori = new Cori("coap://host2:99/path1/path2/path4");
            _dictionary.Add(8, cori);
            Cori ciri2 = new Cori(cori.ToString());
            result = _dictionary.Lookup(ciri2, false);
            Assert.AreEqual(8, result.AsInt32());
            _dictionary.Add(9, new Cori(CBORObject.DecodeFromBytes(Hex.Decode("8405000664666F726D"))));

            Assert.ThrowsException<ArgumentException>(() =>_dictionary.Add(10, CBORObject.FromObject(DateTime.UtcNow)));
        }

        [TestMethod]
        public void AddNegativeKey()
        {
            Assert.ThrowsException<ArgumentException>(() => { _dictionary.Add(-1, "invalid"); });
        }

        [TestMethod]
        public void LookupString()
        {
            CBORObject result = _dictionary.Lookup("http://www.iana.org/assignments/relation/item>", false);
            Assert.AreEqual(CBORType.Integer, result.Type);
            Assert.AreEqual(1, result.AsInt32());

            result = _dictionary.Lookup(CBORObject.FromObject("http://www.iana.org/assignments/relation/item>"), false);
            Assert.AreEqual(CBORType.Integer, result.Type);
            Assert.AreEqual(1, result.AsInt32());

            result = _dictionary.Lookup(CBORObject.FromObject("http://www.iana.org/assignments/relation/item>"), true);
            Assert.AreEqual(CBORType.Integer, result.Type);
            Assert.IsTrue(result.IsTagged);
            Assert.IsTrue(result.HasOneTag(CoralDictionary.DictionaryTag));
            Assert.AreEqual(1, result.Untag().AsInt32());
        }

        [TestMethod]
        public void LookupInteger()
        {
            CBORObject result = _dictionary.Lookup(CBORObject.FromObject(5), true);
            Assert.AreEqual(CBORType.Integer, result.Type);
            Assert.IsFalse(result.IsTagged);
            Assert.AreEqual(5, result.AsInt32());

            result = _dictionary.Lookup(CBORObject.FromObject(5), false);
            Assert.AreEqual(CBORType.Integer, result.Type);
            Assert.IsFalse(result.IsTagged);
            Assert.AreEqual(5, result.AsInt32());

            result = _dictionary.Lookup(CBORObject.FromObject(-5), true);
            Assert.AreEqual(CBORType.Integer, result.Type);
            Assert.IsFalse(result.IsTagged);
            Assert.AreEqual(-5, result.AsInt32());
        }

        [TestMethod]
        public void LookupDate()
        {
            CBORObject result = _dictionary.Lookup(CBORObject.FromObjectAndTag(DateTimeOffset.UtcNow.ToUnixTimeSeconds(), 1), true);
            Assert.AreEqual(CBORType.Integer, result.Type);
            Assert.IsTrue(result.IsTagged);
            Assert.IsTrue(result.HasOneTag(1));
        }

        [TestMethod]
        public void LookupBinary()
        {
            byte[] bytes1 = new byte[]{2, 4, 6, 8, 10, 12, 14, 16};
            byte[] bytes2 = new byte[]{1, 3, 5, 7,  9, 11, 13, 15};

            CBORObject result = _dictionary.Lookup(CBORObject.FromObject(bytes1), true);
            Assert.AreEqual(CBORType.ByteString, result.Type);

            result = _dictionary.Lookup(CBORObject.FromObject(bytes2), true);
            Assert.AreEqual(CBORType.Integer, result.Type);
            Assert.IsTrue(result.IsTagged);
            Assert.IsTrue(result.HasOneTag(CoralDictionary.DictionaryTag));
            Assert.AreEqual(6, result.UntagOne().AsInt32());
        }

        [TestMethod]
        public void ReverseKey()
        {
            CBORObject result = (CBORObject) _dictionary.Reverse(CBORObject.FromObject(1), false);
            Assert.AreEqual("http://www.iana.org/assignments/relation/item>", result.AsString());
        }

        [TestMethod]
        public void ReverseMissingKey()
        {
            CBORObject result = (CBORObject) _dictionary.Reverse(CBORObject.FromObject(99), false);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ReverseNegativeKey()
        {
            CBORObject result = (CBORObject) _dictionary.Reverse(CBORObject.FromObject(-5), true);
            Assert.AreEqual(-5, result.AsInt32());
        }

}
}
