using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using CoAP.Test.Std10.MockItems;
using Com.AugustCellars.CoAP;
using Com.AugustCellars.CoAP.Channel;
using Com.AugustCellars.CoAP.Codec;

namespace CoAP.Test.Std10.MockDriver
{
    public enum DeliveryInstrutions
    {
        Deliver = 0,
        Omit = 1,
        DeferBy1 = 9,
        DeferBy2 = 10,
        DeferBy3 = 11,
        DeferBy4 = 12,
        DeferBy5 = 13,
        DupAndDeferBy0 = 16,
        DupAndDeferBy1 = 17,
        DupAndDeferBy2 = 18,
        DupAndDeferBy3 = 19,
        DupAndDeferBy4 = 20,
        DupAndDeferBy5 = 21,
        DupAndDeferBy6 = 22
    }



    public class MockChannel : IChannel
    {
        public class TransportItem
        {
            public MockQueueItem Item { get; }
            public int DelayCount { get; set; }

            public TransportItem(MockQueueItem item, int delay)
            {
                Item = item;
                DelayCount = delay;
            }
        };

        private List<TransportItem> DelayList = new List<TransportItem>();
        public DeliveryInstrutions[] DeliveryRules { get; set; }
        private int deliveryPoint = 0;

        private MockMessagePump Pump { get; }
        private MockSession Session { get; } = new MockSession();

        public MockChannel(EndPoint localEndpoint, MockMessagePump pump)
        {
            LocalEndPoint = localEndpoint;
            Pump = pump;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public EndPoint LocalEndPoint { get; }

        /// <inheritdoc />
        public event EventHandler<DataReceivedEventArgs> DataReceived;

        /// <inheritdoc />
        public bool AddMulticastAddress(IPEndPoint ep)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void Start()
        {
            //  Nothing to do
            return;
        }

        /// <inheritdoc />
        public void Stop()
        {
            //  Nothing to do
            return;
        }

        /// <inheritdoc />
        public void Abort(ISession session)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void Release(ISession session)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void Send(byte[] data, ISession session, EndPoint ep)
        {
            MockQueueItem item = new MockQueueItem(MockQueueItem.QueueType.NetworkSend, data);
            item.Destination = ep;
            item.Source = LocalEndPoint;
            
            MessageDecoder decoder = new Spec.MessageDecoder18(data);
            if (decoder.IsRequest) {
                item.Request = decoder.DecodeRequest();
            }
            else if (decoder.IsResponse) {
                item.Response = decoder.DecodeResponse();
            }
            else if (decoder.IsEmpty) {
                item.EmptyMessage = decoder.DecodeEmptyMessage();
            }
            else {
                throw new Exception("UNKNOWN MESSAGE TYPE");
            }

            DeliveryInstrutions rule = DeliveryInstrutions.Deliver;

            if (DeliveryRules != null && deliveryPoint < DeliveryRules.Length) {
                int defer = -1;
                switch (DeliveryRules[deliveryPoint]) {
                    case DeliveryInstrutions.Deliver:
                        break;

                    case DeliveryInstrutions.DeferBy1:
                    case DeliveryInstrutions.DeferBy2:
                    case DeliveryInstrutions.DeferBy3:
                    case DeliveryInstrutions.DeferBy4:
                    case DeliveryInstrutions.DeferBy5:
                        defer = DeliveryRules[deliveryPoint] - DeliveryInstrutions.DeferBy1 + 1;
                        rule = DeliveryInstrutions.Omit;
                        break;

                    case DeliveryInstrutions.DupAndDeferBy0:
                    case DeliveryInstrutions.DupAndDeferBy1:
                    case DeliveryInstrutions.DupAndDeferBy2:
                    case DeliveryInstrutions.DupAndDeferBy3:
                    case DeliveryInstrutions.DupAndDeferBy4:
                        defer = DeliveryRules[deliveryPoint] - DeliveryInstrutions.DupAndDeferBy0;
                        break;
                }

                if (defer >= 0) {
                    int insertAt = 0;
                    foreach (TransportItem t in DelayList) {
                        if (t.DelayCount > defer) {
                            break;
                        }

                        insertAt += 1;
                    }

                    DelayList.Insert(insertAt, new TransportItem(item, defer));
                }
            }

            if (rule == DeliveryInstrutions.Deliver) {
                Pump.Queue.Enqueue(item);
            }

            if (DelayList.Count > 0) {
                int removeCount = 0;
                foreach (TransportItem t in DelayList) {
                    t.DelayCount -= 1;
                    if (t.DelayCount == 0) {
                        Pump.Queue.Enqueue(t.Item);
                        removeCount += 1;
                    }
                }

                if (removeCount > 0) {
                    DelayList.RemoveRange(0, removeCount);
                }
            }
        }

        /// <inheritdoc />
        public ISession GetSession(EndPoint ep)
        {
            return Session;
        }

        public void ReceiveData(MockQueueItem item)
        {
            DataReceivedEventArgs args = new DataReceivedEventArgs(item.ItemData, item.Source, item.Destination, Session);
            DataReceived?.Invoke(this, args);
        }
    }
}
