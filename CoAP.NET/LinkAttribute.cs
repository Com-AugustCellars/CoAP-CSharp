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
using System.Text;
using Com.AugustCellars.CoAP.Log;

namespace Com.AugustCellars.CoAP
{
    /// <summary>
    /// Class for linkformat attributes.
    /// 
    /// </summary>
    public class LinkAttribute : IComparable<LinkAttribute>
    {
        private static readonly ILogger _Log = LogManager.GetLogger(typeof(LinkAttribute));

        /// <summary>
        /// Initializes an attribute.
        /// </summary>
        public LinkAttribute(String name, Object value)
        {
            Name = name;
            Value = value;
        }

        /// <summary>
        /// Gets the name of this attribute.
        /// </summary>
        public String Name { get; }

        /// <summary>
        /// Gets the value of this attribute.
        /// </summary>
        public Object Value { get; }

        /// <summary>
        /// Gets the int value of this attribute.
        /// </summary>
        public Int32 IntValue
        {
            get => (Value is Int32) ? (Int32) Value : -1;
        }

        /// <summary>
        /// Gets the string value of this attribute.
        /// </summary>
        public String StringValue
        {
            get => (Value is String) ? (String)Value : null;
        }

        /// <summary>
        /// Serializes this attribute into its string representation.
        /// </summary>
        /// <param name="builder"></param>
        public void Serialize(StringBuilder builder)
        {
            // check if there's something to write
            if (Name != null && Value != null) {
                if (Value is Boolean) {
                    // flag attribute
                    if ((Boolean) Value) {
                        builder.Append(Name);
                    }
                }
                else {
                    // name-value-pair
                    builder.Append(Name);
                    builder.Append('=');
                    if (Value is String) {
                        builder.Append('"');
                        builder.Append((String) Value);
                        builder.Append('"');
                    }
                    else if (Value is Int32) {
                        builder.Append(((Int32) Value));
                    }
                    else {
                        _Log.Error(m => m("Serializing attribute of unexpected type: {0} ({1})", Name, Value.GetType().Name));
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override String ToString()
        {
            return String.Format("name: {0} value: {1}", Name, Value);
        }

        /// <inheritdoc/>
        public Int32 CompareTo(LinkAttribute other)
        {
            Int32 ret = String.Compare(Name, other.Name, StringComparison.Ordinal);
            if (ret == 0) {
                if (Value is String) {
                    return String.Compare(StringValue, other.StringValue, StringComparison.Ordinal);
                }
                else if (Value is Int32) {
                    return IntValue.CompareTo(other.IntValue);
                }
            }
            return ret;
        }
    }
}
