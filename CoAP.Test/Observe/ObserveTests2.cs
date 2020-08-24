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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using CoAP.Test.Std10.MockDriver;
using CoAP.Test.Std10.MockItems;
using Com.AugustCellars.CoAP;
using Com.AugustCellars.CoAP.Log;
using Com.AugustCellars.CoAP.Net;
using Com.AugustCellars.CoAP.Server;
using Com.AugustCellars.CoAP.Server.Resources;
using Com.AugustCellars.CoAP.Stack;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoAP.Test.Std10.Observe
{
    [TestClass]
    public class ObserveTests2
    {
        //  Test cases:
        //  1. Normal observe 5 messages
        //  2. Proactive unobserve
        //  3. Reactive unobserve
        //  4. Out of order notifications
        //  5. End observe on an error
        //  6. Rate limiting
        //  7. CON with return from client
        //  8. CON with no return from client
        //  9. Client asks but server does not add to list
        //  10. Client re-adds to list
        //  11. Re-register based on time out
        //  12. Cancel the re-registration
        //  *. Re-registration cases
        //  *. ETags in the request
        //  *. Reset on unknown token - iff notification
        //  *. Don't cancel if not same cache key
        //  *. Weblinking - obs for /.well-known/core
        //

        [TestMethod]
        public void ObserveTest1()
        {
            CoapConfig clientConfig = new CoapConfig();

            Pump = new MockMessagePump();
            
            IEndPoint clientEndpoint = Pump.AddEndPoint("Client1", MockMessagePump.ClientAddress, clientConfig, new Type[] {typeof(ObserveLayer)});
            clientEndpoint.Start();

            CreateServer();

            CoapClient coapClient = new CoapClient {
                EndPoint = clientEndpoint, 
                Uri = new Uri($"coap://{MockMessagePump.ServerAddress}{_resource.Uri}"), 
                Timeout = 0
            };

            Response lastResponse = null;
            int count = 1;
            _resource.UpdateContent($"First string {count-1}");

            AutoResetEvent trigger = new AutoResetEvent(false);
            CoapObserveRelation relation = coapClient.ObserveAsync(
                (r) => {
                    lastResponse = r;
                    trigger.Set();
                });

            byte[] tokenBytes = relation.Request.Token;
            bool dataSent = false;
            
            while (true) {
                Thread.Sleep(1);
                if (Pump.Queue.Count > 0) {
                    MockQueueItem item = Pump.Queue.Peek();
                    switch (item.ItemType) {
                    case MockQueueItem.QueueType.NetworkSend:
                        if (item.Request != null) {
                            if (tokenBytes == null) {
                                tokenBytes = relation.Request.Token;
                            }

                            Assert.IsTrue(item.Request.HasOption(OptionType.Observe));
                            Assert.AreEqual(0, item.Request.Observe);
                            CollectionAssert.AreEqual(tokenBytes, item.Request.Token);
                        }
                        else if (item.Response != null) {
                            Assert.IsTrue(item.Response.HasOption(OptionType.Observe));
                            Assert.AreEqual(count, item.Response.Observe);
                            CollectionAssert.AreEqual(tokenBytes, item.Response.Token);
                            dataSent = true;
                        }
                        else {
                            Assert.Fail();
                        }

                        break;
                    }

                    Pump.Pump();
                }
                else  if (dataSent) {
                    Assert.IsTrue(trigger.WaitOne(1000));
                    Assert.IsTrue(lastResponse.HasOption(OptionType.Observe));
                    Assert.AreEqual(count, lastResponse.Observe);
                    dataSent = false;
                    lastResponse = null;

                    if (count < 5) {
                        _resource.UpdateContent($"New string {count}");
                        count += 1;
                    }
                    else {
                        break;
                    }
                }
            }
            Assert.AreEqual(5, count);
        }

        [TestMethod]
        public void ObserveTest2()
        {
            CoapConfig clientConfig = new CoapConfig() {
                NotificationReregistrationBackoff = 0
            };

            Pump = new MockMessagePump();

            IEndPoint clientEndpoint = Pump.AddEndPoint("Client1", MockMessagePump.ClientAddress, clientConfig, new Type[] { typeof(ObserveLayer) });
            clientEndpoint.Start();

            CreateServer();

            CoapClient coapClient = new CoapClient {
                EndPoint = clientEndpoint,
                Uri = new Uri($"coap://{MockMessagePump.ServerAddress}{_resource.Uri}"),
                Timeout = 0
            };

            Response lastResponse = null;
            int count = 1;
            int clientObserveNo = 0;
            _resource.UpdateContent($"First string {count - 1}");

            AutoResetEvent trigger = new AutoResetEvent(false);
            CoapObserveRelation relation = coapClient.ObserveAsync(
                (r) => {
                    lastResponse = r;
                    trigger.Set();
                });

            AutoResetEvent reregistered = new AutoResetEvent(false);
            relation.Request.Reregistering += (a, b) => {
                reregistered.Set();
            };

            bool dataSent = false;
            byte[] tokenBytes = relation.Request.Token;

            while (true) {
                Thread.Sleep(1);
                if (Pump.Queue.Count > 0) {
                    MockQueueItem item = Pump.Queue.Peek();
                    switch (item.ItemType) {
                    case MockQueueItem.QueueType.NetworkSend:
                        if (item.Request != null) {
                            if (tokenBytes == null) {
                                tokenBytes = relation.Request.Token;
                            }

                            Assert.IsTrue(item.Request.HasOption(OptionType.Observe));
                            Assert.AreEqual(clientObserveNo, item.Request.Observe);
                            CollectionAssert.AreEqual(tokenBytes, item.Request.Token);
                            clientObserveNo += 1;
                        }
                        else if (item.Response != null) {
                            if (count < 4) {
                                Assert.IsTrue(item.Response.HasOption(OptionType.Observe));
                                Assert.AreEqual(count, item.Response.Observe);
                            }
                            else {
                               // Assert.IsFalse(item.Response.HasOption(OptionType.Observe));
                            }

                            CollectionAssert.AreEqual(tokenBytes, item.Response.Token);
                            dataSent = true;
                        }
                        else {
                            Assert.Fail();
                        }

                        break;
                    }

                    Pump.Pump();
                }
                else if (dataSent) {
                    dataSent = false;
                    if (count > 4) {
                        Assert.IsNull(lastResponse);
                    }
                    else {
                        Assert.IsTrue(trigger.WaitOne(1000));
                        Assert.IsNotNull(lastResponse);
                        if (count == 4) {
                            Assert.IsFalse(lastResponse.HasOption(OptionType.Observe));
                        }
                        else {
                            Assert.IsTrue(lastResponse.HasOption(OptionType.Observe));
                            Assert.AreEqual(count, lastResponse.Observe);
                        }

                        lastResponse = null;
                    }

                    if (count < 5) {
                        if (count == 3) {
                            relation.ProactiveCancel();
                            _resource.UpdateContent($"New string {count}");
                        }
                        else {
                            _resource.UpdateContent($"New string {count}");
                        }
                        count += 1;
                    }
                    else {
                        break;
                    }
                }
                else if (count == 5) {
                    break;
                }
            }

            Assert.AreEqual(2, clientObserveNo);
            Assert.AreEqual(5, count);

            //  Total # of seconds is MaxAge = 1 + backoff = 0 + random(2, 15) 
            Assert.IsFalse(reregistered.WaitOne(17*1000));
        }

        [TestMethod]
        public void ObserveTest3()
        {
            CoapConfig clientConfig = new CoapConfig() {
                MaxRetransmit = 0
            };
            LogManager.Level = LogLevel.Debug;

            Pump = new MockMessagePump();

            IEndPoint clientEndpoint = Pump.AddEndPoint("Client1", MockMessagePump.ClientAddress, clientConfig, new Type[] { typeof(ObserveLayer), typeof(TokenLayer), typeof(ReliabilityLayer) });
            clientEndpoint.Start();

            CreateServer();
            _config.NonTimeout = 100;  // Change this value up - at 10 it cleans up the NON before the RST has a chance to get back.

            CoapClient coapClient = new CoapClient {
                EndPoint = clientEndpoint,
                Uri = new Uri($"coap://{MockMessagePump.ServerAddress}{_resource.Uri}"),
                Timeout = 0
            };

            Response lastResponse = null;
            int count = 1;
            _resource.UpdateContent($"First string {count - 1}");

            AutoResetEvent trigger = new AutoResetEvent(false);
            CoapObserveRelation relation = coapClient.ObserveAsync(
                (r) => {
                    lastResponse = r;
                    trigger.Set();
                });

            int emptyCount = 0;
            bool dataSent = false;
            byte[] tokenBytes = relation.Request.Token;

            while (true) {
                Thread.Sleep(1);
                if (Pump.Queue.Count > 0) {
                    MockQueueItem item = Pump.Queue.Peek();
                    switch (item.ItemType) {
                        case MockQueueItem.QueueType.NetworkSend:
                            if (item.Request != null) {
                                Debug.WriteLine($"Request: {item.Request}");
                                if (tokenBytes == null) {
                                    tokenBytes = relation.Request.Token;
                                }

                                Assert.IsTrue(item.Request.HasOption(OptionType.Observe));
                                Assert.AreEqual(0, item.Request.Observe);
                                CollectionAssert.AreEqual(tokenBytes, item.Request.Token);
                            }
                            else if (item.Response != null) {
                                Debug.WriteLine($"Response: {item.Response}");
                                Assert.IsTrue(item.Response.HasOption(OptionType.Observe));
                                Assert.AreEqual(count, item.Response.Observe);
                                CollectionAssert.AreEqual(tokenBytes, item.Response.Token);
                                dataSent = true;
                            }
                            else {
                                Debug.WriteLine($"RST: {item.EmptyMessage}");
                                emptyCount += 1;
                                dataSent = true;
                            }

                            break;
                    }

                    Pump.Pump();
                }
                else if (dataSent) {
                    dataSent = false;
                    if (count >= 3) {
                        Assert.IsNull(lastResponse);
                    }
                    else {
                        Assert.IsTrue(trigger.WaitOne(10*1000));
                        Assert.IsNotNull(lastResponse);
                        Assert.IsTrue(lastResponse.HasOption(OptionType.Observe));
                        Assert.AreEqual(count, lastResponse.Observe);
                        lastResponse = null;
                    }

                    if (count < 5) {
                        _resource.UpdateContent($"New string {count}");
                        count += 1;
                        if (count == 3) {
                            relation.ReactiveCancel();
                        }
                    }
                    else {
                        break;
                    }
                }
                else if (count == 5) {
                    break;
                }
                else if (emptyCount != 0) {
                    _resource.UpdateContent($"New string {count}");
                    count += 1;
                }
            }

            Assert.AreEqual(1, emptyCount);
            Assert.AreEqual(5, count);
        }

        [TestMethod]
        public void ObserveTest11()
        {
            CoapConfig clientConfig = new CoapConfig() {
                NotificationReregistrationBackoff = 0
            };

            Pump = new MockMessagePump();

            IEndPoint clientEndpoint = Pump.AddEndPoint("Client1", MockMessagePump.ClientAddress, clientConfig, new Type[] { typeof(ObserveLayer) });
            clientEndpoint.Start();

            CreateServer();

            CoapClient coapClient = new CoapClient
            {
                EndPoint = clientEndpoint,
                Uri = new Uri($"coap://{MockMessagePump.ServerAddress}{_resource.Uri}"),
                Timeout = 0
            };

            Response lastResponse = null;
            int count = 1;
            _resource.UpdateContent($"First string {count - 1}");

            AutoResetEvent trigger = new AutoResetEvent(false);
            CoapObserveRelation relation = coapClient.ObserveAsync(
                (r) => {
                    lastResponse = r;
                    trigger.Set();
                });

            AutoResetEvent reregistered = new AutoResetEvent(false);
            relation.Request.Reregistering += (a, b) => {
                reregistered.Set();
            };

            byte[] tokenBytes = relation.Request.Token;
            bool dataSent = false;

            while (true) {
                Thread.Sleep(1);
                if (Pump.Queue.Count > 0) {
                    MockQueueItem item = Pump.Queue.Peek();
                    switch (item.ItemType) {
                        case MockQueueItem.QueueType.NetworkSend:
                            if (item.Request != null) {
                                if (tokenBytes == null) {
                                    tokenBytes = relation.Request.Token;
                                }

                                Assert.IsTrue(item.Request.HasOption(OptionType.Observe));
                                Assert.AreEqual(0, item.Request.Observe);
                                CollectionAssert.AreEqual(tokenBytes, item.Request.Token);
                                Debug.WriteLine($"Request: ");
                            }
                            else if (item.Response != null) {
                                Assert.IsTrue(item.Response.HasOption(OptionType.Observe));
                                Assert.AreEqual(count > 4 ? count-1 : count, item.Response.Observe);
                                CollectionAssert.AreEqual(tokenBytes, item.Response.Token);
                                dataSent = true;
                                Debug.WriteLine($"Response: {item.Response.Observe}");
                            }
                            else {
                                Assert.Fail();
                            }

                            break;
                    }

                    Pump.Pump();
                }
                else if (dataSent) {
                    Assert.IsTrue(trigger.WaitOne(1000));
                    Assert.IsTrue(lastResponse.HasOption(OptionType.Observe));
                    Assert.AreEqual(count > 4 ? count - 1 : count, lastResponse.Observe);
                    dataSent = false;
                    lastResponse = null;

                    if (count == 4) {
                        //  Sleep until it fires a re-registration
                        Assert.IsTrue(reregistered.WaitOne(20*1000));
                        count += 1;
                    }
                    else if (count < 8) {
                        _resource.UpdateContent($"New string {count}");
                        count += 1;
                    }
                    else {
                        break;
                    }
                }
            }
            Assert.AreEqual(8, count);
        }

        [TestMethod]
        public void ObserveTest12()
        {
            CoapConfig clientConfig = new CoapConfig()
            {
                NotificationReregistrationBackoff = 0
            };

            Pump = new MockMessagePump();

            IEndPoint clientEndpoint = Pump.AddEndPoint("Client1", MockMessagePump.ClientAddress, clientConfig, new Type[] { typeof(ObserveLayer) });
            clientEndpoint.Start();

            CreateServer();

            CoapClient coapClient = new CoapClient
            {
                EndPoint = clientEndpoint,
                Uri = new Uri($"coap://{MockMessagePump.ServerAddress}{_resource.Uri}"),
                Timeout = 0
            };

            Response lastResponse = null;
            int count = 1;
            _resource.UpdateContent($"First string {count - 1}");

            AutoResetEvent trigger = new AutoResetEvent(false);
            CoapObserveRelation relation = coapClient.ObserveAsync(
                (r) => {
                    lastResponse = r;
                    trigger.Set();
                });

            AutoResetEvent reregistered = new AutoResetEvent(false);
            relation.Request.Reregistering += (a, b) => {
                reregistered.Set();
                b.RefreshRequest.IsCancelled = true;
            };

            byte[] tokenBytes = relation.Request.Token;
            bool dataSent = false;

            while (true) {
                Thread.Sleep(1);
                if (Pump.Queue.Count > 0) {
                    MockQueueItem item = Pump.Queue.Peek();
                    switch (item.ItemType) {
                        case MockQueueItem.QueueType.NetworkSend:
                            if (item.Request != null) {
                                if (tokenBytes == null) {
                                    tokenBytes = relation.Request.Token;
                                }

                                Assert.IsTrue(item.Request.HasOption(OptionType.Observe));
                                Assert.AreEqual(0, item.Request.Observe);
                                CollectionAssert.AreEqual(tokenBytes, item.Request.Token);
                                Debug.WriteLine($"Request: ");
                            }
                            else if (item.Response != null) {
                                Assert.IsTrue(item.Response.HasOption(OptionType.Observe));
                                Assert.AreEqual(count > 4 ? count - 1 : count, item.Response.Observe);
                                CollectionAssert.AreEqual(tokenBytes, item.Response.Token);
                                dataSent = true;
                                Debug.WriteLine($"Response: {item.Response.Observe}");
                            }
                            else {
                                Assert.Fail();
                            }

                            break;
                    }

                    Pump.Pump();
                }
                else if (dataSent) {
                    Assert.IsTrue(trigger.WaitOne(1000));
                    Assert.IsTrue(lastResponse.HasOption(OptionType.Observe));
                    Assert.AreEqual(count > 4 ? count - 1 : count, lastResponse.Observe);
                    dataSent = false;
                    lastResponse = null;

                    if (count == 4) {
                        //  Sleep until it fires a re-registration
                        Assert.IsTrue(reregistered.WaitOne(20 * 1000));
                        count += 1;
                        Assert.IsTrue(Pump.Queue.Count == 0);
                        break;
                    }
                    else if (count < 8) {
                        _resource.UpdateContent($"New string {count}");
                        count += 1;
                    }
                    else {
                        break;
                    }
                }
            }
            Assert.AreEqual(5, count);
        }

        [TestMethod]
        public void ObserveTest13()
        {
            CoapConfig clientConfig = new CoapConfig()
            {
                NotificationReregistrationBackoff = 0
            };

            Pump = new MockMessagePump();

            IEndPoint clientEndpoint = Pump.AddEndPoint("Client1", MockMessagePump.ClientAddress, clientConfig, new Type[] { typeof(ObserveLayer) });
            clientEndpoint.Start();

            CreateServer();

            CoapClient coapClient = new CoapClient
            {
                EndPoint = clientEndpoint,
                Uri = new Uri($"coap://{MockMessagePump.ServerAddress}{_resource.Uri}"),
                Timeout = 0
            };

            Response lastResponse = null;
            int count = 1;
            _resource.UpdateContent($"First string {count - 1}");

            AutoResetEvent trigger = new AutoResetEvent(false);
            CoapObserveRelation relation = coapClient.ObserveAsync(
                (r) => {
                    lastResponse = r;
                    trigger.Set();
                });

            AutoResetEvent reregistered = new AutoResetEvent(false);
            relation.Request.Reregistering += (a, b) => {
                reregistered.Set();
                b.RefreshRequest.IsCancelled = true;
            };


            byte[] tokenBytes = relation.Request.Token;
            bool dataSent = false;
            int requestNo = 0;
            int observerNo = 1;

            while (true) {
                Thread.Sleep(1);
                if (Pump.Queue.Count > 0) {
                    MockQueueItem item = Pump.Queue.Peek();
                    switch (item.ItemType) {
                        case MockQueueItem.QueueType.NetworkSend:
                            if (item.Request != null) {
                                if (tokenBytes == null) {
                                    tokenBytes = relation.Request.Token;
                                }

                                Assert.IsTrue(item.Request.HasOption(OptionType.Observe));
                                Assert.AreEqual(0, item.Request.Observe);
                                CollectionAssert.AreEqual(tokenBytes, item.Request.Token);

                                switch (requestNo) {
                                    case 0:
                                        Assert.IsFalse(item.Request.HasOption(OptionType.ETag));
                                        break;

                                    case 1:
                                    case 2:
                                        Assert.AreEqual(4, item.Request.ETags.ToArray().Length);
                                        break;
                                }

                                requestNo += 1;
                                Debug.WriteLine($"Request: {item.Request}");
                            }
                            else if (item.Response != null) {
                                Assert.IsTrue(item.Response.HasOption(OptionType.Observe));
                                Assert.AreEqual(observerNo, item.Response.Observe);
                                CollectionAssert.AreEqual(tokenBytes, item.Response.Token);
                                dataSent = true;
                                Debug.WriteLine($"Response: {item.Response} {item.Response.Observe}");
                            }
                            else {
                                Assert.Fail();
                            }

                            break;
                    }

                    Pump.Pump();
                }
                else if (dataSent) {
                    Assert.IsTrue(trigger.WaitOne(1000));
                    Assert.IsTrue(lastResponse.HasOption(OptionType.Observe));
                    Assert.AreEqual(observerNo, lastResponse.Observe);
                    dataSent = false;
                    lastResponse = null;

                    if (count == 4) {
                        relation.UpdateETags(new byte[][]{new byte[]{1}, new byte[]{3}, new byte[]{5}, new byte[]{7}});
                        count += 1;
                    }
                    else if (count == 8) {
                        Assert.IsTrue(reregistered.WaitOne(20*1000));
                        count += 1;
                    }

                    if (count == 9) {
                        count += 1;
                        _resource.UpdateContent($"New string {count}");
                        observerNo += 1;
                        count += 1;
                    }
                    else if (count < 12) {
                        _resource.UpdateContent($"New string {count}");
                        count += 1;
                        observerNo += 1;
                    }
                    else {
                        break;
                    }
                }
            }
            Assert.AreEqual(12, count);
        }

        private CoapConfig _config;
        private ObserveResource _resource;
        private ObserveResource _resource2;
        private CoapServer _server;

        private const string target1 = "Resource1";
        private const string target2 = "Resource2";

        private MockMessagePump Pump { get; set; }

        private void CreateServer()
        {
            _config = new CoapConfig {
                NonTimeout = 10             // 10 ms
            };

            _resource = new ObserveTests2.ObserveResource(target1);
            _resource2 = new ObserveTests2.ObserveResource(target2);
            _server = new CoapServer(_config);
            _server.Add(_resource);
            _server.Add(_resource2);
            IEndPoint endpoint = Pump.AddEndPoint("server #1", MockMessagePump.ServerAddress, _config, new Type[] { typeof(ObserveLayer), typeof(TokenLayer), typeof(ReliabilityLayer) });

            _server.AddEndPoint(endpoint);
            _server.Start();

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

                exchange.MaxAge = 1;
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

