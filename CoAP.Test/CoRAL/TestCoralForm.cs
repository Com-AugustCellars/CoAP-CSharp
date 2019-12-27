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
    public class TestCoralForm
    {
        private static readonly CoralDictionary testDictionary = new CoralDictionary() {
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
            {10, "http://coreapps.org/coap#method"},
            {11, new Cori("coap://host/form3")},
        };

        [TestMethod]
        public void TestConstructors()
        {
            CoralForm form = new CoralForm("formRef", new Cori("coap://host:99/form"));
            Assert.AreEqual("formRef", form.OperationType);
            Assert.AreEqual("coap://host:99/form", form.Target.ToString());
            Assert.IsNull(form.TargetInt);
            Assert.AreEqual(0, form.FormFields.Count);

            form = new CoralForm(CBORObject.DecodeFromBytes(Hex.Decode("830367666F726D5265668405000664666F726D")), new Cori("coap://host:99/form2"), testDictionary);
            Assert.AreEqual("formRef", form.OperationType);
            Assert.IsNull(form.OperationTypeInt);
            Assert.AreEqual("[1, \"coap\", 2, \"host\", 4, 99, 6, \"form\"]", form.Target.Data.ToString());
            Assert.IsNull(form.TargetInt);
            Assert.AreEqual(0, form.FormFields.Count);

            form = new CoralForm(CBORObject.DecodeFromBytes(Hex.Decode("8403068405000664666F726D88666669656C6431F5666669656C64328405000665666F726D33666669656C64330502DA0001869F04")),
                new Cori("coap://host:99/form2"), testDictionary);
            Assert.AreEqual("http://coreapps.org/base#search", form.OperationType);
            Assert.AreEqual(6, form.OperationTypeInt);
            Assert.AreEqual("[1, \"coap\", 2, \"host\", 4, 99, 6, \"form\"]", form.Target.Data.ToString());
            Assert.IsNull(form.TargetInt);
            Assert.AreEqual(4, form.FormFields.Count);

            Assert.AreEqual("field1", form.FormFields[0].FieldType);
            Assert.IsNull(form.FormFields[0].FieldTypeInt);
            Assert.AreEqual(CBORType.Boolean, form.FormFields[0].Literal.Type);
            Assert.IsTrue(form.FormFields[0].Literal.IsTrue);
            Assert.IsNull(form.FormFields[0].LiteralInt);

            Assert.AreEqual("field2", form.FormFields[1].FieldType);
            Assert.IsNull(form.FormFields[1].FieldTypeInt);
            Assert.IsNotNull(form.FormFields[1].Url);
            Assert.AreEqual("[1, \"coap\", 2, \"host\", 4, 99, 6, \"form3\"]", form.FormFields[1].Url.Data.ToString());
            Assert.IsNull(form.FormFields[1].LiteralInt);

            Assert.AreEqual("field3", form.FormFields[2].FieldType);
            Assert.IsNull(form.FormFields[2].FieldTypeInt);
            Assert.IsNotNull(form.FormFields[2].Literal);
            Assert.AreEqual(CBORType.Integer, form.FormFields[2].Literal.Type);
            Assert.AreEqual(5, form.FormFields[2].Literal.AsInt32());
            Assert.IsNull(form.FormFields[2].LiteralInt);

            Assert.AreEqual("http://www.iana.org/assignments/relation/collection", form.FormFields[3].FieldType);
            Assert.IsNotNull(form.FormFields[3].FieldTypeInt);
            Assert.AreEqual(CBORType.TextString, form.FormFields[3].Literal.Type);
            Assert.AreEqual("http://coreapps.org/base#update", form.FormFields[3].Literal.AsString());
            Assert.IsNotNull(form.FormFields[3].LiteralInt);
        }

        [TestMethod]
        public void TestEncodeToCBOR()
        {
            Cori ciri1 = new Cori("coap://host");
            Cori ciri2 = new Cori("http://host");
            CoralForm form = new CoralForm("formRef", new Cori("coap://host/form1"));
            CBORObject cbor = form.EncodeToCBORObject(ciri2, testDictionary);
            Assert.AreEqual(3, cbor[0].AsInt32());
            Assert.AreEqual("formRef", cbor[1].AsString());
            Assert.AreEqual("[3, \"formRef\", [1, \"coap\", 2, \"host\", 4, 5683, 6, \"form1\"]]", cbor.ToString());

            form = new CoralForm("http://coreapps.org/base#lang", new Cori("coap://host/form1"));
            cbor = form.EncodeToCBORObject(ciri1, testDictionary);
            Assert.AreEqual("[3, 9, [6, \"form1\"]]", cbor.ToString());

            form = new CoralForm("http://coreapps.org/base#lang", new Cori("coap://host/form3"));
            cbor = form.EncodeToCBORObject(ciri1, testDictionary);
            Assert.AreEqual("[3, 9, 11]", cbor.ToString());

            form.FormFields.Add(new CoralFormField("field1", CBORObject.True));
            form.FormFields.Add(new CoralFormField("field2", CBORObject.FromObject(39)));
            form.FormFields.Add(new CoralFormField("field3", CBORObject.FromObject("http://coreapps.org/base#update")));
            form.FormFields.Add(new CoralFormField("field4", new Cori("coap://host3/form9")));
            form.FormFields.Add(new CoralFormField("field6", new Cori("coap://host/form6")));
            form.FormFields.Add(new CoralFormField("http://coreapps.org/coap#type", new Cori("coap://host/form3")));

            cbor = form.EncodeToCBORObject(ciri1, testDictionary);
            Assert.AreEqual("[3, 9, 11, [\"field1\", true, \"field2\", 39, \"field3\", 99999(4), \"field4\", [2, \"host3\", 4, 5683, 6, \"form9\"], \"field6\", [6, \"form6\"], 8, 99999(11)]]", cbor.ToString());
        }

    }
}
