using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using CoAP.Test.Std10.OSCOAP;
using Com.AugustCellars.CoAP;
using Com.AugustCellars.CoAP.Codec;
using Com.AugustCellars.CoAP.Net;
using Com.AugustCellars.CoAP.OSCOAP;
using Com.AugustCellars.CoAP.Stack;

namespace CoAP.Test.Std10.MockItems
{
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

        public bool Running => throw new NotImplementedException();

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
            throw new NotImplementedException();
        }

        public void SendResponse(Exchange exchange, Response response)
        {
            if (response.Type == MessageType.Unknown) {
                response.Type = MessageType.ACK;
            }

            response.Session = exchange.Request.Session;
            response.Destination = exchange.Request.Source;
            response.Source = MyAddress;
            MockQueueItem item = new MockQueueItem(MockQueueItem.QueueType.ServerSendResponse, response, exchange);
            item.Source = MyAddress;
            Pump.Queue.Enqueue(item);
        }

        public void Start()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
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

        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Endpoint for {Stack.StackName} {MyAddress}";
        }
    }
}

