using System;
using System.Collections;

using PeterO.Cbor;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Com.AugustCellars.COSE;

namespace CoAP.OSCOAP
{
#if INCLUDE_OSCOAP
    public class SecurityContext
    {
        public class replayWindow
        {
            BitArray _hits;
            Int64 _baseValue;

            public replayWindow(int baseValue, int arraySize)
            {
                _baseValue = baseValue;
                _hits = new BitArray(arraySize);
            }

            public bool HitTest(Int64 index)
            {
                index -= _baseValue;
                if (index < 0) return true;
                if (index > _hits.Length) return false;
                return _hits.Get((int)index);
            }

            public void SetHit(Int64 index)
            {
                index -= _baseValue;
                if (index < 0) return;
                if (index > _hits.Length) {
                    if (index > _hits.Length * 3 / 2) {
                        int v = _hits.Length / 2;
                        _baseValue += v;
                        BitArray t = new BitArray(_hits.Length);
                        for (int i = 0; i < v; i++) t[i] = _hits[i + v];
                        _hits = t;
                        index -= v;
                    }
                    else {
                        _baseValue = index;
                        _hits.SetAll(false);
                        index = 0;
                    }
                }
                _hits.Set((int)index, true);
            }
        }

        public class EntityContext
        {
            CBORObject _algorithm;
            byte[] _baseIV;
            byte[] _key;
            byte[] _id;
            int _sequenceNumber;
            replayWindow _replay;

            public EntityContext() { }

            public EntityContext(EntityContext old)
            {
                _algorithm = old._algorithm;
                _baseIV = (byte[])old._baseIV.Clone();
                _key = (byte[])old._key.Clone();
                _id = (byte[])old._id.Clone();
                _replay = new replayWindow(0, 256);
                _sequenceNumber = old._sequenceNumber;
            }

            public CBORObject Algorithm
            {
                get { return _algorithm; }
                set { _algorithm = value; }
            }
            public byte[] BaseIV { get { return _baseIV; } set { _baseIV = value; } }
            public byte[] Id { get { return _id; } set { _id = value; } }
            public byte[] Key { get { return _key; } set { _key = value; } }
            public int SequenceNumber { get { return _sequenceNumber; } set { _sequenceNumber = value; } }
            public byte[] PartialIV
            {
                get
                {
                    byte[] part = BitConverter.GetBytes(_sequenceNumber);
                    if (BitConverter.IsLittleEndian) Array.Reverse(part);
                    int i;
                    for (i = 0; i < part.Length - 1; i++) if (part[i] != 0) break;
                    Array.Copy(part, i, part, 0, part.Length - i);
                    Array.Resize(ref part, part.Length - i);

                    return part;
                }
            }

            public CBORObject GetIV(CBORObject partialIV)
            {
                return GetIV(partialIV.GetByteString());
            }
            public CBORObject GetIV(byte[] partialIV)
            {
                byte[] IV = (byte[])_baseIV.Clone();
                int offset = IV.Length - partialIV.Length;

                for (int i = 0; i < partialIV.Length; i++) IV[i + offset] ^= partialIV[i];

                return CBORObject.FromObject(IV);
            }
            public replayWindow ReplayWindow { get { return _replay; } set { _replay = value; } }
            public void IncrementSequenceNumber() { _sequenceNumber += 1; }
        }

        static int ContextNumber = 0;
        int _contextNo;
        public int ContextNo { get { return _contextNo; } }

        EntityContext _sender = new EntityContext();
        public EntityContext Sender { get { return _sender; } }

        EntityContext _recipient = new EntityContext();
        public EntityContext Recipient { get { return _recipient; } }

        public SecurityContext() { }
        public SecurityContext(SecurityContext old)
        {
            _contextNo = old._contextNo;
            _sender = new EntityContext(old._sender);
            _recipient = new EntityContext(old._recipient);
        }


        public static SecurityContext DeriveContext(byte[] MasterSecret, byte[] SenderId, byte[] RecipientId, byte[] MasterSalt = null, CBORObject AEADAlg = null, CBORObject KeyAgreeAlg = null)
        {
            SecurityContext ctx = new SecurityContext();

            if (AEADAlg == null) ctx.Sender.Algorithm = AlgorithmValues.AES_CCM_64_64_128;
            else ctx.Sender.Algorithm = AEADAlg;

            if (SenderId == null) throw new ArgumentNullException("SenderId");
            ctx.Sender.Id = SenderId;

            ctx.Recipient.Algorithm = ctx.Sender.Algorithm;
            if (RecipientId == null) throw new ArgumentNullException("RecipientId");
            ctx.Recipient.Id = RecipientId;

            ctx.Recipient.ReplayWindow = new replayWindow(0, 64);

            CBORObject info = CBORObject.NewArray();

            info.Add(SenderId);                 // 0
            info.Add(ctx.Sender.Algorithm);     // 1
            info.Add("Key");                    // 2
            info.Add(128/8);                    // 3 in bytes

            IDigest sha256 = new Sha256Digest();
            IDerivationFunction hkdf = new HkdfBytesGenerator(sha256);
            hkdf.Init(new HkdfParameters(MasterSecret, MasterSalt, info.EncodeToBytes()));

            ctx.Sender.Key = new byte[128/8];
            hkdf.GenerateBytes(ctx.Sender.Key, 0, ctx.Sender.Key.Length);

            info[0] = CBORObject.FromObject(RecipientId);
            hkdf.Init(new HkdfParameters(MasterSecret, MasterSalt, info.EncodeToBytes()));
            ctx.Recipient.Key = new byte[128/8];
            hkdf.GenerateBytes(ctx.Recipient.Key, 0, ctx.Recipient.Key.Length);
 
            info[2] = CBORObject.FromObject("IV");
            info[3] = CBORObject.FromObject(56/8);
            hkdf.Init(new HkdfParameters(MasterSecret, MasterSalt, info.EncodeToBytes()));
            ctx.Recipient.BaseIV = new byte[56/8];
            hkdf.GenerateBytes(ctx.Recipient.BaseIV, 0, ctx.Recipient.BaseIV.Length);

            info[0] = CBORObject.FromObject(SenderId);
            hkdf.Init(new HkdfParameters(MasterSecret, MasterSalt, info.EncodeToBytes()));
            ctx.Sender.BaseIV = new byte[56/8];
            hkdf.GenerateBytes(ctx.Sender.BaseIV, 0, ctx.Sender.BaseIV.Length);

            //  Give a unique context number for doing comparisons

            ctx._contextNo = SecurityContext.ContextNumber;
            SecurityContext.ContextNumber += 1;

            return ctx;
        }

#if DEBUG
        static int _FutzError = 0;
        static public int FutzError {
            get { return _FutzError; }
            set { _FutzError = value; }
        }
#endif
    }
#endif
}
