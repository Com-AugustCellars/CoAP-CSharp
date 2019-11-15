using System;

using Com.AugustCellars.CoAP.Codec;
using Com.AugustCellars.CoAP.Net;

using Com.AugustCellars.COSE;
using Org.BouncyCastle.Bcpg;
#if SUPPORT_TLS_CWT
using Com.AugustCellars.WebToken;
#endif

namespace Com.AugustCellars.CoAP.DTLS
{
    /// <summary>
    /// Client only version of a DTLS end point.
    /// This end point will not accept new DTLS connections from other parities. 
    /// If this is needed then <see cref="DTLSEndPoint"/> instead.
    /// </summary>
    public class DTLSClientEndPoint : CoAPEndPoint
    {
        public EventHandler<TlsEvent> TlsEventHandler;

        /// <summary>
        /// Instantiates a new DTLS endpoint with the specific channel and configuration
        /// </summary>
        /// <param name="userKey">Authentication information</param>
        /// <param name="config">Configuration info</param>
        public DTLSClientEndPoint(OneKey userKey, ICoapConfig config) : this(userKey, 0, config)
        { }

        /// <summary>
        /// Instantiates a new DTLS endpoint with the specific channel and configuration
        /// </summary>
        /// <param name="userKey">Authentication information</param>
        /// <param name="port">Client side port to use</param>
        public DTLSClientEndPoint(OneKey userKey, int port=0) : this(userKey, port, CoapConfig.Default)
        { }

        /// <summary>
        /// Instantiates a new DTLS endpoint with the specific channel and configuration
        /// </summary>
        /// <param name="userKey">Authentication information</param>
        /// <param name="port">Client side port to use</param>
        /// <param name="config">Configuration info</param>
        public DTLSClientEndPoint(OneKey userKey, int port, ICoapConfig config) : this(new TlsKeyPair(userKey), port, config)
        { }

        /// <summary>
        /// Instantiates a new DTLS endpoint with the specific channel and configuration
        /// </summary>
        /// <param name="userKey">Authentication information</param>
        /// <param name="localEP">Client side endpoint to use</param>
        public DTLSClientEndPoint(OneKey userKey, System.Net.EndPoint localEP) : this(userKey, localEP, CoapConfig.Default)
        {
        }

        /// <summary>
        /// Instantiates a new DTLS endpoint with the specific channel and configuration
        /// </summary>
        /// <param name="userKey">Authentication information</param>
        /// <param name="localEP">Client side endpoint to use</param>
        /// <param name="config">Configuration info</param>
        public DTLSClientEndPoint(OneKey userKey, System.Net.EndPoint localEP, ICoapConfig config) : this(new TlsKeyPair(userKey), localEP, config)
        { }

        public DTLSClientEndPoint(TlsKeyPair userKey, int port=0) : this(userKey, port, CoapConfig.Default)
        { }

        public DTLSClientEndPoint(TlsKeyPair userKey, ICoapConfig config) : this(userKey, 0, config)
        { }

        public DTLSClientEndPoint(TlsKeyPair userKey, int port, ICoapConfig config) : this (new DTLSClientChannel(userKey, port), config)
        { }


        public DTLSClientEndPoint(TlsKeyPair userKey, System.Net.EndPoint localEndPoint) : this(userKey, localEndPoint, CoapConfig.Default)
        { }


        public DTLSClientEndPoint(TlsKeyPair userKey, System.Net.EndPoint localEndPoint, ICoapConfig config) : this(new DTLSClientChannel(userKey, localEndPoint), config)
        { }

        /// <summary>
        /// Instantiates a new DTLS endpoint with the specific channel and configuration
        /// </summary>
        /// <param name="channel">Channel interface to the transport</param>
        /// <param name="config">Configuration information for the transport</param>
        private DTLSClientEndPoint(DTLSClientChannel channel, ICoapConfig config) : base(channel, config)
        {
            // Stack.Remove("Reliability");
            MessageEncoder = UdpCoapMesageEncoder;
            MessageDecoder = UdpCoapMessageDecoder;
            EndpointSchema = new []{"coaps", "coaps+udp"};
            channel.TlsEventHandler += OnTlsEvent;
        }

        /// <summary>
        /// Select the correct message decoder and turn the bytes into a message
        /// This is currently the same as the UDP decoder.
        /// </summary>
        /// <param name="data">Data to be decoded</param>
        /// <returns>Interface to decoded message</returns>
        static IMessageDecoder UdpCoapMessageDecoder(byte[] data)
        {
            return new Spec.MessageDecoder18(data);
        }

        /// <summary>
        /// Select the correct message encoder and return it.
        /// This is currently the same as the UDP decoder.
        /// </summary>
        /// <returns>Message encoder</returns>
        static IMessageEncoder UdpCoapMesageEncoder()
        {
            return new Spec.MessageEncoder18();
        }

        private void OnTlsEvent(Object o, TlsEvent e)
        {
            EventHandler<TlsEvent> handler = TlsEventHandler;
            if (handler != null) {
                handler(o, e);
            }

        }

        public KeySet CwtTrustKeySet
        {
            get { return ((DTLSClientChannel) dataChannel).CwtTrustKeySet; }
            set { ((DTLSClientChannel) dataChannel).CwtTrustKeySet = value; }
        }
    }
}
