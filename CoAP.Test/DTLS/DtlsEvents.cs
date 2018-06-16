using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Com.AugustCellars.CoAP.Log;
using Com.AugustCellars.CoAP.Net;
using Com.AugustCellars.CoAP.Server;
using Com.AugustCellars.CoAP.Server.Resources;
using Com.AugustCellars.COSE;
using NUnit.Framework;

using PeterO.Cbor;

namespace Com.AugustCellars.CoAP.DTLS
{
    [TestFixture]
    class DtlsEvents
    {
        private static OneKey PskOne;
        private static OneKey PskTwo;
        private static KeySet UserKeys;

        private CoapServer _server;
        private HelloResource _resource;
        private int _serverPort;

        private static readonly byte[] PskOneName = Encoding.UTF8.GetBytes("KeyOne");
        private static readonly byte[] PskTwoName = Encoding.UTF8.GetBytes("KeyTwo");

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            PskOne = new OneKey();
            PskOne.Add(CoseKeyKeys.KeyType, GeneralValues.KeyType_Octet);
            PskOne.Add(CoseKeyKeys.KeyIdentifier, CBORObject.FromObject(PskOneName));
            PskOne.Add(CoseKeyParameterKeys.Octet_k, CBORObject.FromObject(Encoding.UTF8.GetBytes("abcDEFghiJKL")));

            PskTwo = new OneKey();
            PskTwo.Add(CoseKeyKeys.KeyType, GeneralValues.KeyType_Octet);
            PskTwo.Add(CoseKeyKeys.KeyIdentifier, CBORObject.FromObject(PskTwoName));
            PskTwo.Add(CoseKeyParameterKeys.Octet_k, CBORObject.FromObject(Encoding.UTF8.GetBytes("12345678091234")));

            UserKeys = new KeySet();
            // UserKeys.AddKey(PskOne);
            // UserKeys.AddKey(PskTwo);
        }

        [SetUp]
        public void SetupServer()
        {
            Log.LogManager.Level = LogLevel.Fatal;
            CreateServer();
        }

        [TearDown]
        public void ShutdownServer()
        {
            _server.Dispose();
        }

        [Test]
        public void DtlsTestPskEvents()
        {
            Uri uri = new Uri($"coaps://localhost:{_serverPort}/Hello1");
            DTLSClientEndPoint client = new DTLSClientEndPoint(PskOne);
            client.Start();

            Request req = new Request(Method.GET) {
                URI = uri,
                EndPoint = client
            };

            req.Send();
            Response resp = req.WaitForResponse(50000);
            Assert.AreEqual(null, resp);
            client.Stop();

            DTLSClientEndPoint client2 = new DTLSClientEndPoint(PskTwo);
            client2.Start();
            Request req2 = new Request(Method.GET) {
                URI = uri,
                EndPoint = client2
            };

            req2.Send();
            string txt = req2.WaitForResponse(50000).ResponseText;
            Assert.AreEqual("Hello from KeyTwo", txt);

            client2.Stop();

            Thread.Sleep(5000);

        }


        private void CreateServer()
        {
            DTLSEndPoint endpoint = new DTLSEndPoint(null, UserKeys, 0);
            _resource = new HelloResource("Hello1");
            _server = new CoapServer();
            _server.Add(_resource);

            _server.AddEndPoint(endpoint);
            endpoint.TlsEventHandler += ServerEventHandler;
            _server.Start();
            _serverPort = ((System.Net.IPEndPoint)endpoint.LocalEndPoint).Port;
        }

        private static void ServerEventHandler(Object o, TlsEvent e)
        {
            switch (e.Code) {
                case TlsEvent.EventCode.UnknownPskName:
                    if (e.PskName.SequenceEqual(PskOneName)) {
                        //  We don't recognize this name
                    }
                    else if (e.PskName.SequenceEqual(PskTwoName)) {
                        e.KeyValue = PskTwo;
                   }
                    break;
            }
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
