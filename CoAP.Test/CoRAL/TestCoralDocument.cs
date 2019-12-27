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
            {1, "coap://jimsch.example.com/coreapp/reef#content-type"},
            {2, "coap://jimsch.example.com/coreapp/reef#security"},
            {3, "coap://jimsch.example.com/coreapp/reef#authority-type"},
            {4, "coap://jimsch.example.com/coreapp/reef#authority"},
            {5, "coap://jimsch.example.com/coreapp/reef#rd-register"},
            {6, "coap://jimsch.example.com/coreapp/reef#rd-endpointSearch"},
            {7, "coap://jimsch.example.com/coreapp/reef#rd-resourceSearch"},
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
            Assert.AreEqual("[[2, 5, [5, 2, 6, \"endpoints\"], [[2, 1, 99599], [2, 2, \"OSCORE\"], [2, 3, \"ACE\"], [2, 4, [2, \"ace.example.org\", 4, 5683, 6, \"token\"]]]], [2, 6, [5, 2, 6, \"endpoints\"]], [2, 7, [5, 2, 6, \"resources\"]], [1, [1, \"coaps\", 2, \"jimsch.example.org\", 4, 5684, 6, \"rd\"]], [2, 5, [5, 2, 6, \"endpoints\"], [[2, 1, 99599], [2, 2, \"OSCORE\"], [2, 3, \"ACE\"], [2, 4, [2, \"ace.example.org\", 4, 5684, 6, \"token\"]]]], [2, 6, [5, 2, 6, \"endpoints\"]], [2, 7, [5, 2, 6, \"resources\"]]]", result.ToString());

            CoralDocument document2 = CoralDocument.DecodeFromBytes(result.EncodeToBytes(), new Cori("coap://jimsch.example.org/rd"), _Dictionary);

            //Assert.AreEqual(document, document2);
        }

        [TestMethod]
        public void Constructors()
        {

        }

    }
}
