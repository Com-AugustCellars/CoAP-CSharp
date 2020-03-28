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
        public void TestConstructors()
        {
            Cori cori = new Cori("coap://host:99");
            Assert.ThrowsException<ArgumentException>(() => new CoralBody(CBORObject.DecodeFromBytes(Hex.Decode("01")), cori, null));
            Assert.ThrowsException<ArgumentException>(() => new CoralBody(CBORObject.DecodeFromBytes(Hex.Decode("830202820500")), cori, null));

            CoralBody body = new CoralBody(CBORObject.DecodeFromBytes(Hex.Decode("81830202820500")), cori, _testDictionary);
            Assert.AreEqual(1, body.Length);
            Assert.AreEqual("http://www.iana.org/assignments/relation/collection", ((CoralLink) body[0]).RelationTypeText);

            body = new CoralBody(CBORObject.DecodeFromBytes(Hex.Decode("828302028205008302006377766F")), cori, _testDictionary);
            Assert.AreEqual(2, body.Length);
            Assert.AreEqual("http://www.iana.org/assignments/relation/collection", ((CoralLink)body[0]).RelationTypeText);
            Assert.AreEqual("http://www.w3.org/1999/02/22-rdf-syntax-ns#type", ((CoralLink)body[1]).RelationTypeText);
            
            // [
            // [2
            // [0, "http", 1, "apps.augustcellars.com", 5, "rel1"], 
            // [5, "target1"], 
            // [
            // [2,
            // [0, "http", 1, "apps.augustcellars.com", 5, "rel2"],
            // [4, 1, 5, "target2"]], [1, [0, "coap", 1, "host3", 5, "link1"]], [2, [0, "http", 1, "apps.augustcellars.com", 5, "rel2"], [4, 1, 5, "target2"]]]], [2, [0, "http", 1, "apps.augustcellars.com", 5, "rel3"], [5, "target2"]], [1, [0, "http", 1, "host", 5, "link2", 5, "link3"]], [2, [0, "http", 1, "apps.augustcellars.com", 5, "rel4"], [4, 1, 5, "target2"]], [3, [0, "http", 1, "apps.augustcellars.com", 5, "op-type"], [4, 1, 5, "form"]]]

            body = new CoralBody(CBORObject.DecodeFromBytes(Hex.Decode("858402860064687474700176617070732E61756775737463656C6C6172732E636F6D056472656C3182056774617267657431838302860064687474700176617070732E61756775737463656C6C6172732E636F6D056472656C328404010567746172676574328201860064636F61700165686F73743305656C696E6B318302860064687474700176617070732E61756775737463656C6C6172732E636F6D056472656C328404010567746172676574328302860064687474700176617070732E61756775737463656C6C6172732E636F6D056472656C33820567746172676574328201880064687474700164686F737405656C696E6B3205656C696E6B338302860064687474700176617070732E61756775737463656C6C6172732E636F6D056472656C348404010567746172676574328303860064687474700176617070732E61756775737463656C6C6172732E636F6D05676F702D747970658404010564666F726D")), cori, _testDictionary);
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
                new CoralLink("http://apps.augustcellars.com/link1", new Cori( "coap://host/path1/path2/path3")),
                new CoralBaseDirective(new Cori("coaps://host/path1")),
                new CoralLink("http://apps.augustcellars.com/link1", new Cori( "coap://host/path1/path2/path3")),
                new CoralLink("http://apps.augustcellars.com/link2", new Cori("coaps://host/path1/path2/path3"))
            };

            CoralForm form = new CoralForm("http://apps.augustcellars.com/form1", new Cori("coap://host/path1/path4"));
            form.FormFields.Add(new CoralFormField("http://apps.augustcellars.com/field1", new Cori("coap://host/path2")));
            form.FormFields.Add(new CoralFormField("http://apps.augustcellars.com/field2", new Cori( "coaps://host/path2")));

            body = new CoralBody() {
                new CoralLink("http://apps.augustcellars.com/link1", new Cori("coap://host/path"), body1),
                new CoralBaseDirective(new Cori("http://host")),
                new CoralLink("http://apps.augustcellars.com/link1", new Cori("coap://host/path"), body1),
                new CoralLink("http://apps.augustcellars.com/link2", new Cori("http://host/path1"), body1),
            };

            obj = body.EncodeToCBORObject(new Cori("coap://host"), _testDictionary);
            Assert.AreEqual("[[2, [0, \"http\", 1, \"apps.augustcellars.com\", 5, \"link1\"], [5, \"path\"], [[2, [0, \"http\", 1, \"apps.augustcellars.com\", 5, \"link1\"], [5, \"path1\", 5, \"path2\", 5, \"path3\"]], [1, [0, \"coaps\", 1, \"host\", 5, \"path1\"]], [2, [0, \"http\", 1, \"apps.augustcellars.com\", 5, \"link1\"], [0, \"coap\", 1, \"host\", 5, \"path1\", 5, \"path2\", 5, \"path3\"]], [2, [0, \"http\", 1, \"apps.augustcellars.com\", 5, \"link2\"], [4, 2, 5, \"path2\", 5, \"path3\"]]]], [1, [0, \"http\", 1, \"host\"]], [2, [0, \"http\", 1, \"apps.augustcellars.com\", 5, \"link1\"], [0, \"coap\", 1, \"host\", 5, \"path\"], [[2, [0, \"http\", 1, \"apps.augustcellars.com\", 5, \"link1\"], [0, \"coap\", 1, \"host\", 5, \"path1\", 5, \"path2\", 5, \"path3\"]], [1, [0, \"coaps\", 1, \"host\", 5, \"path1\"]], [2, [0, \"http\", 1, \"apps.augustcellars.com\", 5, \"link1\"], [0, \"coap\", 1, \"host\", 5, \"path1\", 5, \"path2\", 5, \"path3\"]], [2, [0, \"http\", 1, \"apps.augustcellars.com\", 5, \"link2\"], [4, 2, 5, \"path2\", 5, \"path3\"]]]], [2, [0, \"http\", 1, \"apps.augustcellars.com\", 5, \"link2\"], [5, \"path1\"], [[2, [0, \"http\", 1, \"apps.augustcellars.com\", 5, \"link1\"], [0, \"coap\", 1, \"host\", 5, \"path1\", 5, \"path2\", 5, \"path3\"]], [1, [0, \"coaps\", 1, \"host\", 5, \"path1\"]], [2, [0, \"http\", 1, \"apps.augustcellars.com\", 5, \"link1\"], [0, \"coap\", 1, \"host\", 5, \"path1\", 5, \"path2\", 5, \"path3\"]], [2, [0, \"http\", 1, \"apps.augustcellars.com\", 5, \"link2\"], [4, 2, 5, \"path2\", 5, \"path3\"]]]]]", obj.ToString());

        }
    }
}
