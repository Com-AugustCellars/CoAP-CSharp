﻿/*
 * Copyright (c) 2011-2015, Longxiang He <helongxiang@smeshlink.com>,
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
using System.Net;
using System.Net.Sockets;
using Com.AugustCellars.CoAP.Util;

namespace Com.AugustCellars.CoAP
{
    /// <summary>
    /// The class Message models the base class of all CoAP messages.
    /// CoAP messages are of type <see cref="Request"/>, <see cref="Response"/>
    /// or <see cref="EmptyMessage"/>, each of which has a <see cref="MessageType"/>,
    /// a message identifier <see cref="Message.ID"/>, a token (0-8 bytes),
    /// a  collection of <see cref="Option"/>s and a payload.
    /// </summary>
    public class Message
    {
        /// <summary>
        /// Indicates that no ID has been set.
        /// </summary>
        public const int None = -1;

        private byte[] _token;
        private byte[] _payload;
        private string _payloadString;
        private readonly SortedDictionary<OptionType, LinkedList<Option>> _optionMap = new SortedDictionary<OptionType, LinkedList<Option>>();
        private bool _acknowledged;
        private bool _rejected;
        private bool _cancelled;
        private bool _timedOut;

        /// <summary>
        /// Occurs when this message is retransmitting.
        /// </summary>
        public event EventHandler Retransmitting;

        /// <summary>
        /// Occurs when this message has been acknowledged by the remote endpoint.
        /// </summary>
        public event EventHandler Acknowledged;

        /// <summary>
        /// Occurs when this message has been rejected by the remote endpoint.
        /// </summary>
        public event EventHandler Rejected;

        /// <summary>
        /// Occurs when the client stops retransmitting the message and still has
        /// not received anything from the remote endpoint.
        /// </summary>
        public event EventHandler TimedOut;

        /// <summary>
        /// Occurs when this message has been canceled.
        /// </summary>
        public event EventHandler Cancelled;

        /// <summary>
        /// Instantiates a message.
        /// </summary>
        public Message()
        { }

        /// <summary>
        /// Instantiates a message with the given type.
        /// </summary>
        /// <param name="type">the message type</param>
        public Message(MessageType type)
        {
            Type = type;
        }

        /// <summary>
        /// Instantiates a message with the given type and code.
        /// </summary>
        /// <param name="type">the message type</param>
        /// <param name="code">the message code</param>
        public Message(MessageType type, int code)
        {
            Type = type;
            Code = code;
        }

        /// <summary>
        /// Gets or sets the type of this CoAP message.
        /// </summary>
        public MessageType Type { get; set; } = MessageType.Unknown;

        /// <summary>
        /// Gets or sets the ID of this CoAP message.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public int ID { get; set; } = None;

        /// <summary>
        /// Gets the code of this CoAP message.
        /// </summary>
        public int Code { get; set;  }

        /// <summary>
        /// Gets the code's string representation of this CoAP message.
        /// </summary>
        public string CodeString => CoAP.Code.ToString(Code);

        /// <summary>
        /// Gets a value that indicates whether this CoAP message is a request message.
        /// </summary>
        public bool IsRequest => CoAP.Code.IsRequest(Code);

        /// <summary>
        /// Gets a value that indicates whether this CoAP message is a response message.
        /// </summary>
        public bool IsResponse => CoAP.Code.IsResponse(Code);

        public bool IsSignal => CoAP.Code.IsSignal(Code);

        /// <summary>
        /// Gets or sets the 0-8 byte token.
        /// </summary>
        public byte[] Token
        {
            get => _token;
            set
            {
                if (value != null && value.Length > 8) {
                    throw new ArgumentException("Token length must be between 0 and 8 inclusive.", nameof(value));
                }

                _token = value;
            }
        }

        /// <summary>
        /// Gets the token represented as a string.
        /// </summary>
        public string TokenString => _token == null ? null : ByteArrayUtils.ToHexString(_token);

        /// <summary>
        /// Gets or sets the destination endpoint.
        /// </summary>
        public System.Net.EndPoint Destination
        {
            get=>_destination;
            set {
                IPEndPoint ip = (IPEndPoint) value;
                if (ip != null && ip.AddressFamily == AddressFamily.InterNetwork) {
                    if ((ip.Address.GetAddressBytes()[0] & 0xf0) == 0xe0) {
                        IsMulticast = true;
                    }
                    else {
                        IsMulticast = false;
                    }
                }
                else if (ip != null && ip.AddressFamily == AddressFamily.InterNetworkV6) {
                    IsMulticast = ip.Address.IsIPv6Multicast;
                }

                _destination = value;
            }
        }

        private System.Net.EndPoint _destination;
        protected bool IsMulticast;

        /// <summary>
        /// Gets or sets the source endpoint.
        /// </summary>
        public System.Net.EndPoint Source { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this message has been acknowledged.
        /// </summary>
        public bool IsAcknowledged
        {
            get => _acknowledged;
            set
            {
                _acknowledged = value;
                if (value) {
                    OnAcknowledged();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this message has been rejected.
        /// </summary>
        public bool IsRejected
        {
            get => _rejected;
            set
            {
                _rejected = value;
                if (value) {
                    OnRejected();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether this CoAP message has timed out.
        /// Confirmable messages in particular might timeout.
        /// </summary>
        public bool IsTimedOut
        {
            get => _timedOut;
            set
            {
                _timedOut = value;
                if (value) {
                    OnTimedOut();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether this CoAP message is canceled.
        /// </summary>
        public bool IsCancelled
        {
            get => _cancelled;
            set
            {
                _cancelled = value;
                if (value) {
                    OnCanceled();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this message is a duplicate.
        /// </summary>
        public bool Duplicate { get; set; }

        /// <summary>
        /// Gets or sets the serialized message as byte array, or null if not serialized yet.
        /// </summary>
        public byte[] Bytes { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this message has been received or sent,
        /// or <see cref="DateTime.MinValue"/> if neither has happened yet.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the max times this message should be retransmitted if no ACK received.
        /// A value of 0 means that the <see cref="ICoapConfig.MaxRetransmit"/>
        /// should be taken into account, while a negative means NO retransmission.
        /// The default value is 0.
        /// </summary>
        public int MaxRetransmit { get; set; }

        /// <summary>
        /// Gets or sets the amount of time in milliseconds after which this message will time out.
        /// A value of 0 indicates that the time should be decided automatically,
        /// while a negative that never time out. The default value is 0.
        /// </summary>
        public int AckTimeout { get; set; }

        /// <summary>
        /// Gets the size of the payload of this CoAP message.
        /// </summary>
        public int PayloadSize => (null == _payload) ? 0 : _payload.Length;

        /// <summary>
        /// Gets or sets the payload of this CoAP message.
        /// </summary>
        public byte[] Payload
        {
            get => _payload;
            set { _payload = value; _payloadString = null; }
        }

        /// <summary>
        /// Gets or sets the payload of this CoAP message in string representation.
        /// </summary>
        public string PayloadString
        {
            get
            {
                if (_payload == null) {
                    return null;
                }
                else if (_payloadString == null) {
                    _payloadString = System.Text.Encoding.UTF8.GetString(_payload);
                }

                return _payloadString;
            }
            set => SetPayload(value, MediaType.TextPlain);
        }

        /// <summary>
        /// Sets the payload of this CoAP message.
        /// </summary>
        /// <param name="payload">The string representation of the payload</param>
        public Message SetPayload(string payload)
        {
            if (payload == null) {
                payload = string.Empty;
            }

            Payload = System.Text.Encoding.UTF8.GetBytes(payload);
            _payloadString = payload;
            return this;
        }

        /// <summary>
        /// Sets the payload of this CoAP message.
        /// </summary>
        /// <param name="payload">The string representation of the payload</param>
        /// <param name="mediaType">The content-type of the payload</param>
        public Message SetPayload(string payload, int mediaType)
        {
            if (payload == null) {
                payload = string.Empty;
            }

            Payload = System.Text.Encoding.UTF8.GetBytes(payload);
            _payloadString = payload;
            ContentType = mediaType;
            return this;
        }

        /// <summary>
        /// Sets the payload of this CoAP message.
        /// </summary>
        /// <param name="payload">the payload bytes</param>
        /// <param name="mediaType">the content-type of the payload</param>
        public Message SetPayload(byte[] payload, int mediaType)
        {
            Payload = payload;
            ContentType = mediaType;
            return this;
        }

        /// <summary>
        /// Cancels this message.
        /// </summary>
        public void Cancel()
        {
            IsCancelled = true;
        }

        /// <summary>
        /// Called when being acknowledged.
        /// </summary>
        protected virtual void OnAcknowledged()
        {
            Fire(Acknowledged);
        }

        /// <summary>
        /// Called when being rejected.
        /// </summary>
        protected virtual void OnRejected()
        {
            Fire(Rejected);
        }

        /// <summary>
        /// Called when being timed out.
        /// </summary>
        protected virtual void OnTimedOut()
        {
            Fire(TimedOut);
        }

        /// <summary>
        /// Called when being canceled.
        /// </summary>
        protected virtual void OnCanceled()
        {
            Fire(Cancelled);
        }

        internal void FireRetransmitting()
        {
            Fire(Retransmitting);
        }

        private void Fire(EventHandler handler)
        {
            if (handler != null) {
                handler(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// To string.
        /// </summary>
        public override string ToString()
        {
            string payload = PayloadString;
            if (payload == null) {
                payload = "[no payload]";
            }
            else {
                int len = payload.Length, nl = payload.IndexOf('\n');
                if (nl >= 0) {
                    payload = payload.Substring(0, nl);
                }

                if (payload.Length > 24) {
                    payload = payload.Substring(0, 24);
                }

                payload = "\"" + payload + "\"";
                if (payload.Length != len + 2) {
                    payload += "... " + PayloadSize + " bytes";
                }
            }

            return $"{Type}-{CoAP.Code.ToString(Code)} ID={ID}, Token={TokenString}, Options=[{Utils.OptionsToString(this)}], {payload}";
        }

        /// <summary>
        /// Equals.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (object.ReferenceEquals(this, obj)) return true;
            if (GetType() != obj.GetType()) return false;

            Message other = (Message)obj;
            if (Type != other.Type) return false;
            if (Code != other.Code) return false;
            if (ID != other.ID) return false;
            if (_optionMap == null) {
                if (other._optionMap != null) return false;
            }
            else if (!_optionMap.Equals(other._optionMap))  return false;

            if (!Utils.AreSequenceEqualTo(_payload, other._payload)) return false;
            return true;
        }

        /// <summary>
        /// Get hash code.
        /// </summary>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        internal virtual void CopyEventHandler(Message src)
        {
            ForEach(src.Retransmitting, h => this.Retransmitting += h);
            ForEach(src.Acknowledged, h => this.Acknowledged += h);
            ForEach(src.Rejected, h => this.Rejected += h);
            ForEach(src.TimedOut, h => this.TimedOut += h);
            ForEach(src.Cancelled, h => this.Cancelled += h);
        }

        internal static void ForEach(EventHandler src, Action<EventHandler> action)
        {
            if (src == null) {
                return;
            }

            foreach (Delegate item in src.GetInvocationList()) {
                action((EventHandler)item);
            }
        }

        internal static void ForEach<TEventArgs>(EventHandler<TEventArgs> src,
            Action<EventHandler<TEventArgs>> action) where TEventArgs : EventArgs
        {
            if (src == null) return;
            foreach (Delegate item in src.GetInvocationList()) {
                action((EventHandler<TEventArgs>)item);
            }
        }

        #region Options

        /// <summary>
        /// Gets If-Match options.
        /// </summary>
        public IEnumerable<byte[]> IfMatches => SelectOptions(OptionType.IfMatch, o => o.RawValue);

        /// <summary>
        /// Checks if a value is matched by the IfMatch options.
        /// If no IfMatch options exist, then return true.
        /// </summary>
        /// <param name="what">ETag value to check</param>
        /// <returns>what is in the IfMatch list</returns>
        public bool IsIfMatch(byte[] what)
        {
            IEnumerable<Option> ifmatches = GetOptions(OptionType.IfMatch);
            if (ifmatches == null) {
                return true;
            }

            using (IEnumerator<Option> it = ifmatches.GetEnumerator()) {
                if (!it.MoveNext()) {
                    return true;
                }

                do {
                    if (Utils.AreSequenceEqualTo(what, it.Current.RawValue)) {
                        return true;
                    }
                } while (it.MoveNext());
            }
            return false;
        }

        /// <summary>
        /// Add an If-Match option with an ETag
        /// </summary>
        /// <param name="opaque">ETag to add</param>
        /// <returns>Current message</returns>
        public Message AddIfMatch(byte[] opaque)
        {
            if (opaque == null) {
                throw ThrowHelper.ArgumentNull("opaque");
            }

            if (opaque.Length > 8) {
                throw ThrowHelper.Argument("opaque", "Content of If-Match option is too large: " + ByteArrayUtils.ToHexString(opaque));
            }

            return AddOption(Option.Create(OptionType.IfMatch, opaque));
        }

        /// <summary>
        /// Remove an If-Match option from the message
        /// </summary>
        /// <param name="opaque">ETag value to remove</param>
        /// <returns>Current message</returns>
        public Message RemoveIfMatch(byte[] opaque)
        {
            LinkedList<Option> list = GetOptions(OptionType.IfMatch) as LinkedList<Option>;
            if (list != null) {
                Option opt = Utils.FirstOrDefault(list, o => Utils.AreSequenceEqualTo(opaque, o.RawValue));
                if (opt != null) {
                    list.Remove(opt);
                }
            }
            return this;
        }

        /// <summary>
        /// Remove all If-Match options from the message
        /// </summary>
        /// <returns>Current message</returns>
        public Message ClearIfMatches()
        {
            RemoveOptions(OptionType.IfMatch);
            return this;
        }

        /// <summary>
        /// Return all ETags on the message
        /// </summary>
        public IEnumerable<byte[]> ETags
        {
            get { return SelectOptions(OptionType.ETag, o => o.RawValue); }
        }

        /// <summary>
        /// Does the message contain a specific ETag option value?
        /// </summary>
        /// <param name="what">EETag value to check for</param>
        /// <returns>true if present</returns>
        public bool ContainsETag(byte[] what)
        {
            return Utils.Contains(GetOptions(OptionType.ETag), o => Utils.AreSequenceEqualTo(what, o.RawValue));
        }

        /// <summary>
        /// Add an ETag option to the message
        /// </summary>
        /// <param name="opaque">ETag to add</param>
        /// <returns>Current Message</returns>
        public Message AddETag(byte[] opaque)
        {
            if (opaque == null) {
                throw ThrowHelper.ArgumentNull("opaque");
            }

            return AddOption(Option.Create(OptionType.ETag, opaque));
        }

        /// <summary>
        /// Remove an ETag option from a message
        /// </summary>
        /// <param name="opaque">ETag to be removed</param>
        /// <returns>Current message</returns>
        public Message RemoveETag(byte[] opaque)
        {
            LinkedList<Option> list = GetOptions(OptionType.ETag) as LinkedList<Option>;
            if (list != null) {
                Option opt = Utils.FirstOrDefault(list, o => Utils.AreSequenceEqualTo(opaque, o.RawValue));
                if (opt != null) {
                    list.Remove(opt);
                }
            }
            return this;
        }
        
        /// <summary>
        /// Clear all ETags from a message
        /// </summary>
        /// <returns>Current message</returns>
        public Message ClearETags()
        {
            RemoveOptions(OptionType.ETag);
            return this;
        }

        /// <summary>
        /// Get - Does the message have an IfNoneMatch option?
        /// Set - Set or clear IfNoneMatch option to value
        /// </summary>
        public bool IfNoneMatch
        {
            get => HasOption(OptionType.IfNoneMatch);
            set
            {
                if (value) {
                    SetOption(Option.Create(OptionType.IfNoneMatch));
                }
                else {
                    RemoveOptions(OptionType.IfNoneMatch);
                }
            }
        }

        /// <summary>
        /// Get/Set URI host option
        /// </summary>
        public string UriHost
        {
            get
            {
                Option host = GetFirstOption(OptionType.UriHost);
                return host == null ? null : host.StringValue;
            }
            set
            {
                if (value == null) {
                    throw new ArgumentNullException(nameof(value));
                }
                else if (value.Length < 1 || value.Length > 255) {
                    throw new ArgumentException("URI-Host option's length must be between 1 and 255 inclusive", nameof(value));
                }

                SetOption(Option.Create(OptionType.UriHost, value));
            }
        }

        /// <summary>
        /// Get/Set the UriPath options
        /// </summary>
        public string UriPath
        {
            get { return "/" + Option.Join(GetOptions(OptionType.UriPath), "/"); }
            set { SetOptions(Option.Split(OptionType.UriPath, value, "/")); }
        }

        /// <summary>
        /// Get the UriPath as an enumeration
        /// </summary>
        public IEnumerable<string> UriPaths
        {
            get
            {
                IEnumerable<Option> opts = GetOptions(OptionType.UriPath);
                if (opts != null) {
                    foreach (Option opt in opts) {
                        yield return opt.StringValue;
                    }
                }
            }
        }

        /// <summary>
        /// Add Uri Path element corresponding to the path given
        /// </summary>
        /// <param name="path">Path element</param>
        /// <returns>Current Message</returns>
        public Message AddUriPath(string path)
        {
            if (path == null) {
                throw ThrowHelper.ArgumentNull("path");
            }

            if (path.Length > 255) {
                throw ThrowHelper.Argument("path", "Uri Path option's length must be between 0 and 255 inclusive");
            }

            return AddOption(Option.Create(OptionType.UriPath, path));
        }

        /// <summary>
        /// Remove path element from options
        /// </summary>
        /// <param name="path">Current message</param>
        /// <returns></returns>
        public Message RemoveUriPath(string path)
        {
            LinkedList<Option> list = GetOptions(OptionType.UriPath) as LinkedList<Option>;
            if (list != null) {
                Option opt = Utils.FirstOrDefault(list, o => string.Equals(path, o.StringValue));
                if (opt != null) {
                    list.Remove(opt);
                }
            }
            return this;
        }

        /// <summary>
        /// Remove all Uri Path options
        /// </summary>
        /// <returns>Current message</returns>
        public Message ClearUriPath()
        {
            RemoveOptions(OptionType.UriPath);
            return this;
        }

        /// <summary>
        /// Get/Set UriQuery properties
        /// </summary>
        public string UriQuery
        {
            get => Option.Join(GetOptions(OptionType.UriQuery), "&");
            set
            {
                if (!string.IsNullOrEmpty(value) && value.StartsWith("?")) {
                    value = value.Substring(1);
                }

                SetOptions(Option.Split(OptionType.UriQuery, value, "&"));
            }
        }

        /// <summary>
        /// Get enumeration of all UriQuery properties
        /// </summary>
        public IEnumerable<string> UriQueries
        {
            get
            {
                IEnumerable<Option> opts = GetOptions(OptionType.UriQuery);
                if (opts != null) {
                    foreach (Option opt in opts) {
                        yield return opt.StringValue;
                    }
                }
            }
        }

        /// <summary>
        /// Add one Uri Query option
        /// </summary>
        /// <param name="query">query to add</param>
        /// <returns>Current Message</returns>
        public Message AddUriQuery(string query)
        {
            if (query == null) {
                throw ThrowHelper.ArgumentNull("query");
            }

            if (query.Length > 255) {
                throw ThrowHelper.Argument("query", "Uri Query option's length must be between 0 and 255 inclusive");
            }

            return AddOption(Option.Create(OptionType.UriQuery, query));
        }

        /// <summary>
        /// Remove first occurance of URI Query
        /// </summary>
        /// <param name="query">Query to remove</param>
        /// <returns>Current message</returns>
        public Message RemoveUriQuery(string query)
        {
            LinkedList<Option> list = GetOptions(OptionType.UriQuery) as LinkedList<Option>;
            if (list != null) {
                Option opt = Utils.FirstOrDefault(list, o => string.Equals(query, o.StringValue));
                if (opt != null) {
                    list.Remove(opt);
                }
            }
            return this;
        }

        /// <summary>
        /// Remove all Uri Query options
        /// </summary>
        /// <returns>Current message</returns>
        public Message ClearUriQuery()
        {
            RemoveOptions(OptionType.UriQuery);
            return this;
        }

        /// <summary>
        /// Get/Set the UriPort option
        /// </summary>
        public int UriPort
        {
            get
            {
                Option opt = GetFirstOption(OptionType.UriPort);
                return opt == null ? 0 : opt.IntValue;
            }
            set
            {
                if (value == 0) {
                    RemoveOptions(OptionType.UriPort);
                }
                else {
                    SetOption(Option.Create(OptionType.UriPort, value));
                }
            }
        }

        /// <summary>
        /// Return location path and query options
        /// </summary>
        public string Location
        {
            get
            {
                string path = "/" + LocationPath, query = LocationQuery;
                if (!string.IsNullOrEmpty(query)) {
                    path += "?" + query;
                }

                return path;
            }
        }

        /// <summary>
        /// Gets or set the location-path of this CoAP message.
        /// </summary>
        public string LocationPath
        {
            get => Option.Join(GetOptions(OptionType.LocationPath), "/");
            set => SetOptions(Option.Split(OptionType.LocationPath, value, "/"));
        }

        /// <summary>
        /// Return enumeration of all Location Path options
        /// </summary>
        public IEnumerable<string> LocationPaths
        {
            get { return SelectOptions(OptionType.LocationPath, o => o.StringValue); }
        }

        /// <summary>
        /// Add a Location Path option
        /// </summary>
        /// <param name="path">option to add</param>
        /// <returns>Current message</returns>
        public Message AddLocationPath(string path)
        {
            if (path == null) {
                throw ThrowHelper.ArgumentNull("path");
            }

            if (path.Length > 255) {
                throw ThrowHelper.Argument("path", "Location Path option's length must be between 0 and 255 inclusive");
            }

            return AddOption(Option.Create(OptionType.LocationPath, path));
        }

        /// <summary>
        /// Remove specified location path element
        /// </summary>
        /// <param name="path">Element to remove</param>
        /// <returns>Current message</returns>
        public Message RemoveLocationPath(string path)
        {
            LinkedList<Option> list = GetOptions(OptionType.LocationPath) as LinkedList<Option>;
            if (list != null) {
                Option opt = Utils.FirstOrDefault(list, o => string.Equals(path, o.StringValue));
                if (opt != null) {
                    list.Remove(opt);
                }
            }
            return this;
        }

        /// <summary>
        /// Clear all Location-Path options from the message
        /// </summary>
        /// <returns>Current Message</returns>
        public Message ClearLocationPath()
        {
            RemoveOptions(OptionType.LocationPath);
            return this;
        }

        /// <summary>
        /// Return all Location-Query options
        /// </summary>
        public string LocationQuery
        {
            get => Option.Join(GetOptions(OptionType.LocationQuery), "&");
            set
            {
                if (!string.IsNullOrEmpty(value) && value.StartsWith("?")) {
                    value = value.Substring(1);
                }

                SetOptions(Option.Split(OptionType.LocationQuery, value, "&"));
            }
        }

        /// <summary>
        /// Return enumerator of all Location-Query options
        /// </summary>
        public IEnumerable<string> LocationQueries
        {
            get => SelectOptions(OptionType.LocationQuery, o => o.StringValue);
        }

        /// <summary>
        /// Add a Location-Query option
        /// </summary>
        /// <param name="query">query element to add</param>
        /// <returns>Current message</returns>
        public Message AddLocationQuery(string query)
        {
            if (query == null) {
                throw ThrowHelper.ArgumentNull("query");
            }

            if (query.Length > 255) {
                throw ThrowHelper.Argument("query", "Location Query option's length must be between 0 and 255 inclusive");
            }

            return AddOption(Option.Create(OptionType.LocationQuery, query));
        }

        /// <summary>
        /// Remove a given Location-Query from the message
        /// </summary>
        /// <param name="query">query to remove</param>
        /// <returns>Current message</returns>
        public Message RemoveLocationQuery(string query)
        {
            LinkedList<Option> list = GetOptions(OptionType.LocationQuery) as LinkedList<Option>;
            if (list != null) {
                Option opt = Utils.FirstOrDefault(list, o => string.Equals(query, o.StringValue));
                if (opt != null) {
                    list.Remove(opt);
                }
            }
            return this;
        }

        /// <summary>
        /// Remove all Location-Query options
        /// </summary>
        /// <returns>Current message</returns>
        public Message ClearLocationQuery()
        {
            RemoveOptions(OptionType.LocationQuery);
            return this;
        }

        /// <summary>
        /// Gets or sets the content-type of this CoAP message.
        /// </summary>
        public int ContentType
        {
            get
            {
                Option opt = GetFirstOption(OptionType.ContentType);
                return (null == opt) ? MediaType.Undefined : opt.IntValue;
            }
            set
            {
                if (value == MediaType.Undefined) {
                    RemoveOptions(OptionType.ContentType);
                }
                else {
                    SetOption(Option.Create(OptionType.ContentType, value));
                }
            }
        }

        /// <summary>
        /// Gets or sets the content-format of this CoAP message,
        /// same as ContentType, only another name.
        /// </summary>
        public int ContentFormat
        {
            get => ContentType;
            set => ContentType = value;
        }

        /// <summary>
        /// Gets or sets the max-age of this CoAP message.
        /// </summary>
        public long MaxAge
        {
            get
            {
                Option opt = GetFirstOption(OptionType.MaxAge);
                return (null == opt) ? CoapConstants.DefaultMaxAge : opt.LongValue;
            }
            set
            {
                if (value < 0 || value > uint.MaxValue) {
                    throw ThrowHelper.Argument("value", "Max-Age option must be between 0 and " + uint.MaxValue + " (4 bytes) inclusive");
                }

                SetOption(Option.Create(OptionType.MaxAge, value));
            }
        }

        /// <summary>
        /// Get first Accept option
        /// Set - add additional options or remove all for MediaType.Undefined
        /// </summary>
        public int Accept
        {
            get
            {
                Option opt = GetFirstOption(OptionType.Accept);
                return opt == null ? MediaType.Undefined : opt.IntValue;
            }
            set
            {
                if (value == MediaType.Undefined) {
                    RemoveOptions(OptionType.Accept);
                }
                else {
                    SetOption(Option.Create(OptionType.Accept, value));
                }
            }
        }

        /// <summary>
        /// Get/Set the Proxy-Uri option
        /// </summary>
        public Uri ProxyUri
        {
            get
            {
                Option opt = GetFirstOption(OptionType.ProxyUri);
                if (opt == null) {
                    return null;
                }

                string proxyUriString = Uri.UnescapeDataString(opt.StringValue);
                // All proxies that we support are going to require a "://" sequence as
                // the schema and host are needed.  
                int pos = proxyUriString.IndexOf("://", StringComparison.OrdinalIgnoreCase);
                if (pos == -1) {
                    proxyUriString = "coap://" + proxyUriString;
                }

                return new Uri(proxyUriString);
            }
            set
            {
                if (value == null) {
                    RemoveOptions(OptionType.ProxyUri);
                }
                else {
                    SetOption(Option.Create(OptionType.ProxyUri, value.ToString()));
                }
            }
        }

        /// <summary>
        /// Get/Set the ProxySchema option on a message
        /// </summary>
        public string ProxyScheme
        {
            get
            {
                Option opt = GetFirstOption(OptionType.Accept);
                return opt == null ? null : opt.StringValue;
            }
            set
            {
                if (value == null) {
                    RemoveOptions(OptionType.ProxyScheme);
                }
                else {
                    SetOption(Option.Create(OptionType.ProxyScheme, value));
                }
            }
        }

        /// <summary>
        /// Get/Set the observe option value
        /// </summary>
        public int? Observe
        {
            get
            {
                Option opt = GetFirstOption(OptionType.Observe);
                if (opt == null) {
                    return null;
                }
                else {
                    return opt.IntValue;
                }
            }
            set
            {
                if (value == null) {
                    RemoveOptions(OptionType.Observe);
                }
                else if (value < 0 || ((1 << 24) - 1) < value) {
                    throw ThrowHelper.Argument("value", "Observe option must be between 0 and " + ((1 << 24) - 1) + " (3 bytes) inclusive but was " + value);
                }
                else {
                    SetOption(Option.Create(OptionType.Observe, value.Value));
                }
            }
        }

        /// <summary>
        /// Gets or sets the Size1 option. Be <code>null</code> if not set.
        /// </summary>
        public int? Size1
        {
            get
            {
                Option opt = GetFirstOption(OptionType.Size1);
                return opt == null ? default(int?) : opt.IntValue;
            }
            set
            {
                if (value.HasValue) {
                    SetOption(Option.Create(OptionType.Size1, value.Value));
                }
                else {
                    RemoveOptions(OptionType.Size1);
                }
            }
        }

        /// <summary>
        /// Gets or sets the Size2 option. Be <code>null</code> if not set.
        /// </summary>
        public int? Size2
        {
            get
            {
                Option opt = GetFirstOption(OptionType.Size2);
                return opt == null ? default(int?) : opt.IntValue;
            }
            set
            {
                if (value.HasValue) {
                    SetOption(Option.Create(OptionType.Size2, value.Value));
                }
                else {
                    RemoveOptions(OptionType.Size2);
                }
            }
        }

        /// <summary>
        /// Get/Set the Block1 option
        /// </summary>
        public BlockOption Block1
        {
            get => GetFirstOption(OptionType.Block1) as BlockOption;
            set
            {
                if (value == null) {
                    RemoveOptions(OptionType.Block1);
                }
                else {
                    SetOption(value);
                }
            }
        }

        /// <summary>
        /// Create a Block1 option and add it to the message
        /// </summary>
        /// <param name="szx">Size of blocks to use</param>
        /// <param name="m">more data?</param>
        /// <param name="num">block index</param>
        public void SetBlock1(int szx, bool m, int num)
        {
            SetOption(new BlockOption(OptionType.Block1, num, szx, m));
        }

        /// <summary>
        /// Get/Set the Block1 option
        /// </summary>
        public BlockOption Block2
        {
            get => GetFirstOption(OptionType.Block2) as BlockOption;
            set
            {
                if (value == null) {
                    RemoveOptions(OptionType.Block2);
                }
                else {
                    SetOption(value);
                }
            }
        }

        /// <summary>
        /// Create a Block2 option and add it to the message
        /// </summary>
        /// <param name="szx">Size of blocks to use</param>
        /// <param name="m">more data?</param>
        /// <param name="num">block index</param>
        public void SetBlock2(int szx, bool m, int num)
        {
            SetOption(new BlockOption(OptionType.Block2, num, szx, m));
        }

        /// <summary>
        /// Get/Set the OSCOAP option value
        /// </summary>
        public OSCOAP.OscoapOption Oscoap
        {
            get => GetFirstOption(OptionType.Oscore) as OSCOAP.OscoapOption;
            set
            {
                if (value == null) {
                    RemoveOptions(OptionType.Oscore);
                }
                else {
                    SetOption(value);
                }
            }
        }

        private IEnumerable<T> SelectOptions<T>(OptionType optionType, Func<Option, T> func)
        {
            IEnumerable<Option> opts = GetOptions(optionType);
            if (opts != null) {
                foreach (Option opt in opts) {
                    yield return func(opt);
                }
            }
        }

        /// <summary>
        /// Adds an option to the list of options of this CoAP message.
        /// </summary>
        /// <param name="option">the option to add</param>
        public Message AddOption(Option option)
        {
            if (option == null) {
                throw new ArgumentNullException(nameof(option));
            }

            LinkedList<Option> list;
            if (!_optionMap.TryGetValue(option.Type, out list)) {
                list = new LinkedList<Option>();
                _optionMap[option.Type] = list;
            }

            list.AddLast(option);

            return this;
        }

        /// <summary>
        /// Adds all option to the list of options of this CoAP message.
        /// </summary>
        /// <param name="options">the options to add</param>
        public void AddOptions(IEnumerable<Option> options)
        {
            foreach (Option opt in options)
            {
                AddOption(opt);
            }
        }

        /// <summary>
        /// Removes all options of the given type from this CoAP message.
        /// </summary>
        /// <param name="optionType">the type of option to remove</param>
        public bool RemoveOptions(OptionType optionType)
        {
            return _optionMap.Remove(optionType);
        }

        /// <summary>
        /// Gets all options of the given type.
        /// </summary>
        /// <param name="optionType">the option type</param>
        /// <returns></returns>
        public IEnumerable<Option> GetOptions(OptionType optionType)
        {
            return _optionMap.ContainsKey(optionType) ? _optionMap[optionType] : null;
        }

        /// <summary>
        /// Sets an option.
        /// </summary>
        /// <param name="opt">the option to set</param>
        public void SetOption(Option opt)
        {
            if (null != opt) {
                RemoveOptions(opt.Type);
                AddOption(opt);
            }
        }

        /// <summary>
        /// Sets all options with the specified option type.
        /// </summary>
        /// <param name="options">the options to set</param>
        public void SetOptions(IEnumerable<Option> options)
        {
            if (options == null) {
                return;
            }

            List<Option> toAdd = new List<Option>();
            foreach (Option opt in options) {
                RemoveOptions(opt.Type);
                toAdd.Add(opt);
            }
            AddOptions(toAdd);
        }

        /// <summary>
        /// Checks if this CoAP message has options of the specified option type.
        /// </summary>
        /// <param name="type">the option type</param>
        /// <returns>rrue if options of the specified type exist</returns>
        public bool HasOption(OptionType type)
        {
            return GetFirstOption(type) != null;
        }

        /// <summary>
        /// Gets the first option of the specified option type.
        /// </summary>
        /// <param name="optionType">the option type</param>
        /// <returns>the first option of the specified type, or null</returns>
        public Option GetFirstOption(OptionType optionType)
        {
            LinkedList<Option> list;
            if (_optionMap.TryGetValue(optionType, out list)) {
                return list.Count > 0 ? list.First.Value : null;
            }
            return null;
        }

        /// <summary>
        /// Gets a sorted list of all options.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Option> GetOptions()
        {
            List<Option> list = new List<Option>();
            foreach (ICollection<Option> opts in _optionMap.Values) {
                if (opts.Count > 0) {
                    list.AddRange(opts);
                }
            }
            return list;
        }

        #endregion
    }
}
