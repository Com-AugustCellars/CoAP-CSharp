using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Com.AugustCellars.COSE;
using Com.AugustCellars.CoAP;
using Com.AugustCellars.CoAP.Codec;
using Com.AugustCellars.CoAP.Net;

namespace Com.AugustCellars.CoAP.DTLS
{
    public class DTLSEndPoint : CoAPEndPoint
    {
        /// <inheritdoc/>
        public DTLSEndPoint(KeySet serverKeys, KeySet userKeys) : this(serverKeys, userKeys, 0, CoapConfig.Default)
        {
        }

        /// <inheritdoc/>
        public DTLSEndPoint(KeySet serverKeys, KeySet userKeys, ICoapConfig config) : this(serverKeys, userKeys, 0, config)
        {
        }

        /// <inheritdoc/>
        public DTLSEndPoint(KeySet keysServer, KeySet keysUser, Int32 port) : this(new DTLSChannel(keysServer, keysUser, port), CoapConfig.Default)
        {
        }

        public DTLSEndPoint(KeySet keyServer, KeySet keysUser, int port, ICoapConfig config) : this (new DTLSChannel(keysUser, keysUser, port), config)
        { }

        /// <inheritdoc/>
        public DTLSEndPoint(KeySet keysServer, KeySet keysUser, System.Net.EndPoint localEP) : this(keysServer, keysUser, localEP, CoapConfig.Default)
        {
        }

        /// <inheritdoc/>
        public DTLSEndPoint(KeySet keysServer, KeySet keysUser, System.Net.EndPoint localEP, ICoapConfig config) : this(new DTLSChannel(keysServer, keysUser, localEP), config)
        {
        }

        /// <summary>
        /// Instantiates a new DTLS endpoint with the specific channel and configuration
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="config"></param>
        public DTLSEndPoint(DTLSChannel channel, ICoapConfig config) : base(channel, config)
        {
            Stack.Remove(Stack.Get("Reliability"));
            MessageEncoder = UdpCoapMesageEncoder;
            MessageDecoder = UdpCoapMessageDecoder;
            EndpointSchema = "coaps";
        }

        static IMessageDecoder UdpCoapMessageDecoder(byte[] data)
        {
            return new Spec.MessageDecoder18(data);
        }

        static IMessageEncoder UdpCoapMesageEncoder()
        {
            return new Spec.MessageEncoder18();
        }
    }
}
