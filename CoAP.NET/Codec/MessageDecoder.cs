/*
 * Copyright (c) 2011-2014, Longxiang He <helongxiang@smeshlink.com>,
 * SmeshLink Technology Co.
 *
 * Copyright (c) 2017-2020, Jim Schaad <ietf@augustcellars.com>
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
    /// Base class for message decoders.
    /// </summary>
    public abstract class MessageDecoder : IMessageDecoder
    {
        /// <summary>
        /// the bytes reader
        /// </summary>
        protected DatagramReader m_reader;
        /// <summary>
        /// the version of the decoding message
        /// </summary>
        protected int m_version;
        /// <summary>
        /// the type of the decoding message
        /// </summary>
        protected MessageType m_type;
        /// <summary>
        /// the length of token
        /// </summary>
        protected int m_tokenLength;
        /// <summary>
        /// the code of the decoding message
        /// </summary>
        protected int m_code;
        /// <summary>
        /// the id of the decoding message
        /// </summary>
        protected int m_id;

        /// <summary>
        /// Instantiates.
        /// </summary>
        /// <param name="data">the bytes array to decode</param>
        public MessageDecoder(byte[] data)
        {
            m_reader = new DatagramReader(data);
        }

        /// <summary>
        /// Reads protocol headers.
        /// </summary>
        protected abstract void ReadProtocol();

        /// <inheritdoc/>
        public abstract bool IsWellFormed { get; }

        /// <inheritdoc/>
        public bool IsReply => m_type == MessageType.ACK || m_type == MessageType.RST;

        /// <inheritdoc/>
        public virtual bool IsRequest =>
            m_code >= CoapConstants.RequestCodeLowerBound &&
            m_code <= CoapConstants.RequestCodeUpperBound;

        /// <inheritdoc/>
        public virtual bool IsResponse => (m_code >= CoapConstants.ResponseCodeLowerBound && m_code <= CoapConstants.ResponseCodeUpperBound) ||
            m_code == (int) SignalCode.Pong;

        /// <inheritdoc/>
        public bool IsEmpty => m_code == Code.Empty;

        /// <inheritdoc/>
        public bool IsSignal =>
            m_code >= CoapConstants.SignalCodeLowerBound && m_code <= CoapConstants.SignalCodeUpperBound;

        /// <inheritdoc/>
        public int Version => m_version;

        /// <inheritdoc/>
        public int ID => m_id;

        /// <inheritdoc/>
        public Request DecodeRequest()
        {
            System.Diagnostics.Debug.Assert(IsRequest);
            Request request = new Request((Method)m_code);
            request.Type = m_type;
            request.ID = m_id;
            ParseMessage(request);
            return request;
        }

        /// <inheritdoc/>
        public Response DecodeResponse()
        {
            System.Diagnostics.Debug.Assert(IsResponse);
            Response response = new Response((StatusCode)m_code);
            response.Type = m_type;
            response.ID = m_id;
            ParseMessage(response);
            return response;
        }

        /// <inheritdoc/>
        public EmptyMessage DecodeEmptyMessage()
        {
            System.Diagnostics.Debug.Assert(!IsRequest && !IsResponse);
            EmptyMessage message = new EmptyMessage(m_type);
            message.Type = m_type;
            message.ID = m_id;
            ParseMessage(message);
            return message;
        }

        /// <inheritdoc/>
        public SignalMessage DecodeSignal()
        {
            System.Diagnostics.Debug.Assert(IsSignal);
            SignalMessage signal = new SignalMessage((SignalCode)m_code) {
                Type = m_type,
                ID = m_id
            };
            ParseMessage(signal);
            return signal;
        }

        /// <inheritdoc/>
        public Message Decode()
        {
            if (IsRequest) {
                return DecodeRequest();
            }
            else if (IsResponse) {
                return DecodeResponse();
            }
            else if (IsEmpty) {
                return DecodeEmptyMessage();
            }
            else {
                return null;
            }
        }

        /// <summary>
        /// Parses the rest data other than protocol headers into the given message.
        /// </summary>
        /// <param name="message"></param>
        protected abstract void ParseMessage(Message message);
    }
}
