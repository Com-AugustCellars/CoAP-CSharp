/*
 * Copyright (c) 2011-2013, Longxiang He <helongxiang@smeshlink.com>,
 * SmeshLink Technology Co.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY.
 * 
 * This file is part of the CoAP.NET, a CoAP framework in C#.
 * Please see README for more information.
 */

using System;
using System.Collections.Generic;
using Com.AugustCellars.CoAP.Codec;
using Com.AugustCellars.CoAP.Log;
using Com.AugustCellars.CoAP.Util;

namespace Com.AugustCellars.CoAP
{
    public static class Spec
    {
        const Int32 Version = 1;
        const Int32 VersionBits = 2;
        const Int32 TypeBits = 2;
        const Int32 TokenLengthBits = 4;
        const Int32 CodeBits = 8;
        const Int32 IDBits = 16;
        const Int32 OptionDeltaBits = 4;
        const Int32 OptionLengthBits = 4;
        const Byte PayloadMarker = 0xFF;

        public static readonly String Name = "RFC 7252";

        public static IMessageEncoder NewMessageEncoder()
        {
            return new MessageEncoder18();
        }

        public static IMessageDecoder NewMessageDecoder(Byte[] data)
        {
            return new MessageDecoder18(data);
        }

        public static Byte[] Encode(Message msg)
        {
            return NewMessageEncoder().Encode(msg);
        }

        public static Message Decode(Byte[] bytes)
        {
            return NewMessageDecoder(bytes).Decode();
        }

        /// <summary>
        /// Returns the 4-bit option header value.
        /// </summary>
        /// <param name="optionValue">the option value (delta or length) to be encoded</param>
        /// <returns>the 4-bit option header value</returns>
        private static Int32 GetOptionNibble(Int32 optionValue)
        {
            if (optionValue <= 12)
                return optionValue;
            else if (optionValue <= 255 + 13)
                return 13;
            else if (optionValue <= 65535 + 269)
                return 14;
            else
                throw ThrowHelper.Argument("optionValue", "Unsupported option delta " + optionValue);
        }

        /// <summary>
        /// Calculates the value used in the extended option fields as specified
        /// in draft-ietf-core-coap-14, section 3.1.
        /// </summary>
        /// <param name="nibble">the 4-bit option header value</param>
        /// <param name="datagram">the datagram</param>
        /// <returns>the value calculated from the nibble and the extended option value</returns>
        private static Int32 GetValueFromOptionNibble(Int32 nibble, DatagramReader datagram)
        {
            if (nibble < 13) return nibble;
            else if (nibble == 13) return datagram.Read(8) + 13;
            else if (nibble == 14) return datagram.Read(16) + 269;
            else
                throw ThrowHelper.Argument("nibble", "Unsupported option delta " + nibble);
        }

        public class MessageEncoder18 : MessageEncoder
        {
            protected override void Serialize(DatagramWriter writer, Message msg, Int32 code)
            {
                // write fixed-size CoAP headers
                writer.Write(Version, VersionBits);
                writer.Write((Int32)msg.Type, TypeBits);
                writer.Write(msg.Token == null ? 0 : msg.Token.Length, TokenLengthBits);
                writer.Write(code, CodeBits);
                writer.Write(msg.ID, IDBits);

                // write token, which may be 0 to 8 bytes, given by token length field
                writer.WriteBytes(msg.Token);

                Int32 lastOptionNumber = 0;
                IEnumerable<Option> options = msg.GetOptions();

                foreach (Option opt in options)
                {
                    // write 4-bit option delta
                    Int32 optNum = (Int32)opt.Type;
                    Int32 optionDelta = optNum - lastOptionNumber;
                    Int32 optionDeltaNibble = GetOptionNibble(optionDelta);
                    writer.Write(optionDeltaNibble, OptionDeltaBits);

                    // write 4-bit option length
                    Int32 optionLength = opt.Length;
                    Int32 optionLengthNibble = GetOptionNibble(optionLength);
                    writer.Write(optionLengthNibble, OptionLengthBits);

                    // write extended option delta field (0 - 2 bytes)
                    if (optionDeltaNibble == 13)
                    {
                        writer.Write(optionDelta - 13, 8);
                    }
                    else if (optionDeltaNibble == 14)
                    {
                        writer.Write(optionDelta - 269, 16);
                    }

                    // write extended option length field (0 - 2 bytes)
                    if (optionLengthNibble == 13)
                    {
                        writer.Write(optionLength - 13, 8);
                    }
                    else if (optionLengthNibble == 14)
                    {
                        writer.Write(optionLength - 269, 16);
                    }

                    // write option value
                    writer.WriteBytes(opt.RawValue);

                    // update last option number
                    lastOptionNumber = optNum;
                }

                Byte[] payload = msg.Payload;
                if (payload != null && payload.Length > 0)
                {
                    // if payload is present and of non-zero length, it is prefixed by
                    // an one-byte Payload Marker (0xFF) which indicates the end of
                    // options and the start of the payload
                    writer.WriteByte(PayloadMarker);
                    writer.WriteBytes(payload);
                }
            }
        }

        public class MessageDecoder18 : MessageDecoder
        {
            public MessageDecoder18(Byte[] data)
                : base(data)
            {
                ReadProtocol();
            }

            public override Boolean IsWellFormed
            {
                get { return m_version == Version; }
            }

            protected override void ReadProtocol()
            {
                // read headers
                m_version = m_reader.Read(VersionBits);
                m_type = (MessageType)m_reader.Read(TypeBits);
                m_tokenLength = m_reader.Read(TokenLengthBits);
                m_code = m_reader.Read(CodeBits);
                m_id = m_reader.Read(IDBits);
            }

            protected override void ParseMessage(Message msg)
            {
                // read token
                if (m_tokenLength > 0) {
                    msg.Token = m_reader.ReadBytes(m_tokenLength);
                }
                else {
                    msg.Token = CoapConstants.EmptyToken;
                }

                // read options
                int currentOption = 0;
                while (m_reader.BytesAvailable) {
                    byte nextByte = m_reader.ReadNextByte();
                    if (nextByte == PayloadMarker) {
                        if (!m_reader.BytesAvailable) {
                            // the presence of a marker followed by a zero-length payload
                            // must be processed as a message format error
                            throw new InvalidOperationException();
                        }

                        msg.Payload = m_reader.ReadBytesLeft();
                        break;
                    }

                    // the first 4 bits of the byte represent the option delta
                    int optionDeltaNibble = (0xF0 & nextByte) >> 4;
                    currentOption += GetValueFromOptionNibble(optionDeltaNibble, m_reader);

                    // the second 4 bits represent the option length
                    int optionLengthNibble = (0x0F & nextByte);
                    int optionLength = GetValueFromOptionNibble(optionLengthNibble, m_reader);

                    // read option
                    Option opt = Option.Create((OptionType)currentOption);
                    opt.RawValue = m_reader.ReadBytes(optionLength);

                    msg.AddOption(opt);
                }
            }
        }
    }

    namespace Net
    {
        partial class EndPointManager
        {
            private static IEndPoint _Default;

            private static IEndPoint GetDefaultEndPoint()
            {
                if (_Default == null) {
                    lock (typeof(EndPointManager)) {
                        if (_Default == null) {
                            _Default = CreateEndPoint();
                        }
                    }
                }
                return _Default;
            }

            private static IEndPoint CreateEndPoint()
            {
                CoAPEndPoint ep = new CoAPEndPoint(0);
                ep.Start();
                return ep;
            }
        }
    }
}
