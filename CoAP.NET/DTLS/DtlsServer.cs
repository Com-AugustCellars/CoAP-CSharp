using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Org.BouncyCastle.Crypto.Tls;

using Com.AugustCellars.COSE;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Tls.Tests;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using PeterO.Cbor;

namespace Com.AugustCellars.CoAP.DTLS
{
    class DtlsServer : DefaultTlsServer
    {
        private KeySet _serverKeys;
        private KeySet _userKeys;

        public EventHandler<TlsEvent> TlsEventHandler;

        internal DtlsServer(KeySet serverKeys, KeySet userKeys)
        {
            _serverKeys = serverKeys;
            _userKeys = userKeys;
            mPskIdentityManager = new MyIdentityManager(userKeys);
            mPskIdentityManager.TlsEventHandler += OnTlsEvent;
        }

        protected override ProtocolVersion MinimumVersion { get {return ProtocolVersion.DTLSv10;} }
        protected override ProtocolVersion MaximumVersion { get {return ProtocolVersion.DTLSv12;} }

        public OneKey AuthenticationKey
        {
            get => mPskIdentityManager.AuthenticationKey; 
        }

        private void OnTlsEvent(Object o, TlsEvent e)
        {
            EventHandler<TlsEvent> handler = TlsEventHandler;
            if (handler != null) {
                handler(o, e);
            }
        }

        protected override int[] GetCipherSuites()
        {
            int[] i = new int[] {
                CipherSuite.TLS_PSK_WITH_AES_128_CCM_8,
                CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CCM_8,
                CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CCM_8
            };

            TlsEvent e = new TlsEvent(TlsEvent.EventCode.GetCipherSuites) {
                IntValues = i
            };

            EventHandler<TlsEvent> handler = TlsEventHandler;
            if (handler != null) {
                handler(this, e);
            }

            return e.IntValues;
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

        private BigInteger ConvertBigNum(CBORObject cbor)
        {
            byte[] rgb = cbor.GetByteString();
            byte[] rgb2 = new byte[rgb.Length + 2];
            rgb2[0] = 0;
            rgb2[1] = 0;
            for (int i = 0; i < rgb.Length; i++) rgb2[i + 2] = rgb[i];

            return new BigInteger(rgb2);
        }

        protected override TlsSignerCredentials GetECDsaSignerCredentials()
        {
            AsymmetricKeyParameter privateKey = null;

            foreach (OneKey k in _serverKeys) {
                if (k.HasKeyType((int) COSE.GeneralValuesInt.KeyType_EC2) &&
                    k.HasAlgorithm(COSE.AlgorithmValues.ECDSA_256)) {

                    X9ECParameters p = k.GetCurve();
                    ECDomainParameters parameters = new ECDomainParameters(p.Curve, p.G, p.N, p.H);
                    ECPrivateKeyParameters privKey = new ECPrivateKeyParameters("ECDSA", ConvertBigNum(k[CoseKeyParameterKeys.EC_D]), parameters);

                    ECPoint point = k.GetPoint();
                    ECPublicKeyParameters param = new ECPublicKeyParameters(point, parameters);

                    SubjectPublicKeyInfo spi = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(param);

                    return new DefaultTlsSignerCredentials(mContext, new RawPublicKey(spi), privKey, new SignatureAndHashAlgorithm(HashAlgorithm.sha256, SignatureAlgorithm.ecdsa) );
                }
            }

            TlsEvent e = new TlsEvent(TlsEvent.EventCode.SignCredentials) {
                CipherSuite = KeyExchangeAlgorithm.ECDH_ECDSA
            };

            EventHandler<TlsEvent> handler = TlsEventHandler;
            if (handler != null) {
                handler(this, e);
            }

            if (e.SignerCredentials != null) return e.SignerCredentials;
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
                return CreateECDheKeyExchange(keyExchangeAlgorithm);

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

        public override byte GetClientCertificateType(byte[] certificateTypes)
        {
            foreach (byte type in certificateTypes) {
                if (type == 2) return type;  // Assume we only support X.509 certificates
            }
            throw new TlsFatalAlert(AlertDescription.handshake_failure);
        }

        public override byte GetServerCertificateType(byte[] certificateTypes)
        {
            foreach (byte type in certificateTypes) {
                if (type == 2) return type;  // Assume we only support X.509 certificates
            }
            throw new TlsFatalAlert(AlertDescription.handshake_failure);
        }

        public override CertificateRequest GetCertificateRequest()
        {
            byte[] certificateTypes = new byte[]{ ClientCertificateType.rsa_sign,
                ClientCertificateType.ecdsa_sign };

            IList serverSigAlgs = null;
            if (TlsUtilities.IsSignatureAlgorithmsExtensionAllowed(mServerVersion)) {
                serverSigAlgs = TlsUtilities.GetDefaultSupportedSignatureAlgorithms();
            }

            return new CertificateRequest(certificateTypes, serverSigAlgs, null);
        }

        public override void NotifyClientCertificate(AbstractCertificate clientCertificate)
        {
            if (clientCertificate is RawPublicKey) {
                mPskIdentityManager.GetRpkKey((RawPublicKey) clientCertificate);
            }
            else {
                TlsEvent e = new TlsEvent(TlsEvent.EventCode.ClientCertificate) {
                    Certificate = clientCertificate
                };
            }
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
            public EventHandler<TlsEvent> TlsEventHandler;

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


                TlsEvent e = new TlsEvent(TlsEvent.EventCode.UnknownPskName) {
                    PskName = identity
                };
                EventHandler<TlsEvent> handler = TlsEventHandler;
                if (handler != null) {
                    handler(this, e);
                }

                if (e.KeyValue != null) {
                    if (e.KeyValue.HasKeyType((int) COSE.GeneralValuesInt.KeyType_Octet)) {
                        AuthenticationKey = e.KeyValue;
                        return (byte[]) e.KeyValue[CoseKeyParameterKeys.Octet_k].GetByteString().Clone();
                    }
                }
                return null;
            }

            public void GetRpkKey(RawPublicKey rpk)
            {
                AsymmetricKeyParameter key;

                try {
                    key = PublicKeyFactory.CreateKey(rpk.SubjectPublicKeyInfo());
                }
                catch (Exception e) {
                    throw new TlsFatalAlert(AlertDescription.unsupported_certificate, e);
                }

                if (key is ECPublicKeyParameters) {
                    ECPublicKeyParameters ecKey = (ECPublicKeyParameters) key;

                    string s = ecKey.AlgorithmName;
                    OneKey newKey = new OneKey();
                    newKey.Add(CoseKeyKeys.KeyType, GeneralValues.KeyType_EC);
                    if (ecKey.Parameters.Curve.Equals(NistNamedCurves.GetByName("P-256").Curve)) {
                        newKey.Add(CoseKeyParameterKeys.EC_Curve, GeneralValues.P256);
                    }

                    newKey.Add(CoseKeyParameterKeys.EC_X, CBORObject.FromObject(ecKey.Q.Normalize().XCoord.ToBigInteger().ToByteArrayUnsigned()));
                    newKey.Add(CoseKeyParameterKeys.EC_Y,  CBORObject.FromObject(ecKey.Q.Normalize().YCoord.ToBigInteger().ToByteArrayUnsigned()));

                    foreach (OneKey k in _userKeys) {
                        if (k.Compare(newKey)) {
                            AuthenticationKey = k;
                            return;
                        }
                    }

                    throw new TlsFatalAlert(AlertDescription.certificate_unknown);
                }
                else {
                    throw new TlsFatalAlert(AlertDescription.certificate_unknown);
                }
            }
            
        }
    }
}
