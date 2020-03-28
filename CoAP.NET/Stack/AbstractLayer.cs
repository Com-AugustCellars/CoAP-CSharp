﻿/*
 * Copyright (c) 2011-2014, Longxiang He <helongxiang@smeshlink.com>,
 * SmeshLink Technology Co.
 *
 * Copyright (c) 2019-2020, Jim Schaad <ietf@augustcellars.com>
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY.
 * 
 * This file is part of the CoAP.NET, a CoAP framework in C#.
 * Please see README for more information.
 */

using Com.AugustCellars.CoAP.Net;
using Com.AugustCellars.CoAP.Threading;

namespace Com.AugustCellars.CoAP.Stack
{
    /// <summary>
    /// A partial implementation of a layer.
    /// </summary>
    public class AbstractLayer : ILayer
    {
        /// <inheritdoc/>
        public IExecutor Executor { get; set; }

        /// <inheritdoc/>
        public virtual void SendRequest(INextLayer nextLayer, Exchange exchange, Request request)
        {
            nextLayer.SendRequest(exchange, request);
        }

        /// <inheritdoc/>
        public virtual void SendResponse(INextLayer nextLayer, Exchange exchange, Response response)
        {
            nextLayer.SendResponse(exchange, response);
        }

        /// <inheritdoc/>
        public virtual bool SendEmptyMessage(INextLayer nextLayer, Exchange exchange, EmptyMessage message)
        {
            return nextLayer.SendEmptyMessage(exchange, message);
        }

        /// <inheritdoc/>
        public virtual void ReceiveRequest(INextLayer nextLayer, Exchange exchange, Request request)
        {
            nextLayer.ReceiveRequest(exchange, request);
        }

        /// <inheritdoc/>
        public virtual void ReceiveResponse(INextLayer nextLayer, Exchange exchange, Response response)
        {
            nextLayer.ReceiveResponse(exchange, response);
        }

        /// <inheritdoc/>
        public virtual void ReceiveEmptyMessage(INextLayer nextLayer, Exchange exchange, EmptyMessage message)
        {
            nextLayer.ReceiveEmptyMessage(exchange, message);
        }
    }
}
