/*
 * Copyright (c) 2011-2014, Longxiang He <helongxiang@smeshlink.com>,
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Com.AugustCellars.CoAP.Channel
{
    /// <summary>
    /// Channel via UDP protocol.
    /// </summary>
    public partial class UDPChannel : IChannel, ISession
    {
        /// <summary>
        /// Default size of buffer for receiving packet.
        /// </summary>
        public const int DefaultReceivePacketSize = 4096;

        private readonly int _port;
        private readonly SocketSet _unicast = new SocketSet();
        private int _running;
        private int _writing;
        private readonly ConcurrentQueue<RawData> _sendingQueue = new ConcurrentQueue<RawData>();

#if LOG_UDP_CHANNEL
        private static ILogger _Log = LogManager.GetLogger("UDPChannel");
#endif

        /// <inheritdoc/>
        public event EventHandler<DataReceivedEventArgs> DataReceived;

        /// <summary>
        /// Initializes a UDP channel with a random port.
        /// </summary>
        public UDPChannel() 
            : this(0)
        { 
        }

        /// <summary>
        /// Initializes a UDP channel with the given port, both on IPv4 and IPv6.
        /// </summary>
        public UDPChannel(int port)
        {
            _port = port;
        }

        /// <summary>
        /// Initializes a UDP channel with the specific endpoint.
        /// </summary>
        public UDPChannel(System.Net.EndPoint localEP)
        {
            _unicast._localEP = (IPEndPoint) localEP;
        }

        /// <inheritdoc/>
        public System.Net.EndPoint LocalEndPoint
        {
            get
            {
                return _unicast._socket == null
                    ? (_unicast._localEP ?? new IPEndPoint(IPAddress.IPv6Any, _port))
                    : _unicast._socket.Socket.LocalEndPoint;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="Socket.ReceiveBufferSize"/>.
        /// </summary>
        public int ReceiveBufferSize { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Socket.SendBufferSize"/>.
        /// </summary>
        public int SendBufferSize { get; set; }

        /// <summary>
        /// Gets or sets the size of buffer for receiving packet.
        /// The default value is <see cref="DefaultReceivePacketSize"/>.
        /// </summary>
        public int ReceivePacketSize { get; set; } = DefaultReceivePacketSize;

        /// <summary>
        /// True means that it is supported, False means that it may be supported.
        /// </summary>
        public bool BlockTransfer { get; set; } = false;

        /// <summary>
        /// Max message size 
        /// </summary>
        public int MaxSendSize { get; set; }

#if !NETSTANDARD1_3
        private readonly List<SocketSet> _listMultiCastEndpoints = new List<SocketSet>();

        /// <inheritdoc/>
        public bool AddMulticastAddress(IPEndPoint ep) //   IPAddress ep, int port)
        {
            SocketSet s = new SocketSet() {
                _localEP = ep
            };
            _listMultiCastEndpoints.Add(s);
            if (_running == 1) {

            }
            return true;
        }
#endif

        /// <inheritdoc/>
        public void Start()
        {
            if (System.Threading.Interlocked.CompareExchange(ref _running, 1, 0) > 0) {
                return;
            }

#if LOG_UDP_CHANNEL
            _Log.Debug("Start");
#endif
            try {

                StartSocket(_unicast);

#if !NETSTANDARD1_3
                foreach (SocketSet s in _listMultiCastEndpoints) {
                    StartMulticastSocket(s);
                }
#endif
            }
            catch (Exception) {
                _running = 0;
                throw;
            }
        }

        private void StartSocket(SocketSet info)
        { 
            if (info._localEP == null) {
                try {
                    info._socket = SetupUDPSocket(AddressFamily.InterNetworkV6, ReceivePacketSize + 1); // +1 to check for > ReceivePacketSize
                }
                catch (SocketException e) {
                    if (e.SocketErrorCode == SocketError.AddressFamilyNotSupported) {
                        info._socket = null;
                    }
                    else {
                        throw;
                    }
                }

                if (info._socket == null) {
                    // IPv6 is not supported, use IPv4 instead
                    info._socket = SetupUDPSocket(AddressFamily.InterNetwork, ReceivePacketSize + 1);
                    info._socket.Socket.Bind(new IPEndPoint(IPAddress.Any, _port));
                }
                else {
                    try {
                        // Enable IPv4-mapped IPv6 addresses to accept both IPv6 and IPv4 connections in a same socket.
                        info._socket.Socket.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, 0);
                    }
                    catch {
#if LOG_UDP_CHANNEL
                        _Log.Debug("Create backup socket");
#endif
                        // IPv4-mapped address seems not to be supported, set up a separated socket of IPv4.
                        info._socketBackup = SetupUDPSocket(AddressFamily.InterNetwork, ReceivePacketSize + 1);
                    }

                    info._socket.Socket.Bind(new IPEndPoint(IPAddress.IPv6Any, _port));
                    if (info._socketBackup != null) {
                        info._socketBackup.Socket.Bind(new IPEndPoint(IPAddress.Any, _port));
                    }
                }
            }
            else {
                info._socket = SetupUDPSocket(info._localEP.AddressFamily, ReceivePacketSize + 1);

                    info._socket.Socket.Bind(info._localEP);
            }


            if (ReceiveBufferSize > 0) {
                info._socket.Socket.ReceiveBufferSize = ReceiveBufferSize;
                if (info._socketBackup != null) {
                    info._socketBackup.Socket.ReceiveBufferSize = ReceiveBufferSize;
                }
            }

            if (SendBufferSize > 0) {
                info._socket.Socket.SendBufferSize = SendBufferSize;
                if (info._socketBackup != null) {
                    info._socketBackup.Socket.SendBufferSize = SendBufferSize;
                }
            }

            BeginReceive(info);
        }

#if !NETSTANDARD1_3
        private void StartMulticastSocket(SocketSet info)
        {
            if (info._localEP.Address.IsIPv6Multicast) {
                try {
                    info._socket = SetupUDPSocket(AddressFamily.InterNetworkV6, ReceivePacketSize + 1);
                    info._socket.Socket.Bind(new IPEndPoint(IPAddress.IPv6Any, _port));

                    NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
                    foreach (NetworkInterface adapter in nics) {
                        if (!adapter.SupportsMulticast) continue;
                        if (adapter.OperationalStatus != OperationalStatus.Up) continue;
                        if (adapter.Supports(NetworkInterfaceComponent.IPv6)) {
                            IPInterfaceProperties properties = adapter.GetIPProperties();

                            foreach (UnicastIPAddressInformation ip in properties.UnicastAddresses) {
                                if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6) {
                                    IPv6InterfaceProperties v6Ip = adapter.GetIPProperties().GetIPv6Properties();
                                    IPv6MulticastOption mc = new IPv6MulticastOption(info._localEP.Address, v6Ip.Index);
                                    try {
                                        info._socket.Socket.SetSocketOption(SocketOptionLevel.IPv6,
                                                                            SocketOptionName.AddMembership,
                                                                            mc);
                                    }
#pragma warning disable 168
                                    catch (SocketException e) {
#pragma warning restore 168
#if LOG_UDP_CHANNEL
                                        _Log.Info(
                                            m => m(
                                                $"Start Multicast:  Address {info._localEP.Address} had an exception ${e.ToString()}"));
#endif
                                    }

                                    break;
                                }
                            }
                        }
                    }
                }
#pragma warning disable 168
                catch (SocketException e) {
#pragma warning restore 168
#if LOG_UDP_CHANNEL
                    _Log.Info(
                        m => m($"Start Multicast:  Address {info._localEP.Address} had an exception ${e.ToString()}"));
                    throw;
#endif
                }
            }
            else {
                try {
                    info._socket = SetupUDPSocket(AddressFamily.InterNetwork, ReceivePacketSize + 1);
                    info._socket.Socket.Bind(new IPEndPoint(IPAddress.Any, _port));

                    NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();


                    foreach (NetworkInterface adapter in nics) {
                        if (!adapter.SupportsMulticast) continue;
                        if (adapter.OperationalStatus != OperationalStatus.Up) continue;
                        if (adapter.Supports(NetworkInterfaceComponent.IPv4)) {
                            IPInterfaceProperties properties = adapter.GetIPProperties();

                            foreach (UnicastIPAddressInformation ip in properties.UnicastAddresses) {
                                if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) {
                                    MulticastOption mc = new MulticastOption(info._localEP.Address, ip.Address);
                                    info._socket.Socket.SetSocketOption(SocketOptionLevel.IP,
                                                                        SocketOptionName.AddMembership,
                                                                        mc);
                                }
                            }
                        }
                    }
                }
#pragma warning disable 168
                catch (SocketException e) {
#pragma warning restore 168
#if LOG_UDP_CHANNEL
                    _Log.Info(m => m($"Start Multicast:  Address {info._localEP.Address} had an exception ${e.ToString()}"));
#endif
                    throw;
                }
            }


            if (ReceiveBufferSize > 0) {
                info._socket.Socket.ReceiveBufferSize = ReceiveBufferSize;
                if (info._socketBackup != null) {
                    info._socketBackup.Socket.ReceiveBufferSize = ReceiveBufferSize;
                }
            }

            if (SendBufferSize > 0) {
                info._socket.Socket.SendBufferSize = SendBufferSize;
                if (info._socketBackup != null) {
                    info._socketBackup.Socket.SendBufferSize = SendBufferSize;
                }
            }

            BeginReceive(info);
        }
#endif

                    /// <inheritdoc/>
                    public void Stop()
        {
#if LOG_UDP_CHANNEL
            _Log.Debug("Stop");
#endif
            if (System.Threading.Interlocked.Exchange(ref _running, 0) == 0) {
                return;
            }

            if (_unicast._socket != null) {
                _unicast._socket.Dispose();
                _unicast._socket = null;
            }

            if (_unicast._socketBackup != null) {
                _unicast._socketBackup.Dispose();
                _unicast._socketBackup = null;
            }
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

        /// <inheritdoc/>
        public void Send(byte[] data, ISession sessionReceive, System.Net.EndPoint ep)
        {
            RawData raw = new RawData() {
                Data = data,
                EndPoint = ep
            };
            _sendingQueue.Enqueue(raw);
            if (System.Threading.Interlocked.CompareExchange(ref _writing, 1, 0) > 0) {
                return;
            }
            BeginSend();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
#if LOG_UDP_CHANNEL
            _Log.Debug("Dispose");
#endif
            Stop();
        }

        /// <inheritdoc/>
        public ISession GetSession(System.Net.EndPoint ep)
        {
            return this;
        }

        private void BeginReceive(SocketSet info)
        {
#if LOG_UDP_CHANNEL
            _Log.Debug(m => m("BeginRecieve:  _running={0}", _running));
#endif
            if (_running > 0) {
                BeginReceive(info._socket);

                if (info._socketBackup != null) {
                    BeginReceive(info._socketBackup);
                }
            }
        }

        private void EndReceive(UDPSocket socket, byte[] buffer, int offset, int count, System.Net.EndPoint ep)
        {
#if LOG_UDP_CHANNEL
            _Log.Debug(m => m("EndReceive: length={0}", count));
#endif

            if (count > 0) {
                byte[] bytes = new byte[count];
                Buffer.BlockCopy(buffer, offset, bytes, 0, count);

                if (ep.AddressFamily == AddressFamily.InterNetworkV6) {
                    IPEndPoint ipep = (IPEndPoint)ep;
                    if (IPAddressExtensions.IsIPv4MappedToIPv6(ipep.Address)) {
                        ep = new IPEndPoint(IPAddressExtensions.MapToIPv4(ipep.Address), ipep.Port);
                    }
                }

                System.Net.EndPoint epLocal = socket.Socket.LocalEndPoint;
                if (ep.AddressFamily == AddressFamily.InterNetworkV6) {
                    IPEndPoint ipLocal = (IPEndPoint) ep;
                    if (IPAddressExtensions.IsIPv4MappedToIPv6(ipLocal.Address)) {
                        epLocal = new IPEndPoint(IPAddressExtensions.MapToIPv4(ipLocal.Address), ipLocal.Port);
                    }
                }

                FireDataReceived(bytes, ep, epLocal);
            }

#if LOG_UDP_CHANNEL
            _Log.Debug("EndReceive: restart the read");
#endif
            BeginReceive(socket);
        }

        private void EndReceive(UDPSocket socket, Exception ex)
        {
#if LOG_UDP_CHANNEL
            _Log.Warn("EndReceive: Fatal on receive ", ex);
#endif
            BeginReceive(socket);
        }

        private void FireDataReceived(byte[] data, System.Net.EndPoint ep, System.Net.EndPoint epLocal)
        {
#if LOG_UDP_CHANNEL
            _Log.Debug(m => m("FireDataReceived: data length={0}", data.Length));
#endif
            EventHandler<DataReceivedEventArgs> h = DataReceived;
            if (h != null) {
                h(this, new DataReceivedEventArgs(data, ep, epLocal, this));
            }
        }

        private void BeginSend()
        {
            if (_running == 0) {
                return;
            }

            RawData raw;
            if (!_sendingQueue.TryDequeue(out raw)) {
                System.Threading.Interlocked.Exchange(ref _writing, 0);
                return;
            }

            UDPSocket socket = _unicast._socket;
            IPEndPoint remoteEP = (IPEndPoint)raw.EndPoint;

            if (remoteEP.AddressFamily == AddressFamily.InterNetwork) {
                if (_unicast._socketBackup != null) {
                    // use the separated socket of IPv4 to deal with IPv4 conversions.
                    socket = _unicast._socketBackup;
                }
                else if (_unicast._socket.Socket.AddressFamily == AddressFamily.InterNetworkV6) {
                    remoteEP = new IPEndPoint(IPAddressExtensions.MapToIPv6(remoteEP.Address), remoteEP.Port);
                }
            }

            BeginSend(socket, raw.Data, remoteEP);
        }

        private void EndSend(UDPSocket socket, int bytesTransferred)
        {
            BeginSend();
        }

        private void EndSend(UDPSocket socket, Exception ex)
        {
#if LOG_UDP_CHANNEL
            _Log.Warn("EndSend: error trying to send", ex);
#endif
            // TODO may log exception?
            BeginSend();
        }

        private UDPSocket SetupUDPSocket(AddressFamily addressFamily, int bufferSize)
        {
            UDPSocket socket = NewUDPSocket(addressFamily, bufferSize);

#if NETSTANDARD1_3
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
#else
            if (Environment.OSVersion.Platform == PlatformID.Win32NT ||
                Environment.OSVersion.Platform == PlatformID.WinCE) {
#endif
                // do not throw SocketError.ConnectionReset by ignoring ICMP Port Unreachable
                const int SIO_UDP_CONNRESET = -1744830452;
                try {
                    // Set the SIO_UDP_CONNRESET ioctl to true for this UDP socket. If this UDP socket
                    //    ever sends a UDP packet to a remote destination that exists but there is
                    //    no socket to receive the packet, an ICMP port unreachable message is returned
                    //    to the sender. By default, when this is received the next operation on the
                    //    UDP socket that send the packet will receive a SocketException. The native
                    //    (Winsock) error that is received is WSAECONNRESET (10054). Since we don't want
                    //    to wrap each UDP socket operation in a try/except, we'll disable this error
                    //    for the socket with this ioctl call.

                    socket.Socket.IOControl(SIO_UDP_CONNRESET, new byte[] {0}, null);
                }
                catch (Exception) {
                }
            }
            return socket;
        }

        partial class UDPSocket : IDisposable
        {
            public readonly Socket Socket;
        }

        class RawData
        {
            public byte[] Data;
            public System.Net.EndPoint EndPoint;
        }

        class SocketSet
        {
            public IPEndPoint _localEP;
            public UDPSocket _socket;
            public UDPSocket _socketBackup;
        }

#pragma warning disable CS0067
        /// <inheritdoc/>
        public event EventHandler<SessionEventArgs> SessionEvent;
#pragma warning restore CS0067

        /// <inheritdoc/>
        public bool IsReliable
        {
            get => false;
        }
    }
}
