using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Org.BouncyCastle.Crypto.Tls;

using Com.AugustCellars.COSE;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;

namespace Com.AugustCellars.CoAP.DTLS
{
    class DtlsServer : DefaultTlsServer
    {
        private KeySet _serverKeys;
        private KeySet _userKeys;

        internal DtlsServer(KeySet serverKeys, KeySet userKeys)
        {
            _serverKeys = serverKeys;
            _userKeys = userKeys;
            mPskIdentityManager = new MyIdentityManager(userKeys);
        }

        protected override ProtocolVersion MinimumVersion { get {return ProtocolVersion.DTLSv10;} }
        protected override ProtocolVersion MaximumVersion { get {return ProtocolVersion.DTLSv12;} }

        public OneKey AuthenticationKey
        {
            get => mPskIdentityManager.AuthenticationKey; 
        }

        protected override int[] GetCipherSuites()
        {
            return new int[] {
                CipherSuite.TLS_PSK_WITH_AES_128_CCM_8,
                CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CCM_8
            };
#if false
            return Arrays.Concatenate(base.GetCipherSuites(),
                new int[] {
                    CipherSuite.TLS_PSK_WITH_AES_128_CCM_8,
                });
#endif
        }

        public override void NotifyFallback(bool isFallback)
        {
            // M00BUG - Do we care?
            return;
        }

        public override void NotifySecureRenegotiation(bool secureRenegotiation)
        {
            // M00BUG - do we care ?
            return;
        }

        public override TlsCredentials GetCredentials()
        {
            int keyExchangeAlgorithm = TlsUtilities.GetKeyExchangeAlgorithm(mSelectedCipherSuite);

            switch (keyExchangeAlgorithm) {
                case KeyExchangeAlgorithm.DHE_PSK:
                case KeyExchangeAlgorithm.ECDHE_PSK:
                case KeyExchangeAlgorithm.PSK:
                    return null;

                case KeyExchangeAlgorithm.RSA_PSK:
                    return GetRsaEncryptionCredentials();

            case KeyExchangeAlgorithm.ECDHE_ECDSA:
                return GetECDsaSignerCredentials();

                default:
                    /* Note: internal error here; selected a key exchange we don't implement! */
                    throw new TlsFatalAlert(AlertDescription.internal_error);
            }
        }

        protected override TlsSignerCredentials GetECDsaSignerCredentials()
        {
            AsymmetricKeyParameter privateKey = null;

            return new DefaultTlsSignerCredentials(null, new Certificate(new X509CertificateStructure[0]), privateKey);
            throw new TlsFatalAlert(AlertDescription.internal_error);
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

            case KeyExchangeAlgorithm.ECDHE_ECDSA:
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

        protected virtual TlsKeyExchange CreatePskKeyExchange(int keyExchange)
        {
            return new TlsPskKeyExchange(keyExchange, mSupportedSignatureAlgorithms, null, mPskIdentityManager,
                GetDHParameters(), mNamedCurves, mClientECPointFormats, mServerECPointFormats);
        }

        protected override TlsKeyExchange CreateECDHKeyExchange(int keyExchange)
        {
            return new TlsECDHKeyExchange(keyExchange, mSupportedSignatureAlgorithms, mNamedCurves, mClientECPointFormats,
                mServerECPointFormats);
        }

#if false
        protected override TlsSignerCredentials GetECDsaSignerCredentials()
        {
            return TlsTestUtilities.LoadSignerCredentials(mContext, mSupportedSignatureAlgorithms, SignatureAlgorithm.rsa,
                "x509-server.pem", "x509-server-key.pem");
        }
#endif

        private MyIdentityManager mPskIdentityManager;

        internal class MyIdentityManager
            : TlsPskIdentityManager
        {
            private KeySet _userKeys;

            internal MyIdentityManager(KeySet keys)
            {
                _userKeys = keys;
            }

            public OneKey AuthenticationKey { get; private set; }

            public virtual byte[] GetHint()
            {
                return Encoding.UTF8.GetBytes("hint");
            }

            public virtual byte[] GetPsk(byte[] identity)
            {
                foreach (OneKey key in _userKeys) {
                    if (!key.HasKeyType((int) COSE.GeneralValuesInt.KeyType_Octet)) continue;

                    if (identity == null) {
                        if (key.HasKid(null)) {
                            AuthenticationKey = key;
                            return (byte[]) key[CoseKeyParameterKeys.Octet_k].GetByteString().Clone();
                        }
                    }
                    else {
                        if (key.HasKid(identity)) {
                            AuthenticationKey = key;
                            return (byte[]) key[CoseKeyParameterKeys.Octet_k].GetByteString().Clone();
                        }
                    }
                }
                return null;
            }
        }
    }
}
