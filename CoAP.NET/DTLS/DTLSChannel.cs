using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using System.Net;
using System.Net.Sockets;
using Com.AugustCellars.CoAP.Channel;
using Com.AugustCellars.CoAP.Net;
using Org.BouncyCastle.Crypto.Tls;
using Com.AugustCellars.COSE;

namespace Com.AugustCellars.CoAP.DTLS
{
    /// <summary>
    /// Channel implementation to support DTLS that 
    /// </summary>
    public class DTLSChannel : IChannel
    {
        private System.Net.EndPoint _localEP;
        private Int32 _receiveBufferSize;
        private Int32 _sendBufferSize;
        private Int32 _receivePacketSize;
        private readonly int _port;
        private UDPChannel _udpChannel;
        private KeySet _serverKeys;
        private KeySet _userKeys;

        public DTLSChannel(KeySet serverKeys, KeySet userKeys) : this(serverKeys, userKeys, 0)
        {
        }

        public DTLSChannel(KeySet serverKeys, KeySet userKeys, Int32 port)
        {
            _port = port;
            _userKeys = userKeys;
            _serverKeys = serverKeys;
        }

        /// <summary>
        /// Create a DTLS channel with remote ad local keys.
        /// </summary>
        /// <param name="serverKeys"></param>
        /// <param name="userKeys"></param>
        /// <param name="ep"></param>
        public DTLSChannel(KeySet serverKeys, KeySet userKeys, System.Net.EndPoint ep)
        {
            _localEP = ep;
            _userKeys = userKeys;
            _serverKeys = serverKeys;
        }

        /// <inheritdoc/>
        public event EventHandler<DataReceivedEventArgs> DataReceived;

        /// <inheritdoc/>
        public System.Net.EndPoint LocalEndPoint {
            get { return _udpChannel == null ? (_localEP ?? new IPEndPoint(IPAddress.IPv6Any, _port)) : _udpChannel.LocalEndPoint; }
        }

        /// <summary>
        /// Gets or sets the <see cref="Socket.ReceiveBufferSize"/>.
        /// </summary>
        public Int32 ReceiveBufferSize {
            get { return _receiveBufferSize; }
            set { _receiveBufferSize = value; }
        }
        /// <summary>
        /// Gets or sets the <see cref="Socket.SendBufferSize"/>.
        /// </summary>
        public Int32 SendBufferSize {
            get { return _sendBufferSize; }
            set { _sendBufferSize = value; }
        }

        /// <summary>
        /// Gets or sets the size of buffer for receiving packet.
        /// The default value is <see cref="DefaultReceivePacketSize"/>.
        /// </summary>
        public Int32 ReceivePacketSize {
            get { return _receivePacketSize; }
            set { _receivePacketSize = value; }
        }

        private Int32 _running;

        public void Start()
        {
            if (System.Threading.Interlocked.CompareExchange(ref _running, 1, 0) > 0) {
                return;
            }

            if (_udpChannel == null) {


                if (_localEP != null) {
                    _udpChannel = new UDPChannel(_localEP);
                }
                else {
                    _udpChannel = new UDPChannel(_port);

                }
            }

            _udpChannel.DataReceived += ReceiveData;

            _udpChannel.Start();
        }

        public void Stop()
        {
            lock (_sessionList) {
                foreach (DTLSSession session in _sessionList) {
                    session.Stop();
                }
                _sessionList.Clear();
            }
            _udpChannel.Stop();
        }

        public void Dispose()
        {
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
                if (session != null)
                    return session;

                //  No session - create a new one.

                session = new DTLSSession(ipEndPoint, DataReceived, _serverKeys, _userKeys);
                AddSession(session);


                session.Connect(_udpChannel);
            }
            catch {
                ;
            }

            return session;
        }

        public void Send(byte[] data, ISession sessionReceive, System.Net.EndPoint ep)
        {
            try {
                IPEndPoint ipEP = (IPEndPoint) ep;

                DTLSSession session = FindSession(ipEP);
                if (session == null) {

                    session = new DTLSSession(ipEP, DataReceived, _serverKeys, _userKeys);
                    AddSession(session);
                    session.Connect(_udpChannel);
                }
                else if (session != sessionReceive) {
                    //  Don't send it
                    return;
                }
                session.Queue.Enqueue(new QueueItem(/*null, */ data));
                session.WriteData();
            }
            catch (Exception e) {
                Console.WriteLine("Error in DTLSClientChannel Sending - " + e.ToString());
            }
        }

        private void ReceiveData(Object sender, DataReceivedEventArgs e)
        {
            lock (_sessionList) {
                foreach (DTLSSession session in _sessionList) {
                    if (e.EndPoint.Equals(session.EndPoint)) {
                        session.ReceiveData(sender, e);

                        return;
                    }
                }

                DTLSSession sessionNew = new DTLSSession((IPEndPoint) e.EndPoint, DataReceived, _serverKeys, _userKeys);
                _sessionList.Add(sessionNew);
                new Thread(() => Accept(sessionNew, e.Data)).Start();
            }
        }

        private void Accept(DTLSSession session, byte[] message)
        {
            try {
                session.Accept(_udpChannel, message);
            }
            catch (Exception) {
                lock (_sessionList) {
                    _sessionList.Remove(session);
                }
            }
            
        }



        private static List<DTLSSession> _sessionList = new List<DTLSSession>();
        private static void AddSession(DTLSSession session)
        {
            lock (_sessionList) {
                _sessionList.Add(session);
            }
        }

        private static DTLSSession FindSession(IPEndPoint ipEP)
        {
            lock (_sessionList) {

                foreach (DTLSSession session in _sessionList) {
                    if (session.EndPoint.Equals(ipEP))
                        return session;
                }
            }

            return null;
        }
    }
}

