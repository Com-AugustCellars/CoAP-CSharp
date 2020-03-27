using System;
using System.Text;
using Com.AugustCellars.CoAP.Coral;
using Com.AugustCellars.CoAP.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.BouncyCastle.Utilities.Encoders;
using PeterO.Cbor;

namespace CoAP.Test.Std10.CoRAL
{
    [TestClass]
    public class TestCoralLink
    {
        private static readonly CoralDictionary testDictionary = new CoralDictionary() {
            {0, new Cori("http://www.w3.org/1999/02/22-rdf-syntax-ns#type")},
            {1, new Cori("http://www.iana.org/assignments/relation/item>")},
            {2, new Cori("http://www.iana.org/assignments/relation/collection")},
            {3, new Cori("http://coreapps.org/collections#create")},
            {4, new Cori("http://coreapps.org/base#update")},
            {5, new Cori("http://coreapps.org/collections#delete")},
            {6, new Cori("http://coreapps.org/base#search")},
            {7, new Cori("http://coreapps.org/coap#accept")},
            {8, new Cori("http://coreapps.org/coap#type")},
            {9, new Cori("http://coreapps.org/base#lang")},
            {10, new Cori("http://coreapps.org/coap#method")}
        };

        [TestMethod]
        public void OneLink()
        {
            CoralLink link = new CoralLink("http://coreapps.org/reef#rd-unit", "/sensors");

            StringBuilder sb = new StringBuilder();
            link.BuildString(sb, "", null, null);

            Assert.AreEqual("<http://coreapps.org/reef#rd-unit> \"/sensors\"\n", sb.ToString());

            CoralDictionary dictionary = new CoralDictionary();
            
            CBORObject obj = link.EncodeToCBORObject(null, dictionary);
            Assert.AreEqual("[2, [1, \"http\", 2, \"coreapps.org\", 4, 80, 6, \"reef\", 8, \"rd-unit\"], \"/sensors\"]", obj.ToString());

            dictionary.Add(99, new Cori("http://coreapps.org/reef#rd-unit"));
            obj = link.EncodeToCBORObject(null, dictionary);
            Assert.AreEqual("[2, 99, \"/sensors\"]", obj.ToString());

            dictionary.Add(98, "/sensors");
            obj = link.EncodeToCBORObject(null, dictionary);
            Assert.AreEqual("[2, 99, 99999(98)]", obj.ToString());
        }

        [TestMethod]
        public void ParseFromCBOR()
        {
            Cori cori = new Cori("coap://host:99");

            // [2, "relation", "value"]
            CoralLink link = new CoralLink(CBORObject.DecodeFromBytes(Hex.Decode("83028A016468747470026C636F7265617070732E6F7267041850066472656566086772642D756E69746576616C7565")), cori, testDictionary);
            Assert.AreEqual("http://coreapps.org/reef#rd-unit", link.RelationTypeText);
            Assert.AreEqual(null, link.RelationTypeInt);
            Assert.AreEqual("value", link.Value.AsString());
            Assert.AreEqual(null, link.Body);

            // [2, 2, 4]
            link = new CoralLink(CBORObject.DecodeFromBytes(Hex.Decode("830202DA0001869F04")), cori, testDictionary);
            Assert.AreEqual("http://www.iana.org/assignments/relation/collection", link.RelationTypeText);
            Assert.AreEqual(2, link.RelationTypeInt);
            Assert.AreEqual("http://coreapps.org/base#update", link.Target.ToString());
            Assert.AreEqual(4, link.TargetInt);
            Assert.AreEqual(null, link.Body);

            // [2, 99, false]
            link = new CoralLink(CBORObject.DecodeFromBytes(Hex.Decode("83021863F4")), cori, testDictionary);
            Assert.AreEqual(null, link.RelationType);
            Assert.AreEqual(99, link.RelationTypeInt);
            Assert.AreEqual(null, link.Body);

            // [2, 2, 1(999)]   Date
            link = new CoralLink(CBORObject.DecodeFromBytes(Hex.Decode("830202C11903E7")), cori, testDictionary);
            Assert.IsTrue(link.Value.IsTagged);
            Assert.AreEqual(1, link.Value.TagCount);
            Assert.AreEqual(1, link.Value.MostInnerTag);
            Assert.AreEqual(999, link.Value.Untag().AsInt32());
            Assert.AreEqual(null, link.Body);

            // [1, 2, 4]
            Assert.ThrowsException<ArgumentException>(() => { link = new CoralLink(CBORObject.DecodeFromBytes(Hex.Decode("83010204")), cori, testDictionary); }, "Not an encoded CoRAL link");

            // [2, true, false]
            Assert.ThrowsException<ArgumentException>(() => { link = new CoralLink(CBORObject.DecodeFromBytes(Hex.Decode("8302F5F4")), cori, testDictionary); }, "Invalid relation in CoRAL link");

            // [2, 2, false, [2, 3, true]]
            link = new CoralLink(CBORObject.DecodeFromBytes(Hex.Decode("840202F481830203F5")), cori, testDictionary);
            Assert.IsNotNull(link.Body);
            Assert.AreEqual(1, link.Body.Length);

            // [2, 2, 5]
            link = new CoralLink(CBORObject.DecodeFromBytes(Hex.Decode("83020205")), cori, testDictionary);
            Assert.IsNotNull(link.Value.Type);
            Assert.AreEqual(CBORType.Integer, link.Value.Type);
            Assert.AreEqual(5, link.Value.AsInt32());

            // [ 2, 2, [5, 0]]
            link = new CoralLink(CBORObject.DecodeFromBytes(Hex.Decode("830202820500")), cori, testDictionary);
            Assert.IsNotNull(link.Target);
            Assert.AreEqual(cori.Data.ToString(), link.Target.Data.ToString());

            // [ 2, 2, [5, 0, 6, "path2"], [[ 2, 2, [5, 0, 6, "path3"], [5, 2, 6, "path4"]]]]
            link = new CoralLink(CBORObject.DecodeFromBytes(Hex.Decode("84020284050006657061746832828302028405000665706174683383020284050206657061746834")), cori, testDictionary);
            Assert.IsNotNull(link.Target);
            Assert.AreEqual("[1, \"coap\", 2, \"host\", 4, 99, 6, \"path2\"]", link.Target.Data.ToString());
            CoralLink l2 = (CoralLink) link.Body[0];
            Assert.AreEqual("[1, \"coap\", 2, \"host\", 4, 99, 6, \"path3\"]", l2.Target.Data.ToString());
            l2 = (CoralLink) link.Body[1];
            Assert.AreEqual("[1, \"coap\", 2, \"host\", 4, 99, 6, \"path2\", 6, \"path4\"]", l2.Target.Data.ToString());
        }

        [TestMethod]
        public void Constructors()
        {
            CoralLink link;

            link = new CoralLink("http://test.augustcellars.com/relation", CBORObject.FromObject(1));
            Assert.AreEqual(link.RelationTypeText, "http://test.augustcellars.com/relation");
            Assert.AreEqual(1, link.Value.AsInt32());

            link = new CoralLink("http://test.augustcellars.com/relation", "value");
            Assert.AreEqual("http://test.augustcellars.com/relation", link.RelationTypeText);
            Assert.AreEqual(CBORType.TextString, link.Value.Type);
            Assert.AreEqual("value", link.Value.AsString());

            link = new CoralLink("http://test.augustcellars.com/relation", new Cori("coap://host:5/root/path"));
            Assert.AreEqual("http://test.augustcellars.com/relation", link.RelationTypeText);
            Assert.IsNull(link.Value);
            Assert.AreEqual("coap://host:5/root/path", link.Target.ToString());


            Assert.ThrowsException<ArgumentException>(() => link = new CoralLink("relation", CBORObject.FromObject(DateTime.UtcNow)));
            Assert.ThrowsException<ArgumentException>(() => link = new CoralLink("http://test.augustcellars.com/relation", CBORObject.FromObject(DateTime.UtcNow)));
            Assert.ThrowsException<ArgumentException>(() => link = new CoralLink("http://test.augustcellars.com/relation", CBORObject.NewArray()));
            Assert.ThrowsException<ArgumentException>(() => link = new CoralLink("http://test.augustcellars.com/relation", CBORObject.NewMap()));
            Assert.ThrowsException<ArgumentException>(() => link = new CoralLink("http://test.augustcellars.com/relation", CBORObject.FromSimpleValue(99)));
            // Assert.ThrowsException<ArgumentException>(() => link = new CoralLink("relation", new Cori(CBORObject.DecodeFromBytes(Hex.Decode("8102")))));
        }

        [TestMethod]
        public void TestEncode()
        {
            CoralLink link = new CoralLink("http://test.augustcellars.com/relation", "value");
            CBORObject cbor = link.EncodeToCBORObject(null, testDictionary);
            Assert.AreEqual(CBORType.Array, cbor.Type);
            Assert.AreEqual(3, cbor.Values.Count);
            Assert.AreEqual(2, cbor[0].AsInt32());
            Assert.AreEqual(CBORType.Array, cbor[1].Type);
            Assert.AreEqual(CBORType.TextString, cbor[2].Type);
            Assert.AreEqual("[2, [1, \"http\", 2, \"test.augustcellars.com\", 4, 80, 6, \"relation\"], \"value\"]", cbor.ToString());

            link = new CoralLink("http://www.w3.org/1999/02/22-rdf-syntax-ns#type", new Cori("http://www.iana.org/assignments/relation/collection"));
            cbor = link.EncodeToCBORObject(null, testDictionary);
            Assert.AreEqual(CBORType.Array, cbor.Type);
            Assert.AreEqual(3, cbor.Values.Count);
            Assert.AreEqual(2, cbor[0].AsInt32());
            Assert.AreEqual(CBORType.Integer, cbor[1].Type);
            Assert.AreEqual(0, cbor[1].AsInt32());
            Assert.AreEqual(CBORType.Integer, cbor[2].Type);
            Assert.IsTrue(cbor[2].IsTagged);
            Assert.IsTrue(cbor[2].HasOneTag(CoralDictionary.DictionaryTag));
            Assert.AreEqual(2, cbor[2].Untag().  AsInt32());
            Assert.AreEqual("[2, 0, 99999(2)]", cbor.ToString());

            link = new CoralLink("http://test.augustcellars.com/relation", CBORObject.FromObject(15));
            cbor = link.EncodeToCBORObject(null, testDictionary);
            Assert.AreEqual(CBORType.Array, cbor.Type);
            Assert.AreEqual(3, cbor.Values.Count);
            Assert.IsFalse(cbor[2].IsTagged);
            Assert.AreEqual(15, cbor[2].AsInt32());
            Assert.AreEqual("[2, [1, \"http\", 2, \"test.augustcellars.com\", 4, 80, 6, \"relation\"], 15]", cbor.ToString());

            link = new CoralLink("http://www.w3.org/1999/02/22-rdf-syntax-ns#type", new Cori(CBORObject.DecodeFromBytes(Hex.Decode("820501"))));
            cbor = link.EncodeToCBORObject(null, testDictionary);
            Assert.AreEqual("[2, 0, [5, 1]]", cbor.ToString());

            link = new CoralLink("http://test.augustcellars.com/relation", CBORObject.False);
            cbor = link.EncodeToCBORObject(null, testDictionary);
            Assert.IsTrue(cbor[2].IsFalse);
            Assert.AreEqual("[2, [1, \"http\", 2, \"test.augustcellars.com\", 4, 80, 6, \"relation\"], false]", cbor.ToString());
        }
    }
}
