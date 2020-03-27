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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Com.AugustCellars.CoAP.Net;
using Com.AugustCellars.CoAP.Server;
using Com.AugustCellars.CoAP.Server.Resources;


namespace Com.AugustCellars.CoAP.OSCOAP
{
    [TestClass]
    public class Oscoap
    {
        int _serverPort;
        CoapServer _server;
        private static readonly byte[] clientId = Encoding.UTF8.GetBytes("client");
        private static readonly byte[] serverId = Encoding.UTF8.GetBytes("server");
        private static readonly byte[] secret = new byte[] { 01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20, 0x21, 0x22, 0x23 };


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
                OscoreContext = SecurityContext.DeriveContext(secret, null, clientId, serverId)
            };
            Response r = client.Get();
            Assert.IsNotNull(r);
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
            _server.SecurityContexts.Add(SecurityContext.DeriveContext(secret, null, serverId, clientId));
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
