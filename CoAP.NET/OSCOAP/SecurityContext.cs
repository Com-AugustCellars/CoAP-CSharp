﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
// using System.Runtime.Remoting.Messaging;
using System.Text;
using PeterO.Cbor;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Com.AugustCellars.COSE;

namespace Com.AugustCellars.CoAP.OSCOAP
{
    /// <summary>
    /// Security context information for use with the OSCOAP structures.
    /// This structure supports doing both unicast and multicast transmission and
    /// reception of messages.
    /// </summary>
    public class SecurityContext
    {
#region Replay Window Code
        /// <summary>
        /// Class implementation used for doing checking if a message is being replayed at us.
        /// </summary>
        public class ReplayWindow
        {
            private BitArray _hits;
            public long BaseValue { get; private set; }

            /// <summary>
            /// create a replay window and initialize where the floating window is.
            /// </summary>
            /// <param name="baseValue">Start value to check for hits</param>
            /// <param name="arraySize">Size of the replay window</param>
            public ReplayWindow(int baseValue, int arraySize)
            {
                BaseValue = baseValue;
                _hits = new BitArray(arraySize);
            }

            /// <summary>
            /// Check if the value is in the replay window and if it has been set.
            /// </summary>
            /// <param name="index">value to check</param>
            /// <returns>true if should treat as replay</returns>
            public bool HitTest(long index)
            {

                index -= BaseValue;
                if (index < 0) return true;
                if (index >= _hits.Length) return false;
                return _hits.Get((int)index);
            }

            /// <summary>
            /// Set a value has having been seen.
            /// </summary>
            /// <param name="index">value that was seen</param>
            /// <returns>true if the zone was shifted</returns>
            public bool SetHit(long index)
            {
                bool returnValue = false;
                index -= BaseValue;
                if (index < 0) return false;
                if (index >= _hits.Length) {
                    returnValue = true;
                    if (index < _hits.Length * 3 / 2) {
                        int v = _hits.Length / 2;
                        BaseValue += v;
                        BitArray t = new BitArray(_hits.Length);
                        for (int i = 0; i < v; i++) {
                            t[i] = _hits[i + v];
                        }

                        _hits = t;
                        index -= v;
                    }
                    else {
                        BaseValue = index;
                        _hits.SetAll(false);
                        index = 0;
                    }
                }
                _hits.Set((int) index, true);
                return returnValue;
            }
        }
        #endregion

#region Entity Context - information about a single sender

        /// <summary>
        /// Crypto information dealing with a single entity that sends data
        /// </summary>
        public class EntityContext
        {
            /// <summary>
            /// Create new entity crypto context structure
            /// </summary>
            public EntityContext() { }

            /// <summary>
            /// Create new entity crypto context structure
            /// Copy constructor - needed to clone key material
            /// </summary>
            /// <param name="old">old structure</param>
            public EntityContext(EntityContext old)
            {
                Algorithm = old.Algorithm;
                BaseIV = (byte[])old.BaseIV.Clone();
                Key = (byte[])old.Key.Clone();
                Id = (byte[])old.Id.Clone();
                ReplayWindow = new ReplayWindow(0, 256);
                SequenceNumber = old.SequenceNumber;
                SigningKey = old.SigningKey;
            }

            /// <summary>
            /// What encryption algorithm is being used?
            /// </summary>
            public CBORObject Algorithm { get; set; }

            public CBORObject SigningAlgorithm { get; set; }

            /// <summary>
            /// What is the base IV value for this context?
            /// </summary>
            public byte[] BaseIV { get; set; }

            /// <summary>
            /// What is the identity of this context - matches a key identifier.
            /// </summary>
            public byte[] Id { get; set; }

            /// <summary>
            /// What is the cryptographic key?
            /// </summary>
            public byte[] Key { get; set; }

            /// <summary>
            /// What is the current sequence number (IV) for the context?
            /// </summary>
            public long SequenceNumber { get; set; }

            /// <summary>
            /// At what frequency should the IV update event be sent?
            /// SequenceNumber % SequenceInterval == 0
            /// </summary>
            public int SequenceInterval { get; set; } = 100;

            /// <summary>
            /// Should an IV update event be sent?
            /// </summary>
            public bool SendSequenceNumberUpdate => (SequenceNumber % SequenceInterval) == 0;

            /// <summary>
            /// Return the sequence number as a byte array.
            /// </summary>
            public byte[] PartialIV
            {
                get
                {
                    byte[] part = BitConverter.GetBytes(SequenceNumber);
                    if (BitConverter.IsLittleEndian) Array.Reverse(part);
                    int i;
                    for (i = 0; i < part.Length - 1; i++) {
                        if (part[i] != 0) break;
                    }

                    Array.Copy(part, i, part, 0, part.Length - i);
                    Array.Resize(ref part, part.Length - i);

                    return part;
                }
            }

            /// <summary>
            /// Given a partial IV, create the actual IV to use
            /// </summary>
            /// <param name="partialIV">partial IV</param>
            /// <returns>full IV</returns>
            public CBORObject GetIV(CBORObject partialIV)
            {
                return GetIV(partialIV.GetByteString());
            }

            /// <summary>
            /// Given a partial IV, create the actual IV to use
            /// </summary>
            /// <param name="partialIV">partial IV</param>
            /// <returns>full IV</returns>
            public CBORObject GetIV(byte[] partialIV)
            {
                byte[] iv = (byte[])BaseIV.Clone();
                int offset = iv.Length - partialIV.Length;

                for (int i = 0; i < partialIV.Length; i++) {
                    iv[i + offset] ^= partialIV[i];
                }

                return CBORObject.FromObject(iv);
            }

            /// <summary>
            /// Get/Set the replay window checker for the context.
            /// </summary>
            public ReplayWindow ReplayWindow { get; set; }

            /// <summary>
            /// Increment the sequence/parital IV
            /// </summary>
            public void IncrementSequenceNumber()
            {
                SequenceNumber += 1;
                if (SequenceNumber > MaxSequenceNumber) {
                    throw new CoAPException("Oscore Partial IV exhaustion");
                }
            }

            /// <summary>
            /// Check to see if all of the Partial IV Sequence numbers are exhausted.
            /// </summary>
            /// <returns>true if exhausted</returns>
            public bool SequenceNumberExhausted => SequenceNumber >= MaxSequenceNumber;

            private long _maxSequenceNumber = 0xffffffffff;
            /// <summary>
            /// Set/get the maximum sequence number.  Limited to five bits.
            /// </summary>
            public long MaxSequenceNumber
            {
                get => _maxSequenceNumber;
                set {
                    if (value > 0x1f || value < 0) {
                        throw new CoAPException("value must be no more than 0x1f");
                    }
                    _maxSequenceNumber = value;
                }
            }

            /// <summary>
            /// The key to use for counter signing purposes
            /// </summary>
            public OneKey SigningKey { get; set; }

            /// <inheritdoc />
            public override string ToString()
            {
                string ret = $"kid= {BitConverter.ToString(Id)} key={BitConverter.ToString(Key)} IV={BitConverter.ToString(BaseIV)} PartialIV={BitConverter.ToString(PartialIV)}\n";
                if (SigningKey != null) {
                    ret += $" {SigningKey.AsCBOR()}";
                }

                return ret;
            }
        }
#endregion

        private static int _contextNumber;
        private byte[] _masterSecret;
        private byte[] _salt;
        public CBORObject CountersignParams { get; set; }
        public CBORObject CountersignKeyParams { get; set; }
        public int SignatureSize { get; } = 64;

        /// <summary>
        /// What is the global unique context number for this context.
        /// </summary>
        public int ContextNo { get; private set; }

        /// <summary>
        /// Return the sender information object
        /// </summary>
        public EntityContext Sender { get; private set; } = new EntityContext();

        /// <summary>
        /// Return the single recipient object
        /// </summary>
        public EntityContext Recipient { get; private set; }

        /// <summary>
        /// Get the set of all recipients for group.
        /// </summary>
        public Dictionary<byte[], EntityContext> Recipients { get; private set; }

        /// <summary>
        /// Group ID for multi-cast.
        /// </summary>
        public byte[] GroupId { get; set; }

        /// <summary>
        /// Location for a user to place significant information.
        /// For contexts that are created by the system this will be a list of
        /// COSE Web Tokens for authorization
        /// </summary>
        public object UserData { get; set; }

        /// <summary>
        /// Mark this context as being replaced with a new context
        /// </summary>
        public SecurityContext ReplaceWithSecurityContext { get; set; }

        /// <summary>
        /// Create a new empty security context
        /// </summary>
        public SecurityContext() { }

        /// <summary>
        /// Create a new security context to hold info for group.
        /// </summary>
        /// <param name="groupId"></param>
        [Obsolete ("Unused Constructor")]
        public SecurityContext(byte[] groupId)
        {
            Recipients = new Dictionary<byte[], EntityContext>(new ByteArrayComparer());
            GroupId = groupId;
        }

        /// <summary>
        /// Clone a security context - needed because key info needs to be copied.
        /// </summary>
        /// <param name="old">context to clone</param>
        public SecurityContext(SecurityContext old)
        {
            ContextNo = old.ContextNo;
            GroupId = old.GroupId;
            Sender = new EntityContext(old.Sender);
            if (old.Recipient != null) Recipient = new EntityContext(old.Recipient);
            if (old.Recipients != null) {
                Recipients = new Dictionary<byte[], EntityContext>(new ByteArrayComparer());
                foreach (var item in old.Recipients) {
                    Recipients[item.Key] = new EntityContext(new EntityContext(item.Value));
                }
            }
        }



        public void AddRecipient(byte[] recipientId, OneKey signKey)
        {
            if (!signKey.HasAlgorithm(Sender.SigningAlgorithm)) {
                throw new ArgumentException("signature algorithm not correct");
            }
            EntityContext x = DeriveEntityContext(_masterSecret, GroupId, recipientId, _salt, Sender.Algorithm);
            x.SigningKey = signKey;

            Recipients.Add(recipientId, x);
        }

        public void ReplaceSender(byte[] senderId, OneKey signKey)
        {
            if (!signKey.HasAlgorithm(Sender.SigningAlgorithm)) {
                throw new ArgumentException("signature algorithm not correct");
            }

            EntityContext x = DeriveEntityContext(_masterSecret, GroupId, senderId, _salt, Sender.Algorithm);
            x.SigningKey = signKey;
            x.SigningAlgorithm = Sender.SigningAlgorithm;

            Sender = x;
        }


        #region  Key Derivation Functions
        /// <summary>
        /// Given the input security context information, derive a new security context
        /// and return it
        /// </summary>
        /// <param name="rawData"></param>
        public static SecurityContext DeriveContext(CBORObject rawData, bool isServer)
        {
            byte[] groupId = null;
            byte[] senderId = rawData[isServer ? 3 : 2].GetByteString();
            byte[] receiverId = rawData[isServer ? 2 : 3].GetByteString();
            byte[] salt = null;
            CBORObject algAEAD = null;
            CBORObject algKDF = null;

            if (rawData.ContainsKey(7)) {
                groupId = rawData[7].GetByteString();
            }

            if (rawData.ContainsKey(4)) {
                algKDF = rawData[4];
            }

            if (rawData.ContainsKey(5)) {
                algAEAD = rawData[5];
            }

            return DeriveContext(rawData[1].GetByteString(), groupId, senderId, receiverId,  salt, algAEAD, algKDF);
        }

        /// <summary>
        /// Given the set of inputs, perform the cryptographic operations that are needed
        /// to build a security context for a single sender and recipient.
        /// </summary>
        /// <param name="masterSecret">pre-shared key</param>
        /// <param name="senderContext">context for the ID</param>
        /// <param name="senderId">name assigned to sender</param>
        /// <param name="recipientId">name assigned to recipient</param>
        /// <param name="masterSalt">salt value</param>
        /// <param name="algAEAD">encryption algorithm</param>
        /// <param name="algKeyAgree">key agreement algorithm</param>
        /// <returns></returns>
        public static SecurityContext DeriveContext(byte[] masterSecret, byte[] senderContext, byte[] senderId, byte[] recipientId, 
                                                    byte[] masterSalt = null, CBORObject algAEAD = null, CBORObject algKeyAgree = null)
        {
            int cbKey;
            int cbIV;
            SecurityContext ctx = new SecurityContext();

            if (algAEAD == null) {
                ctx.Sender.Algorithm = AlgorithmValues.AES_CCM_16_64_128;
            }
            else {
                ctx.Sender.Algorithm = algAEAD;
            }

            if (ctx.Sender.Algorithm.Type != CBORType.Integer) throw new CoAPException("Unsupported algorithm");
            switch ((AlgorithmValuesInt) ctx.Sender.Algorithm.AsInt32()) {
                case AlgorithmValuesInt.AES_CCM_16_64_128:
                    cbKey = 128/8;
                    cbIV = 13;
                    break;

                case AlgorithmValuesInt.AES_CCM_64_64_128:
                    cbKey = 128/8;
                    cbIV = 56/8;
                    break;

                case AlgorithmValuesInt.AES_CCM_64_128_128:
                    cbKey = 128 / 8;
                    cbIV = 56 / 8;
                    break;

                case AlgorithmValuesInt.AES_CCM_16_128_128:
                    cbKey = 128 / 8;
                    cbIV = 13;
                    break;

                case AlgorithmValuesInt.AES_GCM_128:
                    cbKey = 128 / 8;
                    cbIV = 96 / 8;
                    break;

                default:
                    throw new CoAPException("Unsupported algorithm");
            }

            ctx.Sender.Id = senderId ?? throw new ArgumentNullException(nameof(senderId));

            ctx.Recipient = new EntityContext {
                Algorithm = ctx.Sender.Algorithm,
                Id = recipientId ?? throw new ArgumentNullException(nameof(recipientId)),
                ReplayWindow = new ReplayWindow(0, 64)
            };


            CBORObject info = CBORObject.NewArray();

            info.Add(senderId); // 0
            info.Add(senderContext); // 1
            info.Add(ctx.Sender.Algorithm); // 2
            info.Add("Key"); // 3
            info.Add(cbKey); // 4 in bytes

            IDigest sha256;

            if (algKeyAgree == null || algKeyAgree.Equals(AlgorithmValues.ECDH_SS_HKDF_256)) {

                sha256 = new Sha256Digest();
            }
            else if (algKeyAgree.Equals(AlgorithmValues.ECDH_SS_HKDF_512)) {
                sha256 = new Sha512Digest();
            }
            else {
                throw new ArgumentException("Unrecognized key agreement algorithm");
            }

            IDerivationFunction hkdf = new HkdfBytesGenerator(sha256);
            hkdf.Init(new HkdfParameters(masterSecret, masterSalt, info.EncodeToBytes()));

            ctx.Sender.Key = new byte[cbKey];
            hkdf.GenerateBytes(ctx.Sender.Key, 0, ctx.Sender.Key.Length);

            info[0] = CBORObject.FromObject(recipientId);
            hkdf.Init(new HkdfParameters(masterSecret, masterSalt, info.EncodeToBytes()));
            ctx.Recipient.Key = new byte[cbKey];
            hkdf.GenerateBytes(ctx.Recipient.Key, 0, ctx.Recipient.Key.Length);

            info[0] = CBORObject.FromObject(new byte[0]);
            info[3] = CBORObject.FromObject("IV");
            info[4] = CBORObject.FromObject(cbIV);
            hkdf.Init(new HkdfParameters(masterSecret, masterSalt, info.EncodeToBytes()));
            ctx.Recipient.BaseIV = new byte[cbIV];
            hkdf.GenerateBytes(ctx.Recipient.BaseIV, 0, ctx.Recipient.BaseIV.Length);
            ctx.Sender.BaseIV = (byte[]) ctx.Recipient.BaseIV.Clone();

            int iIv = cbIV - 5 - senderId.Length;
            if (cbIV - 6 < senderId.Length) throw new CoAPException("Sender Id too long");
            ctx.Sender.BaseIV[0] ^= (byte) senderId.Length;
            for (int i = 0; i < senderId.Length; i++) {
                ctx.Sender.BaseIV[iIv + i] ^= senderId[i];
            }

            iIv = cbIV - 5 - recipientId.Length;
            if (cbIV - 6 < recipientId.Length) throw new CoAPException("Recipient Id too long");
            ctx.Recipient.BaseIV[0] ^= (byte) recipientId.Length;
            for (int i = 0; i < recipientId.Length; i++) {
                ctx.Recipient.BaseIV[iIv + i] ^= recipientId[i];
            }

            //  Give a unique context number for doing comparisons

            ctx.ContextNo = _contextNumber;
            _contextNumber += 1;

            return ctx;
        }

        /// <summary>
        /// Given the set of inputs, perform the cryptographic operations that are needed
        /// to build a security context for a single sender and recipient.
        /// </summary>
        /// <param name="masterSecret">pre-shared key</param>
        /// <param name="groupId">identifier for the group</param>
        /// <param name="senderId">name assigned to sender</param>
        /// <param name="algSignature">What is the signature algorithm</param>
        /// <param name="senderSignKey">what is the signing key for the signer</param>
        /// <param name="recipientIds">names assigned to recipients</param>
        /// <param name="recipientSignKeys">keys for any assigned recipients</param>
        /// <param name="masterSalt">salt value</param>
        /// <param name="algAEAD">encryption algorithm</param>
        /// <param name="algKeyAgree">key agreement algorithm</param>
        /// <returns></returns>
        public static SecurityContext DeriveGroupContext(byte[] masterSecret, byte[] groupId, byte[] senderId, CBORObject algSignature, OneKey senderSignKey, 
                                                         byte[][] recipientIds, OneKey[] recipientSignKeys, 
                                                         byte[] masterSalt = null, CBORObject algAEAD = null, CBORObject algKeyAgree = null)
        {
            SecurityContext ctx = new SecurityContext {
                Recipients = new Dictionary<byte[], EntityContext>(new ByteArrayComparer()), 
                _masterSecret = masterSecret,
                _salt = masterSalt
            };

            if ((recipientIds != null && recipientSignKeys != null) && (recipientIds.Length != recipientSignKeys.Length)) {
                throw new ArgumentException("recipientsIds and recipientSignKey must be the same length");
            }

            if (!senderSignKey.HasAlgorithm(algSignature)) {
                throw new ArgumentException("Wrong algorithm for sender sign key");
            }

            ctx.Sender = DeriveEntityContext(masterSecret, groupId, senderId, masterSalt, algAEAD, algKeyAgree);
            ctx.Sender.SigningAlgorithm = algSignature;
            ctx.Sender.SigningKey = senderSignKey;
            
            if (recipientIds != null) {
                if (recipientSignKeys == null) throw new ArgumentException("recipientSignKeys is null when recipientIds is not null");
                ctx.Recipients = new Dictionary<byte[], EntityContext>(new ByteArrayComparer());
                for (int i =0; i<recipientIds.Length; i++ ) {
                    if (!recipientSignKeys[i].HasAlgorithm(algSignature)) {
                        throw new ArgumentException("Wrong algorithm for recipient sign key");
                    }
                    EntityContext et = DeriveEntityContext(masterSecret, groupId, recipientIds[i], masterSalt, algAEAD, algKeyAgree);
                    et.SigningKey = recipientSignKeys[i];
                    ctx.Recipients.Add(recipientIds[i], et);
                }
            }
            else if (recipientSignKeys != null) {
                throw new ArgumentException("recipientIds is null when recipientSignKeys is not null");
            }

            ctx.GroupId = groupId;

            return ctx;
        }


        /// <summary>
        /// Given the set of inputs, perform the cryptographic operations that are needed
        /// to build a security context for a single sender and recipient.
        /// </summary>
        /// <param name="masterSecret">pre-shared key</param>
        /// <param name="groupId">Group/Context Identifier</param>
        /// <param name="entityId">name assigned to sender</param>
        /// <param name="masterSalt">salt value</param>
        /// <param name="algAEAD">encryption algorithm</param>
        /// <param name="algKeyAgree">key agreement algorithm</param>
        /// <returns></returns>
        private static EntityContext DeriveEntityContext(byte[] masterSecret, byte[] groupId, byte[] entityId, byte[] masterSalt = null, CBORObject algAEAD = null, CBORObject algKeyAgree = null)
        {
            EntityContext ctx = new EntityContext();
            int keySize;
            int ivSize;

            ctx.Algorithm = algAEAD ?? AlgorithmValues.AES_CCM_16_64_128;
            ctx.Id = entityId ?? throw new ArgumentNullException(nameof(entityId));
            if (algKeyAgree == null) algKeyAgree = AlgorithmValues.ECDH_SS_HKDF_256;

            if (ctx.Algorithm.Type != CBORType.Integer) throw new ArgumentException("algorithm is unknown" );
            switch ((AlgorithmValuesInt) ctx.Algorithm.AsInt32()) {
                case AlgorithmValuesInt.AES_CCM_16_64_128:
                    keySize = 128 / 8;
                    ivSize = 13;
                    break;

                case AlgorithmValuesInt.AES_GCM_128:
                    keySize = 128 / 8;
                    ivSize = 96 / 8;
                    break;                       

                default:
                    throw new ArgumentException("algorithm is unknown");
            }

            ctx.ReplayWindow = new ReplayWindow(0, 64);

            CBORObject info = CBORObject.NewArray();

            // M00TODO - add the group id into this

            info.Add(entityId);                 // 0
            info.Add(groupId);                  // 1
            info.Add(ctx.Algorithm);            // 2
            info.Add("Key");                    // 3
            info.Add(keySize);                  // 4 in bytes


            IDigest sha256;
            
            if (algKeyAgree == null || algKeyAgree.Equals(AlgorithmValues.ECDH_SS_HKDF_256)) {
                sha256 = new Sha256Digest();
            }
            else if (algKeyAgree.Equals(AlgorithmValues.ECDH_SS_HKDF_512)) {
                sha256 = new Sha512Digest();
            }
            else {
                throw new ArgumentException("Unknown key agree algorithm");
            }

            IDerivationFunction hkdf = new HkdfBytesGenerator(sha256);
            hkdf.Init(new HkdfParameters(masterSecret, masterSalt, info.EncodeToBytes()));

            ctx.Key = new byte[keySize];
            hkdf.GenerateBytes(ctx.Key, 0, ctx.Key.Length);

            info[0] = CBORObject.FromObject(new byte[0]);
            info[3] = CBORObject.FromObject("IV");
            info[4] = CBORObject.FromObject(ivSize);
            hkdf.Init(new HkdfParameters(masterSecret, masterSalt, info.EncodeToBytes()));
            ctx.BaseIV = new byte[ivSize];
            hkdf.GenerateBytes(ctx.BaseIV, 0, ctx.BaseIV.Length);

            // Modify the context 

            if (ivSize - 6 < entityId.Length) throw new CoAPException("Entity id is too long");
            ctx.BaseIV[0] ^= (byte) entityId.Length;
            int i1 = ivSize - 5 - entityId.Length /*- 1*/;
            for (int i = 0; i < entityId.Length; i++) {
                ctx.BaseIV[i1 + i] ^= entityId[i];
            }

            return ctx;
        }
#endregion

        public bool IsGroupContext => Recipients != null;

        public event EventHandler<OscoreEvent> OscoreEvents;

        public void OnEvent(OscoreEvent e)
        {
            EventHandler<OscoreEvent> eventHandler = OscoreEvents;
            eventHandler?.Invoke(this, e);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("SecurityContext: ");
            sb.Append($"Secret: {BitConverter.ToString(_masterSecret)}\n");
            sb.Append($"Sender: {Sender}");
            if (IsGroupContext) {
                foreach (KeyValuePair<byte[], EntityContext> entity in Recipients) {
                    sb.Append($"Entity: {entity.Value}\n");

                }
            }
            else {
                sb.Append($"Recipient: {Recipient}");
            }

            return sb.ToString();
        }

#region Equality comparer for bytes

        public class ByteArrayComparer : EqualityComparer<byte[]>
        {
            public override bool Equals(byte[] first, byte[] second)
            {
                return AreEqual(first, second);
            }

            public static bool AreEqual(byte[] first, byte[] second)
            {
                if (first == null || second == null) {
                    // null == null returns true.
                    // non-null == null returns false.
                    return first == second;
                }
                if (ReferenceEquals(first, second)) {
                    return true;
                }
                if (first.Length != second.Length) {
                    return false;
                }
                // Linq extension method is based on IEnumerable, must evaluate every item.
                return first.SequenceEqual(second);
            }
            public override int GetHashCode(byte[] obj)
            {
                if (obj == null) {
                    throw new ArgumentNullException(nameof(obj));
                }
                // quick and dirty, instantly identifies obviously different
                // arrays as being different
                return obj.Length;
            }
        }
#endregion

    }
}
