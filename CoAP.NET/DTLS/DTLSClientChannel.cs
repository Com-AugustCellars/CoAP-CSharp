using System;
using System.Collections.Generic;

using System.Net;
using System.Net.Sockets;
using Com.AugustCellars.CoAP.Channel;
using Com.AugustCellars.CoAP.Log;
using Com.AugustCellars.COSE;
#if SUPPORT_TLS_CWT
using Com.AugustCellars.WebToken;
#endif

namespace Com.AugustCellars.CoAP.DTLS
{
    /// <summary>
    /// Low level channel for DTLS when dealing only with clients.
    /// This channel will not accept any connections from other parties.
    /// </summary>
    internal class DTLSClientChannel : IChannel
    {
        public const Int32 DefaultReceivePacketSize = 4096;

        private readonly System.Net.EndPoint _localEndPoint;
        private readonly int _port;
        private UDPChannel _udpChannel;
        private readonly TlsKeyPair _userKey;
        public KeySet CwtTrustKeySet { get; set; }

        public EventHandler<TlsEvent> TlsEventHandler;
        private readonly ILogger _log = LogManager.GetLogger(nameof(DTLSClientChannel));


        public DTLSClientChannel(TlsKeyPair userKey, int port)
        {
            _port = port;
            _userKey = userKey ?? throw new ArgumentNullException(nameof(userKey));
        }

        /// <summary>
        /// Create a client only channel and use a given endpoint
        /// </summary>
        /// <param name="userKey">Authentication Key</param>
        /// <param name="ep">client side endpoint</param>
        public DTLSClientChannel(TlsKeyPair userKey, System.Net.EndPoint ep)
        {
            _localEndPoint = ep;
            _userKey = userKey ?? throw new ArgumentNullException(nameof(userKey));
        }


        /// <inheritdoc/>
        public event EventHandler<DataReceivedEventArgs> DataReceived;

        /// <inheritdoc/>
        public System.Net.EndPoint LocalEndPoint => _udpChannel == null ? (_localEndPoint ?? new IPEndPoint(IPAddress.IPv6Any, _port)) : _udpChannel.LocalEndPoint;

        /// <summary>
        /// Gets or sets the <see cref="Socket.ReceiveBufferSize"/>.
        /// </summary>
        public int ReceiveBufferSize { get; set; } = DefaultReceivePacketSize;

        /// <summary>
        /// Gets or sets the <see cref="Socket.SendBufferSize"/>.
        /// </summary>
        public int SendBufferSize { get; set; }

        /// <summary>
        /// Gets or sets the size of buffer for receiving packet.
        /// The default value is <see cref="DefaultReceivePacketSize"/>.
        /// </summary>
        public int ReceivePacketSize { get; set; }

        private int _running;

        /// <inheritdoc/>
        public bool AddMulticastAddress(IPEndPoint ep)
        {
            return false;
        }

        /// <summary>
        /// Tell the channel to set itself up and start processing data
        /// </summary>
        public void Start()
        {
            if (System.Threading.Interlocked.CompareExchange(ref _running, 1, 0) > 0) {
                return;
            }

            if (_udpChannel == null) {
                if (_localEndPoint != null) {
                    _udpChannel = new UDPChannel(_localEndPoint);
                }
                else {
                    _udpChannel = new UDPChannel(_port);
                }
            }

            _udpChannel.DataReceived += ReceiveData;
            if (SendBufferSize > 0) _udpChannel.SendBufferSize = SendBufferSize;
            if (ReceiveBufferSize > 0) _udpChannel.ReceiveBufferSize = ReceiveBufferSize;

            _udpChannel.Start();            
        }

        /// <summary>
        /// Tell the channel to stop processing data and clean itself up.
        /// </summary>
        public void Stop()
        {
            if (System.Threading.Interlocked.Exchange(ref _running, 0) == 0) {
                return;
            }

            lock (_sessionList) {
                foreach (DTLSSession session in _sessionList) {
                    session.Stop();
                }
                _sessionList.Clear();
            }
            _udpChannel.Stop();
        }

        /// <summary>
        /// We don't do anything for this right now because we don't have sessions.
        /// </summary>
        /// <param name="session"></param>
        public void Abort(ISession session)
        {
            return;
        }

        /// <summary>
        /// We don't do anything for this right now because we don't have sessions.
        /// </summary>
        /// <param name="session"></param>
        public void Release(ISession session)
        {
            return;
        }

        /// <summary>
        /// Tell the channel to release all of it's resources
        /// </summary>
        public void Dispose()
        {
            Stop();
            _udpChannel.Dispose();
        }

        /// <summary>
        /// Get an existing session.  If one does not exist then create it and try
        /// to make a connection.
        /// </summary>
        /// <returns>session to use</returns>
        public ISession GetSession(System.Net.EndPoint ep)
        {
            DTLSSession session = null;
            try {
                IPEndPoint ipEndPoint = (IPEndPoint) ep;

                //  Do we already have a session setup for this?

                session = FindSession(ipEndPoint);
                if (session != null) return session;

                //  No session - create a new one.

#if SUPPORT_TLS_CWT
                if (CwtTrustKeySet != null) {
                    session = new DTLSSession(ipEndPoint, DataReceived,  _userKey, CwtTrustKeySet);
                }
                else {
#endif
                    session = new DTLSSession(ipEndPoint, DataReceived, _userKey);
#if SUPPORT_TLS_CWT
                }
#endif

                session.TlsEventHandler += OnTlsEvent;
                AddSession(session);

                session.Connect(_udpChannel);
            }
            catch {
                _log.Error("Failed to establish a DTLS session");
            }


            return session;
        }

        private void OnTlsEvent(object o, TlsEvent e)
        {
            EventHandler<TlsEvent> handler = TlsEventHandler;
            if (handler != null) {
                handler(o, e);
            }

        }

        /// <summary>
        /// Send data through the DTLS channel to other side
        /// </summary>
        /// <param name="data">Data to be sent</param>
        /// <param name="sessionReceive">What session was the request on</param>
        /// <param name="ep">Where to send it</param>
        public void Send(byte[] data, ISession sessionReceive, System.Net.EndPoint ep)
        {
            try {
                //  We currently only support IP addresses with this channel.
                //  This is a restriction is enforce from BouncyCastle where
                //  that is the only end point that can be used.
    
                IPEndPoint ipEndPoint = (IPEndPoint) ep;

                DTLSSession session = FindSession(ipEndPoint);
                if (session == null) {
                    _log.Warn("Setup a new session - ");

                    //  Create a new session to send with if we don't already have one

                    session = new DTLSSession(ipEndPoint, DataReceived, _userKey);
                    AddSession(session);
                    
                    session.Connect(_udpChannel);
                }
                else if (session != sessionReceive) {
                    _log.Warn("Don't send because the sessions are different");
                    return;
                }

                //  Queue the data onto the session.

                session.Queue.Enqueue(new QueueItem(data));
                session.WriteData();
            }
            catch (Exception e) {
                _log.Error($"Error in DTLSClientChannel Sending - {e}");
                throw;
            }
        }

        private void ReceiveData(object sender, DataReceivedEventArgs e)
        {
            lock (_sessionList) {
                foreach (DTLSSession session in _sessionList) {
                    if (e.EndPoint.Equals(session.EndPoint)) {
                        session.ReceiveData(sender, e);

                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Keep track of all of the sessions that have been setup on this channel.
        /// </summary>
        private readonly List<DTLSSession> _sessionList = new List<DTLSSession>();

        private void AddSession(DTLSSession session)
        {
            lock (_sessionList) {
                _sessionList.Add(session);
            }
        }

        private DTLSSession FindSession(IPEndPoint ipEndPoint)
        {
            lock (_sessionList) {

                foreach (DTLSSession session in _sessionList) {
                    if (session.EndPoint.Equals(ipEndPoint)) {
                        return session;
                    }
                }
            }
            return null;
        }
    }
}
