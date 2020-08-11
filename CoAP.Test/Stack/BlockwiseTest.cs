using System;
using System.Collections.Generic;
using System.Text;
using CoAP.Test.Std10.MockItems;
using Com.AugustCellars.CoAP;
using Com.AugustCellars.CoAP.Net;
using Com.AugustCellars.CoAP.Stack;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoAP.Test.Std10.Stack
{
 [TestClass]
 public class BlockwiseTest
    {
        static readonly string ShortPostRequest = "<Short request>";
        private static readonly string ShortPostReponse = "<Short response>";
        private static readonly string ShortGetResponse = ShortPostReponse.ToLower();
        private static string LongPostRequest;
        private static string LongPostResponse;
        private static string LongGetResponse;


        [ClassInitialize]
        public static void Setup(TestContext context)
        {
            int resourceSize = 1024 * 10 + 128;

            string blockFormat = new StringBuilder()
                .Append("/-- Post -----------------------------------------------------\\\r\n")
                .Append("|               RESOURCE BLOCK NO. {0,3} OF {1,3}                 |\r\n")
                .Append("|               [each line contains 64 bytes]                 |\r\n")
                .Append("\\-------------------------------------------------------------/\r\n")
                .ToString();

            string payload = "";
            int count = resourceSize / (64 * 4);
            for (int i = 0; i < count; i++) {
                payload += string.Format(blockFormat, i + 1, count);
            }

            LongPostRequest = payload;
            LongPostResponse = payload.ToLower();
            LongGetResponse = LongPostResponse.Replace("post", " get ");
        }

        private CoapConfig ClientConfig { get; } = new CoapConfig();
        private CoapConfig ServerConfig { get; } = new CoapConfig();


        [TestInitialize]
        public void Initialize()
        {

            ClientConfig.DefaultBlockSize = 1024;
            ClientConfig.MaxMessageSize = 1400;

            ServerConfig.DefaultBlockSize = 1024;
            ServerConfig.MaxMessageSize = 1400;

        }


        //  Set of tests to be run
        //  1.  POST short-short
        //  2. POST short-long
        //  3. POST long-short
        //  4. POST long-long
        //  5. GET short
        //  6. GET long
        //  7. multicast get
        //  8. Observe gets
        //  9. Parallel gets
        //  *. Parallel posts
        //  *. random access
        //  *. pre-negotiate size
        //  *. Different sizes on each side - server bigger
        //  *. Different sizes on each side - client bigger

        [TestMethod]
        public void BlockwiseTest1()
        {

            CommonTestCode(Method.POST, false, false, 1, 1);
        }

        [TestMethod]
        public void BlockwiseTest2()
        {

            CommonTestCode(Method.POST, false, true, 11, 11);
        }

        [TestMethod]
        public void BlockwiseTest3()
        {
            CommonTestCode(Method.POST, true, false, 11, 11);
        }

        [TestMethod]
        public void BlockwiseTest4()
        {
            CommonTestCode(Method.POST, true, true, 21, 21);
        }

        [TestMethod]
        public void BlockwiseTest5()
        {
            CommonTestCode(Method.GET, false, false, 1, 1);
        }

        [TestMethod]
        public void BlockwiseTest6()
        {
            CommonTestCode(Method.GET, false, true, 11, 11);
        }

        [TestMethod]
        public void BlockwiseTest7()
        {
            string response2 = LongGetResponse.Replace("get ", "get2");

            string responseText;

            Request r = new Request(Method.GET) {
                Destination = MockMessagePump.MulticastAddress
            };

            responseText = LongGetResponse;

            MockMessagePump pump = new MockMessagePump(new Type[] {typeof(BlockwiseLayer)}, ClientConfig, ServerConfig);

            int clientCount = 0;
            int serverCount = 0;
            int success = 0;
            int sent = 0;

            pump.SendRequest(r);
            while (pump.Pump()) {
                MockQueueItem item = pump.Queue.Peek();

                switch (item.ItemType) {
                case MockQueueItem.QueueType.ClientSendRequestNetwork:
                    clientCount += 1;
                    break;

                case MockQueueItem.QueueType.ServerSendResponseNetwork:
                    serverCount += 1;
                    break;

                case MockQueueItem.QueueType.ServerSendRequest:
                    pump.Queue.Dequeue();
                    Assert.AreEqual(0, item.Request.PayloadSize);

                    Response s = new Response(StatusCode.Content);
                    s.PayloadString = (sent == 0) ? responseText : response2;
                    sent += 1;
                    item.Exchange.EndPoint.SendResponse(item.Exchange, s);
                    break;

                case MockQueueItem.QueueType.ClientSendResponse:
                    pump.Queue.Dequeue();

                    if (item.Response.PayloadString == responseText) {
                        Assert.IsTrue((success & 1) == 0);
                        success |= 1;
                    }
                    else if (item.Response.PayloadString == response2) {
                        Assert.IsTrue((success & 2) == 0);
                        success |= 2;
                    }
                    else {
                        Assert.Fail();
                    }

                    break;
                }
            }

            Assert.AreEqual(3, success);
            Assert.AreEqual(21, clientCount);
            Assert.AreEqual(22, serverCount);
        }

        [TestMethod]
        public void BlockwiseTest8()
        {
            Observe = true;

            CommonTestCode(Method.GET, false, true, 21, 22);
        }

        [TestMethod]
        public void BlockwiseTest9()
        {
            Parallel = true;
            CommonTestCode(Method.GET, false, true, 22, 22);
        }

        private bool Observe { get; set; }
        private bool Parallel { get; set; }


        private void CommonTestCode(Method method, bool longRequest, bool longResponse, int expectedClient, int expectedServer)
        {
            string requestText = null;
            byte[] observeToken = new byte[] { 0xa, 0xa, 0xa };
            int observeNum = 0;
            string[] currentResourceContent = new string[2];

            Type[] layerTypes = new Type[] { typeof(BlockwiseLayer) };
            MockMessagePump pump = new MockMessagePump(layerTypes, ClientConfig, ServerConfig);

            Request r = new Request(method);
            if (method == Method.POST) {
                requestText = longRequest ? LongPostRequest : ShortPostRequest;
                currentResourceContent[0] = longResponse ? LongPostResponse : ShortPostReponse;
                r.PayloadString = requestText;
            }
            else {
                currentResourceContent[0] = longResponse ? LongGetResponse : ShortGetResponse;
                if (Observe) {
                    r.Observe = 1;
                    r.Token = observeToken;
                }
            }
            pump.SendRequest(r);

            if (Parallel) {
                Request r2 = new Request(method) {
                    UriPath = "/resource2"
                };
                currentResourceContent[1] = longResponse ? LongGetResponse.Replace("get ", "getA") : ShortGetResponse + "P";
                pump.SendRequest(r2);
            }

            int clientCount = 0;
            int serverCount = 0;
            int success = 0;
            Exchange observeExchange = null;

            while (pump.Pump()) {
                MockQueueItem item = pump.Queue.Peek();

                switch (item.ItemType) {
                //  Check conditions of the request when ready to transmit it on the wire
                case MockQueueItem.QueueType.ClientSendRequestNetwork:
                    if (Observe) {
                        if (item.Request.HasOption(OptionType.Observe)) {
                            Assert.IsFalse(item.Request.HasOption(OptionType.Block2));
                        }
                        else {
                            Assert.IsTrue(item.Request.Block2.NUM > 0);
                        }
                    }

                    clientCount += 1;
                    break;

                //  Check conditions of the response when ready to transmit it on the wire
                case MockQueueItem.QueueType.ServerSendResponseNetwork:
                    if (Observe) {
                        if (item.Response.HasOption(OptionType.Observe)) {
                            Assert.AreEqual(0, item.Response.Block2.NUM);
                            CollectionAssert.AreEqual(observeToken, item.Response.Token);
                        }
                        else {
                            Assert.IsTrue(item.Response.Block2.NUM > 0);
                            CollectionAssert.AreNotEqual(observeToken, item.Response.Token);
                        }
                    }
                    serverCount += 1;
                    break;

                // Server Resource is going to respond
                case MockQueueItem.QueueType.ServerSendRequest:
                    pump.Queue.Dequeue();
                    if (method == Method.POST) {
                        Assert.AreEqual(requestText, item.Request.PayloadString);
                    }
                    else {
                        Assert.AreEqual(0, item.Request.PayloadSize);
                    }

                    Response s = new Response(StatusCode.Content);
                    s.PayloadString = currentResourceContent[0];

                    if (Parallel && item.Request.UriPath == "/resource2") {
                        s.PayloadString = currentResourceContent[1];
                    }

                    if (Observe && item.Request.HasOption(OptionType.Observe)) {
                        s.Observe = 3;
                        observeExchange = item.Exchange;
                        s.Type = MessageType.NON;
                    }

                    item.Exchange.EndPoint.SendResponse(item.Exchange, s);
                    break;

                case MockQueueItem.QueueType.ClientSendResponse:
                    pump.Queue.Dequeue();

                    if (Parallel && item.Exchange.Request.UriPath == "/resource2") {
                        Assert.AreEqual(currentResourceContent[1], item.Response.PayloadString);
                        currentResourceContent[1] = currentResourceContent[0].Replace("get ", "get9");
                    }
                    else {
                        Assert.AreEqual(currentResourceContent[0], item.Response.PayloadString);
                        currentResourceContent[0] = currentResourceContent[0].Replace("get ", "get3");
                    }

                    success += 1;

                    //  For observe, send a second observe out
                    if (Observe && observeNum == 0) {
                        observeNum += 1;


                        s = new Response(StatusCode.Content) {
                            PayloadString = currentResourceContent[0], 
                            Observe = 5,
                            Type = MessageType.NON
                        };

                        List<MockStack> stacks = pump.ServerStacks[MockMessagePump.ServerAddress];
                        stacks[0].MyEndPoint.SendResponse(observeExchange, s);

                    }

                    break;
                }
            }

            if (Parallel || Observe) {
                Assert.AreEqual(2, success);
            }
            else {
                Assert.AreEqual(1, success);
            }

            Assert.AreEqual(expectedClient, clientCount);
            Assert.AreEqual(expectedServer, serverCount);
        }
    }
}
