using System;
using System.Collections.Generic;
using System.Text;
using Com.AugustCellars.CoAP.Coral;
using Com.AugustCellars.CoAP.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PeterO.Cbor;

namespace CoAP.Test.Std10.CoRAL
{
    [TestClass]
    public class TestBaseDirective
    {
        [TestMethod]
        public void Encoders()
        {
            CoralBaseDirective directive = new CoralBaseDirective(new Cori("coap://host.example/random"));

            CBORObject o = directive.EncodeToCBORObject(null, null);
            Assert.AreEqual("[1, [1, \"coap\", 2, \"host.example\", 4, 5683, 6, \"random\"]]", o.ToString());

            o = directive.EncodeToCBORObject(new Cori("coap://host.example"), null);
            Assert.AreEqual("[1, [6, \"random\"]]", o.ToString());

            StringBuilder s = new StringBuilder();
            directive.BuildString(s, "", null, null);
            Assert.AreEqual("#base <coap://host.example/random>\n", s.ToString());

            s = new StringBuilder();
            directive.BuildString(s, "", new Cori("coap://host.example"), null);
            Assert.AreEqual("#base <random>\n", s.ToString());

        }
    }
}
