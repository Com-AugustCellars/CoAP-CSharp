using System.Text;
using Com.AugustCellars.CoAP.Util;
using PeterO.Cbor;

namespace Com.AugustCellars.CoAP.Coral
{
    public class CoralDocument : CoralBody
    {
        public CoralDocument()
        {

        }

        public CoralDocument(CBORObject node,  Cori baseCori, CoralDictionary dictionary) : base(node, baseCori, dictionary)
        {
        }

        public static CoralDocument DecodeFromBytes(byte[] encoded, Cori baseCori, CoralDictionary dictionary = null)
        {
            CBORObject obj = CBORObject.DecodeFromBytes(encoded);
            return DecodeFromCbor(obj, baseCori, dictionary);
        }

        public static CoralDocument DecodeFromCbor(CBORObject node, Cori baseCori, CoralDictionary dictionary = null)
        {
            if (dictionary == null)
            {
                dictionary = CoralDictionary.Default;
            }
            return new CoralDocument(node, baseCori, dictionary);
        }

        public byte[] EncodeToBytes(Cori baseCori, CoralDictionary dictionary = null)
        {
            if (dictionary == null) {
                dictionary = CoralDictionary.Default;
            }

            return EncodeToCBORObject(baseCori, dictionary).EncodeToBytes();
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();


            BuildString(builder, "");


            return builder.ToString();
        }

    }
}
