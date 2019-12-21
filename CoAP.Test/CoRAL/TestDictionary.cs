using System;
using Com.AugustCellars.CoAP.Coral;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            };

        }

        [TestMethod]
        public void AddKey()
        {
            CBORObject result = _dictionary.Lookup("missing");
            Assert.AreEqual("missing", result.AsString());

            _dictionary.Add(4, "missing");

            result = _dictionary.Lookup("missing");
            Assert.AreEqual(4, result.AsInt32());
        }

        [TestMethod]
        public void AddNegativeKey()
        {
            Assert.ThrowsException<ArgumentException>(() => { _dictionary.Add(-1, "invalid"); });
        }

        [TestMethod]
        public void LookupString()
        {
            CBORObject result = _dictionary.Lookup("http://www.iana.org/assignments/relation/item>");
            Assert.AreEqual(CBORType.Integer, result.Type);
            Assert.AreEqual(1, result.AsInt32());

            _dictionary.Lookup(CBORObject.FromObject("http://www.iana.org/assignments/relation/item>"));
            Assert.AreEqual(CBORType.Integer, result.Type);
            Assert.AreEqual(1, result.AsInt32());
        }

        [TestMethod]
        public void LookupInteger()
        {
            CBORObject result = _dictionary.Lookup(CBORObject.FromObject(5));
            Assert.AreEqual(CBORType.Integer, result.Type);
            Assert.IsTrue(result.IsTagged);
            Assert.IsTrue(result.HasOneTag(99999));

            result = _dictionary.Lookup(CBORObject.FromObject(-5));
            Assert.AreEqual(CBORType.Integer, result.Type);
            Assert.IsFalse(result.IsTagged);
            Assert.AreEqual(-5, result.AsInt32());
        }

        [TestMethod]
        public void LookupDate()
        {
            CBORObject result = _dictionary.Lookup(CBORObject.FromObjectAndTag(DateTimeOffset.UtcNow.ToUnixTimeSeconds(), 1));
            Assert.AreEqual(CBORType.Integer, result.Type);
            Assert.IsTrue(result.IsTagged);
            Assert.IsTrue(result.HasOneTag(1));
        }

        [TestMethod]
        public void ReverseKey()
        {
            CBORObject result = _dictionary.Reverse(CBORObject.FromObject(1));
            Assert.AreEqual("http://www.iana.org/assignments/relation/item>", result.AsString());
        }

        [TestMethod]
        public void ReverseMissingKey()
        {
            CBORObject result = _dictionary.Reverse(CBORObject.FromObject(99));
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ReverseNegativeKey()
        {
            CBORObject result = _dictionary.Reverse(CBORObject.FromObject(-5));
            Assert.AreEqual(-5, result.AsInt32());
        }

}
}
