/*
 * Copyright (c) 2011-2012, Longxiang He <helongxiang@smeshlink.com>,
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

namespace Com.AugustCellars.CoAP
{
    /// <summary>
    /// This class describes the block options of the CoAP messages
    /// </summary>
    public class BlockOption : Option
    {
        /// <summary>
        /// Initializes a block option.
        /// </summary>
        /// <param name="type">The type of the option</param>
        public BlockOption(OptionType type) : base(type)
        {
            this.IntValue = 0;
        }

        /// <summary>
        /// Initializes a block option.
        /// </summary>
        /// <param name="type">The type of the option</param>
        /// <param name="num">Block number</param>
        /// <param name="szx">Block size</param>
        /// <param name="m">More flag</param>
        public BlockOption(OptionType type, int num, int szx, bool m) : base(type)
        {
            this.IntValue = Encode(num, szx, m);
        }

        /// <summary>
        /// Sets block params.
        /// </summary>
        /// <param name="num">Block number</param>
        /// <param name="szx">Block size</param>
        /// <param name="m">More flag</param>
        public void SetValue(int num, int szx, bool m)
        {
            this.IntValue = Encode(num, szx, m);
        }

        /// <summary>
        /// Gets or sets the block number.
        /// </summary>
        public int NUM
        {
            get => this.IntValue >> 4;
            set => SetValue(value, SZX, M);
        }

        /// <summary>
        /// Gets or sets the block size.
        /// </summary>
        public int SZX
        {
            get => this.IntValue & 0x7;
            set => SetValue(NUM, value, M);
        }

        /// <summary>
        /// Gets or sets the more flag.
        /// </summary>
        public bool M
        {
            get => (this.IntValue >> 3 & 0x1) != 0;
            set => SetValue(NUM, SZX, value);
        }

        /// <summary>
        /// Gets the decoded block size in bytes (B).
        /// </summary>
        public int Size => DecodeSZX(this.SZX);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string y = M ? "+" : string.Empty;
            return $"{NUM} {M} ({y}B/block [{SZX}])";
        }

        /// <summary>
        /// Gets the real block size which is 2 ^ (SZX + 4).
        /// </summary>
        /// <param name="szx"></param>
        /// <returns></returns>
        public static int DecodeSZX(int szx)
        {
            return 1 << (szx + 4);
        }

        /// <summary>
        /// Converts a block size into the corresponding SZX.
        /// </summary>
        /// <param name="blockSize"></param>
        /// <returns></returns>
        public static int EncodeSZX(int blockSize)
        {
            if (blockSize <= 16) return 0;
            if (blockSize <= 32) return 1;
            if (blockSize <= 64) return 2;
            if (blockSize <= 128) return 3;
            if (blockSize <= 256) return 4;
            if (blockSize <= 512) return 5;
            return 6;
        }

        /// <summary>
        /// Checks whether the given SZX is valid or not.
        /// </summary>
        /// <param name="szx"></param>
        /// <returns></returns>
        public static bool ValidSZX(int szx)
        {
            return (szx >= 0 && szx <= 6);
        }

        private static int Encode(int num, int szx, bool m)
        {
            int value = 0;
            value |= (szx & 0x7);
            value |= (m ? 1 : 0) << 3;
            value |= num << 4;
            return value;
        }
    }
}
