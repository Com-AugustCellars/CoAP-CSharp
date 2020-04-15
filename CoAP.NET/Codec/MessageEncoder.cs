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

using System;

namespace Com.AugustCellars.CoAP.Codec
{
    /// <summary>
    /// Base class for message encoders.
    /// </summary>
    public abstract class MessageEncoder : IMessageEncoder
    {
        /// <inheritdoc/>
        public byte[] Encode(Request request)
        {
            DatagramWriter writer = new DatagramWriter();
            Serialize(writer, request, request.Code);
            return writer.ToByteArray();
        }

        /// <inheritdoc/>
        public byte[] Encode(Response response)
        {
            DatagramWriter writer = new DatagramWriter();
            Serialize(writer, response, response.Code);
            return writer.ToByteArray();
        }

        /// <inheritdoc/>
        public byte[] Encode(EmptyMessage message)
        {
            DatagramWriter writer = new DatagramWriter();
            Serialize(writer, message, Code.Empty);
            return writer.ToByteArray();
        }

        /// <inheritdoc/>
        public byte[] Encode(SignalMessage message)
        {
            DatagramWriter writer = new DatagramWriter();
            Serialize(writer, message, message.Code);
            return writer.ToByteArray();
        }

        /// <inheritdoc/>
        public byte[] Encode(Message message)
        {
            if (message.IsRequest) {
                return Encode((Request)message);
            }
            else if (message.IsResponse) {
                return Encode((Response)message);
            }
            else if (message.IsSignal) {
                return Encode((SignalMessage)message);
            }
            else if (message is EmptyMessage) {
                return Encode((EmptyMessage)message);
            }
            else {
                return null;
            }
        }

        /// <summary>
        /// Serializes a message.
        /// </summary>
        /// <param name="writer">the writer</param>
        /// <param name="message">the message to write</param>
        /// <param name="code">the code</param>
        protected abstract void Serialize(DatagramWriter writer, Message message, int code);
    }
}
