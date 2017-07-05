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
using Com.AugustCellars.CoAP.Channel;
using Com.AugustCellars.CoAP.Codec;
using Com.AugustCellars.CoAP.Log;
using Com.AugustCellars.CoAP.Stack;
using Com.AugustCellars.CoAP.Threading;

namespace Com.AugustCellars.CoAP.Net
{
    /// <summary>
    /// EndPoint encapsulates the stack that executes the CoAP protocol.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class CoAPEndPoint : IEndPoint, IOutbox
    {
        static readonly ILogger _Log = LogManager.GetLogger(typeof(CoAPEndPoint));

        /// <summary>
        /// Function that will create and return a message encoder
        /// </summary>
        /// <returns>Message encoder object</returns>
        public delegate IMessageEncoder FindMessageEncoder();

        /// <summary>
        /// Funtion that will create and return a message decoder
        /// </summary>
        /// <param name="data">data to be decoded</param>
        /// <returns>Message decoder object</returns>
        public delegate IMessageDecoder FindMessageDecoder(byte[] data);

        readonly IChannel _channel;
        readonly CoapStack _coapStack;
        private IMessageDeliverer _deliverer;
        private readonly IMatcher _matcher;
        private Int32 _running;
        private IExecutor _executor;

        /// <inheritdoc/>
        public event EventHandler<MessageEventArgs<Request>> SendingRequest;
        /// <inheritdoc/>
        public event EventHandler<MessageEventArgs<Response>> SendingResponse;
        /// <inheritdoc/>
        public event EventHandler<MessageEventArgs<EmptyMessage>> SendingEmptyMessage;
        /// <inheritdoc/>
        public event EventHandler<MessageEventArgs<Request>> ReceivingRequest;
        /// <inheritdoc/>
        public event EventHandler<MessageEventArgs<Response>> ReceivingResponse;
        /// <inheritdoc/>
        public event EventHandler<MessageEventArgs<EmptyMessage>> ReceivingEmptyMessage;

        /// <summary>
        /// Instantiates a new endpoint.
        /// </summary>
        public CoAPEndPoint()
            : this(0, CoapConfig.Default)
        { }

        /// <summary>
        /// Instantiates a new endpoint with the specified configuration.
        /// </summary>
        public CoAPEndPoint(ICoapConfig config)
            : this(0, config)
        { }

        /// <summary>
        /// Instantiates a new endpoint with the specified port.
        /// </summary>
        public CoAPEndPoint(Int32 port)
            : this(port, CoapConfig.Default)
        { }

        /// <summary>
        /// Instantiates a new endpoint with the
        /// specified <see cref="System.Net.EndPoint"/>.
        /// </summary>
        public CoAPEndPoint(System.Net.EndPoint localEndPointP)
            : this(localEndPointP, CoapConfig.Default)
        { }

        /// <summary>
        /// Instantiates a new endpoint with the
        /// specified port and configuration.
        /// </summary>
        public CoAPEndPoint(Int32 port, ICoapConfig config)
            : this(NewUDPChannel(port, config), config)
        { }

        /// <summary>
        /// Instantiates a new endpoint with the
        /// specified <see cref="System.Net.EndPoint"/> and configuration.
        /// </summary>
        public CoAPEndPoint(System.Net.EndPoint localEndPoint, ICoapConfig config)
            : this(NewUDPChannel(localEndPoint, config), config)
        { }

        /// <summary>
        /// Instantiates a new endpoint with the
        /// specified channel and configuration.
        /// </summary>
        public CoAPEndPoint(IChannel channel, ICoapConfig config)
        {
            Config = config;
            _channel = channel;
            _matcher = new Matcher(config);
            _coapStack = new CoapStack(config);
            _channel.DataReceived += ReceiveData;
            EndpointSchema = "coap";
        }

        /// <inheritdoc/>
        public ICoapConfig Config { get; }

        /// <summary>
        /// What execute is used by this endpoint.
        /// </summary>
        public IExecutor Executor
        {
            get => _executor;
            set
            {
                _executor = value ?? Executors.NoThreading;
                _coapStack.Executor = _executor;
            }
        }

        /// <summary>
        /// What is the endpoint schema supported by this endpoint
        /// </summary>
        public String EndpointSchema { get; set; }

        /// <inheritdoc/>
        public System.Net.EndPoint LocalEndPoint { get; private set; }

        /// <inheritdoc/>
        public IMessageDeliverer MessageDeliverer
        {
            set => _deliverer = value;
            get => _deliverer ?? (_deliverer = new ClientMessageDeliverer());
        }

        /// <summary>
        /// Return the message decoder to use with the end point
        /// </summary>
        public FindMessageDecoder MessageDecoder { set; get; } = Spec.NewMessageDecoder;

        /// <summary>
        /// Return the message encoder to use with the end point
        /// </summary>
        public FindMessageEncoder MessageEncoder { set; get; } = Spec.NewMessageEncoder;

        /// <inheritdoc/>
        public IOutbox Outbox => this;

        /// <inheritdoc/>
        public Boolean Running
        {
            get => _running > 0;
        }

        /// <summary>
        /// Return the stack used by this end point
        /// </summary>
        public CoapStack Stack
        {
            get => _coapStack;
        }

        /// <inheritdoc/>
        public void Start()
        {
            if (System.Threading.Interlocked.CompareExchange(ref _running, 1, 0) > 0) {
                return;
            }

            if (_executor == null) {
                Executor = Executors.Default;
            }

            LocalEndPoint = _channel.LocalEndPoint;
            try {
                _matcher.Start();
                _channel.Start();
                LocalEndPoint = _channel.LocalEndPoint;
            }
            catch {
                _Log.Warn(m => m("Cannot start endpoint at {0}", LocalEndPoint));
                Stop();
                throw;
            }
            _Log.Debug(m => m("Starting endpoint bound to {0}", LocalEndPoint));
        }

        /// <inheritdoc/>
        public void Stop()
        {
            if (System.Threading.Interlocked.Exchange(ref _running, 0) == 0) {
                return;
            }

            _Log.Debug(m => m("Stopping endpoint bound to {0}", LocalEndPoint));
            _channel.Stop();
            _matcher.Stop();
            _matcher.Clear();
        }

        /// <inheritdoc/>
        public void Clear()
        {
            _matcher.Clear();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (Running) {
                Stop();
            }

            _channel.Dispose();
            IDisposable d = _matcher as IDisposable;
            if (d != null) {
                d.Dispose();
            }
        }

        /// <inheritdoc/>
        public void SendRequest(Request request)
        {
            if (request.URI.Scheme != EndpointSchema) throw new Exception("Schema is incorrect for the end point");
            _executor.Start(() => _coapStack.SendRequest(request));
        }

        /// <inheritdoc/>
        public void SendResponse(Exchange exchange, Response response)
        {
            _executor.Start(() => _coapStack.SendResponse(exchange, response));
        }

        /// <inheritdoc/>
        public void SendEmptyMessage(Exchange exchange, EmptyMessage message)
        {
            _executor.Start(() => _coapStack.SendEmptyMessage(exchange, message));
        }

        private void ReceiveData(Object sender, DataReceivedEventArgs e)
        {
            _executor.Start(() => ReceiveData(e));
        }

        /// <summary>
        /// We have received a blob of data from a channel.
        /// Decode the message, set some fields and dispatch it up the chain.
        /// </summary>
        /// <param name="e"></param>
        private void ReceiveData(DataReceivedEventArgs e)
        {
            IMessageDecoder decoder = MessageDecoder(e.Data);

            if (decoder.IsRequest) {
                Request request;

                try {
                    request = decoder.DecodeRequest();
                }
                catch (Exception) {

                    if (decoder.IsReply) {
                        _Log.Warn(m => m("Message format error caused by {0}", e.EndPoint));
                    }
                    else {
                        // manually build RST from raw information
                        EmptyMessage rst = new EmptyMessage(MessageType.RST) {
                            Destination = e.EndPoint,
                            ID = decoder.ID
                        };

                        Fire(SendingEmptyMessage, rst);

                        _channel.Send(Serialize(rst), e.Session, rst.Destination);

                        _Log.Warn(m => m("Message format error caused by {0} and reseted.", e.EndPoint));
                    }
                    return;
                }

                request.Source = e.EndPoint;
                request.Session = e.Session;

                Fire(ReceivingRequest, request);

                if (!request.IsCancelled) {
                    Exchange exchange = _matcher.ReceiveRequest(request);
                    if (exchange != null) {
                        exchange.EndPoint = this;
                        _coapStack.ReceiveRequest(exchange, request);
                    }
                }
            }
            else if (decoder.IsResponse) {
                Response response;

                try {
                    response = decoder.DecodeResponse();
                }
                catch (Exception ex) {
                    _Log.Debug(m => m("ReceiveData: Decode Response Failed  data={0}\nException={1}", BitConverter.ToString(e.Data), ex.ToString()));
                    return;
                }

                response.Source = e.EndPoint;
                _Log.Debug(m => m("ReceiveData: {0}", Util.Utils.ToString(response)));

                Fire(ReceivingResponse, response);

                if (!response.IsCancelled) {
                    Exchange exchange = _matcher.ReceiveResponse(response);
                    if (exchange != null) {
                        response.RTT = (DateTime.Now - exchange.Timestamp).TotalMilliseconds;
                        exchange.EndPoint = this;
                        _coapStack.ReceiveResponse(exchange, response);
                    }
                    else if (response.Type != MessageType.ACK) {
                        _Log.Debug(m => m("Rejecting unmatchable response from {0}", e.EndPoint));
                        Reject(response);
                    }
                }
            }
            else if (decoder.IsEmpty) {
                EmptyMessage message;

                try {
                    message = decoder.DecodeEmptyMessage();
                }
                catch (Exception ex) {
                    _Log.Debug(m => m("ReceiveData: Decode Empty Failed  data={0}\nException={1}", BitConverter.ToString(e.Data), ex.ToString()));
                    return;
                }

                message.Source = e.EndPoint;

                Fire(ReceivingEmptyMessage, message);

                if (!message.IsCancelled) {
                    // CoAP Ping
                    if (message.Type == MessageType.CON || message.Type == MessageType.NON) {
                        _Log.Debug(m => m("Responding to ping by {0}", e.EndPoint));
                        Reject(message);
                    }
                    else {
                        Exchange exchange = _matcher.ReceiveEmptyMessage(message);
                        if (exchange != null) {
                            exchange.EndPoint = this;
                            _coapStack.ReceiveEmptyMessage(exchange, message);
                        }
                    }
                }
            }
            else {
                _Log.Debug(m => m("Silently ignoring non-CoAP message from {0}", e.EndPoint));
            }
        }

        private void Reject(Message message)
        {
            EmptyMessage rst = EmptyMessage.NewRST(message);

            Fire(SendingEmptyMessage, rst);

            if (!rst.IsCancelled)
                _channel.Send(Serialize(rst), null /*message.Session*/, rst.Destination);
        }

        private Byte[] Serialize(EmptyMessage message)
        {
            Byte[] bytes = message.Bytes;
            if (bytes == null) {
                bytes = MessageEncoder().Encode(message);  //  Spec.NewMessageEncoder().Encode(message);
                message.Bytes = bytes;
            }
            return bytes;
        }

        private Byte[] Serialize(Request request)
        {
            Byte[] bytes = request.Bytes;
            if (bytes == null) {
                bytes = MessageEncoder().Encode(request); //  Spec.NewMessageEncoder().Encode(request);
                request.Bytes = bytes;
            }
            return bytes;
        }

        private Byte[] Serialize(Response response)
        {
            Byte[] bytes = response.Bytes;
            if (bytes == null) {
                bytes = MessageEncoder().Encode(response); // Spec.NewMessageEncoder().Encode(response);
                response.Bytes = bytes;
            }
            return bytes;
        }

        private void Fire<T>(EventHandler<MessageEventArgs<T>> handler, T msg) where T : Message
        {
            if (handler != null) {
                handler(this, new MessageEventArgs<T>(msg));
            }
        }

        static IChannel NewUDPChannel(Int32 port, ICoapConfig config)
        {
            UDPChannel channel = new UDPChannel(port) {
                ReceiveBufferSize = config.ChannelReceiveBufferSize,
                SendBufferSize = config.ChannelSendBufferSize,
                ReceivePacketSize = config.ChannelReceivePacketSize
            };
            return channel;
        }

        static IChannel NewUDPChannel(System.Net.EndPoint localEndPoint, ICoapConfig config)
        {
            UDPChannel channel = new UDPChannel(localEndPoint) {
                ReceiveBufferSize = config.ChannelReceiveBufferSize,
                SendBufferSize = config.ChannelSendBufferSize,
                ReceivePacketSize = config.ChannelReceivePacketSize
            };
            return channel;
        }

        void IOutbox.SendRequest(Exchange exchange, Request request)
        {
            _matcher.SendRequest(exchange, request);

            Fire(SendingRequest, request);

            if (!request.IsCancelled) {
                if (request.Session == null) {
                    request.Session = _channel.GetSession(request.Destination);
                }
                _channel.Send(Serialize(request), request.Session, request.Destination);
            }
        }

        void IOutbox.SendResponse(Exchange exchange, Response response)
        {
            _matcher.SendResponse(exchange, response);

            Fire(SendingResponse, response);

            if (!response.IsCancelled) {
                _channel.Send(Serialize(response), response.Session, response.Destination);
            }
        }

        void IOutbox.SendEmptyMessage(Exchange exchange, EmptyMessage message)
        {
            _matcher.SendEmptyMessage(exchange, message);

            Fire(SendingEmptyMessage, message);

            if (!message.IsCancelled) {
                _channel.Send(Serialize(message), exchange.Request.Session, message.Destination);
            }
        }
    }
}
