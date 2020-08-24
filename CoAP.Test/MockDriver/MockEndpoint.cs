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
using System.Net;
using CoAP.Test.Std10.MockItems;
using Com.AugustCellars.CoAP;
using Com.AugustCellars.CoAP.Codec;
using Com.AugustCellars.CoAP.Net;
using Com.AugustCellars.CoAP.OSCOAP;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoAP.Test.Std10.MockDriver
{
#pragma warning disable CS0067 // Event is never used

    public class MockEndpoint : IEndPoint, IOutbox
    {
        private MockMessagePump Pump { get; }
        private MockStack Stack { get; set; }
        private IMatcher Matcher { get; }
        private EndPoint MyAddress { get; }
        private ISession Session { get; set; }
        public MockEndpoint(MockMessagePump pump, MockStack stack, EndPoint myAddress)
        {
            Outbox = this;
            Pump = pump;
            Stack = stack;
            Matcher = new Matcher(CoapConfig.Default);
            Matcher.Start();
            MyAddress = myAddress;
            Session = new MockSession();
        }

        public ICoapConfig Config => throw new NotImplementedException();

        public EndPoint LocalEndPoint => throw new NotImplementedException();

        public bool Running => true;

        public IMessageDeliverer MessageDeliverer { get; set; }
        public SecurityContextSet SecurityContexts { get; set; }

        public IOutbox Outbox { get; }

        public event EventHandler<MessageEventArgs<Request>> SendingRequest;
        public event EventHandler<MessageEventArgs<Response>> SendingResponse;
        public event EventHandler<MessageEventArgs<EmptyMessage>> SendingEmptyMessage;
        public event EventHandler<MessageEventArgs<Request>> ReceivingRequest;
        public event EventHandler<MessageEventArgs<Response>> ReceivingResponse;
        public event EventHandler<MessageEventArgs<EmptyMessage>> ReceivingEmptyMessage;
        public event EventHandler<MessageEventArgs<SignalMessage>> ReceivingSignalMessage;

        public bool AddMulticastAddress(IPEndPoint ep)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void SendEmptyMessage(Exchange exchange, EmptyMessage message)
        {
            throw new NotImplementedException();
        }

        public void SendRequest(Request request)
        {
            Stack.SendRequest(request);
        }

        public void SendResponse(Exchange exchange, Response response)
        {
            response.Session = exchange.Request.Session;
            response.Destination = exchange.Request.Source;
            response.Source = MyAddress;
            MockQueueItem item = new MockQueueItem(MockQueueItem.QueueType.ServerSendResponse, response, exchange);
            item.Source = MyAddress;
            Pump.Queue.Enqueue(item);
        }

        public void Start()
        {
            return;
        }

        public void Stop()
        {
            return;
        }

        public void ReceiveData(MockQueueItem item)
        {
            IMessageDecoder decoder = Spec.NewMessageDecoder(item.ItemData);

            if (decoder.IsRequest) {
                Request request = decoder.DecodeRequest();

                request.Source = item.Source;
                request.Destination = item.Destination;

                Exchange exchange = Matcher.ReceiveRequest(request);
                exchange.EndPoint = this;

                request.Session = Session;
                
                Stack.ReceiveRequest(exchange, request);
            }
            else if (decoder.IsResponse) {
                Response response = decoder.DecodeResponse();

                response.Source = item.Source;
                response.Session = Session;
                Exchange exchange = Matcher.ReceiveResponse(response);
                if (exchange != null) {
                    exchange.EndPoint = this;
                    Stack.ReceiveResponse(exchange, response);
                }
                else if (response.Type != MessageType.ACK) {
                    Reject(response);
                }

            }
            else if (decoder.IsEmpty) {
                EmptyMessage message;

                try {
                    message = decoder.DecodeEmptyMessage();
                }
                catch (Exception ex) {
                    return;
                }

                message.Source = item.Source;

                if (!message.IsCancelled) {
                    // CoAP Ping
                    if (message.Type == MessageType.CON || message.Type == MessageType.NON) {
                        Reject(message);
                    }
                    else {
                        Exchange exchange = Matcher.ReceiveEmptyMessage(message);
                        if (exchange != null) {
                            exchange.EndPoint = this;
                            Stack.ReceiveEmptyMessage(exchange, message);
                        }
                    }
                }
            }
            else {
                Assert.Fail();
            }
        }

        private void Reject(Message message)
        {
            EmptyMessage rst = EmptyMessage.NewRST(message);

            if (!rst.IsCancelled) {
                MockQueueItem item = new MockQueueItem(MockQueueItem.QueueType.ClientSendEmptyMessageNetwork, rst);
                item.Destination = message.Destination;
                item.Source = MyAddress;
                Pump.Queue.Enqueue(item);
            }
        }

        void IOutbox.SendRequest(Exchange exchange, Request request)
        {
            Matcher.SendRequest(exchange, request);
            request.Session = Session;

            MockQueueItem item = new MockQueueItem(MockQueueItem.QueueType.ClientSendRequestNetwork, request);
            Pump.Queue.Enqueue(item);
        }

        void IOutbox.SendResponse(Exchange exchange, Response response)
        {
            if (response.Type == MessageType.Unknown) {
                response.Type = MessageType.ACK;
            }

            if (response.Token == null) {
                response.Token = exchange.CurrentRequest.Token;
            }

            if (response.ID == -1) {
                response.ID = exchange.CurrentRequest.ID;
            }

            response.Source = MyAddress;

            Matcher.SendResponse(exchange, response);

            MockQueueItem item = new MockQueueItem(MockQueueItem.QueueType.ServerSendResponseNetwork, response);
            Pump.Queue.Enqueue(item);

        }

        void IOutbox.SendEmptyMessage(Exchange exchange, EmptyMessage message)
        {
            Matcher.SendEmptyMessage(exchange, message);

            if (!message.IsCancelled) {
                MockQueueItem item = new MockQueueItem(MockQueueItem.QueueType.ClientSendEmptyMessageNetwork, message);
                item.Destination = message.Destination;
                item.Source = MyAddress;
                Pump.Queue.Enqueue(item);
            }

        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Endpoint for {Stack.StackName} {MyAddress}";
        }
    }
}

