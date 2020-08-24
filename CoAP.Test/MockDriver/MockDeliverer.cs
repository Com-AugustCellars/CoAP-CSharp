using CoAP.Test.Std10.MockItems;
using Com.AugustCellars.CoAP;
using Com.AugustCellars.CoAP.Net;

namespace CoAP.Test.Std10.MockDriver
{
    public class MockDeliverer : IMessageDeliverer
    {
        public MockMessagePump Pump { get; set; }
        public bool IsServer { get; set; }

        /// <inheritdoc />
        public void DeliverRequest(Exchange exchange)
        {
            MockQueueItem item = new MockQueueItem(MockQueueItem.QueueType.ServerSendRequest, exchange.Request, exchange);
            Pump.Queue.Enqueue(item);
        }

        /// <inheritdoc />
        public void DeliverResponse(Exchange exchange, Response response)
        {
            MockQueueItem item = new MockQueueItem(MockQueueItem.QueueType.ClientSendResponse, response, exchange);
            Pump.Queue.Enqueue(item);
        }
    }
}
