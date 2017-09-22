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
using System.Net;
using System.Text.RegularExpressions;
using Com.AugustCellars.CoAP.Net;
using Com.AugustCellars.CoAP.Observe;
#if INCLUDE_OSCOAP
using Com.AugustCellars.CoAP.OSCOAP;
#endif

namespace Com.AugustCellars.CoAP
{
    /// <summary>
    /// This class describes the functionality of a CoAP Request as
    /// a subclass of a CoAP Message. It provides:
    /// 1. operations to answer a request by a response using respond()
    /// 2. different ways to handle incoming responses: receiveResponse() or Responsed event
    /// </summary>
    public class Request : Message
    {
        private Uri _uri;
        private Response _currentResponse;
        private IEndPoint _endPoint;
        private Object _sync;

        /// <summary>
        /// Fired when a response arrives.
        /// </summary>
        public event EventHandler<ResponseEventArgs> Respond;

        /// <summary>
        /// Occurs when a block of response arrives in a blockwise transfer.
        /// </summary>
        public event EventHandler<ResponseEventArgs> Responding;

        /// <summary>
        /// Occurs when a observing request is reregistering.
        /// </summary>
        public event EventHandler<ReregisterEventArgs> Reregistering;

        /// <summary>
        /// Initializes a request message.
        /// </summary>
        public Request(Method method)
            : this(method, true)
        { }

        /// <summary>
        /// Initializes a request message.
        /// </summary>
        /// <param name="method">The method code of the message</param>
        /// <param name="confirmable">True if the request is Confirmable</param>
        public Request(Method method, Boolean confirmable)
            : base(confirmable ? MessageType.CON : MessageType.NON, (Int32)method)
        {
            Method = method;
        }

        /// <summary>
        /// Gets the request method.
        /// </summary>
        public Method Method { get; internal set;  }

        /// <summary>
        /// Gets or sets a value indicating whether this request is a multicast request or not.
        /// </summary>
        public Boolean Multicast { get; set; }

        // ReSharper disable once InconsistentNaming
        private static readonly Regex regIP = new Regex("(\\[[0-9a-f:]+\\]|[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3})", RegexOptions.IgnoreCase);

        /// <summary>
        /// Gets or sets the URI of this CoAP message.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public Uri URI
        {
            get
            {
                if (_uri == null) {
                    // M00BUG - The scheme will be wrong in many circumstances!!!!!
                    UriBuilder ub = new UriBuilder {
                        Scheme = CoapConstants.UriScheme,
                        Host = UriHost ?? "localhost",
                        Port = UriPort,
                        Path = UriPath,
                        Query = UriQuery
                    };
                    _uri = ub.Uri;
                }
                return _uri;
            }
            set
            {
                if (null != value) {
                    String host = value.Host;
                    Int32 port = value.Port;
                    string scheme = value.Scheme;

                    if (string.IsNullOrEmpty(scheme)) scheme = CoapConstants.UriScheme;

                    // set Uri-Host option if not IP literal
                    if (!String.IsNullOrEmpty(host) && !regIP.IsMatch(host)
                        && !host.Equals("localhost", StringComparison.OrdinalIgnoreCase)) {
                        UriHost = host;
                    }

                    if (port >= 0) {
                        if (scheme.Equals(CoapConstants.UriScheme) && port != CoapConstants.DefaultPort) {
                            UriPort = port;
                        }
                        else if (scheme.Equals(CoapConstants.SecureUriScheme) && port != CoapConstants.DefaultSecurePort) {
                            UriPort = port;
                        }
                        else {
                            UriPort = port;
                        }
                    }
                    else {
                        if (String.IsNullOrEmpty(value.Scheme) ||
                            String.Equals(value.Scheme, CoapConstants.UriScheme)) {
                            port = CoapConstants.DefaultPort;
                        }
                        else if (String.Equals(value.Scheme, CoapConstants.SecureUriScheme)) {
                            port = CoapConstants.DefaultSecurePort;
                        }
                    }

                    Destination = new IPEndPoint(Dns.GetHostAddresses(host)[0], port);

                    UriPath = value.AbsolutePath;
                    UriQuery = value.Query;
                }
                _uri = value;
            }
        }

        /// <summary>
        /// Return the endpoint to use for the request
        /// </summary>
        public IEndPoint EndPoint
        {
            get => _endPoint ?? (_endPoint = EndPointManager.Default);
            set => _endPoint = value;
        }

        /// <summary>
        /// Gets or sets the response to this request.
        /// </summary>
        public Response Response
        {
            get => _currentResponse;
            set
            {
                _currentResponse = value;
                if (_sync != null) {
                    NotifyResponse();
                }

                FireRespond(value);
            }
        }

        /// <summary>
        /// Alias function to set the URI on the request
        /// </summary>
        /// <param name="uri">URI to send the message to</param>
        /// <returns>Current message</returns>
        public Request SetUri(String uri)
        {
            URI = new Uri(uri);
            return this;
        }

        /// <summary>
        /// Should we attempt to reconnect to keep an observe relationship fresh
        /// in the event the MAX-AGE expires on the current value?
        /// </summary>
        public Boolean ObserveReconnect { get; set; } = true;

        /// <summary>
        /// Sets CoAP's observe option. If the target resource of this request
	    /// responds with a success code and also sets the observe option, it will
        /// send more responses in the future whenever the resource's state changes.
        /// </summary>
        /// <returns>Current requeset</returns>
        public Request MarkObserve()
        {
            Observe = 0;
            return this;
        }

        /// <summary>
        /// Sets CoAP's observe option to the value of 1 to proactively cancel.
        /// </summary>
        /// <returns>Current requeset</returns>
        public Request MarkObserveCancel()
        {
            Observe = 1;
            return this;
        }

        /// <summary>
        /// Gets the value of a query parameter as a <code>String</code>,
        /// or <code>null</code> if the parameter does not exist.
        /// </summary>
        /// <param name="name">a <code>String</code> specifying the name of the parameter</param>
        /// <returns>a <code>String</code> representing the single value of the parameter</returns>
        public String GetParameter(String name)
        {
            foreach (Option query in GetOptions(OptionType.UriQuery)) {
                String val = query.StringValue;
                if (String.IsNullOrEmpty(val)) {
                    continue;
                }

                if (val.StartsWith(name + "=")) {
                    return val.Substring(name.Length + 1);
                }
            }
            return null;
        }

        /// <summary>
        /// Send the request.
        /// </summary>
        [Obsolete("Call Send() instead.  Will be removed in drop 1.7")]
        public void Execute()
        {
            Send();
        }

        /// <summary>
        /// Sends this message.
        /// </summary>
        public Request Send()
        {
            ValidateBeforeSending();
            EndPoint.SendRequest(this);
            return this;
        }

        /// <summary>
        /// Sends the request over the specified endpoint.
        /// </summary>
        public Request Send(IEndPoint endpoint)
        {
            ValidateBeforeSending();
            _endPoint = endpoint;
            endpoint.SendRequest(this);
            return this;
        }

        /// <summary>
        /// Wait for a response.
        /// </summary>
        /// <exception cref="System.Threading.ThreadInterruptedException"></exception>
        public Response WaitForResponse()
        {
            return WaitForResponse(System.Threading.Timeout.Infinite);
        }

        /// <summary>
        /// Wait for a response.
        /// </summary>
        /// <param name="millisecondsTimeout">the maximum time to wait in milliseconds</param>
        /// <returns>the response, or null if timeout occured</returns>
        /// <exception cref="System.Threading.ThreadInterruptedException"></exception>
        public Response WaitForResponse(Int32 millisecondsTimeout)
        {
            // lazy initialization of a lock
            if (_sync == null) {
                lock (this) {
                    if (_sync == null) {
                        _sync = new Byte[0];
                    }
                }
            }

            lock (_sync) {
                if (_currentResponse == null &&
                    !IsCancelled && !IsTimedOut && !IsRejected) {
                    System.Threading.Monitor.Wait(_sync, millisecondsTimeout);
                }
                Response resp = _currentResponse;
                _currentResponse = null;
                return resp;
            }
        }

        /// <inheritdoc/>
        protected override void OnRejected()
        {
            if (_sync != null) {
                NotifyResponse();
            }

            base.OnRejected();
        }

        /// <inheritdoc/>
        protected override void OnTimedOut()
        {
            if (_sync != null) {
                NotifyResponse();
            }

            base.OnTimedOut();
        }

        /// <inheritdoc/>
        protected override void OnCanceled()
        {
            if (_sync != null) {
                NotifyResponse();
            }

            base.OnCanceled();
        }

        private void NotifyResponse()
        {
            lock (_sync) {
                System.Threading.Monitor.PulseAll(_sync);
            }
        }

        private void FireRespond(Response response)
        {
            EventHandler<ResponseEventArgs> h = Respond;
            if (h != null) {
                h(this, new ResponseEventArgs(response));
            }
        }

        internal void FireResponding(Response response)
        {
            EventHandler<ResponseEventArgs> h = Responding;
            if (h != null) {
                h(this, new ResponseEventArgs(response));
            }
        }

        internal void FireReregister(Request refresh)
        {
            EventHandler<ReregisterEventArgs> h = Reregistering;
            if (h != null) {
                h(this, new ReregisterEventArgs(refresh));
            }
        }

        private void ValidateBeforeSending()
        {
            if (Destination == null) {
                throw new InvalidOperationException("Missing Destination");
            }
        }

        internal override void CopyEventHandler(Message src)
        {
            base.CopyEventHandler(src);

            Request srcReq = src as Request;
            if (srcReq != null) {
                ForEach(srcReq.Respond, h => Respond += h);
                ForEach(srcReq.Responding, h => Responding += h);
            }
        }

        /// <summary>
        /// Construct a GET request.
        /// </summary>
        public static Request NewGet()
        {
            return new Request(Method.GET);
        }

        /// <summary>
        /// Construct a POST request.
        /// </summary>
        public static Request NewPost()
        {
            return new Request(Method.POST);
        }

        /// <summary>
        /// Construct a PUT request.
        /// </summary>
        public static Request NewPut()
        {
            return new Request(Method.PUT);
        }

        /// <summary>
        /// Construct a DELETE request.
        /// </summary>
        public static Request NewDelete()
        {
            return new Request(Method.DELETE);
        }

#if INCLUDE_OSCOAP
        /// <summary>
        /// Set the context structure used to OSCOAP protect the message
        /// </summary>
        public SecurityContext OscoapContext { get; set; }
#endif

        /// <summary>
        /// Return the security context associated with TLS.
        /// </summary>
        public ISecureSession TlsContext
        {
            get =>(ISecureSession) Session;
        }

        /// <summary>
        /// Give information about what session the request came from.
        /// </summary>
        public ISession Session { get; set; }
    }
}
