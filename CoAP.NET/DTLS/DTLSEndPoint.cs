using System;

using Com.AugustCellars.COSE;
using Com.AugustCellars.CoAP.Codec;
using Com.AugustCellars.CoAP.Net;

namespace Com.AugustCellars.CoAP.DTLS
{
    /// <summary>
    /// This class is used to support the use of DTLS for servers.
    /// This class supports both client and server sides of a DTLS connection.
    /// </summary>
    public class DTLSEndPoint : CoAPEndPoint
    {
        /// <inheritdoc/>
        public DTLSEndPoint(TlsKeyPairSet serverKeys, KeySet userKeys) : this(serverKeys, userKeys, 0, CoapConfig.Default)
        {
        }

        /// <inheritdoc/>
        public DTLSEndPoint(TlsKeyPairSet serverKeys, KeySet userKeys, ICoapConfig config) : this(serverKeys, userKeys, 0, config)
        {
        }

        /// <inheritdoc/>
        public DTLSEndPoint(TlsKeyPairSet keysServer, KeySet keysUser, Int32 port, KeySet cwtTrustRoots = null) : this(new DTLSChannel(keysServer, keysUser, port, cwtTrustRoots), CoapConfig.Default)
        {
        }

        /// <inheritdoc/>
        public DTLSEndPoint(TlsKeyPairSet keyServer, KeySet keysUser, int port, ICoapConfig config) : this (new DTLSChannel(keyServer, keysUser, port), config)
        { }

        /// <inheritdoc/>
        public DTLSEndPoint(TlsKeyPairSet keysServer, KeySet keysUser, System.Net.EndPoint localEndPoint) : this(keysServer, keysUser, localEndPoint, CoapConfig.Default)
        {
        }

        /// <inheritdoc/>
        public DTLSEndPoint(TlsKeyPairSet keysServer, KeySet keysUser, System.Net.EndPoint localEndPoint, ICoapConfig config) : this(new DTLSChannel(keysServer, keysUser, localEndPoint), config)
        {
        }

        /// <summary>
        /// Instantiates a new DTLS endpoint with the specific channel and configuration
        /// </summary>
        /// <param name="channel">The DTLS Channel object to use for low level transmission</param>
        /// <param name="config">Configuration interface</param>
        public DTLSEndPoint(DTLSChannel channel, ICoapConfig config) : base(channel, config)
        {
            // Stack.Remove(Stack.Get("Reliability"));
            MessageEncoder = UdpCoapMesageEncoder;
            MessageDecoder = UdpCoapMessageDecoder;
            EndpointSchema = new []{"coaps", "coaps+udp"};
            channel.TlsEventHandler += OnTlsEvent;
        }

        static IMessageDecoder UdpCoapMessageDecoder(byte[] data)
        {
            return new Spec.MessageDecoder18(data);
        }

        static IMessageEncoder UdpCoapMesageEncoder()
        {
            return new Spec.MessageEncoder18();
        }

        public EventHandler<TlsEvent> TlsEventHandler;

        private void OnTlsEvent(Object o, TlsEvent e)
        {
            EventHandler<TlsEvent> handler = TlsEventHandler;
            if (handler != null) {
                handler(o, e);
            }
        }

    }
}
