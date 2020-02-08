/*
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
using System.Collections.Generic;
using System.Text;
using Com.AugustCellars.CoAP.Util;

namespace Com.AugustCellars.CoAP
{
    /// <summary>
    /// This class describes the options of the CoAP messages.
    /// </summary>
    public class Option
    {
        private static readonly IConvertor<int> int32Convertor = new Int32Convertor();
        private static readonly IConvertor<long> int64Convertor = new Int64Convertor();
        private static readonly IConvertor<string> stringConvertor = new StringConvertor();

        /// <summary>
        /// Initializes an option.
        /// </summary>
        /// <param name="type">The type of the option</param>
        protected Option(OptionType type)
        {
            Type = type;
        }

        /// <summary>
        /// Gets the type of the option.
        /// </summary>
        public OptionType Type { get; }

        /// <summary>
        /// Gets the name of the option that corresponds to its type.
        /// </summary>
        public string Name => ToString(Type);

        /// <summary>
        /// Gets the value's length in bytes of the option.
        /// </summary>
        public int Length => null == RawValue ? 0 : RawValue.Length;

        /// <summary>
        /// Gets or sets raw bytes value of the option in network byte order (big-endian).
        /// </summary>
        public byte[] RawValue { get; set; }

        /// <summary>
        /// Gets or sets string value of the option.
        /// </summary>
        public string StringValue
        {
            get => stringConvertor.Decode(RawValue);
            set
            {
                if (value == null) throw ThrowHelper.ArgumentNull("value");
                RawValue = stringConvertor.Encode(value);
            }
        }



        /// <summary>
        /// Gets or sets int value of the option.
        /// </summary>
        public int IntValue
        {
            get => int32Convertor.Decode(RawValue);
            set => RawValue = int32Convertor.Encode(value);
        }

        /// <summary>
        /// Gets or sets long value of the option.
        /// </summary>
        public long LongValue
        {
            get => int64Convertor.Decode(RawValue);
            set => RawValue = int64Convertor.Encode(value);
        }

        /// <summary>
        /// Gets the value of the option according to its type.
        /// </summary>
        public object Value
        {
            get
            {
                IConvertor convertor = GetConvertor(Type);
                return convertor?.Decode(RawValue);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the option has a default value according to the draft.
        /// </summary>
        public bool IsDefault
        {
            get {
                if (Type == OptionType.MaxAge) {
                    return IntValue == CoapConstants.DefaultMaxAge;
                }
                return false;
            }
        }

        /// <summary>
        /// Format the value based on expected type.
        /// </summary>
        /// <returns></returns>
        public string ToValueString()
        {
            switch (GetFormatByType(Type))
            {
                case OptionFormat.Integer:
                    return (Type == OptionType.Accept || Type == OptionType.ContentFormat) ?
                        ("\"" + MediaType.ToString(IntValue) + "\"") :
                        IntValue.ToString();
                case OptionFormat.String:
                    return "\"" + StringValue + "\"";
                default:
                    return ByteArrayUtils.ToHexString(RawValue);
            }
        }

        /// <summary>
        /// Returns a human-readable string representation of the option's value.
        /// </summary>
        public override string ToString()
        {
            return ToString(Type) + ": " + ToValueString();
        }

        /// <summary>
        /// Gets the hash code of this object
        /// </summary>
        /// <returns>The hash code</returns>
        public override int GetHashCode()
        {
            const int prime = 31;
            int result = (int) Type;
            result = prime * result + ByteArrayUtils.ComputeHash(RawValue);
            return result;
        }

        
        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (null == obj) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (GetType() != obj.GetType()) return false;

            Option other = (Option) obj;
            if (Type != other.Type) return false;
            if (RawValue == other.RawValue) return true;
            if (RawValue == null || other.RawValue == null) return false;
            if (RawValue.Length != other.RawValue.Length) return false;
            return Utils.AreSequenceEqualTo(RawValue, other.RawValue);
        }

        /// <summary>
        /// Creates an option.
        /// </summary>
        /// <param name="type">The type of the option</param>
        /// <returns>The new option</returns>
        public static Option Create(OptionType type)
        {
            switch (type) {
                case OptionType.Block1:
                case OptionType.Block2:
                    return new BlockOption(type);
                case OptionType.Oscoap:
                    return new OSCOAP.OscoapOption();
                default:
                    return new Option(type);
            }
        }

        /// <summary>
        /// Creates an option.
        /// </summary>
        /// <param name="type">The type of the option</param>
        /// <param name="raw">The raw bytes value of the option</param>
        /// <returns>The new option</returns>
        public static Option Create(OptionType type, byte[] raw)
        {
            Option opt = Create(type);
            opt.RawValue = raw;
            return opt;
        }

        /// <summary>
        /// Creates an option.
        /// </summary>
        /// <param name="type">The type of the option</param>
        /// <param name="str">The string value of the option</param>
        /// <returns>The new option</returns>
        public static Option Create(OptionType type, string str)
        {
            Option opt = Create(type);
            opt.StringValue = str;
            return opt;
        }

        /// <summary>
        /// Creates an option.
        /// </summary>
        /// <param name="type">The type of the option</param>
        /// <param name="val">The int value of the option</param>
        /// <returns>The new option</returns>
        public static Option Create(OptionType type, int val)
        {
            Option opt = Create(type);
            opt.IntValue = val;
            return opt;
        }

        /// <summary>
        /// Creates an option.
        /// </summary>
        /// <param name="type">The type of the option</param>
        /// <param name="val">The long value of the option</param>
        /// <returns>The new option</returns>
        public static Option Create(OptionType type, long val)
        {
            Option opt = Create(type);
            opt.LongValue = val;
            return opt;
        }

        /// <summary>
        /// Splits a string into a set of options, e.g. a uri path.
        /// </summary>
        /// <param name="type">The type of options</param>
        /// <param name="s">The string to be split</param>
        /// <param name="delimiter">The separator string</param>
        /// <returns><see cref="System.Collections.Generic.IEnumerable&lt;T&gt;"/> of options</returns>
        public static IEnumerable<Option> Split(OptionType type, string s, string delimiter)
        {
            List<Option> opts = new List<Option>();
            if (!string.IsNullOrEmpty(s)) {
                s = s.TrimStart('/');
            }
            if (!string.IsNullOrEmpty(s) &&  s.Contains("%")) {
                s = s.Replace("%22", "\"");
            }
            if (string.IsNullOrEmpty(s)) {
                return opts;
            }
            string combined = null;
            foreach (string segment in s.Split(new string[] { delimiter }, StringSplitOptions.None)) {
                // Combine segments which are quoted
                if (!string.IsNullOrEmpty(segment) && segment[0] == '"') {
                    if (segment[segment.Length - 1] == '"') {
                        opts.Add(Create(type, segment.Substring(1, segment.Length - 1)));
                        continue;
                    }
                    combined = segment.Substring(1);
                }

                if (combined != null) {
                    combined += delimiter;
                    combined += segment;
                    if (!string.IsNullOrEmpty(segment) && segment[segment.Length - 1] == '"') {
                        combined = combined.Substring(1, combined.Length - 1);
                        opts.Add(Create(type, combined));
                        combined = null;
                    }

                    continue;
                }
                // empty path segments are allowed (e.g., /test vs /test/)
                if ("/".Equals(delimiter) || !string.IsNullOrEmpty(segment)) {
                    opts.Add(Create(type, segment));
                }
            }
            return opts;
        }

        /// <summary>
        /// Joins the string values of a set of options.
        /// </summary>
        /// <param name="options">The list of options to be joined</param>
        /// <param name="delimiter">The separator string</param>
        /// <returns>The joined string</returns>
        public static string Join(IEnumerable<Option> options, string delimiter)
        {
            if (null == options) {
                return string.Empty;
            }
            else {
                StringBuilder sb = new StringBuilder();
                bool append = false;
                foreach (Option opt in options) {
                    if (append) {
                        sb.Append(delimiter);
                    }
                    else {
                        append = true;
                    }

                    sb.Append(opt.StringValue);
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Returns a string representation of the option type.
        /// </summary>
        /// <param name="type">The option type to describe</param>
        /// <returns>A string describing the option type</returns>
        public static string ToString(OptionType type)
        {
            OptionData od;
            OptionData.OptionInfoDictionary.TryGetValue(type, out od);

            if (od != null) {
                return od.OptionName;
            }


            switch (type) {
                case OptionType.Reserved:
                    return "Reserved";
                default:
                    return $"Unknown ({type})";
            }
        }

        /// <summary>
        /// Returns the option format based on the option type.
        /// </summary>
        /// <param name="type">the option type</param>
        /// <returns>the option format corresponding to the option type</returns>
        public static OptionFormat GetFormatByType(OptionType type)
        {
            OptionData od;
            OptionData.OptionInfoDictionary.TryGetValue(type, out od);

            if (od != null) {
                return od.OptionFormat;
            }

            return OptionFormat.Unknown;
        }

        /// <summary>
        /// Checks whether an option is critical.
        /// </summary>
        /// <param name="type">the option type to check</param>
        /// <returns><code>true</code> if the option is critical</returns>
        public static bool IsCritical(OptionType type)
        {
            return ((int)type & 1) > 0;
        }

        /// <summary>
        /// Checks whether an option is elective.
        /// </summary>
        /// <param name="type">the option type to check</param>
        /// <returns><code>true</code> if the option is elective</returns>
        public static bool IsElective(OptionType type)
        {
            return !IsCritical(type);
        }

        /// <summary>
        /// Checks whether an option is unsafe.
        /// </summary>
        /// <param name="type">the option type to check</param>
        /// <returns><code>true</code> if the option is unsafe</returns>
        public static bool IsUnsafe(OptionType type)
        {
            return ((int)type & 2) > 0;
        }

        /// <summary>
        /// Checks whether an option is safe.
        /// </summary>
        /// <param name="type">the option type to check</param>
        /// <returns><code>true</code> if the option is safe</returns>
        public static bool IsSafe(OptionType type)
        {
            return !IsUnsafe(type);
        }

        public static bool IsNotCacheKey(OptionType type)
        {
            return ((int) type & 0x1e) == 0x1c;
        }

        public static bool IsCacheKey(OptionType type)
        {
            return !IsNotCacheKey(type);
        }

        private static IConvertor GetConvertor(OptionType type)
        {
            OptionData od;
            OptionData.OptionInfoDictionary.TryGetValue(type, out od);
            if (od == null) {
                return null;
            }

            switch (od.OptionFormat) {
                case OptionFormat.Integer:
                    return int32Convertor;

                case OptionFormat.String:
                    return stringConvertor;

                case OptionFormat.Opaque:
                case OptionFormat.Unknown:
                default:
                    return null;
            }
        }

        private interface IConvertor
        {
            object Decode(byte[] bytes);
        }

        private interface IConvertor<T> : IConvertor
        {
            new T Decode(byte[] bytes);
            byte[] Encode(T value);
        }

        private class Int32Convertor : IConvertor<int>
        {
            public int Decode(byte[] bytes)
            {
                if (null == bytes || bytes.Length == 0) {
                    return 0;
                }

                if (BitConverter.IsLittleEndian && bytes.Length > 1) {
                    byte[] bytes2 = new byte[bytes.Length];
                    Array.Copy(bytes, bytes2, bytes.Length);
                    Array.Reverse(bytes2);
                    bytes = bytes2;
                }
                byte[] intBytes = new byte[4];
                Array.Copy(bytes, 0, intBytes, 0, bytes.Length);

                return BitConverter.ToInt32(intBytes, 0);
            }

            public byte[] Encode(int value)
            {
                byte[] bytes = BitConverter.GetBytes(value);
                if (BitConverter.IsLittleEndian) {
                    Array.Reverse(bytes);
                }

                int i;
                for (i = 0; i < bytes.Length; i++) {
                    if (bytes[i] != 0) {
                        break;
                    }
                }

                if (bytes.Length == i) {
                    return new byte[0];
                }
                byte[] returnBytes = new byte[bytes.Length-i];
                Array.Copy(bytes, i, returnBytes, 0, returnBytes.Length);

                return returnBytes;
            }

            object IConvertor.Decode(byte[] bytes)
            {
                return Decode(bytes);
            }
        }

        private class Int64Convertor : IConvertor<long>
        {
            public long Decode(byte[] bytes)
            {
                if (null == bytes) {
                    return 0;
                }

                if (BitConverter.IsLittleEndian && bytes.Length > 1) {
                    byte[] bytes2 = new byte[bytes.Length];
                    Array.Copy(bytes, bytes2, bytes.Length);
                    Array.Reverse(bytes2);
                    bytes = bytes2;
                }
                byte[] intBytes = new byte[8];
                Array.Copy(bytes, 0, intBytes, 0, bytes.Length);

                return BitConverter.ToInt64(intBytes, 0);
            }

            public byte[] Encode(long value)
            {
                byte[] bytes = BitConverter.GetBytes(value);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(bytes);
                }

                int i;
                for (i = 0; i < bytes.Length; i++) {
                    if (bytes[i] != 0) {
                        break;
                    }
                }

                byte[] returnBytes = new byte[bytes.Length - i];
                Array.Copy(bytes, i, returnBytes, 0, returnBytes.Length);

                return returnBytes;
            }

            object IConvertor.Decode(byte[] bytes)
            {
                return Decode(bytes);
            }
        }

        private class StringConvertor : IConvertor<string>
        {
            public string Decode(byte[] bytes)
            {
                return null == bytes ? null : Encoding.UTF8.GetString(bytes);
            }

            public byte[] Encode(string value)
            {
                return Encoding.UTF8.GetBytes(value);
            }

            object IConvertor.Decode(byte[] bytes)
            {
                return Decode(bytes);
            }
        }
    }
}
