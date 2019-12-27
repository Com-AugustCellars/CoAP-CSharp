using System;
using System.Collections.Generic;
using System.Text;
using Com.AugustCellars.CoAP.Coral;
using Com.AugustCellars.CoAP.Server;
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
            Cori cori = new Cori("coap://host:99");
            Assert.ThrowsException<ArgumentException>(() => new CoralBody(CBORObject.DecodeFromBytes(Hex.Decode("01")), cori, null));
            Assert.ThrowsException<ArgumentException>(() => new CoralBody(CBORObject.DecodeFromBytes(Hex.Decode("830202820500")), cori, null));

            CoralBody body = new CoralBody(CBORObject.DecodeFromBytes(Hex.Decode("81830202820500")), cori, _testDictionary);
            Assert.AreEqual(1, body.Length);
            Assert.AreEqual("http://www.iana.org/assignments/relation/collection", ((CoralLink) body[0]).RelationType);

            body = new CoralBody(CBORObject.DecodeFromBytes(Hex.Decode("828302028205008302006377766F")), cori, _testDictionary);
            Assert.AreEqual(2, body.Length);
            Assert.AreEqual("http://www.iana.org/assignments/relation/collection", ((CoralLink)body[0]).RelationType);
            Assert.AreEqual("http://www.w3.org/1999/02/22-rdf-syntax-ns#type", ((CoralLink)body[1]).RelationType);

            // [
            //   [2, "rel1", [6, "target1"], [
            //     [2, "rel2", [5, 2, 6, "target2"]],
            //     [1, [1, "coap", 2, "host3", 4, 5683, 6, "link1"]],
            //     [2, "rel2", [5, 2, 6, "target2"]]
            //   ]],
            //   [2, "rel3", [6, "target2"]],
            //   [1, [1, "http", 2, "host", 4, 80, 6, "link2", 6, "link3"]],
            //   [2, "rel4", [5, 2, 6, "target2"]],
            //   [3, "op-type", [5, 2, 6, "form"]]
            // ]
            body = new CoralBody(CBORObject.DecodeFromBytes(Hex.Decode("8584026472656C31820667746172676574318383026472656C328405020667746172676574328201880164636F61700265686F7374330419163306656C696E6B3183026472656C3284050206677461726765743283026472656C338206677461726765743282018A0164687474700264686F737404185006656C696E6B3206656C696E6B3383026472656C348405020667746172676574328303676F702D747970658405020664666F726D")), cori, _testDictionary);
            Assert.AreEqual(5, body.Length);

            Assert.IsTrue(body[0] is CoralLink);
            Assert.IsTrue(body[1] is CoralLink);
            Assert.IsTrue(body[2] is CoralBaseDirective);
            Assert.IsTrue(body[3] is CoralLink);
            Assert.IsTrue(body[4] is CoralForm);

            CoralLink link = (CoralLink) body[0];
            Assert.AreEqual("coap://host:99/target1", link.Target.ToString());
            Assert.AreEqual(3, link.Body.Length);

            Assert.IsTrue(link.Body[0] is CoralLink);
            Assert.IsTrue(link.Body[1] is CoralBaseDirective);
            Assert.IsTrue(link.Body[2] is CoralLink);

            CoralLink link2 = (CoralLink) link.Body[0];
            Assert.AreEqual("coap://host:99/target1/target2", link2.Target.ToString());
            link2 = (CoralLink) link.Body[2];
            Assert.AreEqual("coap://host3/link1/target2", link2.Target.ToString());

            link = (CoralLink) body[1];
            Assert.AreEqual("coap://host:99/target2", link.Target.ToString());

            link = (CoralLink) body[3];
            Assert.AreEqual("http://host/link2/link3/target2", link.Target.ToString());

            Assert.AreEqual("http://host/link2/link3/form", ((CoralForm) body[4]).Target.ToString());
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

            //

            CoralBody body1 = new CoralBody() {
                new CoralLink("link1", new Cori( "coap://host/path1/path2/path3")),
                new CoralBaseDirective(new Cori("coaps://host/path1")),
                new CoralLink("link1", new Cori( "coap://host/path1/path2/path3")),
                new CoralLink("link2", new Cori("coaps://host/path1/path2/path3"))
            };

            CoralForm form = new CoralForm("form1", new Cori("coap://host/path1/path4"));
            form.FormFields.Add(new CoralFormField("field1", new Cori("coap://host/path2")));
            form.FormFields.Add(new CoralFormField("field2", new Cori( "coaps://host/path2")));

            body = new CoralBody() {
                new CoralLink("link1", new Cori("coap://host/path"), body1),
                new CoralBaseDirective(new Cori("http://host")),
                new CoralLink("link1", new Cori("coap://host/path"), body1),
                new CoralLink("link2", new Cori("http://host/path1"), body1),
            };

            obj = body.EncodeToCBORObject(new Cori("coap://host"), _testDictionary);
            Assert.AreEqual("[[2, \"link1\", [6, \"path\"], [[2, \"link1\", [6, \"path1\", 6, \"path2\", 6, \"path3\"]], [1, [1, \"coaps\", 2, \"host\", 4, 5684, 6, \"path1\"]], [2, \"link1\", [1, \"coap\", 2, \"host\", 4, 5683, 6, \"path1\", 6, \"path2\", 6, \"path3\"]], [2, \"link2\", [5, 2, 6, \"path2\", 6, \"path3\"]]]], [1, [1, \"http\", 2, \"host\", 4, 80]], [2, \"link1\", [1, \"coap\", 2, \"host\", 4, 5683, 6, \"path\"], [[2, \"link1\", [1, \"coap\", 2, \"host\", 4, 5683, 6, \"path1\", 6, \"path2\", 6, \"path3\"]], [1, [1, \"coaps\", 2, \"host\", 4, 5684, 6, \"path1\"]], [2, \"link1\", [1, \"coap\", 2, \"host\", 4, 5683, 6, \"path1\", 6, \"path2\", 6, \"path3\"]], [2, \"link2\", [5, 2, 6, \"path2\", 6, \"path3\"]]]], [2, \"link2\", [6, \"path1\"], [[2, \"link1\", [1, \"coap\", 2, \"host\", 4, 5683, 6, \"path1\", 6, \"path2\", 6, \"path3\"]], [1, [1, \"coaps\", 2, \"host\", 4, 5684, 6, \"path1\"]], [2, \"link1\", [1, \"coap\", 2, \"host\", 4, 5683, 6, \"path1\", 6, \"path2\", 6, \"path3\"]], [2, \"link2\", [5, 2, 6, \"path2\", 6, \"path3\"]]]]]", obj.ToString());

        }
    }
}
