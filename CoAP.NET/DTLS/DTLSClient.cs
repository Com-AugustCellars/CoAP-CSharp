using System;
using System.Collections;
using System.IO;
using Com.AugustCellars.COSE;
#if SUPPORT_TLS_CWT
using Com.AugustCellars.WebToken.CWT;
using Com.AugustCellars.WebToken;
#endif
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;
using PeterO.Cbor;


namespace Com.AugustCellars.CoAP.DTLS
{

    public class DtlsClient : DefaultTlsClient
    {
        private readonly TlsPskIdentity _mPskIdentity;
        private TlsSession _mSession;
        public EventHandler<TlsEvent> TlsEventHandler;
        private readonly TlsKeyPair _tlsKeyPair;

        public DtlsClient(TlsSession session, TlsPskIdentity pskIdentity)
        {
            _mSession = session;
            _mPskIdentity = pskIdentity;
        }

        public DtlsClient(TlsSession session, TlsKeyPair userKey)
        {
            _mSession = session;
            _tlsKeyPair = userKey ?? throw new ArgumentNullException(nameof(userKey));
        }

#if SUPPORT_TLS_CWT
        public KeySet CwtTrustKeySet { get; set; }
        public DtlsClient(TlsSession session, TlsKeyPair tlsKey, KeySet cwtTrustKeys)
        {
            _mSession = session;
            _tlsKeyPair = tlsKey ?? throw new ArgumentNullException(nameof(tlsKey));
            CwtTrustKeySet = cwtTrustKeys;
        }
#endif

        public override ProtocolVersion MinimumVersion => ProtocolVersion.DTLSv10;

        public override ProtocolVersion ClientVersion => ProtocolVersion.DTLSv12;

        public override int[] GetCipherSuites()
        {
            int[] i;

            if (_tlsKeyPair != null) {
                if (_tlsKeyPair.X509Certificate != null) {
                i = new int[] {
                    CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CCM_8,
                    CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CCM_8
                };
            }
#if SUPPORT_RPK
                else if (_tlsKeyPair.PublicKey != null) {
                    i = new int[] {
                        CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CCM_8,
                        CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CCM_8
                    };
                }
#endif
#if SUPPORT_TLS_CWT
                else if (_tlsKeyPair.CertType == CertificateType.CwtPublicKey) {
                i = new int[] {
                    CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CCM_8,
                    CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CCM_8
                };
            }
#endif
                else {
                    //  We should never get here
                    i = new int[0];
                }
            }
            else {
                    //  We should never get here
                    i = new int[] {
                        CipherSuite.TLS_PSK_WITH_AES_128_CCM_8
                    };
            }
 
            TlsEvent e = new TlsEvent(TlsEvent.EventCode.GetCipherSuites) {
                IntValues = i
            };

            EventHandler<TlsEvent> handler = TlsEventHandler;
            if (handler != null) {
                handler(this, e);
            }

            return e.IntValues;
        }

#if SUPPORT_TLS_CWT
        public override AbstractCertificate ParseServerCertificate(short certificateType, Stream io)
        {
            switch (certificateType) {
            case CertificateType.CwtPublicKey:
                try {
                    CwtPublicKey cwtPub = CwtPublicKey.Parse(io);

                    Cwt cwtServer = Cwt.Decode(cwtPub.EncodedCwt(), CwtTrustKeySet, CwtTrustKeySet);

                    AsymmetricKeyParameter pubKey = cwtServer.Cnf.Key.AsPublicKey();

                    SubjectPublicKeyInfo spi = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(pubKey);
                    cwtPub.SetSubjectPublicKeyInfo(spi);

                    return cwtPub;
                }
                catch {
                    return null;
                }

            default:
                return null;
            }
        }
#endif

        /// <summary>
        /// Decide which type of client and server certificates are going to be supported.
        /// By default, we assume that only those certificate types which match the clients
        /// certificate are going to be supported for the server.
        /// </summary>
        /// <returns></returns>
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

#if SUPPORT_RPK
                if (_tlsKeyPair != null && _tlsKeyPair.CertType == CertificateType.RawPublicKey) {
                    TlsExtensionsUtilities.AddClientCertificateTypeExtensionClient(clientExtensions, new byte[] {2});
                    TlsExtensionsUtilities.AddServerCertificateTypeExtensionClient(clientExtensions, new byte[] {2});
                }
#endif

#if SUPPORT_TLS_CWT
                if (_tlsKeyPair != null && _tlsKeyPair.CertType == CertificateType.CwtPublicKey) {
                    TlsExtensionsUtilities.AddClientCertificateTypeExtensionClient(clientExtensions, new byte[] {254});
                    TlsExtensionsUtilities.AddServerCertificateTypeExtensionClient(clientExtensions, new byte[] {254});
                }
#endif
            }

            TlsEvent e = new TlsEvent(TlsEvent.EventCode.GetExtensions) {
                Dictionary = clientExtensions
            };


            EventHandler<TlsEvent> handler = TlsEventHandler;
            if (handler != null) {
                handler(this, e);
            }

            return e.Dictionary;
        }

        public override TlsAuthentication GetAuthentication()
        {
#if SUPPORT_RPK
            if (_tlsKeyPair != null && _tlsKeyPair.CertType == CertificateType.RawPublicKey) {
                MyTlsAuthentication auth = new MyTlsAuthentication(mContext, _tlsKeyPair);
                auth.TlsEventHandler += MyTlsEventHandler;
                return auth;
            }
#endif
#if SUPPORT_TLS_CWT
            if (_tlsKeyPair != null && _tlsKeyPair.CertType == CertificateType.CwtPublicKey) {
                MyTlsAuthentication auth = new MyTlsAuthentication(mContext, _tlsKeyPair, CwtTrustKeySet);
                auth.TlsEventHandler += MyTlsEventHandler;
                return auth;
            }
#endif
            if (_tlsKeyPair != null && _tlsKeyPair.CertType == CertificateType.X509) {
                MyTlsAuthentication auth = new MyTlsAuthentication(mContext, _tlsKeyPair);
                auth.TlsEventHandler += MyTlsEventHandler;
                return auth;
            }

            throw new CoAPException("ICE");
        }

        private void MyTlsEventHandler(object sender, TlsEvent tlsEvent)
        {
            EventHandler<TlsEvent> handler = TlsEventHandler;
            if (handler != null) {
                handler(sender, tlsEvent);
            }
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

        private static BigInteger ConvertBigNum(CBORObject cbor)
        {
            byte[] rgb = cbor.GetByteString();
            byte[] rgb2 = new byte[rgb.Length + 2];
            rgb2[0] = 0;
            rgb2[1] = 0;
            for (int i = 0; i < rgb.Length; i++) {
                rgb2[i + 2] = rgb[i];
            }

            return new BigInteger(rgb2);
        }

        protected TlsSignerCredentials GetECDsaSignerCredentials()
        {
#if SUPPORT_RPK
            if (_tlsKeyPair != null && _tlsKeyPair.CertType == CertificateType.RawPublicKey) {
                OneKey k = _tlsKeyPair.PublicKey;
                if (k.HasKeyType((int)GeneralValuesInt.KeyType_EC2) &&
                    k.HasAlgorithm(AlgorithmValues.ECDSA_256)) {

                    X9ECParameters p = k.GetCurve();
                    ECDomainParameters parameters = new ECDomainParameters(p.Curve, p.G, p.N, p.H);
                    ECPrivateKeyParameters privKey = new ECPrivateKeyParameters("ECDSA", ConvertBigNum(k[CoseKeyParameterKeys.EC_D]), parameters);

                    ECPoint point = k.GetPoint();
                    ECPublicKeyParameters param = new ECPublicKeyParameters(point, parameters);

                    SubjectPublicKeyInfo spi = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(param);

                    return new DefaultTlsSignerCredentials(mContext, new RawPublicKey(spi), privKey, new SignatureAndHashAlgorithm(HashAlgorithm.sha256, SignatureAlgorithm.ecdsa));
                }
            }
#endif

#if SUPPORT_TLS_CWT
            if (_tlsKeyPair != null && _tlsKeyPair.CertType == CertificateType.CwtPublicKey) {
                OneKey k = _tlsKeyPair.PublicCwt.Cnf.Key;
                if (k.HasKeyType((int)GeneralValuesInt.KeyType_EC2) &&
                    k.HasAlgorithm(AlgorithmValues.ECDSA_256)) {

                    X9ECParameters p = k.GetCurve();
                    ECDomainParameters parameters = new ECDomainParameters(p.Curve, p.G, p.N, p.H);
                    ECPrivateKeyParameters privKey = new ECPrivateKeyParameters("ECDSA", ConvertBigNum(k[CoseKeyParameterKeys.EC_D]), parameters);

                    return new DefaultTlsSignerCredentials(mContext, new CwtPublicKey(_tlsKeyPair.PublicCwt.EncodeToBytes()), privKey, new SignatureAndHashAlgorithm(HashAlgorithm.sha256, SignatureAlgorithm.ecdsa));
                }
            }
#endif

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
            public EventHandler<TlsEvent> TlsEventHandler;
#if SUPPORT_RPK || SUPPORT_TLS_CWT
            private KeySet _serverKeys;
#endif
            private TlsKeyPair TlsKey { get; set; }
#if SUPPORT_TLS_CWT
            public KeySet CwtTrustKeySet { get; set; }
#endif

            internal MyTlsAuthentication(TlsContext context, TlsKeyPair rawPublicKey)
            {
                this._mContext = context;
                TlsKey = rawPublicKey;
            }

#if SUPPORT_TLS_CWT
            internal MyTlsAuthentication(TlsContext context, TlsKeyPair cwt, KeySet trustKeys)
            {
                this._mContext = context;
                TlsKey = cwt;
                CwtTrustKeySet = trustKeys;
            }
#endif

            public OneKey AuthenticationKey { get; private set; }

#if SUPPORT_RPK || SUPPORT_TLS_CWT

            protected virtual AbstractCertificate ParseServerCertificate2(short serverCertificateType, Stream stm)
            {
                return null;
            }

            public virtual void NotifyServerCertificate(AbstractCertificate serverCertificate)
            {
                if (serverCertificate is RawPublicKey) {
                    GetRpkKey((RawPublicKey)serverCertificate);
                }
#if SUPPORT_TLS_CWT
                else if (serverCertificate is CwtPublicKey) {
                    GetCwtKey((CwtPublicKey) serverCertificate);
                }
#endif
                else {
                    TlsEvent e = new TlsEvent(TlsEvent.EventCode.ServerCertificate) {
                        Certificate = serverCertificate
                    };

                    EventHandler<TlsEvent> handler = TlsEventHandler;
                    if (handler != null) {
                        handler(this, e);
                    }

                    if (!e.Processed) {
                        throw new TlsFatalAlert(AlertDescription.certificate_unknown);
                    }
                }
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
                    ECPublicKeyParameters ecKey = (ECPublicKeyParameters)key;

                    string s = ecKey.AlgorithmName;
                    OneKey newKey = new OneKey();
                    newKey.Add(CoseKeyKeys.KeyType, GeneralValues.KeyType_EC);
                    if (ecKey.Parameters.Curve.Equals(NistNamedCurves.GetByName("P-256").Curve)) {
                        newKey.Add(CoseKeyParameterKeys.EC_Curve, GeneralValues.P256);
                    }

                    newKey.Add(CoseKeyParameterKeys.EC_X, CBORObject.FromObject(ecKey.Q.Normalize().XCoord.ToBigInteger().ToByteArrayUnsigned()));
                    newKey.Add(CoseKeyParameterKeys.EC_Y, CBORObject.FromObject(ecKey.Q.Normalize().YCoord.ToBigInteger().ToByteArrayUnsigned()));

                    if (_serverKeys != null) {
                        foreach (OneKey k in _serverKeys) {
                            if (k.Compare(newKey)) {
                                AuthenticationKey = k;
                                return;
                            }
                        }
                    }

                    TlsEvent ev = new TlsEvent(TlsEvent.EventCode.ServerCertificate) {
                        KeyValue = newKey
                    };

                    EventHandler<TlsEvent> handler = TlsEventHandler;
                    if (handler != null) {
                        handler(this, ev);
                    }

                    if (!ev.Processed) {
                        //                    throw new TlsFatalAlert(AlertDescription.certificate_unknown);
                    }

                    AuthenticationKey = ev.KeyValue;
                }
                else {
                    // throw new TlsFatalAlert(AlertDescription.certificate_unknown);
                }

            }

#endif

#if SUPPORT_TLS_CWT
            public void GetCwtKey(CwtPublicKey rpk)
            {
                try {
                    Cwt cwt = Cwt.Decode(rpk.EncodedCwt(), CwtTrustKeySet, CwtTrustKeySet);

                    AsymmetricKeyParameter pub = cwt.Cnf.Key.AsPublicKey();
                    SubjectPublicKeyInfo spi = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(pub);
                    rpk.SetSubjectPublicKeyInfo(spi);

                    AuthenticationKey = cwt.Cnf.Key;
                    return;
                }
                catch {
                }

                TlsEvent ev = new TlsEvent(TlsEvent.EventCode.ServerCertificate) {
                    Certificate = rpk
                };

                EventHandler<TlsEvent> handler = TlsEventHandler;
                if (handler != null) {
                    handler(this, ev);
                }

                if (!ev.Processed) {
                    throw new TlsFatalAlert(AlertDescription.certificate_unknown);
                }

                AuthenticationKey = ev.KeyValue;

            }
#endif

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
                TlsEvent e = new TlsEvent(TlsEvent.EventCode.ServerCertificate) {

                    Certificate = serverCertificate,
                    CertificateType = CertificateType.X509
                };

                EventHandler<TlsEvent> handler = TlsEventHandler;
                if (handler != null) {
                    handler(this, e);
                }

                if (!e.Processed) {
                    throw new TlsFatalAlert(AlertDescription.certificate_unknown);
                }
            }

            private BigInteger ConvertBigNum(CBORObject cbor)
            {
                byte[] rgb = cbor.GetByteString();
                byte[] rgb2 = new byte[rgb.Length + 2];
                rgb2[0] = 0;
                rgb2[1] = 0;
                for (int i = 0; i < rgb.Length; i++) {
                    rgb2[i + 2] = rgb[i];
                }

                return new BigInteger(rgb2);
            }

            public virtual TlsCredentials GetClientCredentials(CertificateRequest certificateRequest)
            {
                if (certificateRequest.CertificateTypes == null ||
                    !Arrays.Contains(certificateRequest.CertificateTypes, ClientCertificateType.ecdsa_sign)) {
                    return null;
                }

                if (TlsKey != null) {
                    if (TlsKey.CertType == CertificateType.X509) {

                        return new DefaultTlsSignerCredentials(_mContext, new Certificate(TlsKey.X509Certificate), TlsKey.PrivateKey.AsPrivateKey(),
                            new SignatureAndHashAlgorithm(HashAlgorithm.sha256, SignatureAlgorithm.ecdsa));
                    }
#if SUPPORT_RPK
                    else if (TlsKey.CertType == CertificateType.RawPublicKey) {
                        OneKey k = TlsKey.PrivateKey;
                        if (k.HasKeyType((int) GeneralValuesInt.KeyType_EC2) &&
                            k.HasAlgorithm(AlgorithmValues.ECDSA_256)) {

                            X9ECParameters p = k.GetCurve();
                            ECDomainParameters parameters = new ECDomainParameters(p.Curve, p.G, p.N, p.H);
                            ECPrivateKeyParameters privKey = new ECPrivateKeyParameters("ECDSA", ConvertBigNum(k[CoseKeyParameterKeys.EC_D]), parameters);

                            ECPoint point = k.GetPoint();
                            ECPublicKeyParameters param = new ECPublicKeyParameters("ECDSA", point, /*parameters*/ SecObjectIdentifiers.SecP256r1);

                            SubjectPublicKeyInfo spi = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(param);

                        return new DefaultTlsSignerCredentials(_mContext, new RawPublicKey(spi), privKey, new SignatureAndHashAlgorithm(HashAlgorithm.sha256, SignatureAlgorithm.ecdsa));
                    }

                }
#endif
#if SUPPORT_TLS_CWT

                    else if (TlsKey.CertType == CertificateType.CwtPublicKey) {
                    OneKey k = TlsKey.PublicCwt.Cnf.Key;
                        if (k.HasKeyType((int) GeneralValuesInt.KeyType_EC2) &&
                            k.HasAlgorithm(AlgorithmValues.ECDSA_256)) {

                            AsymmetricKeyParameter privKey = TlsKey.PrivateKey.AsPrivateKey();

                            return new DefaultTlsSignerCredentials(_mContext, new CwtPublicKey(TlsKey.PublicCwt.EncodeToBytes()), privKey,
                                new SignatureAndHashAlgorithm(HashAlgorithm.sha256, SignatureAlgorithm.ecdsa));
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
        };

        protected virtual TlsKeyExchange CreatePskKeyExchange(int keyExchange)
        {
            return new TlsPskKeyExchange(keyExchange, mSupportedSignatureAlgorithms, _mPskIdentity, null, null, null, mNamedCurves,
                mClientECPointFormats, mServerECPointFormats);
        }

        protected override TlsKeyExchange CreateECDHKeyExchange(int keyExchange)        {
            return new TlsECDHKeyExchange(keyExchange, mSupportedSignatureAlgorithms, mNamedCurves, mClientECPointFormats,
                mServerECPointFormats);
        }
    }
}

