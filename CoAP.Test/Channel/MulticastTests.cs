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
using Com.AugustCellars.CoAP.Log;
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

        static readonly IPAddress multicastAddress = IPAddress.Parse("224.0.1.187");
        static readonly IPAddress multicastAddress2 = IPAddress.Parse("[FF02:0:0:0:0:0:0:FD]");

        private static readonly IPAddress multicastAddress3 = IPAddress.Parse("224.0.1.180");
        private static readonly IPAddress multicastAddress4 = IPAddress.Parse("[FF02::DD]");

        int _serverPort;
        CoapServer _server;
        Resource _resource;

        [TestInitialize]
        public void SetupServer()
        {
            LogManager.Level = LogLevel.All;
            CreateServer();
        }


        [TestCleanup]
        public void ShutdownServer()
        {
            _server.Dispose();
        }

        [TestMethod]
        public void GetUnicast()
        {
            Uri uri;
            CoapClient client;
            Response response;

            uri = new Uri($"coap://localhost:{_serverPort}/{UnicastTarget}");

            client = new CoapClient(uri);
            response = client.Get();
            Assert.IsNotNull(response);
            Assert.AreEqual(UnicastResponse, response.ResponseText);
        }

        [TestMethod]
        public void TestUnicastOfMulticastResource()
        {
            Uri uri;
            CoapClient client;
            Response response;

            uri = new Uri($"coap://localhost:{_serverPort}/{MulticastTarget}");
            client = new CoapClient(uri);
            response = client.Get();
            Assert.IsNotNull(response);
            Assert.AreEqual(StatusCode.MethodNotAllowed, response.StatusCode);
        }

        [TestMethod]
        public void TestMulticastV4_Base()
        {
            LogManager.Level = LogLevel.All;

            Uri uri;
            CoapClient client;

            //  Check that multicast returns on multicast 

            List<Response> responseList = new List<Response>();
            AutoResetEvent trigger = new AutoResetEvent(false);
            uri = new Uri($"coap://{multicastAddress3}:{_serverPort}/{MulticastTarget}");
            client = new CoapClient(uri);
            client.UseNONs();
            client.GetAsync(r => {
                responseList.Add(r);
                trigger.Set();
            });

            Assert.IsTrue(trigger.WaitOne(10*1000));

            Assert.IsTrue(responseList.Count == 1);
            foreach (Response r in responseList)
            {
                Assert.AreEqual(StatusCode.Content, r.StatusCode);
                Assert.AreEqual(MulticastResponse, r.ResponseText);
            }

        }

        [TestMethod]
        public void TestMulticastV4_Offset()
        {
            Uri uri;
            CoapClient client;

            //  Check that multicast returns on multicast 

            List<Response> responseList = new List<Response>();
            AutoResetEvent trigger = new AutoResetEvent(false);
            uri = new Uri($"coap://{multicastAddress}:{_serverPort + PortJump}/{MulticastTarget}");
            client = new CoapClient(uri);
            client.UseNONs();
            client.GetAsync(r => {
                responseList.Add(r);
                trigger.Set();
            });

            trigger.WaitOne(1000);

            Assert.IsTrue(responseList.Count == 1);
            foreach (Response r in responseList)
            {
                Assert.AreEqual(StatusCode.Content, r.StatusCode);
                Assert.AreEqual(MulticastResponse, r.ResponseText);
            }

        }

        [TestMethod]
        public void TestMulticastIPv6_Base()
        {
            Uri uri;
            CoapClient client;
            List<Response> responseList = new List<Response>();
            AutoResetEvent trigger = new AutoResetEvent(false);

            uri = new Uri($"coap://[{multicastAddress4}]:{_serverPort}/{MulticastTarget}");
            client = new CoapClient(uri);
            client.UseNONs();
            client.GetAsync(r => {
                responseList.Add(r);
                trigger.Set();
            });

            Assert.IsTrue(trigger.WaitOne(2 * 1000));

            Console.WriteLine($"response count = {responseList.Count}");
            Assert.IsTrue(responseList.Count == 1);
            foreach (Response r in responseList)
            {
                Assert.AreEqual(StatusCode.Content, r.StatusCode);
                Assert.AreEqual(MulticastResponse, r.ResponseText);
            }
        }


        [TestMethod]
        public void TestMulticastIPv6_Offset()
        {
            Uri uri;
            CoapClient client;
            List<Response> responseList = new List<Response>();
            AutoResetEvent trigger = new AutoResetEvent(false);

            uri = new Uri($"coap://[{multicastAddress2}]:{_serverPort + PortJump}/{MulticastTarget}");
            client = new CoapClient(uri);
            client.UseNONs();
            client.GetAsync(r => {
                responseList.Add(r);
                trigger.Set();
            });

            Assert.IsTrue(trigger.WaitOne(2*1000));

            Console.WriteLine($"response count = {responseList.Count}");
            Assert.IsTrue(responseList.Count == 1);
            foreach (Response r in responseList)
            {
                Assert.AreEqual(StatusCode.Content, r.StatusCode);
                Assert.AreEqual(MulticastResponse, r.ResponseText);
            }
        }

        [TestMethod]
        public void TestMulticastV4OfUnicastResource()
        {
            Uri uri;
            CoapClient client;
            AutoResetEvent trigger = new AutoResetEvent(false);

            uri = new Uri($"coap://{multicastAddress}:{_serverPort + PortJump}/{UnicastTarget}");
            client = new CoapClient(uri);
            client.UseNONs();
            client.GetAsync(r => {
                trigger.Set();
            });

            Assert.IsFalse(trigger.WaitOne(1000));
        }

        [TestMethod]
        public void TestMulticastV6OfUnicastResource()
        {
            Uri uri;
            CoapClient client;
            AutoResetEvent trigger = new AutoResetEvent(false);

            uri = new Uri($"coap://[{multicastAddress2}]:{_serverPort + PortJump}/{UnicastTarget}");
            client = new CoapClient(uri);
            client.UseNONs();
            client.GetAsync(r => {
                trigger.Set();
            });

            Assert.IsFalse(trigger.WaitOne(1000));
        }



        private void CreateServer()
        {
            CoapConfig config = new CoapConfig();

            CoAPEndPoint endpoint = new CoAPEndPoint(0, config);

            _resource = new MulticastResource(MulticastTarget, MulticastResponse);
            _server = new CoapServer();
            _server.Add(_resource);

            _server.AddEndPoint(endpoint);
            _server.Start();
            _serverPort = ((IPEndPoint)endpoint.LocalEndPoint).Port;

            endpoint.AddMulticastAddress(new IPEndPoint(multicastAddress3, _serverPort));
            endpoint.AddMulticastAddress(new IPEndPoint(multicastAddress4, _serverPort));
            endpoint.AddMulticastAddress(new IPEndPoint(multicastAddress, _serverPort + PortJump));
            endpoint.AddMulticastAddress(new IPEndPoint(multicastAddress2, _serverPort + PortJump));

            Resource r2 = new UnicastResource(UnicastTarget, UnicastResponse);
            _server.Add(r2);
        }



        class UnicastResource : Resource
        {
            private readonly string _content;

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

                exchange.Respond(StatusCode.Content, _content);
            }
        }

        class MulticastResource : Resource
        {
            private readonly string _content;

            public MulticastResource(string name, string content)
                : base(name)
            {
                _content = content;
            }

            protected override void DoGet(CoapExchange exchange)
            {
                Request request = exchange.Request;

                if (!request.IsMulticast) {
                    exchange.Respond(StatusCode.MethodNotAllowed);
                    return;
                }
                exchange.Respond(StatusCode.Content, _content);
            }
        }

    }
}
