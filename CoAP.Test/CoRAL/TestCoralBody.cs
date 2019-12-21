using System;
using System.Collections.Generic;
using System.Text;
using Com.AugustCellars.CoAP.Coral;
using Com.AugustCellars.CoAP.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.BouncyCastle.Utilities.Encoders;
using PeterO.Cbor;

namespace CoAP.Test.Std10.CoRAL
{
    [TestClass]
    public class TestCoralBody
    {
        private static CoralDictionary _testDictionary = new CoralDictionary() {
            {0, "http://www.w3.org/1999/02/22-rdf-syntax-ns#type"},
            {1, "http://www.iana.org/assignments/relation/item>"},
            {2, "http://www.iana.org/assignments/relation/collection"},
            {3, "http://coreapps.org/collections#create"},
            {4, "http://coreapps.org/base#update"},
            {5, "http://coreapps.org/collections#delete"},
            {6, "http://coreapps.org/base#search"},
            {7, "http://coreapps.org/coap#accept"},
            {8, "http://coreapps.org/coap#type"},
            {9, "http://coreapps.org/base#lang"},
            {10, "http://coreapps.org/coap#method"}
        };

        [TestMethod]
        public void TestConstructors()
        {
            Ciri ciri = new Ciri("coap://host:99");
            Assert.ThrowsException<ArgumentException>(() => new CoralBody(CBORObject.DecodeFromBytes(Hex.Decode("01")), ciri, null));
            Assert.ThrowsException<ArgumentException>(() => new CoralBody(CBORObject.DecodeFromBytes(Hex.Decode("830202820500")), ciri, null));

            CoralBody body = new CoralBody(CBORObject.DecodeFromBytes(Hex.Decode("81830202820500")), ciri, _testDictionary);
            Assert.AreEqual(1, body.Length);
            Assert.AreEqual("http://www.iana.org/assignments/relation/collection", ((CoralLink) body[0]).RelationType);

            body = new CoralBody(CBORObject.DecodeFromBytes(Hex.Decode("828302028205008302006377766F")), ciri, _testDictionary);
            Assert.AreEqual(2, body.Length);
            Assert.AreEqual("http://www.iana.org/assignments/relation/collection", ((CoralLink)body[0]).RelationType);
            Assert.AreEqual("http://www.w3.org/1999/02/22-rdf-syntax-ns#type", ((CoralLink)body[1]).RelationType);
        }

        [TestMethod]
        public void TestEncode()
        {
            CoralBody body = new CoralBody {
                new CoralLink("http://coreapps.org/collections#create", "yes"), 
                new CoralLink("http://coreapps.org/coap#type", CBORObject.False)
            };
            CBORObject obj = body.EncodeToCBORObject(null);
            Assert.AreEqual("[[2, 3, \"yes\"], [2, 8, false]]", obj.ToString());
        }
    }
}
