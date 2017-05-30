using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using System.Threading.Tasks;

using NUnit.Framework;

using Com.AugustCellars.CoAP.DTLS;
using Com.AugustCellars.CoAP.Server;
using Com.AugustCellars.COSE;
using PeterO.Cbor;
using Com.AugustCellars.CoAP.Net;
using Com.AugustCellars.CoAP.Server.Resources;
using NUnit.Framework.Internal;

namespace Com.AugustCellars.CoAP.DTLS
{
    [TestFixture]
    class DTLSResourceTest
    {
        private static OneKey PskOne;
        private static OneKey PskTwo;
        private static KeySet UserKeys;

        private CoapServer _server;
        private Resource _resource;
        private int _serverPort;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            PskOne = new OneKey();
            PskOne.Add(CoseKeyKeys.KeyType, GeneralValues.KeyType_Octet);
            PskOne.Add(CoseKeyKeys.KeyIdentifier, CBORObject.FromObject(Encoding.UTF8.GetBytes("KeyOne")));
            PskOne.Add(CoseKeyParameterKeys.Octet_k, CBORObject.FromObject(Encoding.UTF8.GetBytes("abcDEFghiJKL")));

            PskTwo = new OneKey();
            PskTwo.Add(CoseKeyKeys.KeyType, GeneralValues.KeyType_Octet);
            PskTwo.Add(CoseKeyKeys.KeyIdentifier, CBORObject.FromObject(Encoding.UTF8.GetBytes("KeyTwo")));
            PskTwo.Add(CoseKeyParameterKeys.Octet_k, CBORObject.FromObject(Encoding.UTF8.GetBytes("12345678091234")));

            UserKeys = new KeySet();
            UserKeys.AddKey(PskOne);
            UserKeys.AddKey(PskTwo);
        }

        [SetUp]
        public void SetupServer()
        {
            Log.LogManager.Level = Log.LogLevel.Fatal;
            CreateServer();

        }

        [TearDown]
        public void ShutdownServer()
        {
            _server.Dispose();
        }

        [Test]
        public void DTLSTestPskSpecific()
        {
            Uri uri = new Uri($"coaps://localhost:{_serverPort}/Hello1");
            DTLSClientEndPoint client = new DTLSClientEndPoint(PskOne);
            client.Start();

            Request req = new Request(Method.GET) {
                URI = uri,
                EndPoint = client
            };

            req.Send();
            String txt = req.WaitForResponse(50000).ResponseText;
            Assert.AreEqual("Hello from KeyOne", txt);
            client.Stop();

            DTLSClientEndPoint client2 = new DTLSClientEndPoint(PskTwo);
            client2.Start();
            Request req2 = new Request(Method.GET) {
                URI = uri,
                EndPoint = client2
            };

            req2.Send();
            txt = req2.WaitForResponse(50000).ResponseText;
            Assert.AreEqual("Hello from KeyTwo", txt);

            client2.Stop();

            Thread.Sleep(5000);
        }

        [Test]
        public void DTLSTestPskAsyncGet()
        {
            Uri uri = new Uri($"coaps://localhost:{_serverPort}/Hello1");
            DTLSClientEndPoint client = new DTLSClientEndPoint(PskOne);
            client.Start();

            Request req = new Request(Method.GET) {
                URI = uri,
                EndPoint = client
            };


            DTLSClientEndPoint client2 = new DTLSClientEndPoint(PskTwo);
            client2.Start();
            Request req2 = new Request(Method.GET) {
                URI = uri,
                EndPoint = client2
            };

            req.Send();
            req2.Send();
            String txt = req.WaitForResponse(50000).ResponseText;
            Assert.AreEqual("Hello from KeyOne", txt);

            txt = req2.WaitForResponse(50000).ResponseText;
            Assert.AreEqual("Hello from KeyTwo", txt);

            client2.Stop();
            client.Stop();

            Thread.Sleep(5000);
        }

        private void CreateServer()
        {
            CoAPEndPoint endpoint = new DTLSEndPoint(null, UserKeys, 0);
            _resource = new HelloResource("Hello1");
            _server = new CoapServer();
            _server.Add(_resource);

            _server.AddEndPoint(endpoint);
            _server.Start();
            _serverPort = ((System.Net.IPEndPoint) endpoint.LocalEndPoint).Port;
        }

        class HelloResource : Resource
        {
            public HelloResource(String name) : base(name)
            {
                
            }

            protected override void DoGet(CoapExchange exchange)
            {
                String content = $"Hello from ";

                content += Encoding.UTF8.GetString(exchange.Request.TlsContext.AuthenticationKey[CoseKeyKeys.KeyIdentifier].GetByteString());

                exchange.Respond(content);
            }
        }

    }
}
