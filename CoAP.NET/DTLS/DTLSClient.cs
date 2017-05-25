using System;
using System.Collections;

using System.Threading.Tasks;

using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.Utilities.Encoders;


namespace Com.AugustCellars.CoAP.DTLS
{

    class DtlsClient : DefaultTlsClient
    {
        private TlsPskIdentity _mPskIdentity;
        private TlsSession _mSession;

        internal DtlsClient(TlsSession session, TlsPskIdentity pskIdentity)
        {
            _mSession = session;
            _mPskIdentity = pskIdentity;
        }

        public override ProtocolVersion MinimumVersion {
            get { return ProtocolVersion.DTLSv10; }
        }

        public override ProtocolVersion ClientVersion
        {
            get { return ProtocolVersion.DTLSv12; }
        }

        public override int[] GetCipherSuites()
        {
            return new int[] { CipherSuite.TLS_PSK_WITH_AES_128_CCM_8};
#if false
            return Arrays.Concatenate(base.GetCipherSuites(),
                new int[] {
                    CipherSuite.TLS_PSK_WITH_AES_128_CCM_8,
                });
#endif
        }


        public override IDictionary GetClientExtensions()
        {
            IDictionary clientExtensions = TlsExtensionsUtilities.EnsureExtensionsInitialised(base.GetClientExtensions());
            // TlsExtensionsUtilities.AddEncryptThenMacExtension(clientExtensions);
            // TlsExtensionsUtilities.AddExtendedMasterSecretExtension(clientExtensions);
            {
                /*
                 * NOTE: If you are copying test code, do not blindly set these extensions in your own client.
                 */
            //   TlsExtensionsUtilities.AddMaxFragmentLengthExtension(clientExtensions, MaxFragmentLength.pow2_9);
            //    TlsExtensionsUtilities.AddPaddingExtension(clientExtensions, mContext.SecureRandom.Next(16));
            //    TlsExtensionsUtilities.AddTruncatedHMacExtension(clientExtensions);
            }
            return clientExtensions;
        }
        public override TlsAuthentication GetAuthentication()
        {
            return new MyTlsAuthentication(mContext);
        }

        public override TlsKeyExchange GetKeyExchange()
        {
            int keyExchangeAlgorithm = TlsUtilities.GetKeyExchangeAlgorithm(mSelectedCipherSuite);

            switch (keyExchangeAlgorithm) {
                case KeyExchangeAlgorithm.DHE_PSK:
                case KeyExchangeAlgorithm.ECDHE_PSK:
                case KeyExchangeAlgorithm.PSK:
                case KeyExchangeAlgorithm.RSA_PSK:
                    return CreatePskKeyExchange(keyExchangeAlgorithm);

                case KeyExchangeAlgorithm.ECDH_anon:
                case KeyExchangeAlgorithm.ECDH_ECDSA:
                case KeyExchangeAlgorithm.ECDH_RSA:
                    return CreateECDHKeyExchange(keyExchangeAlgorithm);
                
                default:
                    /*
                        * Note: internal error here; the TlsProtocol implementation verifies that the
                        * server-selected cipher suite was in the list of client-offered cipher suites, so if
                        * we now can't produce an implementation, we shouldn't have offered it!
                        */
                    throw new TlsFatalAlert(AlertDescription.internal_error);
            }
        }

        /// <summary>
        /// We don't care if we cannot do secure renegotiation at this time.
        /// This needs to be reviewed in the future M00TODO
        /// </summary>
        /// <param name="secureRenegotiation"></param>
        public override void NotifySecureRenegotiation(bool secureRenegotiation)
        {
            //  M00TODO - should we care?
        }




        internal class MyTlsAuthentication
            : TlsAuthentication
        {
            private readonly TlsContext _mContext;

            internal MyTlsAuthentication(TlsContext context)
            {
                this._mContext = context;
            }

            public virtual void NotifyServerCertificate(Certificate serverCertificate)
            {
/*                
                X509CertificateStructure[] chain = serverCertificate.GetCertificateList();
                Console.WriteLine("DTLS client received server certificate chain of length " + chain.Length);
                for (int i = 0; i != chain.Length; i++) {
                    X509CertificateStructure entry = chain[i];
                    // TODO Create fingerprint based on certificate signature algorithm digest
                    Console.WriteLine("    fingerprint:SHA-256 " + TlsTestUtilities.Fingerprint(entry) + " ("
                                      + entry.Subject + ")");
                }
                */
            }
  
            public virtual TlsCredentials GetClientCredentials(CertificateRequest certificateRequest)
            {
                /*
                byte[] certificateTypes = certificateRequest.CertificateTypes;
                if (certificateTypes == null || !Arrays.Contains(certificateTypes, ClientCertificateType.rsa_sign))
                    return null;

                return TlsTestUtilities.LoadSignerCredentials(mContext, certificateRequest.SupportedSignatureAlgorithms,
                    SignatureAlgorithm.rsa, "x509-client.pem", "x509-client-key.pem");
                    */
                return null;
            }


        };
        protected virtual TlsKeyExchange CreatePskKeyExchange(int keyExchange)
        {
            return new TlsPskKeyExchange(keyExchange, mSupportedSignatureAlgorithms, _mPskIdentity, null, null, mNamedCurves,
                mClientECPointFormats, mServerECPointFormats);
        }

        protected override TlsKeyExchange CreateECDHKeyExchange(int keyExchange)        {
            return new TlsECDHKeyExchange(keyExchange, mSupportedSignatureAlgorithms, mNamedCurves, mClientECPointFormats,
                mServerECPointFormats);
        }

    }
}

