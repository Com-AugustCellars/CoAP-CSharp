using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using Com.AugustCellars.CoAP.DTLS;
using Com.AugustCellars.COSE;
using NUnit.Framework.Internal;
using PeterO.Cbor;

namespace Com.AugustCellars.CoAP.DTLS
{
    [TestFixture]
    class DTLSClientEndPointTest
    {
        private static OneKey PskOneKey;
        private static OneKey RpkOneKey;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            RpkOneKey = OneKey.GenerateKey(AlgorithmValues.ECDSA_256, GeneralValues.KeyType_EC, "P-256");

            PskOneKey = new OneKey();
            PskOneKey.Add(CoseKeyKeys.KeyType, GeneralValues.KeyType_Octet);
            PskOneKey.Add(CoseKeyParameterKeys.Octet_k, CBORObject.FromObject(new byte[10]));
        }

        [Test]
        public void TestNoKey()
        {
            DTLSClientEndPoint ep = new DTLSClientEndPoint(null);
        }

        [Test]
        public void TestRpk()
        {
            DTLSClientEndPoint ep = new DTLSClientEndPoint(RpkOneKey);
        }

        [Test]
        public void NoEndPoint()
        {
            DTLSClientEndPoint ep=  new DTLSClientEndPoint(PskOneKey);
            Request req = new Request(Method.GET) {
                URI = new Uri("coaps://localhost:5682/.well-known/core"),
                EndPoint = ep
            };

            ep.Start();

            req.Send();
            req.WaitForResponse(5000);
        }

        [Test]
        public void CoapURL()
        {
            DTLSClientEndPoint ep = new DTLSClientEndPoint(PskOneKey);
            Request req = new Request(Method.GET) {
                URI = new Uri("coap://localhost/.well-known/core"),
                EndPoint = ep
            };

            ep.Start();

            Exception e = Assert.Throws<Exception>(() => req.Send());
            Assert.That(e.Message == "Schema is incorrect for the end point");
            req.WaitForResponse(5000);
        }
    }
}
