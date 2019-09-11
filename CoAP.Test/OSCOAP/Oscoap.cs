using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
using TestInitialize = NUnit.Framework.SetUpAttribute;
using TestCleanup = NUnit.Framework.TearDownAttribute;
using Com.AugustCellars.CoAP.Net;
using Com.AugustCellars.CoAP.OSCOAP;
using Com.AugustCellars.CoAP.Server;
using Com.AugustCellars.CoAP.Server.Resources;


namespace Com.AugustCellars.CoAP
{
    [TestClass]
    public class Oscoap
    {
        Int32 _serverPort;
        CoapServer _server;
        Resource _resource;
        String _expected;
        Int32 _notifications;
        Boolean _failed;
        private static readonly byte[] _ClientId = Encoding.UTF8.GetBytes("client");
        private static readonly byte[] _ServerId = Encoding.UTF8.GetBytes("server");
        private static readonly byte[] _Secret = new byte[] { 01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20, 0x21, 0x22, 0x23 };


        [TestInitialize]
        public void SetupServer()
        {
            Log.LogManager.Level = Log.LogLevel.Fatal;
            CreateServer();
        }

        [TestCleanup]
        public void ShutdownServer()
        {
            _server.Dispose();
        }

        [TestMethod]
        public void Ocoap_Get()
        {
            CoapClient client = new CoapClient($"coap://localhost:{_serverPort}/abc") {
                OscoapContext = SecurityContext.DeriveContext(_Secret, null, _ClientId, _ServerId)
            };
            Response r = client.Get();
            Assert.AreEqual("/abc", r.PayloadString);

        }

        private void CreateServer()
        {
            CoAPEndPoint endpoint = new CoAPEndPoint(0);
            _server = new CoapServer();

            //            _resource = new StorageResource(TARGET, CONTENT_1);
            //           _server.Add(_resource);

            Resource r2 = new EchoLocation("abc");
            _server.Add(r2);

            r2.Add(new EchoLocation("def"));

            _server.AddEndPoint(endpoint);
            _server.Start();
            _serverPort = ((System.Net.IPEndPoint) endpoint.LocalEndPoint).Port;

            SecurityContextSet oscoapContexts = new SecurityContextSet();
            SecurityContextSet.AllContexts.Add(SecurityContext.DeriveContext(_Secret, null, _ServerId, _ClientId));
        }

        class EchoLocation : Resource
        {

            public EchoLocation(String name)
                : base(name)
            {
                Observable = true;
                RequireSecurity = true;
            }

            protected override void DoGet(CoapExchange exchange)
            {
                String c = this.Uri;
                String querys = exchange.Request.UriQuery;
                if (querys != "") {
                    c += "?" + querys;
                }

                exchange.Respond(c);
            }
        }

    }
}
