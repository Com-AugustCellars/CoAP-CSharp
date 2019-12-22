using System;

using System.Net;
using System.Collections.Concurrent;
using System.Threading;
using Com.AugustCellars.CoAP.Channel;
using Com.AugustCellars.COSE;
using PeterO.Cbor;

using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Security;
using DataReceivedEventArgs = Com.AugustCellars.CoAP.Channel.DataReceivedEventArgs;

namespace Com.AugustCellars.CoAP.DTLS
{
    /// <summary>
    /// Return information about a specific DTLS session
    /// </summary>
    public  class DTLSSession : ISecureSession
    {
        private DtlsClient _client;
        private DtlsTransport _dtlsSession;
        private readonly OurTransport _transport;
        private readonly TlsKeyPair _userKey;
        private readonly KeySet _userKeys;
        private readonly TlsKeyPairSet _serverKeys;

        private readonly EventHandler<DataReceivedEventArgs> _dataReceived;

        // private readonly EventHandler<SessionEventArgs> _sessionEvents;

        public EventHandler<TlsEvent> TlsEventHandler;
#if SUPPORT_TLS_CWT
        private KeySet CwtTrustKeySet { get; }
#endif

        /// <summary>
        /// List of event handlers to inform about session events.
        /// </summary>
        public event EventHandler<SessionEventArgs> SessionEvent;

        /// <summary>
        /// Create a session for the server side
        /// </summary>
        /// <param name="ipEndPoint">Where to talk from</param>
        /// <param name="dataReceived">Where to send receive events</param>
        /// <param name="serverKeys">Server authentication keys - asymmetric</param>
        /// <param name="userKeys">Shared secret keys</param>
        /// <param name="cwtTrustKeySet">Keys used for trusting CWT authentication</param>
        public DTLSSession(IPEndPoint ipEndPoint, EventHandler<DataReceivedEventArgs> dataReceived, TlsKeyPairSet serverKeys, KeySet userKeys, KeySet cwtTrustKeySet = null)
        {
            EndPoint = ipEndPoint;
            _dataReceived = dataReceived;
            _userKeys = userKeys ?? throw new ArgumentNullException(nameof(userKeys));
            _serverKeys = serverKeys;
            _transport = new OurTransport(ipEndPoint);
#if SUPPORT_TLS_CWT
            CwtTrustKeySet = cwtTrustKeySet;
#endif
        }

        /// <summary>
        /// Create a session for initiating a session.
        /// </summary>
        /// <param name="ipEndPoint">Where to talk from</param>
        /// <param name="dataReceived">Where to send receive events</param>
        /// <param name="privKey">user authentication key</param>
        /// <param name="cwtTrustKeys">Authentication keys for CWTs</param>
        public DTLSSession(IPEndPoint ipEndPoint, EventHandler<DataReceivedEventArgs> dataReceived, TlsKeyPair privKey, KeySet cwtTrustKeys = null)
        {
            EndPoint = ipEndPoint;
            _dataReceived = dataReceived;
            _userKey = privKey ?? throw new ArgumentNullException(nameof(privKey));
#if SUPPORT_TLS_CWT
            CwtTrustKeySet = cwtTrustKeys;
#endif
            _transport = new OurTransport(ipEndPoint);
        }

        public OneKey AuthenticationKey { get; private set; }
        public Certificate AuthenticationCertificate { get; private set; }

        /// <inheritdoc/>
        public bool IsReliable => false;

        /// <summary>
        /// True means that it is supported, False means that it may be supported.
        /// </summary>
        public bool BlockTransfer { get; set; } = true;

        /// <summary>
        /// Max message size 
        /// </summary>
        public int MaxSendSize { get; set; } = 1152;

        /// <summary>
        /// Queue of items to be written on the session.
        /// </summary>
        public ConcurrentQueue<QueueItem> Queue { get; } = new ConcurrentQueue<QueueItem>();

        /// <summary>
        /// Endpoint to which the session is connected.
        /// </summary>
        public IPEndPoint EndPoint { get; }


        /// <summary>
        /// Create the DTLS connection over a specific UDP channel.
        /// We already know what address we are going to use
        /// </summary>
        /// <param name="udpChannel">UDP channel to use</param>
        public void Connect(UDPChannel udpChannel)
        {
#if SUPPORT_TLS_CWT
            if (CwtTrustKeySet != null) {
                _client = new DtlsClient(null, _userKey, CwtTrustKeySet);
            }
            else { 
#endif
                if (_userKey.PrivateKey.HasKeyType((int) GeneralValuesInt.KeyType_Octet)) {
                    CBORObject kid = _userKey.PrivateKey[CoseKeyKeys.KeyIdentifier];

                    BasicTlsPskIdentity pskIdentity;
                    pskIdentity = new BasicTlsPskIdentity(kid != null ? kid.GetByteString() : new byte[0],
                        _userKey.PrivateKey[CoseKeyParameterKeys.Octet_k].GetByteString());
                    _client = new DtlsClient(null, pskIdentity);
                }
                else if (_userKey.PrivateKey.HasKeyType((int) GeneralValuesInt.KeyType_EC2)) {
                    _client = new DtlsClient(null, _userKey);
                }
#if SUPPORT_TLS_CWT
            }
#endif

            _client.TlsEventHandler += OnTlsEvent;

            DtlsClientProtocol clientProtocol = new DtlsClientProtocol(new SecureRandom());

            _transport.UDPChannel = udpChannel;
            AuthenticationKey = _userKey.PrivateKey;

            _listening = 1;
            DtlsTransport dtlsClient = clientProtocol.Connect(_client, _transport);
            _listening = 0;
            _dtlsSession = dtlsClient;

            //  We are now in the world of a connected system -
            //  We need to do the receive calls

            new Thread(StartListen).Start();
        }

        /// <summary>
        /// Start up a session on the server side
        /// </summary>
        /// <param name="udpChannel">What channel are we on</param>
        /// <param name="message">What was the last message we got?</param>
        public void Accept(UDPChannel udpChannel, byte[] message)
        {
            DtlsServerProtocol serverProtocol = new DtlsServerProtocol(new SecureRandom());

            DtlsServer server = new DtlsServer(_serverKeys, _userKeys);
            server.TlsEventHandler += OnTlsEvent;
#if SUPPORT_TLS_CWT
            server.CwtTrustKeySet = CwtTrustKeySet;
#endif

            _transport.UDPChannel = udpChannel;
            _transport.Receive(message);

            //  Make sure we do not startup a listing thread as the correct call is always made
            //  byt the DTLS accept protocol.


            _listening = 1;
            DtlsTransport dtlsServer = serverProtocol.Accept(server, _transport);
            _listening = 0;

            _dtlsSession = dtlsServer;
            AuthenticationKey = server.AuthenticationKey;
            AuthenticationCertificate = server.AuthenticationCertificate;

            new Thread(StartListen).Start();
        }

        /// <summary>
        /// Stop the current session
        /// </summary>
        public void Stop()
        {
            if (_dtlsSession != null) {
                _dtlsSession.Close();
                EventHandler<SessionEventArgs> h = SessionEvent;
                if (h != null) {
                    SessionEventArgs thisEvent = new SessionEventArgs(SessionEventArgs.SessionEvent.Closed, this);
                    h(this, thisEvent);
                }
                _dtlsSession = null;
            }
            _client = null;
        }


        private Int32 _writing;
        private readonly Object _writeLock = new Object();

        /// <summary>
        /// If there is data in the write queue, push it out
        /// </summary>
        public void WriteData()
        {
            if (Queue.Count == 0) {
                return;
            }

            lock (_writeLock) {
                if (_writing > 0) {
                    return;
                }

                _writing = 1;
            }

            while (Queue.Count > 0) {
                QueueItem q;
                if (!Queue.TryDequeue(out q)) {
                    break;
                }

                if (_dtlsSession != null) {
                    _dtlsSession.Send(q.Data, 0, q.Data.Length);
                }
            }

            lock (_writeLock) {
                _writing = 0;
                if (Queue.Count > 0) {
                    WriteData();
                }
            }
        }

        /// <summary>
        /// Event handler to deal with data coming from the UDP Channel
        /// </summary>
        /// <param name="sender">Event creator</param>
        /// <param name="e">Event data</param>
        public void ReceiveData(Object sender, DataReceivedEventArgs e)
        {
            _transport.Receive(e.Data);
            lock (_transport.Queue) {
                if (_listening == 0) {
                    new Thread(StartListen).Start();
                }
            }
        }

        private int _listening;

        /// <summary>
        /// Independent function that runs on a separate thread.
        /// If there is nothing left in the queue, then the thread will exit.
        /// </summary>
        void StartListen()
        {
            if (Interlocked.CompareExchange(ref _listening, 1, 0) > 0) {
                return;
            }

            byte[] buf = new byte[2000];
            while (true) {
                int size = -1;
                try {
                    size = _dtlsSession.Receive(buf, 0, buf.Length, 1000);
                }
                catch (Exception) {
                }

                if (size == -1) {
                    lock (_transport.Queue) {
                        if (_transport.Queue.Count == 0) {
                            Interlocked.Exchange(ref _listening, 0);
                            return;
                        }
                    }
                }
                else {
                    byte[] buf2 = new byte[size];
                    Array.Copy(buf, buf2, size);
                    FireDataReceived(buf2, EndPoint, null);  // M00BUG
                }
            }
        }

        private void FireDataReceived(Byte[] data, System.Net.EndPoint ep, System.Net.EndPoint epLocal)
        {
            EventHandler<DataReceivedEventArgs> h = _dataReceived;
            if (h != null) {
                h(this, new DataReceivedEventArgs(data, ep, epLocal, this));
            }
        }

        private void OnTlsEvent(Object o, TlsEvent e)
        {
            EventHandler<TlsEvent> handler = TlsEventHandler;
            if (handler != null) {
                handler(o, e);
            }

        }

        private class OurTransport : DatagramTransport
        {
            private readonly System.Net.EndPoint _ep;


            public OurTransport(System.Net.EndPoint ep)
            {
                _ep = ep;
            }

            public UDPChannel UDPChannel { get; set; }

            public void Close()
            {
//                _udpChannel = null;
                lock (Queue) {
                    Monitor.PulseAll(Queue);
                }
            }

            public int GetReceiveLimit()
            {
                int limit = UDPChannel.ReceiveBufferSize;
                if (limit <= 0) limit = 1149;
                return limit;
            }

            public int GetSendLimit()
            {
                int limit = UDPChannel.SendBufferSize;
                if (limit <= 0) {
                    limit = 1149;
                }

                return limit;
            }

            public int Receive(byte[] buf, int off, int len, int waitMillis)
            {
                lock (Queue) {
                    if (Queue.Count < 1) {
                        try {
                            Monitor.Wait(Queue, waitMillis);
                        }
#if NETSTANDARD1_3
                        catch (ThreadStateException) {
                            // TODO keep waiting until full wait expired?
                        }
#else
                        catch (ThreadInterruptedException) {
                            // TODO Keep waiting until full wait expired?
                        }
#endif
                        if (Queue.Count < 1) {
                            return -1;
                        }
                    }

                    byte[] packet;
                    Queue.TryDequeue(out packet);
                    int copyLength = Math.Min(len, packet.Length);
                    Array.Copy(packet, 0, buf, off, copyLength);
                    // Debug.Print($"OurTransport::Receive - EP:{_ep} Data Length: {packet.Length}");
                    // Debug.Print(BitConverter.ToString(buf, off, copyLength));
                    return copyLength;
                }
            }

            public void Send(byte[] buf, int off, int len)
            {
                // Debug.Print($"OurTransport::Send Data Length: {len}");
                // Debug.Print(BitConverter.ToString(buf, off, len));
                byte[] newBuf = new byte[len];
                Array.Copy(buf, off, newBuf, 0, newBuf.Length);
                buf = newBuf;
                UDPChannel.Send(buf, UDPChannel, _ep);
            }

            public ConcurrentQueue<byte[]> Queue { get; } = new ConcurrentQueue<byte[]>();

            public void Receive(byte[] buf)
            {
                lock (Queue) {
                    Queue.Enqueue(buf);
                    Monitor.PulseAll(Queue);
                }
            }

        }
    }
}
