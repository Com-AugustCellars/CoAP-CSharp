using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Com.AugustCellars.COSE;
using Com.AugustCellars.WebToken;
using Org.BouncyCastle.Security;

namespace Com.AugustCellars.CoAP.DTLS
{
    public class TlsKeyPair
    {
        public OneKey PrivateKey { get; }
        public OneKey PublicKey { get; }
        public CWT PublicCwt { get; }
        public byte[] X509Certificate { get; }

        public byte CertType { get; }

        public TlsKeyPair(OneKey publicKey, OneKey privateKey)
        {
            this.PrivateKey = privateKey;
            this.PublicKey = publicKey;
            CertType = 2; // RPK
        }

        public TlsKeyPair(CWT publicKey, OneKey privateKey)
        {
            this.PrivateKey = privateKey;
            this.PublicCwt = publicKey;
            CertType = 254; // CWT
        }

        public TlsKeyPair(byte[] certificate, OneKey privateKey)
        {
            this.PrivateKey = privateKey;
            this.X509Certificate = certificate;
            CertType = 1; // X.509 Certificate
        }

        public bool Compare(TlsKeyPair other)
        {
            return false;
        }
    }

    public class TlsKeyPairSet
    {
        List<TlsKeyPair> _keyList = new List<TlsKeyPair>();

        /// <summary>
        /// Return number of keys in the key set.
        /// </summary>
        public int Count => _keyList.Count;

        /// <summary>
        /// Return first key in the set.
        /// </summary>
        public TlsKeyPair FirstKey => _keyList.First();

        /// <summary>
        /// Return the i-th element in the key set.
        /// </summary>
        /// <param name="i">index of element to return</param>
        /// <returns>OneKey</returns>
        public TlsKeyPair this[int i] => _keyList[i];

        /// <summary>
        /// Add a key to the key set.  The function will do a minimal check for equality to existing keys in the set.
        /// </summary>
        /// <param name="key">OneKey: key to be added</param>
        public void AddKey(TlsKeyPair key)
        {
            foreach (TlsKeyPair k in _keyList)
            {
                if (key.Compare(k))
                    return;
            }
            _keyList.Add(key);
        }

        /// <summary>
        /// All forall to be used to enumerate the keys in a key set.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<TlsKeyPair> GetEnumerator()
        {
            return _keyList.GetEnumerator();
        }


    }
}
