/*
 * Copyright (c) 2011-2015, Longxiang He <helongxiang@smeshlink.com>,
 * SmeshLink Technology Co.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY.
 * 
 * This file is part of the CoAP.NET, a CoAP framework in C#.
 * Please see README for more information.
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Com.AugustCellars.CoAP.Observe;
using Com.AugustCellars.CoAP.Stack;
using Com.AugustCellars.CoAP.Util;
using Com.AugustCellars.CoAP.OSCOAP;

namespace Com.AugustCellars.CoAP.Net
{
    /// <summary>
    /// Represents the complete state of an exchange of one request
    /// and one or more responses. The lifecycle of an exchange ends
    /// when either the last response has arrived and is acknowledged,
    /// when a request or response has been rejected from the remote endpoint,
    /// when the request has been canceled, or when a request or response timed out,
    /// i.e., has reached the retransmission limit without being acknowledged.
    /// </summary>
    public class Exchange
    {
        private readonly ConcurrentDictionary<object, object> _attributes = new ConcurrentDictionary<object, object>();
        private bool _timedOut;
        private bool _complete;
        private IOutbox _outbox;
        private IMessageDeliverer _deliverer;

        public event EventHandler Completed;

        public Exchange(Request request, Origin origin)
        {
            Origin = origin;
            CurrentRequest = request;
            Timestamp = DateTime.Now;
        }

        public Exchange(Exchange other)
        {
            Origin = other.Origin;
            CurrentRequest = other.Request;
            Timestamp = other.Timestamp;
            OscoreContext = other.OscoreContext;
        }

        public Origin Origin { get; }

        /// <summary>
        /// Gets or sets the endpoint which has created and processed this exchange.
        /// </summary>
        public IEndPoint EndPoint { get; set; }

        public bool TimedOut
        {
            get => _timedOut;
            set
            {
                _timedOut = value;
                if (value) {
                    Complete = true;
                }
            }
        }

        public Request Request { get; set; }

        public Request CurrentRequest { get; set; }

        public List<Option> PreSecurityOptions { get; set; }

        /// <summary>
        /// Gets or sets the status of the blockwise transfer of the request,
        /// or null in case of a normal transfer,
        /// </summary>
        public BlockwiseStatus RequestBlockStatus { get; set; }

        public Response Response { get; set; }

        public Response CurrentResponse { get; set; }

        /// <summary>
        /// Gets or sets the status of the blockwise transfer of the response,
        /// or null in case of a normal transfer,
        /// </summary>
        public BlockwiseStatus ResponseBlockStatus { get; set; }

        public ObserveRelation Relation { get; set; }

        /// <summary>
        /// Gets or sets the block option of the last block of a blockwise sent request.
        /// When the server sends the response, this block option has to be acknowledged.
        /// </summary>
        public BlockOption Block1ToAck { get; set; }

        /// <summary>
        /// Gets or sets the status of the security blockwise transfer of the request,
        /// or null in case of a normal transfer,
        /// </summary>
        public BlockwiseStatus OscoreRequestBlockStatus { get; set; }

        /// <summary>
        /// Gets or sets the status of the security blockwise transfer of the response,
        /// or null in case of a normal transfer,
        /// </summary>
        public BlockwiseStatus OSCOAP_ResponseBlockStatus { get; set; }

        /// <summary>
        /// Gets or sets the OSCOAP security context for the exchange
        /// </summary>
        public SecurityContext OscoreContext { get; set; }

        /// <summary>
        /// Gets or sets the sequence number used to link requests and responses
        /// in OSCOAP authentication
        /// </summary>
        public byte[] OscoapSequenceNumber { get; set; }

        public byte[] OscoapSenderId { get; set; }

        /// <summary>
        /// Gets the time when this exchange was created.
        /// </summary>
        public DateTime Timestamp { get; }

        public IOutbox Outbox
        {
            get => _outbox ?? (EndPoint == null ? null : EndPoint.Outbox);
            set => _outbox = value;
        }

        public IMessageDeliverer Deliverer
        {
            get => _deliverer ?? (EndPoint == null ? null : EndPoint.MessageDeliverer);
            set => _deliverer = value;
        }

        public bool Complete
        {
            get => _complete;
            set
            {
                _complete = value;
                if (value)
                {
                    EventHandler h = Completed;
                    if (h != null) {
                        h(this, EventArgs.Empty);
                    }
                }
            }
        }

        /// <summary>
        /// Reject this exchange and therefore the request.
        /// Sends an RST back to the client.
        /// </summary>
        public virtual void SendReject()
        {
            System.Diagnostics.Debug.Assert(Origin == Origin.Remote);
            Request.IsRejected = true;
            EmptyMessage rst = EmptyMessage.NewRST(Request);
            EndPoint.SendEmptyMessage(this, rst);
        }

        /// <summary>
        /// Accept this exchange and therefore the request. Only if the request's
        /// type was a <code>CON</code> and the request has not been acknowledged
        /// yet, it sends an ACK to the client.
        /// </summary>
        public virtual void SendAccept()
        {
            System.Diagnostics.Debug.Assert(Origin == Origin.Remote);
            if (Request.Type == MessageType.CON && !Request.IsAcknowledged)
            {
                Request.IsAcknowledged = true;
                EmptyMessage ack = EmptyMessage.NewACK(Request);
                EndPoint.SendEmptyMessage(this, ack);
            }
        }

        /// <summary>
        /// Sends the specified response over the same endpoint
        /// as the request has arrived.
        /// </summary>
        public virtual void SendResponse(Response response)
        {
            response.Destination = Request.Source;
            Response = response;
            response.Session = Request.Session;
            EndPoint.SendResponse(this, response);
        }

        public T Get<T>(object key)
        {
            return (T)Get(key);
        }

        public object Get(object key)
        {
            object value;
            _attributes.TryGetValue(key, out value);
            return value;
        }

        public object GetOrAdd(object key, object value)
        {
            return _attributes.GetOrAdd(key, value);
        }

        public T GetOrAdd<T>(object key, object value)
        {
            return (T)GetOrAdd(key, value);
        }

        public object GetOrAdd(object key, Func<object, object> valueFactory)
        {
            return _attributes.GetOrAdd(key, valueFactory);
        }

        public T GetOrAdd<T>(object key, Func<object, object> valueFactory)
        {
            return (T)GetOrAdd(key, valueFactory);
        }

        /// <summary>
        /// Set an object in the attribute map based on it's key.
        /// If a previous object existed, return it.
        /// </summary>
        /// <param name="key">Key to use to save the object</param>
        /// <param name="value">value to save</param>
        /// <returns>old object if one exists.</returns>
        public object Set(object key, object value)
        {
            object old = null;
            _attributes.AddOrUpdate(key, value, (k, v) => {
                old = v;
                return value;
            });
            return old;
        }

        public object Remove(object key)
        {
            object obj;
            _attributes.TryRemove(key, out obj);
            return obj;
        }

        public class KeyID
        {
            private readonly int _id;
            private readonly System.Net.EndPoint _endpoint;
            private readonly ISession _session;
            private readonly int _hash;

            public KeyID(int id, System.Net.EndPoint ep, ISession session)
            {
                _id = id;
                _endpoint = ep;
                _session = session;
                _hash = id * 31 + (ep == null ? 0 : ep.GetHashCode());
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                return _hash;
            }

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                KeyID other = obj as KeyID;
                if (other == null) {
                    return false;
                }

                return _id == other._id && Equals(_endpoint, other._endpoint); // && (_session == other._session);
            }

            /// <inheritdoc/>
            public override string ToString()
            {
                return "KeyID[" + _id + " for " + _endpoint + "]";
            }
        }

        public class KeyToken
        {
            private readonly byte[] _token;
            private readonly int _hash;

            public KeyToken(byte[] token)
            {
                _token = token ?? throw new ArgumentNullException(nameof(token));
                _hash = ByteArrayUtils.ComputeHash(_token);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                return _hash;
            }

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                KeyToken other = obj as KeyToken;
                if (other == null) {
                    return false;
                }

                return _hash == other._hash;
            }

            /// <inheritdoc/>
            public override string ToString()
            {
                return "KeyToken[" + BitConverter.ToString(_token) + "]";
            }
        }

        public class KeyUri
        {
            private readonly System.Net.EndPoint _endpoint;
            private readonly int _hash;
            private readonly List<Option> _listOptions = new List<Option>();

            public KeyUri(Message message, System.Net.EndPoint endpoint) : this(message.GetOptions(), endpoint) { }


            public KeyUri(IEnumerable<Option> optionList, System.Net.EndPoint endpoint)
            {
                _hash = endpoint.GetHashCode();
                _endpoint = endpoint;

                foreach (Option option in optionList) {
                    if (Option.IsNotCacheKey(option.Type) || option.Type == OptionType.Block1 || option.Type == OptionType.Block2) {
                        continue;
                    }

                    _listOptions.Add(option);
                    _hash += option.GetHashCode() * 71;
                }
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                return _hash;
            }

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                KeyUri other = obj as KeyUri;
                if (other == null) {
                    return false;
                }

                if (!_endpoint.Equals(other._endpoint) || _listOptions.Count != other._listOptions.Count) {
                    return false;
                }

                foreach (bool b in _listOptions.Zip(other._listOptions, ((option, option1) => option.Equals(option1)))) {
                    if (!b) {
                        return false;
                    }
                }

                return true;
            }

            /// <inheritdoc/>
            public override string ToString()
            {
                StringBuilder sb = new StringBuilder($"KeyUri[EP = {_endpoint}");

                foreach (Option option in _listOptions) {
                    sb.Append(" ");
                    sb.Append(option);
                }

                sb.Append("]");

                return sb.ToString();
            }
        }
    }

    /// <summary>
    /// The origin of an exchange.
    /// </summary>
    public enum Origin
    {
        Local,
        Remote
    }
}
