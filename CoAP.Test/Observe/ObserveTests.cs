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
using System.Threading;
using Com.AugustCellars.CoAP;
using Com.AugustCellars.CoAP.Log;
using Com.AugustCellars.CoAP.Net;
using Com.AugustCellars.CoAP.Server;
using Com.AugustCellars.CoAP.Server.Resources;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoAP.Test.Std10.Observe
{
    [TestClass]
    public class ObserveTests
    {
        private static readonly string target1 = "target1";
        private static readonly string target2 = "target2";

        int _serverPort;
        CoapServer _server;
        ObserveResource _resource;
        private ObserveResource _resource2;

        string _expected;
        int _notifications;
        bool _failed;
        private CoapConfig _config;


        [TestInitialize]
        public void SetupServer()
        {
            LogManager.Level = LogLevel.Debug;
            CreateServer();
        }

        [TestCleanup]
        public void ShutdownServer()
        {
            _server.Dispose();
        }

#if false
        [TestMethod]
        public void TestOutOfOrder()
        {

            //  Check what happens with out of order delivery

            Uri uri = new Uri($"coap://localhost:{_serverPort}/{target1}");
            CoapClient client = new CoapClient(uri);

            _expected = "No Content Yet";
            CoapObserveRelation obs1 = client.Observe(response => {
                Assert.AreEqual(_expected, response.ResponseText);
                Assert.IsTrue(response.HasOption(OptionType.Observe));
                Assert.IsTrue(response.Observe.HasValue);
                int oNumber = response.Observe.Value;
                Assert.IsTrue(oNumber > lastObserve);
                lastObserve = oNumber;

            });
            Assert.IsFalse(obs1.Canceled);

            Thread.Sleep(100);
            _resource.Changed();

            _resource2.ObserveNo = lastObserve + 3;

        }
#endif

        [TestMethod]
        public void TestCancel()
        {
            //  Check what happens with out of order delivery

            Uri uri = new Uri($"coap://localhost:{_serverPort}/{target1}");
            CoapClient client = new CoapClient(uri);

            AutoResetEvent trigger = new AutoResetEvent(false);
            Response nextResponse = null;
            int lastObserve = -1;

            _expected = "No Content Yet";
            CoapObserveRelation obs1 = client.Observe(response => {
                nextResponse = response;
                trigger.Set();

            });
            Assert.IsFalse(obs1.Canceled);

            Assert.IsTrue(trigger.WaitOne(1000));
            Assert.IsTrue(Code.IsSuccess(nextResponse.Code));
            Assert.AreEqual(_expected, nextResponse.ResponseText);
            Assert.IsTrue(nextResponse.HasOption(OptionType.Observe));
            Assert.IsTrue(nextResponse.Observe.HasValue);
            int oNumber = nextResponse.Observe.Value;
            Assert.IsTrue(oNumber > lastObserve);
            lastObserve = oNumber;

            _resource.Changed();
            Assert.IsTrue(trigger.WaitOne(1000));
            Assert.IsTrue(Code.IsSuccess(nextResponse.Code));
            Assert.AreEqual(_expected, nextResponse.ResponseText);
            Assert.IsTrue(nextResponse.HasOption(OptionType.Observe));
            Assert.IsTrue(nextResponse.Observe.HasValue);
            oNumber = nextResponse.Observe.Value;
            Assert.IsTrue(oNumber > lastObserve);
            lastObserve = oNumber;

            _resource.Changed();
            Assert.IsTrue(trigger.WaitOne(1000));
            Assert.IsTrue(Code.IsSuccess(nextResponse.Code));
            Assert.AreEqual(_expected, nextResponse.ResponseText);
            Assert.IsTrue(nextResponse.HasOption(OptionType.Observe));
            Assert.IsTrue(nextResponse.Observe.HasValue);
            oNumber = nextResponse.Observe.Value;
            Assert.IsTrue(oNumber > lastObserve);
            lastObserve = oNumber;

            _resource.Canceled(false);
            Assert.IsFalse(trigger.WaitOne(1000));

            _resource.Changed();
            Assert.IsFalse(trigger.WaitOne(1000));

            client = null;
        }

        [TestMethod]
        public void TestCancelWithNotify()
        {
            //  Check what happens with out of order delivery

            Uri uri = new Uri($"coap://localhost:{_serverPort}/{target1}");
            CoapClient client = new CoapClient(uri);

            AutoResetEvent trigger = new AutoResetEvent(false);
            Response nextResponse = null;
            int lastObserve = -1;

            _expected = "No Content Yet";
            CoapObserveRelation obs1 = client.Observe(response => {
                nextResponse = response;
                trigger.Set();

            });
            Assert.IsFalse(obs1.Canceled);

            Assert.IsTrue(trigger.WaitOne(1000));
            Assert.IsTrue(Code.IsSuccess(nextResponse.Code));
            Assert.AreEqual(_expected, nextResponse.ResponseText);
            Assert.IsTrue(nextResponse.HasOption(OptionType.Observe));
            Assert.IsTrue(nextResponse.Observe.HasValue);
            int oNumber = nextResponse.Observe.Value;
            Assert.IsTrue(oNumber > lastObserve);
            lastObserve = oNumber;

            _resource.Changed();
            Assert.IsTrue(trigger.WaitOne(1000));
            Assert.IsTrue(Code.IsSuccess(nextResponse.Code));
            Assert.AreEqual(_expected, nextResponse.ResponseText);
            Assert.IsTrue(nextResponse.HasOption(OptionType.Observe));
            Assert.IsTrue(nextResponse.Observe.HasValue);
            oNumber = nextResponse.Observe.Value;
            Assert.IsTrue(oNumber > lastObserve);
            lastObserve = oNumber;

            _resource.Changed();
            Assert.IsTrue(trigger.WaitOne(1000));
            Assert.IsTrue(Code.IsSuccess(nextResponse.Code));
            Assert.AreEqual(_expected, nextResponse.ResponseText);
            Assert.IsTrue(nextResponse.HasOption(OptionType.Observe));
            Assert.IsTrue(nextResponse.Observe.HasValue);
            oNumber = nextResponse.Observe.Value;
            Assert.IsTrue(oNumber > lastObserve);
            lastObserve = oNumber;

            _resource.Canceled(true);
            Assert.IsTrue(trigger.WaitOne(1000));
            Assert.IsTrue(!Code.IsSuccess(nextResponse.Code));
            Assert.AreEqual(null, nextResponse.ResponseText);
            Assert.IsFalse(nextResponse.HasOption(OptionType.Observe));

            _resource.Changed();
            Assert.IsFalse(trigger.WaitOne(1000));

            client = null;
        }

        [TestMethod]
        public void CheckOnCount()
        {
            int timeout = 1000;
            Uri uri = new Uri($"coap://localhost:{_serverPort}/{target1}");
            CoapClient client = new CoapClient(uri);

            AutoResetEvent trigger = new AutoResetEvent(false);
            Response nextResponse = null;
            int lastObserve = -1;

            _expected = "No Content Yet";
            CoapObserveRelation obs1 = client.Observe(response => {
                nextResponse = response;
                trigger.Set();

            });
            Assert.IsFalse(obs1.Canceled);

            Assert.IsTrue(trigger.WaitOne(timeout));
            Assert.IsTrue(Code.IsSuccess(nextResponse.Code));
            Assert.AreEqual(_expected, nextResponse.ResponseText);
            Assert.IsTrue(nextResponse.HasOption(OptionType.Observe));
            Assert.IsTrue(nextResponse.Observe.HasValue);
            int oNumber = nextResponse.Observe.Value;
            Assert.IsTrue(oNumber > lastObserve);
            lastObserve = oNumber;

            _config.NotificationCheckIntervalCount = 100;
            int conCount = 0;

            for (int i = 1; i < 210; i++) {
                _expected = $"Content for {i}";
                _resource.UpdateContent(_expected);
                Assert.IsTrue(trigger.WaitOne(timeout));

                Assert.IsTrue(Code.IsSuccess(nextResponse.Code));
                Assert.AreEqual(_expected, nextResponse.ResponseText);
                Assert.IsTrue(nextResponse.HasOption(OptionType.Observe));
                Assert.IsTrue(nextResponse.Observe.HasValue);
                oNumber = nextResponse.Observe.Value;
                Assert.IsTrue(oNumber > lastObserve);
                lastObserve = oNumber;

                conCount += nextResponse.Type == MessageType.CON ? 1 : 0;
            }

            Assert.IsTrue(conCount > 0);


        }

        [TestMethod]
        public void CheckOnTime()
        {
            Uri uri = new Uri($"coap://localhost:{_serverPort}/{target1}");
            CoapClient client = new CoapClient(uri);

            AutoResetEvent trigger = new AutoResetEvent(false);
            Response nextResponse = null;
            int lastObserve = -1;

            _expected = "No Content Yet";
            CoapObserveRelation obs1 = client.Observe(response => {
                nextResponse = response;
                trigger.Set();

            });
            Assert.IsFalse(obs1.Canceled);

            Assert.IsTrue(trigger.WaitOne(1000));
            Assert.IsTrue(Code.IsSuccess(nextResponse.Code));
            Assert.AreEqual(_expected, nextResponse.ResponseText);
            Assert.IsTrue(nextResponse.HasOption(OptionType.Observe));
            Assert.IsTrue(nextResponse.Observe.HasValue);
            int oNumber = nextResponse.Observe.Value;
            Assert.IsTrue(oNumber > lastObserve);
            lastObserve = oNumber;

            _config.NotificationCheckIntervalTime = 500;
            int count = 0;

            for (int i = 0; i < 20; i++) {
                Thread.Sleep(100);
                _expected = $"Content for {i}";
                _resource.UpdateContent(_expected);
                Assert.IsTrue(trigger.WaitOne(1000));

                Assert.IsTrue(Code.IsSuccess(nextResponse.Code));
                Assert.AreEqual(_expected, nextResponse.ResponseText);
                Assert.IsTrue(nextResponse.HasOption(OptionType.Observe));
                Assert.IsTrue(nextResponse.Observe.HasValue);
                oNumber = nextResponse.Observe.Value;
                Assert.IsTrue(oNumber > lastObserve);
                lastObserve = oNumber;

                Console.WriteLine($"lastObserve = {lastObserve},  type = {nextResponse.Type}");
                count += (nextResponse.Type == MessageType.CON) ? 1 : 0;
            }

            Assert.IsTrue(count > 0);
        }

#if false
        [TestMethod]
        public void Reregister()
        {
            Uri uri = new Uri($"coap://localhost:{_serverPort}/{target1}");
            CoapClient client = new CoapClient(uri);

            AutoResetEvent trigger = new AutoResetEvent(false);
            Response nextResponse = null;
            int lastObserve = -1;
            

            _expected = "No Content Yet";
            CoapObserveRelation obs1 = client.Observe(response => {
                nextResponse = response;
                trigger.Set();

            });
            Assert.IsFalse(obs1.Canceled);
            Assert.IsTrue(trigger.WaitOne(1000));

            int oNumber = -1;

            for (int i = 0; i < 20; i++) {
                _expected = $"Content for {i}";
                _resource.UpdateContent(_expected);
                Assert.IsTrue(trigger.WaitOne(1000));

                Assert.IsTrue(Code.IsSuccess(nextResponse.Code));
                Assert.AreEqual(_expected, nextResponse.ResponseText);
                Assert.IsTrue(nextResponse.HasOption(OptionType.Observe));
                Assert.IsTrue(nextResponse.Observe.HasValue);
                oNumber = nextResponse.Observe.Value;
                Assert.IsTrue(oNumber > lastObserve);
                lastObserve = oNumber;

                Console.WriteLine($"lastObserve = {lastObserve},  type = {nextResponse.Type}");
            }

            Request request = Request.NewGet();
            request.URI = uri;
            request.MarkObserve();
            request.Token = nextResponse.Token;

            client.Send(request);

            for (int i = 0; i < 20; i++) {
                _expected = $"Content for {i}";
                _resource.UpdateContent(_expected);
                Assert.IsTrue(trigger.WaitOne(1000));

                Assert.IsTrue(Code.IsSuccess(nextResponse.Code));
                Assert.AreEqual(_expected, nextResponse.ResponseText);
                Assert.IsTrue(nextResponse.HasOption(OptionType.Observe));
                Assert.IsTrue(nextResponse.Observe.HasValue);
                oNumber = nextResponse.Observe.Value;
                Assert.IsTrue(oNumber > lastObserve);
                lastObserve = oNumber;

                Console.WriteLine($"lastObserve = {lastObserve},  type = {nextResponse.Type}");
            }

        }
#endif


        private void CreateServer()
        {
            _config = new CoapConfig();
            _config.NonTimeout = 10; // 10 ms

            CoAPEndPoint endpoint = new CoAPEndPoint(0, _config);
            _resource = new ObserveResource(target1);
            _resource2 = new ObserveResource(target2);
            _server = new CoapServer(_config);
            _server.Add(_resource);
            _server.Add(_resource2);

            _server.AddEndPoint(endpoint);
            _server.Start();
            _serverPort = ((System.Net.IPEndPoint)endpoint.LocalEndPoint).Port;

        }

        class ObserveResource : Resource
        {
            private string _content = "No Content Yet";
            public int ObserveNo { get; set; }

            public ObserveResource(string name)
                : base(name)
            {
                Observable = true;
            }

            protected override void DoGet(CoapExchange exchange)
            {
                IEnumerable<string> queries = exchange.Request.UriQueries;
                string c = _content;

                exchange.Respond(_content);
            }

            protected override void DoPost(CoapExchange exchange)
            {
                string old = _content;
                _content = exchange.Request.PayloadString;
                exchange.Respond(StatusCode.Changed, old);
                Changed();
            }

            public void Canceled(bool notify)
            {
                if (notify) {
                    ClearAndNotifyObserveRelations(StatusCode.BadRequest);
                }
                else {
                    ClearObserveRelations();
                }
            }

            public void UpdateContent(string text)
            {
                _content = text;
                Changed();
            }
        }


    }
}
