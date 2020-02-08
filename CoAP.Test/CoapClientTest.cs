/*
 * Copyright (c) 2011-2015, Longxiang He <helongxiang@smeshlink.com>,
 * SmeshLink Technology Co.
 *
 * Copyright (c) 2019-2020, Jim Schaad <ietf@augustcellars.com>
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY.
 * 
 * This file is part of the CoAP.NET, a CoAP framework in C#.
 * Please see README for more information.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Com.AugustCellars.CoAP.Net;
using Com.AugustCellars.CoAP.Server;
using Com.AugustCellars.CoAP.Server.Resources;

namespace Com.AugustCellars.CoAP
{
    [TestClass]
    public class CoapClientTest
    {
        static readonly string TARGET = "storage";
        static readonly string CONTENT_1 = "one";
        static readonly string CONTENT_2 = "two";
        static readonly string CONTENT_3 = "three";
        static readonly string CONTENT_4 = "four";
        static readonly string QUERY_UPPER_CASE = "uppercase";

        int _serverPort;
        CoapServer _server;
        Resource _resource;
        string _expected;
        int _notifications;
        bool _failed;

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
        public void TestSynchronousCall()
        {
            _notifications = 0;

            Uri uri = new Uri("coap://localhost:" + _serverPort + "/" + TARGET);
            CoapClient client = new CoapClient(uri);

            // Check that we get the right content when calling get()
            string resp1 = client.Get().ResponseText;
            Assert.AreEqual(CONTENT_1, resp1);

            string resp2 = client.Get().ResponseText;
            Assert.AreEqual(CONTENT_1, resp2);

            // Change the content to "two" and check
            string resp3 = client.Post(CONTENT_2).ResponseText;
            Assert.AreEqual(CONTENT_1, resp3);

            string resp4 = client.Get().ResponseText;
            Assert.AreEqual(CONTENT_2, resp4);

            // Observe the resource
            _expected = CONTENT_2;

            object blockLock = new object();
            CoapObserveRelation obs1 = client.Observe(response =>
                {
                    lock (blockLock) { // Make sure only one at a time is in here.
                        Interlocked.Increment(ref _notifications);
                        string payload = response.ResponseText;
                        Assert.AreEqual(_expected, payload);
                        Assert.IsTrue(response.HasOption(OptionType.Observe));
                    }
                }, Fail);
            Assert.IsFalse(obs1.Canceled);

            Thread.Sleep(100);
            _resource.Changed();
            Thread.Sleep(100);
            _resource.Changed();
            Thread.Sleep(100);
            _resource.Changed();

            Thread.Sleep(100);
            _expected = CONTENT_3;
            string resp5 = client.Post(CONTENT_3).ResponseText;
            Assert.AreEqual(CONTENT_2, resp5);

            // Try a put and receive a METHOD_NOT_ALLOWED
            StatusCode code6 = client.Put(CONTENT_4).StatusCode;
            Assert.AreEqual(StatusCode.MethodNotAllowed, code6);

            // Cancel observe relation of obs1 and check that it does no longer receive notifications
            Thread.Sleep(100);
            _expected = null; // The next notification would now cause a failure
            obs1.ReactiveCancel();
            Thread.Sleep(100);
            _resource.Changed();
            Assert.AreEqual(5, _notifications);

            // Make another post
            Thread.Sleep(100);
            string resp7 = client.Post(CONTENT_4).ResponseText;
            Assert.AreEqual(CONTENT_3, resp7);

            // Try to use the builder and add a query
            UriBuilder ub = new UriBuilder("coap", "localhost", _serverPort, TARGET);
            ub.Query = QUERY_UPPER_CASE;

            string resp8 = new CoapClient(ub.Uri).Get().ResponseText;
            Assert.AreEqual(CONTENT_4.ToUpper(), resp8);

            // Check that we indeed received 5 notifications
            // 1 from origin GET request, 3 x from changed(), 1 from post()
            Thread.Sleep(100);
            Assert.AreEqual(5, _notifications);
            Assert.IsFalse(_failed);
        }

        [TestMethod]
        public void TestAsynchronousCall()
        {
            _notifications = 0;

            Uri uri = new Uri("coap://localhost:" + _serverPort + "/" + TARGET);
            CoapClient client = new CoapClient(uri);
            client.Error += (o, e) => Fail(e.Reason);

            // Check that we get the right content when calling get()
            client.GetAsync(response => Assert.AreEqual(CONTENT_1, response.ResponseText));
            Thread.Sleep(100);

            client.GetAsync(response => Assert.AreEqual(CONTENT_1, response.ResponseText));
            Thread.Sleep(100);

            // Change the content to "two" and check
            client.PostAsync(CONTENT_2, response => Assert.AreEqual(CONTENT_1, response.ResponseText));
            Thread.Sleep(100);

            client.GetAsync(response => Assert.AreEqual(CONTENT_2, response.ResponseText));
            Thread.Sleep(100);

            // Observe the resource
            _expected = CONTENT_2;
            CoapObserveRelation obs1 = client.ObserveAsync(response =>
                {
                    Interlocked.Increment(ref _notifications);
                    string payload = response.ResponseText;
                    Assert.AreEqual(_expected, payload);
                    Assert.IsTrue(response.HasOption(OptionType.Observe));
                }
            );

            Thread.Sleep(100);
            _resource.Changed();
            Thread.Sleep(100);
            _resource.Changed();
            Thread.Sleep(100);
            _resource.Changed();

            Thread.Sleep(100);
            _expected = CONTENT_3;
            client.PostAsync(CONTENT_3, response => Assert.AreEqual(CONTENT_2, response.ResponseText));
            Thread.Sleep(100);

            // Try a put and receive a MethodNotAllowed
            client.PutAsync(CONTENT_4, response => Assert.AreEqual(StatusCode.MethodNotAllowed, response.StatusCode));

            // Cancel observe relation of obs1 and check that it does no longer receive notifications
            Thread.Sleep(100);
            _expected = null; // The next notification would now cause a failure
            obs1.ReactiveCancel();
            Thread.Sleep(100);
            _resource.Changed();

            // Make another post
            Thread.Sleep(100);
            client.PostAsync(CONTENT_4, response => Assert.AreEqual(CONTENT_3, response.ResponseText));
            Thread.Sleep(100);

            UriBuilder ub = new UriBuilder("coap", "localhost", _serverPort, TARGET);
            ub.Query = QUERY_UPPER_CASE;

            // Try to use the builder and add a query
            new CoapClient(ub.Uri).GetAsync(response => Assert.AreEqual(CONTENT_4.ToUpper(), response.ResponseText));

            // Check that we indeed received 5 notifications
            // 1 from origin GET request, 3 x from changed(), 1 from post()
            Thread.Sleep(100);
            Assert.AreEqual(5, _notifications);
            Assert.IsFalse(_failed);
        }

        [TestMethod]
        public void TestCoapClient_UriPath()
        {
            Uri uri = new Uri("coap://localhost:" + _serverPort + "/");
            CoapClient client = new CoapClient(uri);

            IEnumerable<WebLink> resources = client.Discover();
            Assert.AreEqual(4, resources.Count());

            client.UriPath = "abc";

            Response r = client.Get();
            Assert.AreEqual("/abc", r.PayloadString);

            client.UriPath = "/abc/def";
            r = client.Get();
            Assert.AreEqual("/abc/def", r.PayloadString);
        }

        [TestMethod]
        public void TestCoapClient_Discover()
        {
            Uri uri = new Uri($"coap://localhost:{_serverPort}/");
            CoapClient client = new CoapClient(uri);

            IEnumerable<WebLink> resources = client.Discover();
            Assert.AreEqual(4, resources.Count());

            resources = client.Discover(MediaType.ApplicationLinkFormat);
            Assert.AreEqual(4, resources.Count());
        }

        [TestMethod]
        public void TestCoapClient_UriQuery()
        {
            Uri uri = new Uri("coap://localhost:" + _serverPort + "/");
            CoapClient client = new CoapClient(uri);

            IEnumerable<WebLink> resources = client.Discover();
            Assert.AreEqual(4, resources.Count());

            client.UriPath = "abc";
            client.UriQuery = "upper_case";

            Response r = client.Get();
            Assert.AreEqual("/abc?upper_case", r.PayloadString);

            client.UriQuery = "upper_case&lower_case";
            r = client.Get();
            Assert.AreEqual("/abc?upper_case&lower_case", r.PayloadString);
        }

        private void Fail(CoapClient.FailReason reason)
        {
            _failed = true;
            Assert.Fail();
        }

        private void CreateServer()
        {
            CoAPEndPoint endpoint = new CoAPEndPoint(0);
            _resource = new StorageResource(TARGET, CONTENT_1);
            _server = new CoapServer();
            _server.Add(_resource);

            _server.AddEndPoint(endpoint);
            _server.Start();
            _serverPort = ((System.Net.IPEndPoint)endpoint.LocalEndPoint).Port;

            Resource r2 = new EchoLocation("abc");
            _server.Add(r2);

            r2.Add(new EchoLocation("def"));
        }

        class StorageResource : Resource
        {
            private string _content;

            public StorageResource(string name, string content)
                : base(name)
            {
                _content = content;
                Observable = true;
            }

            protected override void DoGet(CoapExchange exchange)
            {
                IEnumerable<string> queries = exchange.Request.UriQueries;
                string c = _content;
                foreach (string q in queries) {
                    if (QUERY_UPPER_CASE.Equals(q)) {
                        c = _content.ToUpper();
                    }
                }

                exchange.Respond(c);
            }

            protected override void DoPost(CoapExchange exchange)
            {
                string old = _content;
                _content = exchange.Request.PayloadString;
                exchange.Respond(StatusCode.Changed, old);
                Changed();
            }
        }

        class EchoLocation : Resource
        {

            public EchoLocation(string name)
                : base(name)
            {
                Observable = true;
            }

            protected override void DoGet(CoapExchange exchange)
            {
                string c = this.Uri;
                string querys = exchange.Request.UriQuery;
                if (querys != "") {
                    c += "?" + querys;
                }

                exchange.Respond(c);
            }
        }


    }
}
