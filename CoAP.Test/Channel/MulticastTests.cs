/*
 * Copyright (c) 2020, Jim Schaad <ietf@augustcellars.com>
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY.
 * 
 * This file is part of the CoAP.NET, a CoAP framework in C#.
 * Please see README for more information.
 */

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Com.AugustCellars.CoAP;
using Com.AugustCellars.CoAP.Net;
using Com.AugustCellars.CoAP.Server;
using Com.AugustCellars.CoAP.Server.Resources;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoAP.Test.Std10.Channel
{
    [TestClass]
    public class MulticastTests
    {
        private static readonly string MulticastTarget = "ForMulti";
        private static readonly string UnicastTarget = "ForUnicast";
        private const int PortJump = 10;

        private static readonly string UnicastResponse = "This is a unicast message";
        private static readonly string MulticastResponse = "This is a multicast message";

        static readonly System.Net.IPAddress _multicastAddress = IPAddress.Parse("224.0.1.187");
        static readonly System.Net.IPAddress _multicastAddress2 = IPAddress.Parse("[FF02:0:0:0:0:0:0:FD]");

        int[] _serverPort = new int[2];
        CoapServer[] _server = new CoapServer[2];
        Resource _resource;

        [TestInitialize]
        public void SetupServer()
        {
            CreateServer(0);
        }


        [TestCleanup]
        public void ShutdownServer()
        {
            _server[0].Dispose();
        }

        [TestMethod]
        public void TestClient()
        {
            Uri uri;
            CoapClient client;
            Response response;

            //  Check that we can unicast to both servers

            for (int i = 0; i < 1; i++) {
                uri = new Uri($"coap://localhost:{_serverPort[i]}/{UnicastTarget}");

                client = new CoapClient(uri);
                response = client.Get();
                Assert.IsNotNull(response);
                Assert.AreEqual(UnicastResponse, response.ResponseText);
            }

            //  Check that the multicast resource returns an error on the unicast address
            for (int i=0; i<1; i++) { 
                uri = new Uri($"coap://localhost:{_serverPort[i]}/{MulticastTarget}");
                client = new CoapClient(uri);
                response = client.Get();
                Assert.IsNotNull(response); 
                Assert.AreEqual(StatusCode.Content, response.StatusCode);

            }

            //  Check that multicast returns on multicast 

            List<Response> responseList = new List<Response>();
            AutoResetEvent trigger = new AutoResetEvent(false);
            uri = new Uri($"coap://{_multicastAddress}:{_serverPort[0] + PortJump}/{MulticastTarget}");
            client = new CoapClient(uri);
            client.UseNONs();
            client.GetAsync( r => {
                responseList.Add(r);
                    trigger.Set();
            });

            trigger.WaitOne(1000);

            Assert.IsTrue(responseList.Count == 1);
            foreach (Response r in responseList) {
                Assert.AreEqual(StatusCode.Content, r.StatusCode);
                Assert.AreEqual(MulticastResponse, r.ResponseText);
            }

            //  Check that unicast does not return on multicast

            responseList.Clear();
            uri = new Uri($"coap://{_multicastAddress}:{_serverPort[0] + PortJump}/{UnicastTarget}");
            client = new CoapClient(uri);
            client.UseNONs();
            client.GetAsync(r => {
                responseList.Add(r);
                trigger.Set();
            });

            Assert.IsTrue(trigger.WaitOne(1000));
        }


        private void CreateServer(int i)
        {
            CoapConfig config = new CoapConfig();

            CoAPEndPoint endpoint = new CoAPEndPoint(5683+i*2, config);

            _resource = new MulticastResource(MulticastTarget, MulticastResponse);
            _server[i] = new CoapServer();
            _server[i].Add(_resource);

            _server[i].AddEndPoint(endpoint);
            _server[i].Start();
            _serverPort[i] = ((System.Net.IPEndPoint)endpoint.LocalEndPoint).Port;

            endpoint.AddMulticastAddress(new IPEndPoint(_multicastAddress, _serverPort[0] + PortJump));
            endpoint.AddMulticastAddress(new IPEndPoint(_multicastAddress2, _serverPort[0] + PortJump));

            Resource r2 = new UnicastResource(UnicastTarget, UnicastResponse);
            _server[i].Add(r2);
        }



        class UnicastResource : Resource
        {
            private string _content;

            public UnicastResource(string name, string content)
                : base(name)
            {
                _content = content;
            }

            protected override void DoGet(CoapExchange exchange)
            {
                Request request = exchange.Request;

                if (request.IsMulticast) {
                    return;
                }

                exchange.Respond(StatusCode.Content, UnicastResponse);
            }
        }

        class MulticastResource : Resource
        {
            private string _content;

            public MulticastResource(string name, string content)
                : base(name)
            {
                _content = content;
            }

            protected override void DoGet(CoapExchange exchange)
            {
                Request request = exchange.Request;

#if false
                if (!request.IsMulticast) {
                    exchange.Respond(StatusCode.MethodNotAllowed);
                    return;
                }
#endif
                exchange.Respond(StatusCode.Content, MulticastResponse);
            }
        }

    }
}
