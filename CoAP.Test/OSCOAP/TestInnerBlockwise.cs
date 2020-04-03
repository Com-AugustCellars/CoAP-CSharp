using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using Com.AugustCellars.CoAP;
using Com.AugustCellars.CoAP.Net;
using Com.AugustCellars.CoAP.OSCOAP;
using Com.AugustCellars.CoAP.Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoAP.Test.Std10.OSCOAP
{
    [TestClass]
    public class TestInnerBlockwise
    {
        static readonly string SHORT_POST_REQUEST = "<Short request>";
        static readonly string LONG_POST_REQUEST = "<Long request 1x2x3x4x5x>".Replace("x", "ABCDEFGHIJKLMNOPQRSTUVWXYZ ");
        static readonly string SHORT_POST_RESPONSE = "<Short response>";
        static readonly string LONG_POST_RESPONSE = "<Long response 1x2x3x4x5x>".Replace("x", "ABCDEFGHIJKLMNOPQRSTUVWXYZ ");
        static readonly string SHORT_GET_RESPONSE = SHORT_POST_RESPONSE.ToLower();
        static readonly string LONG_GET_RESPONSE = LONG_POST_RESPONSE.ToLower();

        private static readonly byte[] clientId = Encoding.UTF8.GetBytes("client");
        private static readonly byte[] serverId = Encoding.UTF8.GetBytes("server");
        private static readonly byte[] secret = new byte[] { 01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20, 0x21, 0x22, 0x23 };

        int _serverPort;
        CoapConfig _config = new CoapConfig();
        CoapServer _server;
        IEndPoint _clientEndpoint;

        bool request_short = true;
        bool respond_short = true;

        [TestInitialize]
        public void SetupServer()
        {
            Com.AugustCellars.CoAP.Log.LogManager.Level = Com.AugustCellars.CoAP.Log.LogLevel.Debug;
            _config = new CoapConfig();
            _config.OSCOAP_DefaultBlockSize = 32;
            _config.OSCOAP_MaxMessageSize = 32;
            _config.AckTimeout = 60 * 60 * 1000;

            CreateServer();
            _clientEndpoint = new CoAPEndPoint(_config);
            _clientEndpoint.Start();
        }

        [TestCleanup]
        public void ShutdownServer()
        {
            _server?.Dispose();
            _clientEndpoint?.Dispose();
        }

        [TestMethod]
        public void Test_POST_short_short()
        {
            request_short = true;
            respond_short = true;
            ExecutePOSTRequest();
        }

        [TestMethod]
        public void Test_POST_long_short()
        {
            request_short = false;
            respond_short = true;
            ExecutePOSTRequest();
        }

        [TestMethod]
        public void Test_POST_short_long()
        {
            request_short = true;
            respond_short = false;
            ExecutePOSTRequest();
        }

        [TestMethod]
        public void Test_POST_long_long()
        {
            request_short = false;
            respond_short = false;
            ExecutePOSTRequest();
        }

        [TestMethod]
        public void Test_GET_short()
        {
            respond_short = true;
            ExecuteGETRequest();
        }

        [TestMethod]
        public void Test_GET_long()
        {
            respond_short = false;
            ExecuteGETRequest();
        }

        private void ExecuteGETRequest()
        {
            string payload = "nothing";
            try {
                Request request = Request.NewGet();
                
                request.Destination = new IPEndPoint(IPAddress.Loopback, _serverPort);
                request.OscoreContext = SecurityContext.DeriveContext(secret, null, clientId, serverId);

                request.Send(_clientEndpoint);

                // receive response and check
                Response response = request.WaitForResponse(/*1000*/);

                Assert.IsNotNull(response);
                payload = response.PayloadString;
                Assert.AreEqual(respond_short ? SHORT_GET_RESPONSE : LONG_GET_RESPONSE, payload);
            }
            finally {
                Thread.Sleep(100); // Quickly wait until last ACKs arrive
            }
        }

        private void ExecutePOSTRequest()
        {
            string payload = "--no payload--";
            try
            {
                Request request = new Request(Method.POST);
                request.OscoreContext = SecurityContext.DeriveContext(secret, null, clientId, serverId);
                request.SetUri("coap://localhost:" + _serverPort + "/" + request_short + respond_short);
                request.SetPayload(request_short ? SHORT_POST_REQUEST : LONG_POST_REQUEST);

                request.Send(_clientEndpoint);

                // receive response and check
                Response response = request.WaitForResponse(/*1000*/);

                Assert.IsNotNull(response);
                payload = response.PayloadString;

                if (respond_short) {
                    Assert.AreEqual(SHORT_POST_RESPONSE, payload);
                }
                else {
                    Assert.AreEqual(LONG_POST_RESPONSE, payload);
                }
            }
            finally
            {
                Thread.Sleep(100); // Quickly wait until last ACKs arrive
            }
        }

        private void CreateServer()
        {
            _server = new CoapServer();
            CoAPEndPoint endpoint = new CoAPEndPoint(_serverPort, _config);
            _server.AddEndPoint(endpoint);
            _server.MessageDeliverer = new MessageDeliverer(this);
            _server.SecurityContexts.Add(SecurityContext.DeriveContext(secret, null, serverId, clientId));
            _server.Start();
            _serverPort = ((System.Net.IPEndPoint)endpoint.LocalEndPoint).Port;
        }

        class MessageDeliverer : IMessageDeliverer
        {
            readonly TestInnerBlockwise _test;

            public MessageDeliverer(TestInnerBlockwise test)
            {
                _test = test;
            }

            public void DeliverRequest(Exchange exchange)
            {
                if (exchange.Request.Method == Method.GET) {
                    ProcessGET(exchange);
                }
                else {
                    ProcessPOST(exchange);
                }
            }

            public void DeliverResponse(Exchange exchange, Response response)
            { }

            private void ProcessGET(Exchange exchange)
            {
                Response response = new Response(StatusCode.Content);
                if (_test.respond_short) {
                    response.SetPayload(SHORT_GET_RESPONSE);
                }
                else {
                    response.SetPayload(LONG_GET_RESPONSE);
                }

                exchange.SendResponse(response);
            }

            private void ProcessPOST(Exchange exchange)
            {
                string payload = exchange.Request.PayloadString;
                if (_test.request_short) {
                    Assert.AreEqual(payload, SHORT_POST_REQUEST);
                }
                else {
                    Assert.AreEqual(payload, LONG_POST_REQUEST);
                }

                Response response = new Response(StatusCode.Changed);
                response.SetPayload(_test.respond_short ? SHORT_POST_RESPONSE : LONG_POST_RESPONSE);

                exchange.SendResponse(response);
            }
        }
    }
}
