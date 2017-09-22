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
using System.Collections.Generic;
using Com.AugustCellars.CoAP.Log;
using Com.AugustCellars.CoAP.Net;

namespace Com.AugustCellars.CoAP
{
    /// <summary>
    /// Provides convenient methods for accessing CoAP resources.
    /// </summary>
    public class CoapClient
    {
        private static readonly ILogger _Log = LogManager.GetLogger(typeof(CoapClient));
        private static readonly IEnumerable<WebLink> _EmptyLinks = new WebLink[0];
        private readonly ICoapConfig _config;
        private MessageType _type = MessageType.CON;

        /// <summary>
        /// Occurs when a response has arrived.
        /// </summary>
        public event EventHandler<ResponseEventArgs> Respond;

        /// <summary>
        /// Occurs if an exception is thrown while executing a request.
        /// </summary>
        public event EventHandler<ErrorEventArgs> Error;

        /// <summary>
        /// Instantiates with default config.
        /// </summary>
        public CoapClient()
            : this((Uri)null, null)
        { }

        /// <summary>
        /// Instantiates with default config.
        /// </summary>
        /// <param name="uri">the Uri of remote resource</param>
        public CoapClient(Uri uri)
            : this(uri, null)
        { }

        /// <summary>
        /// Instantiates.
        /// </summary>
        /// <param name="uri">URI to connect to</param>
        public CoapClient(string uri) : this(new Uri(uri), null)
        {
            
        }

        /// <summary>
        /// Instantiates.
        /// </summary>
        /// <param name="config">the config</param>
        public CoapClient(ICoapConfig config)
            : this((Uri) null, config)
        { }

        /// <summary>
        /// Instantiates.
        /// </summary>
        /// <param name="uri">the Uri of remote resource</param>
        /// <param name="config">the config</param>
        public CoapClient(string uri, ICoapConfig config) : this(new Uri(uri), config)
        {
        }

        /// <summary>
        /// Instantiates.
        /// </summary>
        /// <param name="uri">the Uri of remote resource</param>
        /// <param name="config">the config</param>
        public CoapClient(Uri uri, ICoapConfig config)
        {
            Uri = uri;
            _config = config ?? CoapConfig.Default;
        }

        /// <summary>
        /// Gets or sets the destination URI of this client.
        /// </summary>
        public Uri Uri { get; set; }

        /// <summary>
        /// Path elements to be added to the URI as Uri-Path options
        /// </summary>
        public String UriPath { get; set; }

        /// <summary>
        /// Query elements to be added tothe URI as Uri-Query options
        /// </summary>
        public String UriQuery { get; set; }

        /// <summary>
        /// Gets or sets the endpoint this client is supposed to use.
        /// </summary>
        public IEndPoint EndPoint { get; set; }

        /// <summary>
        /// Gets or sets the timeout how long synchronous method calls will wait
        /// until they give up and return anyways. The default value is <see cref="System.Threading.Timeout.Infinite"/>.
        /// </summary>
        public Int32 Timeout { get; set; } = System.Threading.Timeout.Infinite;

        /// <summary>
        /// Gets or sets the current blockwise size.
        /// 
        /// If the value is zero, then no blockwise value is sent on a request.
        /// Legal block sizes are (16, 32, 64, 128, 256, 512, or 1024).
        /// If a non-legal value is given, then it will be rounded to a legal value.
        /// </summary>
        public int Blockwise { get; set; }

        /// <summary>
        /// OSCOAP context to use for the message
        /// </summary>
        public OSCOAP.SecurityContext OscoapContext { get; set; }

        /// <summary>
        /// Let the client use Confirmable requests.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public CoapClient UseCONs()
        {
            _type = MessageType.CON;
            return this;
        }

        /// <summary>
        /// Let the client use Non-Confirmable requests.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public CoapClient UseNONs()
        {
            _type = MessageType.NON;
            return this;
        }

        /// <summary>
        /// Let the client use early negotiation for the blocksize
        /// (16, 32, 64, 128, 256, 512, or 1024). Other values will
        /// be matched to the closest logarithm dualis.
        /// </summary>
        public CoapClient UseEarlyNegotiation(Int32 size)
        {
            Blockwise = size;
            return this;
        }

        /// <summary>
        /// Let the client use late negotiation for the block size (default).
        /// </summary>
        public CoapClient UseLateNegotiation()
        {
            Blockwise = 0;
            return this;
        }

        /// <summary>
        /// Performs a CoAP ping.
        /// </summary>
        /// <returns>success of the ping</returns>
        public Boolean Ping()
        {
            return Ping(Timeout);
        }

        /// <summary>
        /// Performs a CoAP ping and gives up after the given number of milliseconds.
        /// </summary>
        /// <param name="timeout">the time to wait for a pong in milliseconds</param>
        /// <returns>success of the ping</returns>
        public Boolean Ping(Int32 timeout)
        {
            try {
                Request request = new Request(Code.Empty, true) {
                    Token = CoapConstants.EmptyToken,
                    URI = Uri
                };
                request.Send().WaitForResponse(timeout);
                return request.IsRejected;
            }
            catch (System.Threading.ThreadInterruptedException) {
                /* ignore */
            }
            return false;
        }

        /// <summary>
        /// Discovers remote resources.
        /// </summary>
        /// <param name="mediaType">format to use - defaults to any</param>
        /// <returns>the descoverd <see cref="WebLink"/> representing remote resources, or null if no response</returns>
        public IEnumerable<WebLink> Discover(int mediaType = MediaType.Undefined)
        {
            return Discover(null, mediaType);
        }

        /// <summary>
        /// Discovers remote resources.
        /// </summary>
        /// <param name="query">the query to filter resources</param>
        /// <param name="mediaType">format to use - defaults to any</param>
        /// <returns>the descoverd <see cref="WebLink"/> representing remote resources, or null if no response</returns>
        public IEnumerable<WebLink> Discover(String query, int mediaType = MediaType.Undefined)
        {
            Request discover = Prepare(Request.NewGet());
            discover.ClearUriPath().ClearUriQuery().UriPath = CoapConstants.DefaultWellKnownURI;
            if (!String.IsNullOrEmpty(query)) {
                discover.UriQuery = query;
            }
            if (mediaType != MediaType.Undefined) {
                discover.Accept = mediaType;
            }

            Response links = discover.Send().WaitForResponse(Timeout);
            if (links == null) {
                // if no response, return null (e.g., timeout)
                return null;
            }

            switch (links.ContentType) {
                case MediaType.ApplicationLinkFormat:
                    return LinkFormat.Parse(links.PayloadString);

                case MediaType.ApplicationCbor:
                    return LinkFormat.ParseCbor(links.Payload);

                case MediaType.ApplicationJson:
                    return LinkFormat.ParseJson(links.PayloadString);

                default:
                    return _EmptyLinks;
            }
        }

        /// <summary>
        /// Sends a GET request and blocks until the response is available.
        /// </summary>
        /// <returns>the CoAP response</returns>
        public Response Get()
        {
            return Send(Request.NewGet());
        }

        /// <summary>
        /// Sends a GET request with the specified Accept option and blocks
        /// until the response is available.
        /// </summary>
        /// <param name="accept">the Accept option</param>
        /// <returns>the CoAP response</returns>
        public Response Get(Int32 accept)
        {
            return Send(Accept(Request.NewGet(), accept));
        }

        /// <summary>
        /// Sends a GET request asynchronizely.
        /// </summary>
        /// <param name="done">the callback when a response arrives</param>
        /// <param name="fail">the callback when an error occurs</param>
        public void GetAsync(Action<Response> done = null, Action<FailReason> fail = null)
        {
            SendAsync(Request.NewGet(), done, fail);
        }

        /// <summary>
        /// Sends a GET request with the specified Accept option asynchronizely.
        /// </summary>
        /// <param name="accept">the Accept option</param>
        /// <param name="done">the callback when a response arrives</param>
        /// <param name="fail">the callback when an error occurs</param>
        public void GetAsync(Int32 accept, Action<Response> done = null, Action<FailReason> fail = null)
        {
            SendAsync(Accept(Request.NewGet(), accept), done, fail);
        }

        /// <summary>
        /// Sends a POST request with the specified payload
        /// </summary>
        /// <param name="payload">Text based payload to send</param>
        /// <param name="format">Content format - defaults to plain text</param>
        /// <returns>the CoAP response</returns>
        public Response Post(String payload, Int32 format = MediaType.TextPlain)
        {
            return Send((Request)Request.NewPost().SetPayload(payload, format));
        }

        /// <summary>
        /// Sends a POST request with the specified payload
        /// </summary>
        /// <param name="payload">Text based payload to send</param>
        /// <param name="format">Content format - defaults to plain text</param>
        /// <param name="accept">what content format is to be returned</param>
        /// <returns>the CoAP response</returns>
        public Response Post(String payload, Int32 format, Int32 accept)
        {
            return Send(Accept((Request)Request.NewPost().SetPayload(payload, format), accept));
        }

        /// <summary>
        /// Sends a POST request with the specified payload
        /// </summary>
        /// <param name="payload">Binary payload to send</param>
        /// <param name="format">Content format</param>
        /// <returns>the CoAP response</returns>
        public Response Post(Byte[] payload, Int32 format)
        {
            return Send((Request)Request.NewPost().SetPayload(payload, format));
        }

        /// <summary>
        /// Sends a POST request with the specified payload
        /// </summary>
        /// <param name="payload">Binary payload to send</param>
        /// <param name="format">Content format</param>
        /// <param name="accept">what content format is to be returned</param>
        /// <returns>the CoAP response</returns>
        public Response Post(Byte[] payload, Int32 format, Int32 accept)
        {
            return Send(Accept((Request)Request.NewPost().SetPayload(payload, format), accept));
        }

        /// <summary>
        /// Sends a POST request with the specified payload asynchnously
        /// </summary>
        /// <param name="payload">Text payload to send</param>
        /// <param name="done">Action for a respone message</param>
        /// <param name="fail">Action internal errors</param>
        public void PostAsync(String payload,
            Action<Response> done = null, Action<FailReason> fail = null)
        {
            PostAsync(payload, MediaType.TextPlain, done, fail);
        }

        /// <summary>
        /// Sends a POST request with the specified payload asynchnously
        /// </summary>
        /// <param name="payload">Text payload to send</param>
        /// <param name="format">Content Format for the payload</param>
        /// <param name="done">Action for a respone message</param>
        /// <param name="fail">Action internal errors</param>
        public void PostAsync(String payload, Int32 format,
            Action<Response> done = null, Action<FailReason> fail = null)
        {
            SendAsync((Request)Request.NewPost().SetPayload(payload, format), done, fail);
        }

        /// <summary>
        /// Sends a POST request with the specified payload asynchnously
        /// </summary>
        /// <param name="payload">Text payload to send</param>
        /// <param name="format">Content Format for the payload</param>
        /// <param name="accept">What return content format is acceptable</param>
        /// <param name="done">Action for a respone message</param>
        /// <param name="fail">Action internal errors</param>
        public void PostAsync(String payload, Int32 format, Int32 accept,
            Action<Response> done = null, Action<FailReason> fail = null)
        {
            SendAsync(Accept((Request)Request.NewPost().SetPayload(payload, format), accept), done, fail);
        }

        /// <summary>
        /// Sends a POST request with the specified payload asynchnously
        /// </summary>
        /// <param name="payload">Binary payload to send</param>
        /// <param name="format">Content Format for the payload</param>
        /// <param name="done">Action for a respone message</param>
        /// <param name="fail">Action internal errors</param>
        public void PostAsync(Byte[] payload, Int32 format,
            Action<Response> done = null, Action<FailReason> fail = null)
        {
            SendAsync((Request)Request.NewPost().SetPayload(payload, format), done, fail);
        }

        /// <summary>
        /// Sends a POST request with the specified payload asynchnously
        /// </summary>
        /// <param name="payload">Binary payload to send</param>
        /// <param name="format">Content Format for the payload</param>
        /// <param name="accept">What return content format is acceptable</param>
        /// <param name="done">Action for a respone message</param>
        /// <param name="fail">Action internal errors</param>
        public void PostAsync(Byte[] payload, Int32 format, Int32 accept,
            Action<Response> done = null, Action<FailReason> fail = null)
        {
            SendAsync(Accept((Request)Request.NewPost().SetPayload(payload, format), accept), done, fail);
        }

        /// <summary>
        /// Sends a PUT request with the specified payload
        /// </summary>
        /// <param name="payload">Text based payload to send</param>
        /// <param name="format">Content format - defaults to plain text</param>
        /// <returns>the CoAP response</returns>
        public Response Put(String payload, Int32 format = MediaType.TextPlain)
        {
            return Send((Request)Request.NewPut().SetPayload(payload, format));
        }

        /// <summary>
        /// Sends a PUT request with the specified payload
        /// </summary>
        /// <param name="payload">Binary payload to send</param>
        /// <param name="format">Content format</param>
        /// <param name="accept">What return content format is acceptable</param>
        /// <returns>the CoAP response</returns>
        public Response Put(Byte[] payload, Int32 format, Int32 accept)
        {
            return Send(Accept((Request)Request.NewPut().SetPayload(payload, format), accept));
        }

        /// <summary>
        /// Sends a PUT request with the specified payload with an If-Match option
        /// </summary>
        /// <param name="payload">Text based payload to send</param>
        /// <param name="format">Content format</param>
        /// <param name="etags">ETags to match before doing update</param>
        /// <returns>the CoAP response</returns>
        public Response PutIfMatch(String payload, Int32 format, params Byte[][] etags)
        {
            return Send(IfMatch((Request)Request.NewPut().SetPayload(payload, format), etags));
        }

        /// <summary>
        /// Sends a PUT request with the specified payload with an If-Match option
        /// </summary>
        /// <param name="payload">Binary payload to send</param>
        /// <param name="format">Content format</param>
        /// <param name="etags">ETags to match before doing update</param>
        /// <returns>the CoAP response</returns>
        public Response PutIfMatch(Byte[] payload, Int32 format, params Byte[][] etags)
        {
            return Send(IfMatch((Request)Request.NewPut().SetPayload(payload, format), etags));
        }

        /// <summary>
        /// Sends a PUT request with the specified payload if target does not exist
        /// </summary>
        /// <param name="payload">Text based payload to send</param>
        /// <param name="format">Content format</param>
        /// <returns>the CoAP response</returns>
        public Response PutIfNoneMatch(String payload, Int32 format)
        {
            return Send(IfNoneMatch((Request)Request.NewPut().SetPayload(payload, format)));
        }

        /// <summary>
        /// Sends a PUT request with the specified payload if target does not exist
        /// </summary>
        /// <param name="payload">Binary payload to send</param>
        /// <param name="format">Content format</param>
        /// <returns>the CoAP response</returns>
        public Response PutIfNoneMatch(Byte[] payload, Int32 format)
        {
            return Send(IfNoneMatch((Request)Request.NewPut().SetPayload(payload, format)));
        }

        /// <summary>
        /// Sends a PUT request with the specified payload asynchronsly
        /// </summary>
        /// <param name="payload">Text based payload to send</param>
        /// <param name="done">Action for a respone message</param>
        /// <param name="fail">Action for internal errors</param>
        /// <returns>the CoAP response</returns>
        public void PutAsync(String payload,
            Action<Response> done = null, Action<FailReason> fail = null)
        {
            PutAsync(payload, MediaType.TextPlain, done, fail);
        }

        /// <summary>
        /// Sends a PUT request with the specified payload asynchronsly
        /// </summary>
        /// <param name="payload">Text based payload to send</param>
        /// <param name="format">Content format</param>
        /// <param name="done">Action for a respone message</param>
        /// <param name="fail">Action for internal errors</param>
        /// <returns>the CoAP response</returns>
        public void PutAsync(String payload, Int32 format,
            Action<Response> done = null, Action<FailReason> fail = null)
        {
            SendAsync((Request)Request.NewPut().SetPayload(payload, format), done, fail);
        }

        /// <summary>
        /// Sends a PUT request with the specified payload asynchronsly
        /// </summary>
        /// <param name="payload">Text based payload to send</param>
        /// <param name="format">Content format</param>
        /// <param name="accept">What return content format is acceptable</param>
        /// <param name="done">Action for a respone message</param>
        /// <param name="fail">Action for internal errors</param>
        /// <returns>the CoAP response</returns>
        public void PutAsync(Byte[] payload, Int32 format, Int32 accept,
            Action<Response> done = null, Action<FailReason> fail = null)
        {
            SendAsync(Accept((Request)Request.NewPut().SetPayload(payload, format), accept), done, fail);
        }

        /// <summary>
        /// Sends a DELETE request and waits for the response.
        /// </summary>
        /// <returns>the CoAP response</returns>
        public Response Delete()
        {
            return Send(Request.NewDelete());
        }

        /// <summary>
        /// Sends a DELETE request asynchronizely.
        /// </summary>
        /// <param name="done">the callback when a response arrives</param>
        /// <param name="fail">the callback when an error occurs</param>
        public void DeleteAsync(Action<Response> done = null, Action<FailReason> fail = null)
        {
            SendAsync(Request.NewDelete(), done, fail);
        }

        /// <summary>
        /// Send a GET request to see if one of the ETAG values matches
        /// one of the currently valid contents
        /// </summary>
        /// <param name="etags">ETAG values to check as current content</param>
        /// <returns>CoAP Response</returns>
        public Response Validate(params Byte[][] etags)
        {
            return Send(ETags(Request.NewGet(), etags));
        }

        /// <summary>
        /// Set a GET request with an observer option
        /// </summary>
        /// <param name="notify">Action to take for each notification</param>
        /// <param name="error">Action to take on internal error</param>
        /// <returns>New observe relation</returns>
        public CoapObserveRelation Observe(Action<Response> notify = null, Action<FailReason> error = null)
        {
            return Observe(Request.NewGet().MarkObserve(), notify, error);
        }

        /// <summary>
        /// Set a GET request with an observer option
        /// </summary>
        /// <param name="accept">Specify the content format to return</param>
        /// <param name="notify">Action to take for each notification</param>
        /// <param name="error">Action to take on internal error</param>
        /// <returns>New observe relation</returns>
        public CoapObserveRelation Observe(Int32 accept, Action<Response> notify = null, Action<FailReason> error = null)
        {
            return Observe(Accept(Request.NewGet().MarkObserve(), accept), notify, error);
        }

        /// <summary>
        /// Set a GET request with an observer option asynchronously
        /// </summary>
        /// <param name="notify">Action to take for each notification</param>
        /// <param name="error">Action to take on internal error</param>
        /// <returns>New observe relation</returns>
        public CoapObserveRelation ObserveAsync(Action<Response> notify = null, Action<FailReason> error = null)
        {
            return ObserveAsync(Request.NewGet().MarkObserve(), notify, error);
        }

        /// <summary>
        /// Set a GET request with an observer option asynchronously
        /// </summary>
        /// <param name="accept">Specify the content format to return</param>
        /// <param name="notify">Action to take for each notification</param>
        /// <param name="error">Action to take on internal error</param>
        /// <returns>New observe relation</returns>
        public CoapObserveRelation ObserveAsync(Int32 accept, Action<Response> notify = null, Action<FailReason> error = null)
        {
            return ObserveAsync(Accept(Request.NewGet().MarkObserve(), accept), notify, error);
        }

        /// <summary>
        /// Send a user created request to the server
        /// </summary>
        /// <param name="request">request to be sent</param>
        /// <returns>CoAP response</returns>
        public Response Send(Request request)
        {
            return Prepare(request).Send().WaitForResponse(Timeout);
        }

        /// <summary>
        /// Send a user created request to the server
        /// </summary>
        /// <param name="request">request to be sent</param>
        /// <param name="done">Action to take for each notification</param>
        /// <param name="fail">Action to take on internal error</param>
        /// <returns>CoAP response</returns>
        public void SendAsync(Request request, Action<Response> done = null, Action<FailReason> fail = null)
        {
            request.Respond += (o, e) => Deliver(done, e);
            request.Rejected += (o, e) => Fail(fail, FailReason.Rejected);
            request.TimedOut += (o, e) => Fail(fail, FailReason.TimedOut);
            
            Prepare(request).Send();
        }

        /// <summary>
        /// Set properties on the request that are based on properties on this object
        /// </summary>
        /// <param name="request">Request we are going to send</param>
        /// <returns>Request passed in</returns>
        protected Request Prepare(Request request)
        {
            return Prepare(request, GetEffectiveEndpoint(request));
        }

        /// <summary>
        /// Set properties on the request that are based on properties on this object
        /// </summary>
        /// <param name="request">Request we are going to send</param>
        /// <param name="endpoint">Endpoint to use sending the message</param>
        /// <returns>Request passed in</returns>
        protected Request Prepare(Request request, IEndPoint endpoint)
        {
            request.Type = _type;
            request.URI = Uri;
            request.OscoapContext = OscoapContext;

            if (UriPath != null) {
                request.UriPath = UriPath;
            }

            if (UriQuery != null) {
                request.UriQuery = UriQuery;
            }
            
            if (Blockwise != 0) {
                request.SetBlock2(BlockOption.EncodeSZX(Blockwise), false, 0);
            }

            if (endpoint != null) {
                request.EndPoint = endpoint;
            }

            return request;
        }

        /// <summary>
        /// Gets the effective endpoint that the specified request
        /// is supposed to be sent over.
        /// </summary>
        protected IEndPoint GetEffectiveEndpoint(Request request)
        {
            return EndPoint ?? EndPointManager.Default;
        }

        private CoapObserveRelation Observe(Request request, Action<Response> notify, Action<FailReason> error)
        {
            CoapObserveRelation relation = ObserveAsync(request, notify, error);
            Response response = relation.Request.WaitForResponse(Timeout);
            if (response == null || !response.HasOption(OptionType.Observe)) {
                relation.Canceled = true;
            }

            relation.Current = response;
            return relation;
        }

        private CoapObserveRelation ObserveAsync(Request request, Action<Response> notify, Action<FailReason> error)
        {
            IEndPoint endpoint = GetEffectiveEndpoint(request);
            CoapObserveRelation relation = new CoapObserveRelation(request, endpoint, _config);

            request.Respond += (o, e) =>
            {
                Response resp = e.Response;
                lock (relation) {
                    if (relation.Orderer.IsNew(resp)) {
                        relation.Current = resp;
                        Deliver(notify, e);
                    }
                    else {
                        _Log.Debug(m => m("Dropping old notification: {0}", resp));
                    }
                }
            };
            Action<FailReason> fail = r =>
            {
                relation.Canceled = true;
                Fail(error, r);
            };
            request.Rejected += (o, e) => fail(FailReason.Rejected);
            request.TimedOut += (o, e) => fail(FailReason.TimedOut);

            Prepare(request, endpoint).Send();
            return relation;
        }

        private void Deliver(Action<Response> act, ResponseEventArgs e)
        {
            if (act != null) {
                act(e.Response);
            }

            EventHandler<ResponseEventArgs> h = Respond;
            if (h != null) {
                h(this, e);
            }
        }

        private void Fail(Action<FailReason> fail, FailReason reason)
        {
            if (fail != null) {
                fail(reason);
            }

            EventHandler<ErrorEventArgs> h = Error;
            if (h != null) {
                h(this, new ErrorEventArgs(reason));
            }
        }

        private static Request Accept(Request request, Int32 accept)
        {
            request.Accept = accept;
            return request;
        }

        private static Request IfMatch(Request request, params Byte[][] etags)
        {
            foreach (Byte[] etag in etags) {
                request.AddIfMatch(etag);
            }
            return request;
        }

        private static Request IfNoneMatch(Request request)
        {
            request.IfNoneMatch = true;
            return request;
        }

        private static Request ETags(Request request, params Byte[][] etags)
        {
            foreach (Byte[] etag in etags) {
                request.AddETag(etag);
            }
            return request;
        }

        /// <summary>
        /// Provides details about errors.
        /// </summary>
        public enum FailReason
        {
            /// <summary>
            /// The request has been rejected.
            /// </summary>
            Rejected,
            /// <summary>
            /// The request has been timed out.
            /// </summary>
            TimedOut
        }

        /// <summary>
        /// Provides event args for errors.
        /// </summary>
        public class ErrorEventArgs : EventArgs
        {
            internal ErrorEventArgs(FailReason reason)
            {
                Reason = reason;
            }

            /// <summary>
            /// Gets the reason why failed.
            /// </summary>
            public FailReason Reason { get; }
        }
    }
}
