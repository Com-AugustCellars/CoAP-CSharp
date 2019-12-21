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

        public CoralDocument(CBORObject node,  Ciri baseCiri, CoralDictionary dictionary) : base(node, baseCiri, dictionary)
        {
        }

        public static CoralDocument DecodeFromBytes(byte[] encoded, Ciri baseCiri, CoralDictionary dictionary = null)
        {
            CBORObject obj = CBORObject.DecodeFromBytes(encoded);
            return DecodeFromCbor(obj, baseCiri, dictionary);
        }

        public static CoralDocument DecodeFromCbor(CBORObject node, Ciri baseCiri, CoralDictionary dictionary = null)
        {
            if (dictionary == null)
            {
                dictionary = CoralDictionary.Default;
            }
            return new CoralDocument(node, baseCiri, dictionary);
        }

        public byte[] EncodeToBytes(Ciri baseCiri, CoralDictionary dictionary = null)
        {
            if (dictionary == null) {
                dictionary = CoralDictionary.Default;
            }

            return EncodeToCBORObject(baseCiri, dictionary).EncodeToBytes();
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();


            BuildString(builder);


            return builder.ToString();
        }

    }
}
