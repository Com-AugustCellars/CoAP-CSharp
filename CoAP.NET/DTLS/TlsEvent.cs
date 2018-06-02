using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Com.AugustCellars.COSE;
using Org.BouncyCastle.Crypto.Tls;

namespace Com.AugustCellars.CoAP.DTLS
{
    /// <summary>
    /// Tell event handlers that something iteresting has happened in the underlying TLS
    /// code that they may want to respond to.
    /// </summary>
    public class TlsEvent
    {
        public enum EventCode
        {
            UnknownPskName = 1,
            GetCipherSuites = 2,
            SignCredentials = 3,
            GetExtensions = 4,
            ClientCertificate = 5,
            ServerCertificate = 6
        }

        public TlsEvent(EventCode code)
        {
            Code = code;
        }

        public EventCode Code { get; }

        /// <summary>
        /// For code UnknownPskName - contains the name of the PSK 
        /// </summary>
        public byte[] PskName { get; set; }

        /// <summary>
        /// Return the key to be used for a PskName if you have one
        /// </summary>
        public OneKey KeyValue { get; set; }

        public int[] IntValues { get; set; }

        public int CipherSuite { get; set; }
        public TlsSignerCredentials SignerCredentials { get; set; }

        public IDictionary Dictionary { get; set; }

        public AbstractCertificate Certificate { get; set; }
    }
}
