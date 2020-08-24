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
using System.Linq;
using System.Net;
using CoAP.Test.Std10.MockItems;
using Com.AugustCellars.CoAP;
using Com.AugustCellars.CoAP.Channel;
using Com.AugustCellars.CoAP.Codec;
using Com.AugustCellars.CoAP.Net;
using Com.AugustCellars.CoAP.Server;
using Com.AugustCellars.CoAP.Server.Resources;
using Com.AugustCellars.CoAP.Stack;
using Com.AugustCellars.CoAP.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoAP.Test.Std10.MockDriver
{
    public class MockMessagePump
    {
        /// <summary>
        /// Queue of network events that are still to be processed.
        /// If the mock deliverer is used, then the delivery events will also be on the queue.
        /// </summary>
        public Queue<MockQueueItem> Queue { get; } = new Queue<MockQueueItem>();


        public MockStack ClientStack { get; set; }

        public static EndPoint ClientAddress { get; } = new IPEndPoint(0x010044c0, 5683);
        public static EndPoint ServerAddress { get; } = new IPEndPoint(0xF00044c0, 5683);
        public static EndPoint ServerAddress2 { get; } = new IPEndPoint(0xF10044c0, 5683);
        public static EndPoint MulticastAddress { get; } = new IPEndPoint(IPAddress.Parse("224.0.0.9"), 5683);

        public Dictionary<EndPoint, List<MockStack>> ServerStacks { get; } = new Dictionary<EndPoint, List<MockStack>>();
        public Dictionary<EndPoint, List<MockChannel>> ChannelsByEndpoint { get;  } = new Dictionary<EndPoint, List<MockChannel>>();

        private MockEndpoint ClientEndpoint { get; }

        public MockMessagePump()
        {

        }

        public MockMessagePump(Type[] layers, ICoapConfig configClient = null, ICoapConfig configServer = null)
        {
            if (configClient == null) {
                configClient = CoapConfig.Default;
            }

            if (configServer == null) {
                configServer = CoapConfig.Default;
            }

            ClientEndpoint = AddClientEndpoint("Client", (IPEndPoint) ClientAddress, configClient, layers, true);
            ClientStack = ServerStacks[ClientAddress].First();

            AddServerEndpoint("Server #1", (IPEndPoint) ServerAddress, configServer, layers, true);
            MockStack x = ServerStacks[ServerAddress].First();
            AddEndpointToAddress((IPEndPoint) MulticastAddress, x);

            AddServerEndpoint("Server #2", (IPEndPoint) ServerAddress2, configServer, layers, true);
            x = ServerStacks[ServerAddress2].First();
            AddEndpointToAddress((IPEndPoint) MulticastAddress, x);
        }

        public MockEndpoint AddClientEndpoint(string endpointName, IPEndPoint endPoint, ICoapConfig config, Type[] layers, bool useMockDelivery = false)
        {
            return AddEndPoint(endpointName, endPoint, config, layers, false, useMockDelivery);
        }

        public MockEndpoint AddServerEndpoint(string endpointName, IPEndPoint endPoint, ICoapConfig config, Type[] layers, bool useMockDelivery = false, IResource root = null)
        {
            return AddEndPoint(endpointName, endPoint, config, layers, true, useMockDelivery, root);
        }

        public MockEndpoint AddEndPoint(string endpointName, IPEndPoint endPoint, ICoapConfig config, Type[] layers, bool isServer, bool useMockDelivery = false, IResource root = null)
        {
            MockStack stack = new MockStack(layers, config) {
                StackName = endpointName
            };

            MockEndpoint ep = new MockEndpoint(this, stack, endPoint);
            stack.MyEndPoint = ep;

            if (useMockDelivery) {
                ep.MessageDeliverer = new MockDeliverer() {
                    IsServer = isServer,
                    Pump = this
                };
            }
            else {
                ep.MessageDeliverer = isServer ? new ServerMessageDeliverer(config, root) : (IMessageDeliverer) new ClientMessageDeliverer();
            }

            ServerStacks.Add(endPoint, new List<MockStack>() {stack});

            return ep;
        }



        public IEndPoint AddEndPoint(string endpointName, EndPoint address, ICoapConfig config, Type[] layers)
        {
            MockChannel channel = new MockChannel(address, this);
            CoAPEndPoint endpoint = new CoAPEndPoint(channel, config);

            if (layers != null) {
                CoapStack stack = endpoint.Stack;

                foreach (IEntry<ILayer, INextLayer> e in stack.GetAll().ToArray()) {
                    if (e.Name == "head" || e.Name == "tail") {
                        continue;
                    }
                    if (!layers.Contains(e.Filter.GetType())) {
                        stack.Remove(e.Filter);
                    }
                }

            }

            ChannelsByEndpoint.Add(address, new List<MockChannel>(){channel});

            return endpoint;
        }



        public void AddEndpointToAddress(IPEndPoint ipAddress, MockStack endPoint)
        {
            List<MockStack> stacks;

            if (!ServerStacks.TryGetValue(ipAddress, out stacks)) {
                stacks = new List<MockStack>();
                ServerStacks.Add(ipAddress, stacks);
            }

            stacks.Add(endPoint);
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
                item2.Destination = item.Response.Destination;
                Queue.Enqueue(item2);
                break;

            // state #7 - client receives response over network
            case MockQueueItem.QueueType.ClientSendResponseNetwork:
                Queue.Dequeue();

                serverStacks = ServerStacks[item.Destination];
                Assert.AreEqual(1, serverStacks.Count);
                foreach (MockStack s in serverStacks) {
                    s.MyEndPoint.ReceiveData(item);
                }

                break;

                case MockQueueItem.QueueType.ClientSendResponse:
                    break;

            // state #8 - client application to process response
            case MockQueueItem.QueueType.ClientSendEmptyMessageNetwork:
                Queue.Dequeue();
                encoder = new Spec.MessageEncoder18();
                encodedRequest = encoder.Encode(item.EmptyMessage);

                item2 = new MockQueueItem(MockQueueItem.QueueType.ServerSendEmptyMessageNetwork, encodedRequest);
                item2.Source = item.Source;
                item2.Destination = item.EmptyMessage.Destination;
                Queue.Enqueue(item2);
                break;

            //  state #3 - server receives network send
            case MockQueueItem.QueueType.ServerSendEmptyMessageNetwork:
                Queue.Dequeue();
                serverStacks = ServerStacks[item.Destination];
                foreach (MockStack s in serverStacks) {
                    s.MyEndPoint.ReceiveData(item);
                }
                break;

                case MockQueueItem.QueueType.NetworkSend:
                    Queue.Dequeue();
                    List<MockChannel> channels = ChannelsByEndpoint[item.Destination];

                    foreach (MockChannel c in channels) {
                        c.ReceiveData(item);
                    }

                    break;


            default:
                Assert.Fail("Unknown item in the pump");
                break;
            }

            return Queue.Count > 0;
        }
    }
}
