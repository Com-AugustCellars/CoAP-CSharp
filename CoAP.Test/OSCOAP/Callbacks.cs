/*
 * Copyright (c) 2019-2020, Jim Schaad <ietf@augustcellars.com>
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY.
 * 
 * This file is part of the CoAP.NET, a CoAP framework in C#.
 * Please see README for more information.
 */

using System;
using System.Text;
using Com.AugustCellars.CoAP;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Com.AugustCellars.CoAP.Server;
using Com.AugustCellars.CoAP.Net;
using Com.AugustCellars.CoAP.Log;
using Com.AugustCellars.CoAP.OSCOAP;
using Com.AugustCellars.CoAP.Server.Resources;
using Com.AugustCellars.COSE;

namespace CoAP.Test.Std10.OSCOAP
{
    [TestClass]
    public class Callbacks
    {
        private int _serverPort;
        private CoapServer _server;
        private OscoreEvent.EventCode _callbackCode;
        private static readonly byte[] clientId = Encoding.UTF8.GetBytes("client");
        private static readonly byte[] clientId2 = Encoding.UTF8.GetBytes("client2");
        private static readonly byte[] serverId = Encoding.UTF8.GetBytes("server");
        private static readonly byte[] serverId2 = Encoding.UTF8.GetBytes("server2");
        private static readonly byte[] groupId1 = Encoding.UTF8.GetBytes("Group1");
        private static readonly byte[] groupId2 = Encoding.UTF8.GetBytes("Group2");
        private static readonly byte[] secret = new byte[] { 01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20, 0x21, 0x22, 0x23 };
        private static readonly byte[] secret2 = new byte[] { 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20, 0x21, 0x22, 0x23, 0x24 };

        private static OneKey _clientSign1;
        private static OneKey _clientSign2;
        private static OneKey _serverSign1;

        [ClassInitialize]
        public static void ClassInit(TestContext e)
        {
            _clientSign1 = OneKey.GenerateKey(AlgorithmValues.EdDSA, GeneralValues.KeyType_OKP);
            _clientSign2 = OneKey.GenerateKey(AlgorithmValues.EdDSA, GeneralValues.KeyType_OKP);
            _serverSign1 = OneKey.GenerateKey(AlgorithmValues.EdDSA, GeneralValues.KeyType_OKP);
        }

        [TestInitialize]
        public void SetupServer()
        {
            LogManager.Level = LogLevel.Fatal;
            CreateServer();
        }

        [TestCleanup]
        public void ShutdownServer()
        {
            _server.Stop();
            _server.Dispose();
        }

        private int _clientEventChoice;
        private OscoreEvent.EventCode _clientCallbackCode;
        private void ClientEventHandler(object o, OscoreEvent e)
        {
            _clientCallbackCode = e.Code;
            switch (_clientEventChoice) {
            case 0:
                _callbackCode = e.Code;
                break;

            case 1:
                _callbackCode = e.Code;
                e.SecurityContext = SecurityContext.DeriveGroupContext(secret2, groupId2, clientId, AlgorithmValues.EdDSA, _clientSign1, null, null);
                break;

            default:
                Assert.Fail();
                break;
            }
        }


        [TestMethod]
        public void PivExhaustion()
        {
            SecurityContext context = SecurityContext.DeriveGroupContext(secret, groupId1, clientId, AlgorithmValues.EdDSA, _clientSign1, null, null);
            SecurityContext context2 = SecurityContext.DeriveGroupContext(secret2, groupId2, clientId, AlgorithmValues.EdDSA, _clientSign1, null, null);
            for (int i = 0; i < 10; i++) {
                context.Sender.IncrementSequenceNumber();
            }

            context.Sender.MaxSequenceNumber = 10;
            context.OscoreEvents += ClientEventHandler;

            CoapClient client = new CoapClient($"coap://localhost:{_serverPort}/abc") {
                OscoreContext = context,
                Timeout = 20
            };

            Response r = client.Get();

            Assert.AreEqual(OscoreEvent.EventCode.PivExhaustion, _clientCallbackCode);

            _clientEventChoice = 1;
            client.Timeout = 1000 * 60;
            r = client.Get();
            Assert.AreEqual(OscoreEvent.EventCode.UnknownGroupIdentifier, _callbackCode);

        }

        [TestMethod]
        public void NoGroupId()
        {
            SecurityContext context = SecurityContext.DeriveGroupContext(secret, groupId1, clientId, AlgorithmValues.EdDSA, _clientSign1, null, null);

            CoapClient client = new CoapClient($"coap://localhost:{_serverPort}/abc") {
                OscoreContext = context,
//                Timeout = 60
            };
            Console.WriteLine($"--Server port = {_serverPort}");


            Response r = client.Get();
            Assert.AreEqual(OscoreEvent.EventCode.UnknownGroupIdentifier, _callbackCode);
        }

        [TestMethod]
        public void SetGroupId()
        {
            SecurityContext context = SecurityContext.DeriveGroupContext(secret, groupId1, clientId, AlgorithmValues.EdDSA, _clientSign1,
                new byte[][]{serverId},  new OneKey[]{_serverSign1});

            CoapClient client = new CoapClient($"coap://localhost:{_serverPort}/abc") {
                OscoreContext = context,
                Timeout = 60*1000
            };
            Console.WriteLine($"--Server port = {_serverPort}");

            _serverEventChoice = 1;
            Response r = client.Get();
            Assert.IsNotNull(r);
            Assert.AreEqual("/abc", r.PayloadString);
        }

        [TestMethod]
        public void MissingKeyId()
        {
            SecurityContext context = SecurityContext.DeriveGroupContext(secret, groupId1, clientId, AlgorithmValues.EdDSA, _clientSign1,
                new byte[][] { serverId }, new OneKey[] { _serverSign1 });
            SecurityContext serverContext = SecurityContext.DeriveGroupContext(secret, groupId1, serverId, AlgorithmValues.EdDSA, _serverSign1,
                new byte[][]{clientId2}, new OneKey[]{_clientSign2});
            _server.SecurityContexts.Add(serverContext);
            serverContext.OscoreEvents += ServerEventHandler;

            CoapClient client = new CoapClient($"coap://localhost:{_serverPort}/abc")
            {
                OscoreContext = context,
                Timeout = 60 * 1000
            };

            _serverEventChoice = 0;
            Response r = client.Get();
            Assert.AreEqual(OscoreEvent.EventCode.UnknownKeyIdentifier, _callbackCode);
        }

        [TestMethod]
        public void SupplyMissingKeyId()
        {
            SecurityContext context = SecurityContext.DeriveGroupContext(secret, groupId1, clientId, AlgorithmValues.EdDSA, _clientSign1,
                new byte[][] { serverId }, new OneKey[] { _serverSign1 });
            SecurityContext serverContext = SecurityContext.DeriveGroupContext(secret, groupId1, serverId, AlgorithmValues.EdDSA, _serverSign1,
                new byte[][] { clientId2 }, new OneKey[] { _clientSign2 });
            _server.SecurityContexts.Add(serverContext);
            serverContext.OscoreEvents += ServerEventHandler;

            CoapClient client = new CoapClient($"coap://localhost:{_serverPort}/abc")
            {
                OscoreContext = context,
                Timeout = 60 * 1000
            };

            _serverEventChoice = 2;
            Response r = client.Get();
            Assert.IsNotNull(r);
            Assert.AreEqual("/abc", r.PayloadString);

        }

        [TestMethod]
        public void ServerIvExhaustion()
        {
            SecurityContext context = SecurityContext.DeriveGroupContext(secret, groupId1, clientId, AlgorithmValues.EdDSA, _clientSign1,
                new byte[][] { serverId }, new OneKey[] { _serverSign1 });
            SecurityContext serverContext = SecurityContext.DeriveGroupContext(secret, groupId1, serverId, AlgorithmValues.EdDSA, _serverSign1,
                new byte[][] { clientId2, clientId }, new OneKey[] { _clientSign2, _clientSign1 });
            _server.SecurityContexts.Add(serverContext);
            serverContext.OscoreEvents += ServerEventHandler;

            for (int i = 0; i < 10; i++) {
                serverContext.Sender.IncrementSequenceNumber();
            }

            serverContext.Sender.MaxSequenceNumber = 10;

            CoapClient client = new CoapClient($"coap://localhost:{_serverPort}/abc")
            {
                OscoreContext = context,
                Timeout = 60 * 1000
            };

            _serverEventChoice = 0;
            Response r = client.Get();
            Assert.IsNotNull(r);
            Assert.AreEqual("/abc", r.PayloadString);

            client = new CoapClient($"coap://localhost:{_serverPort}/abc") {
                OscoreContext = context,
                Timeout = 30 * 1000,
            };
            client.Observe();

            Assert.AreEqual(OscoreEvent.EventCode.PivExhaustion, _callbackCode);
        }

        [TestMethod]
        public void ServerNewSender()
        {
            SecurityContext context = SecurityContext.DeriveGroupContext(secret, groupId1, clientId, AlgorithmValues.EdDSA, _clientSign1,
                new byte[][] {serverId, serverId2}, new OneKey[] {_serverSign1, _serverSign1});
            SecurityContext serverContext = SecurityContext.DeriveGroupContext(secret, groupId1, serverId, AlgorithmValues.EdDSA, _serverSign1,
                new byte[][] {clientId2, clientId}, new OneKey[] {_clientSign2, _clientSign1});
            _server.SecurityContexts.Add(serverContext);
            serverContext.OscoreEvents += ServerEventHandler;

            for (int i = 0; i < 10; i++) {
                serverContext.Sender.IncrementSequenceNumber();
            }

            serverContext.Sender.MaxSequenceNumber = 10;

            CoapClient client = new CoapClient($"coap://localhost:{_serverPort}/abc") {
                OscoreContext = context,
                Timeout = 60 * 1000
            };

            _serverEventChoice = 3;
            client.Observe(o => { Assert.AreEqual("/abc", o.PayloadString); });

            Assert.AreEqual(OscoreEvent.EventCode.PivExhaustion, _callbackCode);
        }

        [TestMethod]
        public void ServerNewSenderGroup()
        {
            SecurityContext context = SecurityContext.DeriveGroupContext(secret, groupId1, clientId, AlgorithmValues.EdDSA, _clientSign1,
                new byte[][] { serverId, serverId2 }, new OneKey[] { _serverSign1, _serverSign1 });
            SecurityContext serverContext = SecurityContext.DeriveGroupContext(secret, groupId1, serverId, AlgorithmValues.EdDSA, _serverSign1,
                new byte[][] { clientId2, clientId }, new OneKey[] { _clientSign2, _clientSign1 });
            _server.SecurityContexts.Add(serverContext);
            serverContext.OscoreEvents += ServerEventHandler;

            serverContext.Sender.SequenceNumber = 10;

            serverContext.Sender.MaxSequenceNumber = 10;

            CoapClient client = new CoapClient($"coap://localhost:{_serverPort}/abc")
            {
                OscoreContext = context,
                Timeout = 60 * 1000
            };

            _serverEventChoice = 4;
            client.Observe(o => { Assert.AreEqual("/abc", o.PayloadString); });

            Assert.AreEqual(OscoreEvent.EventCode.PivExhaustion, _callbackCode);
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
            _serverPort = ((System.Net.IPEndPoint)endpoint.LocalEndPoint).Port;
            Console.WriteLine($"Server port = {_serverPort}");

            SecurityContextSet oscoapContexts = new SecurityContextSet();
            _server.SecurityContexts.Add(SecurityContext.DeriveContext(secret, null, serverId, clientId));
            _server.SecurityContexts.OscoreEvents += ServerEventHandler;
        }

        private int _serverEventChoice;

        private void ServerEventHandler(object o, OscoreEvent e)
        {
            _callbackCode = e.Code;
            switch (_serverEventChoice) {
            case 0:
                break;

            case 1:
                e.SecurityContext = SecurityContext.DeriveGroupContext(secret, groupId1, serverId, AlgorithmValues.EdDSA, _serverSign1,
                    new byte[][]{clientId}, new OneKey[]{_clientSign1});
                break;

                case 2:
                    e.SecurityContext.AddRecipient(clientId, _clientSign1);
                    e.RecipientContext = e.SecurityContext.Recipients[clientId];
                    break;

                case 3:
                    e.SecurityContext.ReplaceSender(serverId2, _serverSign1);
                    break;

                case 4:
                    e.SecurityContext = SecurityContext.DeriveGroupContext(secret2, groupId2, serverId, AlgorithmValues.EdDSA, _serverSign1,
                        new byte[][]{clientId}, new OneKey[]{_clientSign1} );
                    break;

            default:
                Assert.Fail();
                break;
            }
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
                if (querys != "")
                {
                    c += "?" + querys;
                }

                exchange.Respond(c);
            }
        }
    }
}
