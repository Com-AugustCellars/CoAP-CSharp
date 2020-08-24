/*
 * Copyright (c) 2020, Jim Schaad <ietf@augustcellars.com>
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY.
 * 
 * This file is part of the CoAP.NET, a CoAP framework in C#.
 * Please see README for more information.
 */

using System.Net;
using Com.AugustCellars.CoAP;
using Com.AugustCellars.CoAP.Net;

namespace CoAP.Test.Std10.MockItems
{
    public class MockQueueItem
    {
        public enum QueueType
        {
            ClientSendRequest = 1,
            ClientSendRequestNetwork = 2,
            ServerSendRequestNetwork = 3,
            ServerSendRequest = 4,

            ServerSendResponse = 5,
            ServerSendResponseNetwork,
            ClientSendResponseNetwork,
            ClientSendResponse,

            ClientSendEmptyMessage = 10,
            ClientSendEmptyMessageNetwork,
            ServerSendEmptyMessageNetwork,
            ServerSendEmptyMessage,

            NetworkSend = 20
        }

        public QueueType ItemType { get; }
        public byte[] ItemData { get; }
        public Request Request { get; set; }
        public Response Response { get; set; }
        public EmptyMessage EmptyMessage { get; set; }
        public Exchange Exchange { get; }

        public EndPoint Source { get; set; }
        public EndPoint Destination { get; set; }

        public MockQueueItem(QueueType itemType, byte[] itemData)
        {
            ItemType = itemType;
            ItemData = itemData;
        }

        public MockQueueItem(QueueType itemType, Request request, Exchange exhange = null)
        {
            ItemType = itemType;
            Request = request;
            Exchange = exhange;
        }

        public MockQueueItem(QueueType itemType, Response response, Exchange exchange = null)
        {
            ItemType = itemType;
            Response = response;
            Exchange = exchange;
        }

        public MockQueueItem(QueueType itemType, EmptyMessage request, Exchange exhange = null)
        {
            ItemType = itemType;
            EmptyMessage = request;
            Exchange = exhange;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            EndPoint source = Source;
            EndPoint dest = Destination;

            if (Response != null) {
                if (source == null) {
                    source = Response.Source;
                }

                if (dest == null) {
                    dest = Response.Destination;
                }
            }
            if (Request != null) {
                if (source == null) {
                    source = Request.Source;
                }
                if (dest == null) {
                    dest = Request.Destination;
                }
            }

            if (source == null) {
                source = new IPEndPoint(IPAddress.None, 0);
            }

            if (dest == null) {
                dest = new IPEndPoint(IPAddress.None, 0);
            }

            return $"QUEUE: {ItemType} {source}=>{dest}";
        }
    }
}
