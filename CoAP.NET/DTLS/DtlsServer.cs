using System;
using System.Collections;
using System.IO;
using System.Text;
using Org.BouncyCastle.Crypto.Tls;

using Com.AugustCellars.COSE;
#if SUPPORT_TLS_CWT
using Com.AugustCellars.WebToken.CWT;
#endif
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using PeterO.Cbor;

namespace Com.AugustCellars.CoAP.DTLS
{
    public class DtlsServer : DefaultTlsServer
    {
        private readonly TlsKeyPairSet _serverKeys;
        private KeySet _userKeys;

        public EventHandler<TlsEvent> TlsEventHandler;

        public KeySet CwtTrustKeySet { get; set; }

        public DtlsServer(TlsKeyPairSet serverKeys, KeySet userKeys)
        {
            _serverKeys = serverKeys;
            _userKeys = userKeys;
            mPskIdentityManager = new MyIdentityManager(userKeys);
            mPskIdentityManager.TlsEventHandler += OnTlsEvent;
        }

        protected override ProtocolVersion MinimumVersion => ProtocolVersion.DTLSv10;
        protected override ProtocolVersion MaximumVersion => ProtocolVersion.DTLSv12;

        public OneKey AuthenticationKey => mPskIdentityManager.AuthenticationKey;
        public Certificate AuthenticationCertificate { get; private set; }

        // Chain all of our events to the next level up.

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

            //  Give the outside code a chance to change this.

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

#if SUPPORT_TLS_CWT
        public override AbstractCertificate ParseCertificate(short certificateType, Stream io)
        {
            switch (certificateType)
            {
            case CertificateType.CwtPublicKey:
                try
                {
                    CwtPublicKey cwtPub = CwtPublicKey.Parse(io);

                    Cwt cwtServer = Cwt.Decode(cwtPub.EncodedCwt(), CwtTrustKeySet, CwtTrustKeySet);

                    AsymmetricKeyParameter pubKey = cwtServer.Cnf.Key.AsPublicKey();

                    SubjectPublicKeyInfo spi = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(pubKey);
                    cwtPub.SetSubjectPublicKeyInfo(spi);

                    return cwtPub;
                }
                catch
                {
                    return null;
                }

            default:
                return null;
            }
        }
#endif
        protected override TlsSignerCredentials GetECDsaSignerCredentials()
        {
            byte[] certTypes;

            if (mClientExtensions.Contains(ExtensionType.server_certificate_type)) {
                certTypes = (byte[]) mClientExtensions[ExtensionType.server_certificate_type];
            }
            else {
                certTypes = new byte[]{CertificateType.X509};
            }

            foreach (byte b in certTypes) {
                if (b == CertificateType.X509)
                {
                    foreach (TlsKeyPair kp in _serverKeys)
                    {
                        if (b != kp.CertType) continue;

                        OneKey k = kp.PrivateKey;
                        if (k.HasKeyType((int)GeneralValuesInt.KeyType_EC2) &&
                            k.HasAlgorithm(AlgorithmValues.ECDSA_256))
                        {

                            return new DefaultTlsSignerCredentials(
                                mContext,
                                new Certificate(kp.X509Certificate), 
                                kp.PrivateKey.AsPrivateKey(),
                                new SignatureAndHashAlgorithm(HashAlgorithm.sha256, SignatureAlgorithm.ecdsa));
                        }
                    }
                }
#if SUPPORT_RPK
                if (b == CertificateType.RawPublicKey) {
                    foreach (TlsKeyPair kp in _serverKeys) {
                        if (b != kp.CertType) continue;

                        OneKey k = kp.PublicKey;
                        if (k.HasKeyType((int) GeneralValuesInt.KeyType_EC2) &&
                            k.HasAlgorithm(AlgorithmValues.ECDSA_256)) {

                            AsymmetricKeyParameter param = k.AsPublicKey();

                            SubjectPublicKeyInfo spi = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(param);

                            return new DefaultTlsSignerCredentials(mContext, new RawPublicKey(spi), kp.PrivateKey.AsPrivateKey(),
                                                                   new SignatureAndHashAlgorithm(
                                                                       HashAlgorithm.sha256, SignatureAlgorithm.ecdsa));
                        }
                    }
                }
#endif
#if SUPPORT_TLS_CWT
                if (b == CertificateType.CwtPublicKey) {
                    foreach (TlsKeyPair kp in _serverKeys) {
                        if (b != kp.CertType) continue;

                        OneKey k = kp.PrivateKey;
                        if (k.HasKeyType((int) GeneralValuesInt.KeyType_EC2) &&
                            k.HasAlgorithm(AlgorithmValues.ECDSA_256)) {

                            CwtPublicKey cwtKey = new CwtPublicKey(kp.PublicCwt.EncodeToBytes());
                            AsymmetricKeyParameter pubKey = kp.PublicCwt.Cnf.Key.AsPublicKey();
                            cwtKey.SetSubjectPublicKeyInfo(SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(pubKey));

                            return new DefaultTlsSignerCredentials(
                                mContext, cwtKey, kp.PrivateKey.AsPrivateKey(),
                                new SignatureAndHashAlgorithm(HashAlgorithm.sha256, SignatureAlgorithm.ecdsa));
                        }
                    }
                }
#endif
            }

            // If we did not fine appropriate signer credentials - ask for help

            TlsEvent e = new TlsEvent(TlsEvent.EventCode.SignCredentials) {
                CipherSuite = KeyExchangeAlgorithm.ECDHE_ECDSA
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
            return new TlsPskKeyExchange(keyExchange, mSupportedSignatureAlgorithms, null, mPskIdentityManager, null,
                GetDHParameters(), mNamedCurves, mClientECPointFormats, mServerECPointFormats);
        }

        protected override TlsKeyExchange CreateECDHKeyExchange(int keyExchange)
        {
            byte[] serverCertTypes;
            if (mClientExtensions.Contains(ExtensionType.server_certificate_type)) {
                serverCertTypes = (byte[]) mClientExtensions[ExtensionType.server_certificate_type];
            }
            return new TlsECDHKeyExchange(keyExchange, mSupportedSignatureAlgorithms, mNamedCurves, mClientECPointFormats,
                mServerECPointFormats);
        }

#if SUPPORT_RPK
        public override byte GetClientCertificateType(byte[] certificateTypes)
        {
            TlsEvent e = new TlsEvent(TlsEvent.EventCode.ClientCertType) {
                Bytes = certificateTypes,
                Byte = 0xff
            };

            EventHandler<TlsEvent> handler = TlsEventHandler;
            if (handler != null) {
                handler(this, e);
            }

            if (e.Byte != 0xff) {
                return e.Byte;
            }

            foreach (byte type in certificateTypes) {
                if (type == 1) return type;
#if SUPPORT_RPK
                if (type == 2) return type;  // Assume we only support Raw Public Key
#endif
#if SUPPORT_TLS_CWT
                if (type == 254) return type;
#endif
            }


            throw new TlsFatalAlert(AlertDescription.handshake_failure);
        }

        public override byte GetServerCertificateType(byte[] certificateTypes)
        {
            TlsEvent e = new TlsEvent(TlsEvent.EventCode.ServerCertType) {
                Bytes = certificateTypes,
                Byte = 0xff
            };

            EventHandler<TlsEvent> handler = TlsEventHandler;
            if (handler != null) {
                handler(this, e);
            }

            if (e.Byte != 0xff) {
                return e.Byte;
            }

            foreach (byte type in certificateTypes) {
                if (type == 1) return type;
                if (type == 2) return type;  // Assume we only support Raw Public Key
                if (type == 254) return type; 
            }
            throw new TlsFatalAlert(AlertDescription.handshake_failure);
        }
#endif

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

#if SUPPORT_RPK
        public override void NotifyClientCertificate(AbstractCertificate clientCertificate)
        {
            if (clientCertificate is RawPublicKey) {
                mPskIdentityManager.GetRpkKey((RawPublicKey) clientCertificate);
            }
#if SUPPORT_TLS_CWT
            else if (clientCertificate is CwtPublicKey) {
                mPskIdentityManager.CwtTrustRoots = CwtTrustKeySet;
                mPskIdentityManager.GetCwtKey((CwtPublicKey) clientCertificate);
            }
#endif
            else if (clientCertificate is Certificate) {
                TlsEvent e = new TlsEvent(TlsEvent.EventCode.ClientCertificate) {
                    Certificate = clientCertificate,
                    CertificateType = CertificateType.X509
                };

                EventHandler<TlsEvent> handler = TlsEventHandler;
                if (handler != null) {
                    handler(this, e);
                }

                if (!e.Processed) {
                    throw new TlsFatalAlert(AlertDescription.certificate_unknown);
                }

                AuthenticationCertificate = (Certificate) clientCertificate;
            }
            else {
                throw new TlsFatalAlert(AlertDescription.certificate_unknown);
            }
        }
#else
        public override void NotifyClientCertificate(Certificate clientCertificate)
        {
                TlsEvent e = new TlsEvent(TlsEvent.EventCode.ClientCertificate) {
                    Certificate = clientCertificate
                };

                EventHandler<TlsEvent> handler = TlsEventHandler;
                if (handler != null) {
                    handler(this, e);
                }

                if (!e.Processed) {
                    throw new TlsFatalAlert(AlertDescription.certificate_unknown);
                }

                AuthenticationCertificate = (Certificate) clientCertificate;
            
                AuthenticationCertificate = (Certificate) clientCertificate;
            
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
#if SUPPORT_TLS_CWT
                CwtAuthenticationKey = null;
#endif
            }

            public OneKey AuthenticationKey { get; private set; }

#if SUPPORT_TLS_CWT
            public KeySet CwtTrustRoots { get; set; }
            public Cwt CwtAuthenticationKey { get; }
#endif

            public virtual byte[] GetHint()
            {
                return Encoding.UTF8.GetBytes("hint");
            }

            public virtual byte[] GetPsk(byte[] identity)
            {
                foreach (OneKey key in _userKeys) {
                    if (!key.HasKeyType((int) GeneralValuesInt.KeyType_Octet)) continue;

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
                    if (e.KeyValue.HasKeyType((int) GeneralValuesInt.KeyType_Octet)) {
                        AuthenticationKey = e.KeyValue;
                        return (byte[]) e.KeyValue[CoseKeyParameterKeys.Octet_k].GetByteString().Clone();
                    }
                }

                return null;
            }

            public void GetCertKey(Certificate certificate)
            {

            }

#if SUPPORT_RPK
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
                }
                else {
                    // throw new TlsFatalAlert(AlertDescription.certificate_unknown);
                }

                TlsEvent ev = new TlsEvent(TlsEvent.EventCode.ClientCertificate) {
                    Certificate = rpk
                };

                EventHandler<TlsEvent> handler = TlsEventHandler;
                if (handler != null) {
                    handler(this, ev);
                }

                if (!ev.Processed) {
                    throw new TlsFatalAlert(AlertDescription.certificate_unknown);
                }
            }
#endif

#if SUPPORT_TLS_CWT
            public void GetCwtKey(CwtPublicKey rpk)
            {
                Cwt cwt;

                try {
                    cwt = Cwt.Decode(rpk.EncodedCwt(), CwtTrustRoots, CwtTrustRoots);

                    AuthenticationKey = cwt.Cnf.Key;
                }
                catch (Exception e)
                {
                    TlsEvent ev = new TlsEvent(TlsEvent.EventCode.ClientCertificate)
                    {
                        Certificate = rpk
                    };

                    EventHandler<TlsEvent> handler = TlsEventHandler;
                    if (handler != null)
                    {
                        handler(this, ev);
                    }

                    if (!ev.Processed)
                    {
                        throw new TlsFatalAlert(AlertDescription.certificate_unknown);
                    }

                    AuthenticationKey = ev.KeyValue;
                }
            }
#endif

        }
    }
}
