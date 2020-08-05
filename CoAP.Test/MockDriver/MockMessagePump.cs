using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net;
using System.Text;
using CoAP.Test.Std10.OSCOAP;
using Com.AugustCellars.CoAP;
using Com.AugustCellars.CoAP.Codec;
using Com.AugustCellars.CoAP.Net;
using Com.AugustCellars.CoAP.Stack;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoAP.Test.Std10.MockItems
{
    public class MockMessagePump
    {

        public Queue<MockQueueItem> Queue = new Queue<MockQueueItem>();
        public MockStack ClientStack { get; set; }

        public static EndPoint ClientAddress { get;  } = new IPEndPoint(0x010044c0, 5683);
        public static EndPoint ServerAddress { get; } = new IPEndPoint(0xF00044c0, 5683);
        public static EndPoint ServerAddress2 { get;  } = new IPEndPoint(0xF10044c0, 5683);
        public static EndPoint MulticastAddress { get; } = new IPEndPoint(IPAddress.Parse("224.0.0.9"), 5683);

        public Dictionary<EndPoint, List<MockStack>> ServerStacks { get; } = new Dictionary<EndPoint, List<MockStack>>();

        private MockEndpoint ClientEndpoint { get; }
        private IMessageDeliverer ClientDeliverer { get; }

        public MockMessagePump(Type[] layers, ICoapConfig configClient = null, ICoapConfig configServer = null)
        {
            if (configClient == null) {
                configClient = CoapConfig.Default;
            }

            if (configServer == null) {
                configServer = CoapConfig.Default;
            }

            ClientStack = new MockStack(layers, configClient) {
                StackName = "Client"
            };

            MockStack stack = new MockStack(layers, configServer) {
                StackName = "Server #1"
            };
            ServerStacks.Add(ServerAddress, new List<MockStack>(){stack});
            stack.MyEndPoint = new MockEndpoint(this, stack, ServerAddress);
            ServerStacks.Add(MulticastAddress, new List<MockStack>(){stack});
            MockDeliverer serverDeliverer = new MockDeliverer()
            {
                IsServer = true,
                Pump = this
            };
            stack.MyEndPoint.MessageDeliverer = serverDeliverer;


            stack = new MockStack(layers, configServer) {
                StackName = "Server #2"
            };
            ServerStacks.Add(ServerAddress2, new List<MockStack>(){stack});
            stack.MyEndPoint = new MockEndpoint(this, stack, ServerAddress2);
            ServerStacks[MulticastAddress].Add(stack);
            serverDeliverer = new MockDeliverer()
            {
                IsServer = true,
                Pump = this
            };
            stack.MyEndPoint.MessageDeliverer = serverDeliverer;

            ClientEndpoint = new MockEndpoint(this, ClientStack, ClientAddress);
            ClientDeliverer = new MockDeliverer() {
                IsServer = false,
                Pump = this
            };
            ClientEndpoint.MessageDeliverer = ClientDeliverer;
        }

        private Exchange lastExchange;
        public void RegisterExchange(Request request, Exchange exchange)
        {
            if (lastExchange != null) {
                throw new Exception("Registration of exchanges is messed up.");
            }
            lastExchange = exchange;
        }

        public void SendRequest(Request request)
        {
                request.EndPoint = ClientEndpoint;
            MockQueueItem item = new MockQueueItem(MockQueueItem.QueueType.ClientSendRequest, request);
            Queue.Enqueue(item);
        }

        public void SendResponse(Response response, Exchange exchange)
        {
            MockQueueItem item = new MockQueueItem(MockQueueItem.QueueType.ServerSendResponse, response, exchange);
            Queue.Enqueue(item);
            response.Session = exchange.Request.Session;
        }

        public bool Pump()
        {
            List<MockStack> serverStacks;

            if (Queue.Count == 0) {
                return false;
            }

            MockQueueItem item = Queue.Peek();

            switch (item.ItemType) {
            //  state #1 - client requested a send of a request
            case MockQueueItem.QueueType.ClientSendRequest:
                Queue.Dequeue();
                if (item.Request.Destination == null) {
                    item.Request.Destination = ServerAddress;
                }
                ClientStack.SendRequest(item.Request);
                break;

            //  state #2 - client ready for network send
            case MockQueueItem.QueueType.ClientSendRequestNetwork:
                Queue.Dequeue();
                MessageEncoder encoder = new Spec.MessageEncoder18();
                byte[] encodedRequest = encoder.Encode(item.Request);
                EndPoint destination = item.Request.Destination;

                item = new MockQueueItem(MockQueueItem.QueueType.ServerSendRequestNetwork, encodedRequest);
                item.Source = ClientAddress;
                item.Destination = destination;
                Queue.Enqueue(item);
                    break;

            //  state #3 - server receives network send
            case MockQueueItem.QueueType.ServerSendRequestNetwork:
                Queue.Dequeue();
                serverStacks = ServerStacks[item.Destination];
                foreach (MockStack s in serverStacks) {
                    s.MyEndPoint.ReceiveData(item);
                }

                break;

            //  state #4 - server application receives request
            case MockQueueItem.QueueType.ServerSendRequest:
                break;


                // state #5 - server sends an response
                case MockQueueItem.QueueType.ServerSendResponse:
                    Queue.Dequeue();
                    serverStacks = ServerStacks[item.Source];
                    Assert.AreEqual(1, serverStacks.Count);
                    foreach (MockStack s in serverStacks) {
                        s.SendResponse(item.Exchange, item.Response);
                    }
                    break;

                // state #6 - server ready to send over network
                case MockQueueItem.QueueType.ServerSendResponseNetwork:
                    Queue.Dequeue();
                    encoder = new Spec.MessageEncoder18();
                    byte[] encodedResponse = encoder.Encode(item.Response);

                    MockQueueItem item2 = new MockQueueItem(MockQueueItem.QueueType.ClientSendResponseNetwork, encodedResponse);
                    item2.Source = item.Response.Source;
                    item2.Destination = item.Destination;
                    Queue.Enqueue(item2);
                    break;

                // state #7 - client receives response over network
                case MockQueueItem.QueueType.ClientSendResponseNetwork:
                    Queue.Dequeue();

                    ClientEndpoint.ReceiveData(item);

                    break;

                // state #8 - client application to process response
                case MockQueueItem.QueueType.ClientSendResponse:
                    break;
            }

            return Queue.Count > 0;
        }
    }
}
