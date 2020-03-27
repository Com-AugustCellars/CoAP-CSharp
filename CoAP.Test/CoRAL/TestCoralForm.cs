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
            {10, new Cori("http://coreapps.org/coap#method")},
            {11, new Cori("coap://host/form3")},
        };

        [TestMethod]
        public void TestConstructors()
        {
            CoralForm form = new CoralForm("http://apps.augustcellar.com/formRef", new Cori("coap://host:99/form"));
            Assert.AreEqual("http://apps.augustcellar.com/formRef", form.OperationTypeText);
            Assert.AreEqual("coap://host:99/form", form.Target.ToString());
            Assert.IsNull(form.TargetInt);
            Assert.AreEqual(0, form.FormFields.Count);

            form = new CoralForm(CBORObject.DecodeFromBytes(Hex.Decode("8303880164687474700276617070732E61756775737463656C6C6172732E636F6D0418500667666F726D5265668405000664666F726D")), new Cori("coap://host:99/form2"), testDictionary);
            Assert.AreEqual("http://apps.augustcellars.com/formRef", form.OperationTypeText);
            Assert.IsNull(form.OperationTypeInt);
            Assert.AreEqual("[1, \"coap\", 2, \"host\", 4, 99, 6, \"form\"]", form.Target.Data.ToString());
            Assert.IsNull(form.TargetInt);
            Assert.AreEqual(0, form.FormFields.Count);

            form = new CoralForm(CBORObject.DecodeFromBytes(Hex.Decode("8403068405000664666F726D88880164687474700276617070732E61756775737463656C6C6172732E636F6D04185006666669656C6431F5880164687474700276617070732E61756775737463656C6C6172732E636F6D04185006666669656C64328405000665666F726D33880164687474700276617070732E61756775737463656C6C6172732E636F6D04185006666669656C64330502DA0001869F04")),
                new Cori("coap://host:99/form2"), testDictionary);
            Assert.AreEqual("http://coreapps.org/base#search", form.OperationTypeText);
            Assert.AreEqual(6, form.OperationTypeInt);
            Assert.AreEqual("[1, \"coap\", 2, \"host\", 4, 99, 6, \"form\"]", form.Target.Data.ToString());
            Assert.IsNull(form.TargetInt);
            Assert.AreEqual(4, form.FormFields.Count);

            Assert.AreEqual("http://apps.augustcellars.com/field1", form.FormFields[0].FieldTypeText);
            Assert.IsNull(form.FormFields[0].FieldTypeInt);
            Assert.AreEqual(CBORType.Boolean, form.FormFields[0].Literal.Type);
            Assert.IsTrue(form.FormFields[0].Literal.IsTrue);
            Assert.IsNull(form.FormFields[0].LiteralInt);

            Assert.AreEqual("http://apps.augustcellars.com/field2", form.FormFields[1].FieldTypeText);
            Assert.IsNull(form.FormFields[1].FieldTypeInt);
            Assert.IsNotNull(form.FormFields[1].Url);
            Assert.AreEqual("[1, \"coap\", 2, \"host\", 4, 99, 6, \"form3\"]", form.FormFields[1].Url.Data.ToString());
            Assert.IsNull(form.FormFields[1].LiteralInt);

            Assert.AreEqual("http://apps.augustcellars.com/field3", form.FormFields[2].FieldTypeText);
            Assert.IsNull(form.FormFields[2].FieldTypeInt);
            Assert.IsNotNull(form.FormFields[2].Literal);
            Assert.AreEqual(CBORType.Integer, form.FormFields[2].Literal.Type);
            Assert.AreEqual(5, form.FormFields[2].Literal.AsInt32());
            Assert.IsNull(form.FormFields[2].LiteralInt);

            Assert.AreEqual("http://www.iana.org/assignments/relation/collection", form.FormFields[3].FieldTypeText);
            Assert.IsNotNull(form.FormFields[3].FieldTypeInt);
            Assert.AreEqual("http://www.iana.org/assignments/relation/collection", form.FormFields[3].FieldTypeText);
            Assert.IsNotNull(form.FormFields[3].LiteralInt);
            Assert.AreEqual("http://coreapps.org/base#update", form.FormFields[3].Url.ToString());
        }

        [TestMethod]
        public void TestEncodeToCBOR()
        {
            Cori ciri1 = new Cori("coap://host");
            Cori ciri2 = new Cori("http://host");
            CoralForm form = new CoralForm("http://apps.augustcellars.com/formRef", new Cori("coap://host/form1"));
            CBORObject cbor = form.EncodeToCBORObject(ciri2, testDictionary);
            Assert.AreEqual(3, cbor[0].AsInt32());
            Assert.AreEqual(CBORType.Array, cbor[1].Type);
            Assert.AreEqual("[3, [1, \"http\", 2, \"apps.augustcellars.com\", 4, 80, 6, \"formRef\"], [1, \"coap\", 2, \"host\", 4, 5683, 6, \"form1\"]]", cbor.ToString());

            form = new CoralForm("http://coreapps.org/base#lang", new Cori("coap://host/form1"));
            cbor = form.EncodeToCBORObject(ciri1, testDictionary);
            Assert.AreEqual("[3, 9, [6, \"form1\"]]", cbor.ToString());

            form = new CoralForm("http://coreapps.org/base#lang", new Cori("coap://host/form3"));
            cbor = form.EncodeToCBORObject(ciri1, testDictionary);
            Assert.AreEqual("[3, 9, 11]", cbor.ToString());

            form.FormFields.Add(new CoralFormField("http://apps.augustcellars.com/field1", CBORObject.True));
            form.FormFields.Add(new CoralFormField("http://apps.augustcellars.com/field2", CBORObject.FromObject(39)));
            form.FormFields.Add(new CoralFormField("http://apps.augustcellars.com/field3", new Cori("http://coreapps.org/base#update")));
            form.FormFields.Add(new CoralFormField("http://apps.augustcellars.com/field4", new Cori("coap://host3/form9")));
            form.FormFields.Add(new CoralFormField("http://apps.augustcellars.com/field6", new Cori("coap://host/form6")));
            form.FormFields.Add(new CoralFormField("http://coreapps.org/coap#type", new Cori("coap://host/form3")));

            cbor = form.EncodeToCBORObject(ciri1, testDictionary);
            Assert.AreEqual("[3, 9, 11, [[1, \"http\", 2, \"apps.augustcellars.com\", 4, 80, 6, \"field1\"], true, [1, \"http\", 2, \"apps.augustcellars.com\", 4, 80, 6, \"field2\"], 39, [1, \"http\", 2, \"apps.augustcellars.com\", 4, 80, 6, \"field3\"], 99999(4), [1, \"http\", 2, \"apps.augustcellars.com\", 4, 80, 6, \"field4\"], [2, \"host3\", 4, 5683, 6, \"form9\"], [1, \"http\", 2, \"apps.augustcellars.com\", 4, 80, 6, \"field6\"], [6, \"form6\"], 8, 99999(11)]]", cbor.ToString());
        }

    }
}
