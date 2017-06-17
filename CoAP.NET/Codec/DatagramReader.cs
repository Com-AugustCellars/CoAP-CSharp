/*
 * Copyright (c) 2011-2014, Longxiang He <helongxiang@smeshlink.com>,
 * SmeshLink Technology Co.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY.
 * 
 * This file is part of the CoAP.NET, a CoAP framework in C#.
 * Please see README for more information.
 */

using System;
using System.IO;

namespace Com.AugustCellars.CoAP.Codec
{
    /// <summary>
    /// This class describes the functionality to read raw network-ordered datagrams on bit-level.
    /// </summary>
    public class DatagramReader
    {
        private readonly MemoryStream _stream;
        private byte _currentByte;
        private int _currentBitIndex;

        /// <summary>
        /// Initializes a new DatagramReader object
        /// </summary>
        /// <param name="buffer">The byte array to read from</param>
        public DatagramReader(byte[] buffer)
        {
            _stream = new MemoryStream(buffer, false);
            _currentByte = 0;
            _currentBitIndex = -1;
        }

        /// <summary>
        /// Reads a sequence of bits from the stream
        /// </summary>
        /// <param name="numBits">The number of bits to read</param>
        /// <returns>An integer containing the bits read</returns>
        public int Read(int numBits)
        {
            int bits = 0; // initialize all bits to zero
            for (int i = numBits - 1; i >= 0; i--) {
                // check whether new byte needs to be read
                if (_currentBitIndex < 0) {
                    ReadCurrentByte();
                }

                // test current bit
                bool bit = ((_currentByte >> _currentBitIndex) & 1) != 0;
                if (bit) {
                    // set bit at i-th position
                    bits |= (1 << i);
                }

                // decrease current bit index
                --_currentBitIndex;
            }
            return bits;
        }

        /// <summary>
        /// Reads a sequence of bytes from the stream
        /// </summary>
        /// <param name="count">The number of bytes to read</param>
        /// <returns>The sequence of bytes read from the stream</returns>
        public byte[] ReadBytes(int count)
        {
            // for negative count values, read all bytes left
            if (count < 0) {
                count = (int)(_stream.Length - _stream.Position);
            }

            byte[] bytes = new byte[count];

            // are there bits left to read in buffer?
            if (_currentBitIndex >= 0) {
                for (int i = 0; i < count; i++) {
                    bytes[i] = (byte)Read(8);
                }
            }
            else {
                _stream.Read(bytes, 0, bytes.Length);
            }

            return bytes;
        }

        /// <summary>
        /// Reads the next byte from the stream.
        /// </summary>
        public byte ReadNextByte()
        {
            return ReadBytes(1)[0];
        }

        /// <summary>
        /// Reads the complete sequence of bytes left in the stream
        /// </summary>
        /// <returns>The sequence of bytes left in the stream</returns>
        public byte[] ReadBytesLeft()
        {
            return ReadBytes(-1);
        }

        /// <summary>
        /// Checks if there are remaining bytes to read.
        /// </summary>
        public bool BytesAvailable
        {
            get => (_stream.Length - _stream.Position) > 0;
        }

        private void ReadCurrentByte()
        {
            int val = _stream.ReadByte();

            if (val >= 0) {
                _currentByte = (byte) val;
            }
            else {
                // EOF
                _currentByte = 0;
            }

            // reset current bit index
            _currentBitIndex = 7;
        }
    }
}
