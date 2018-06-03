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

            //  Get cipher suites - Change IntValues with array of suites
            GetCipherSuites = 2,

            //  Return a set of signer credentials
            //  Ciphersuite provides the cipher suite
            SignCredentials = 3,


            GetExtensions = 4,

            //  Notify the UI what certificate has been provided by the other party
            //  Input: Certificate - the certificate
            //  Result: Processed - TRUE (if acceptable)
            //      will normally raise an exception if the certificate is not legal, but
            //      not if not processed.
            ClientCertificate = 5,
            ServerCertificate = 6,

            //  Select certificate type from the list
            //  Input: Bytes contains the client supported values
            //  Result: Byte contains -1 if non supported else the selected value

            ServerCertType = 7,
            ClientCertType = 8,
        }

        public TlsEvent(EventCode code)
        {
            Code = code;
            Processed = false;
        }

        public EventCode Code { get; }

        public bool Processed { get; set; }

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

        public byte[] Bytes { get; set; }
        public byte Byte { get; set; }
    }
}
