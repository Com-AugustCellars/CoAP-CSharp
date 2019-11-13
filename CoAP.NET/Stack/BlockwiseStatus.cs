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
using System.Collections.Generic;

namespace Com.AugustCellars.CoAP.Stack
{
    /// <summary>
    /// Represents the status of a blockwise transfer of a request or a response.
    /// </summary>
    public class BlockwiseStatus
    {
        public const int NoObserve = -1;
        private readonly List<byte[]> _blocks = new List<byte[]>();

        /// <summary>
        /// Instantiates a new blockwise status.
        /// </summary>
        public BlockwiseStatus(int contentFormat)
        {
            ContentFormat = contentFormat;
        }

        /// <summary>
        /// Instantiates a new blockwise status.
        /// </summary>
        public BlockwiseStatus(int contentFormat, int num, int szx)
        {
            ContentFormat = contentFormat;
            CurrentNUM = num;
            CurrentSZX = szx;
        }

        /// <summary>
        /// Gets or sets the current num.
        /// </summary>
        public int CurrentNUM { get; set; }

        /// <summary>
        /// Gets or sets the current szx.
        /// </summary>
        public int CurrentSZX { get; set; }

        /// <summary>
        /// Gets or sets if this status is for random access.
        /// </summary>
        public bool IsRandomAccess { get; set; }

        /// <summary>
        /// Gets the initial Content-Format, which must stay the same for the whole transfer.
        /// </summary>
        public int ContentFormat { get; }

        /// <summary>
        /// Gets or sets a value indicating if this is complete.
        /// </summary>
        public bool Complete { get; set; }

        /// <summary>
        /// Get/Set the observation number
        /// </summary>
        public int Observe { get; set; } = -1;

        /// <summary>
        /// Gets the number of blocks.
        /// </summary>
        public int BlockCount
        {
            get =>_blocks.Count;
        }

        /// <summary>
        /// Gets all blocks.
        /// </summary>
        public IEnumerable<byte[]> Blocks
        {
            get =>_blocks;
        }

        /// <summary>
        /// Get the size in bytes of all blocks accumulated.
        /// </summary>
        public int BlocksByteCount { get; private set; }

        /// <summary>
        /// Adds the specified block to the current list of blocks.
        /// </summary>
        public void AddBlock(byte[] block)
        {
            if (block != null) {
                _blocks.Add(block);
                BlocksByteCount += block.Length;
            }
        }

        /// <inheritdoc/>
        public override String ToString()
        {
            return $"[CurrentNum={CurrentNUM}, CurrentSzx={CurrentSZX}, Complete={Complete}, RandomAccess={IsRandomAccess}]";
        }
    }
}
