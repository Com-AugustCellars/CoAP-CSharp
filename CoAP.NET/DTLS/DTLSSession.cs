using System;

using System.Net;
using System.Collections.Concurrent;
using System.Diagnostics;
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
        private readonly IPEndPoint _ipEndPoint;
        private DtlsTransport _dtlsSession;
        private readonly OurTransport _transport;
        private readonly OneKey _userKey;
        private readonly KeySet _userKeys;
        private readonly KeySet _serverKeys;
        private OneKey _authKey;

        private readonly ConcurrentQueue<QueueItem> _queue = new ConcurrentQueue<QueueItem>();
        private readonly EventHandler<DataReceivedEventArgs> _dataReceived;

        private readonly EventHandler<SessionEventArgs> _sessionEvents;

        /// <summary>
        /// List of event handlers to inform about session events.
        /// </summary>
        public event EventHandler<SessionEventArgs> SessionEvent;

        public DTLSSession(IPEndPoint ipEndPoint, EventHandler<DataReceivedEventArgs> dataReceived, OneKey userKey)
        {
            _ipEndPoint = ipEndPoint;
            _dataReceived = dataReceived;
            _userKey = userKey;
            _transport = new OurTransport(ipEndPoint);
        }

        public DTLSSession(IPEndPoint ipEndPoint, EventHandler<DataReceivedEventArgs> dataReceived, KeySet serverKeys, KeySet userKeys)
        {
            _ipEndPoint = ipEndPoint;
            _dataReceived = dataReceived;
            _userKeys = userKeys;
            _serverKeys = serverKeys;
            _transport = new OurTransport(ipEndPoint);
        }

        public OneKey AuthenticationKey
        {
            get => _authKey;
        }
        /// <inheritdoc/>
        public bool IsReliable
        {
            get => false;
        }

        /// <summary>
        /// Queue of items to be written on the session.
        /// </summary>
        public ConcurrentQueue<QueueItem> Queue
        {
            get => _queue;
        }

        /// <summary>
        /// Endpoint to which the session is connected.
        /// </summary>
        public IPEndPoint EndPoint
        {
            get => _ipEndPoint;
        }


        /// <summary>
        /// Create the DTLS connection over a specific UDP channel.
        /// We already know what address we are going to use
        /// </summary>
        /// <param name="udpChannel">UDP channel to use</param>
        public void Connect(UDPChannel udpChannel)
        {
            BasicTlsPskIdentity pskIdentity = null;

            if (_userKey != null) {
                if (_userKey.HasKeyType((int) COSE.GeneralValuesInt.KeyType_Octet)) {
                    CBORObject kid = _userKey[COSE.CoseKeyKeys.KeyIdentifier];

                    if (kid != null) {
                        pskIdentity = new BasicTlsPskIdentity(kid.GetByteString(), _userKey[CoseKeyParameterKeys.Octet_k].GetByteString());
                    }
                    else {
                        pskIdentity = new BasicTlsPskIdentity(new byte[0], _userKey[CoseKeyParameterKeys.Octet_k].GetByteString());
                    }
                }   
            }
            _client = new DtlsClient(null, pskIdentity);

            DtlsClientProtocol clientProtocol = new DtlsClientProtocol(new SecureRandom());

            _transport.UDPChannel = udpChannel;
            _authKey = _userKey;

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

            _transport.UDPChannel = udpChannel;
            _transport.Receive(message);

            //  Make sure we do not startup a listing thread as the correct call is always made
            //  byt the DTLS accept protocol.


            _listening = 1;
            DtlsTransport dtlsServer = serverProtocol.Accept(server, _transport);
            _listening = 0;

            _dtlsSession = dtlsServer;
            _authKey = server.AuthenticationKey;

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
            if (_queue.Count == 0)
                return;
            lock (_writeLock) {
                if (_writing > 0)
                    return;
                _writing = 1;
            }

            while (Queue.Count > 0) {
                QueueItem q;
                if (!_queue.TryDequeue(out q))
                    break;

                if (_dtlsSession != null) {
                    _dtlsSession.Send(q.Data, 0, q.Data.Length);
                }
            }

            lock (_writeLock) {
                _writing = 0;
                if (_queue.Count > 0)
                    WriteData();
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
            if (System.Threading.Interlocked.CompareExchange(ref _listening, 1, 0) > 0) {
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
                            System.Threading.Interlocked.Exchange(ref _listening, 0);
                            return;
                        }
                    }
                }
                byte[] buf2 = new byte[size];
                Array.Copy(buf, buf2, size);
                FireDataReceived(buf2, _ipEndPoint);
            }
        }

        private void FireDataReceived(Byte[] data, System.Net.EndPoint ep)
        {
            EventHandler<DataReceivedEventArgs> h = _dataReceived;
            if (h != null) {
                h(this, new DataReceivedEventArgs(data, ep, this));
            }
        }

        private class OurTransport : DatagramTransport
        {
            private UDPChannel _udpChannel;
            private readonly System.Net.EndPoint _ep;


            public OurTransport(System.Net.EndPoint ep)
            {
                _ep = ep;
            }

            public UDPChannel UDPChannel
            {
                set => _udpChannel = value;
            }

            public void Close()
            {
//                _udpChannel = null;
                lock (_receivingQueue) {
                    Monitor.PulseAll(_receivingQueue);
                }
            }

            public int GetReceiveLimit()
            {
                int limit = _udpChannel.ReceiveBufferSize;
                if (limit <= 0) limit = 1149;
                return limit;
            }

            public int GetSendLimit()
            {
                int limit = _udpChannel.SendBufferSize;
                if (limit <= 0)
                    limit = 1149;
                return limit;
            }

            public int Receive(byte[] buf, int off, int len, int waitMillis)
            {
                lock (_receivingQueue) {
                    if (_receivingQueue.Count < 1) {
                        try {
                          Monitor.Wait(_receivingQueue, waitMillis);
                        }
                        catch (ThreadInterruptedException) {
                            // TODO Keep waiting until full wait expired?
                        }
                        if (_receivingQueue.Count < 1) {
                            return -1;
                        }
                    }

                    byte[] packet;
                    _receivingQueue.TryDequeue(out packet);
                    int copyLength = Math.Min(len, packet.Length);
                    Array.Copy(packet, 0, buf, off, copyLength);
                    Debug.Print($"OurTransport::Receive - EP:{_ep} Data Length: {packet.Length}");
                    Debug.Print(BitConverter.ToString(buf, off, copyLength));
                    return copyLength;
                }
            }

            public void Send(byte[] buf, int off, int len)
            {
                Debug.Print($"OurTransport::Send Data Length: {len}");
                Debug.Print(BitConverter.ToString(buf, off, len));
                byte[] newBuf = new byte[len];
                Array.Copy(buf, off, newBuf, 0, newBuf.Length);
                buf = newBuf;
                _udpChannel.Send(buf, _udpChannel, _ep);
            }

            private readonly ConcurrentQueue<byte[]> _receivingQueue = new ConcurrentQueue<byte[]>();

            public ConcurrentQueue<byte[]> Queue
            {
                get => _receivingQueue;
            }

            public void Receive(byte[] buf)
            {
                lock (_receivingQueue) {
                    _receivingQueue.Enqueue(buf);
                    Monitor.PulseAll(_receivingQueue);
                }
            }

        }
    }
}
