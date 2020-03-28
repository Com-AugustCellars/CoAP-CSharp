using System;
using Com.AugustCellars.CoAP.Coral;
using Com.AugustCellars.CoAP.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PeterO.Cbor;

namespace CoAP.Test.Std10.CoRAL
{
    [TestClass]
    public class TestCoralDocument
    {
        public static CoralDictionary _Dictionary = new CoralDictionary() {
            {1, new Cori("coap://jimsch.example.com/coreapp/reef#content-type")},
            {2, new Cori("coap://jimsch.example.com/coreapp/reef#security")},
            {3, new Cori("coap://jimsch.example.com/coreapp/reef#authority-type")},
            {4, new Cori("coap://jimsch.example.com/coreapp/reef#authority")},
            {5, new Cori("coap://jimsch.example.com/coreapp/reef#rd-register")},
            {6, new Cori("coap://jimsch.example.com/coreapp/reef#rd-endpointSearch")},
            {7, new Cori("coap://jimsch.example.com/coreapp/reef#rd-resourceSearch")},
        };

        [TestMethod]
        public void ExampleTest()
        {
            CoralDocument document = new CoralDocument();

            CoralBody body = new CoralBody();
            body.Add(new CoralLink("coap://jimsch.example.com/coreapp/reef#content-type", CBORObject.FromObject(99599)));
            body.Add(new CoralLink("coap://jimsch.example.com/coreapp/reef#security", "OSCORE"));
            body.Add(new CoralLink("coap://jimsch.example.com/coreapp/reef#authority-type", "ACE"));
            body.Add(new CoralLink("coap://jimsch.example.com/coreapp/reef#authority", new Cori("coap://ace.example.org/token")));
            document.Add(new CoralLink("coap://jimsch.example.com/coreapp/reef#rd-register", new Cori("coap://jimsch.example.org/rd/endpoints"), body));

            document.Add(new CoralLink("coap://jimsch.example.com/coreapp/reef#rd-endpointSearch", new Cori("coap://jimsch.example.org/rd/endpoints")));
            document.Add(new CoralLink("coap://jimsch.example.com/coreapp/reef#rd-resourceSearch", new Cori("coap://jimsch.example.org/rd/resources")));

            document.Add(new CoralBaseDirective(new Cori("coaps://jimsch.example.org/rd")));

            body = new CoralBody();
            body.Add(new CoralLink("coap://jimsch.example.com/coreapp/reef#content-type", CBORObject.FromObject(99599)));
            body.Add(new CoralLink("coap://jimsch.example.com/coreapp/reef#security", "OSCORE"));
            body.Add(new CoralLink("coap://jimsch.example.com/coreapp/reef#authority-type", "ACE"));
            body.Add(new CoralLink("coap://jimsch.example.com/coreapp/reef#authority", new Cori("coaps://ace.example.org/token")));
            document.Add(new CoralLink("coap://jimsch.example.com/coreapp/reef#rd-register", new Cori("coaps://jimsch.example.org/rd/endpoints"), body));

            document.Add(new CoralLink("coap://jimsch.example.com/coreapp/reef#rd-endpointSearch", new Cori("coaps://jimsch.example.org/rd/endpoints")));
            document.Add(new CoralLink("coap://jimsch.example.com/coreapp/reef#rd-resourceSearch", new Cori("coaps://jimsch.example.org/rd/resources")));

            CBORObject result = document.EncodeToCBORObject(new Cori("coap://jimsch.example.org/rd"), _Dictionary);
            Assert.AreEqual("[[2, 5, [4, 2, 5, \"endpoints\"], [[2, 1, 99599], [2, 2, \"OSCORE\"], [2, 3, \"ACE\"], [2, 4, [1, \"ace.example.org\", 5, \"token\"]]]], [2, 6, [4, 2, 5, \"endpoints\"]], [2, 7, [4, 2, 5, \"resources\"]], [1, [0, \"coaps\", 1, \"jimsch.example.org\", 5, \"rd\"]], [2, 5, [4, 2, 5, \"endpoints\"], [[2, 1, 99599], [2, 2, \"OSCORE\"], [2, 3, \"ACE\"], [2, 4, [1, \"ace.example.org\", 5, \"token\"]]]], [2, 6, [4, 2, 5, \"endpoints\"]], [2, 7, [4, 2, 5, \"resources\"]]]", result.ToString());

            CoralDocument document2 = CoralDocument.DecodeFromBytes(document.EncodeToBytes(new Cori("coap://jimsch.example.org/rd"), _Dictionary), new Cori("coap://jimsch.example.org/rd"), _Dictionary);

            //Assert.AreEqual(document, document2);

            CoralUsing dict = new CoralUsing() {
                {"reef", "coap://jimsch.example.com/coreapp/reef#"}
            };

            string resultOut =
                "reef:rd-register <./endpoints> [\n  reef:content-type 99599\n  reef:security \"OSCORE\"\n  reef:authority-type \"ACE\"\n  reef:authority <//ace.example.org/token>\n]\nreef:rd-endpointSearch <./endpoints>\nreef:rd-resourceSearch <./resources>\n#base <coaps://jimsch.example.org/rd>\nreef:rd-register <./endpoints> [\n  reef:content-type 99599\n  reef:security \"OSCORE\"\n  reef:authority-type \"ACE\"\n  reef:authority <//ace.example.org/token>\n]\nreef:rd-endpointSearch <./endpoints>\nreef:rd-resourceSearch <./resources>\n";
            Assert.AreEqual(resultOut, document.EncodeToString(new Cori("coap://jimsch.example.org/rd"), dict));

            resultOut = "<coap://jimsch.example.com/coreapp/reef#rd-register> <coap://jimsch.example.org/rd/endpoints> [\n  <coap://jimsch.example.com/coreapp/reef#content-type> 99599\n  <coap://jimsch.example.com/coreapp/reef#security> \"OSCORE\"\n  <coap://jimsch.example.com/coreapp/reef#authority-type> \"ACE\"\n  <coap://jimsch.example.com/coreapp/reef#authority> <coap://ace.example.org/token>\n]\n<coap://jimsch.example.com/coreapp/reef#rd-endpointSearch> <coap://jimsch.example.org/rd/endpoints>\n<coap://jimsch.example.com/coreapp/reef#rd-resourceSearch> <coap://jimsch.example.org/rd/resources>\n#base <coaps://jimsch.example.org/rd>\n<coap://jimsch.example.com/coreapp/reef#rd-register> <./endpoints> [\n  <coap://jimsch.example.com/coreapp/reef#content-type> 99599\n  <coap://jimsch.example.com/coreapp/reef#security> \"OSCORE\"\n  <coap://jimsch.example.com/coreapp/reef#authority-type> \"ACE\"\n  <coap://jimsch.example.com/coreapp/reef#authority> <//ace.example.org/token>\n]\n<coap://jimsch.example.com/coreapp/reef#rd-endpointSearch> <./endpoints>\n<coap://jimsch.example.com/coreapp/reef#rd-resourceSearch> <./resources>\n";
            Assert.AreEqual(resultOut, document.ToString());
        }

        [TestMethod]
        public void Constructors()
        {

        }

    }
}
