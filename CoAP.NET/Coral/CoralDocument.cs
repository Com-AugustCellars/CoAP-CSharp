using System.Collections.Generic;
using System.Text;
using Com.AugustCellars.CoAP.Util;
using PeterO.Cbor;

namespace Com.AugustCellars.CoAP.Coral
{
    public class CoralDocument : CoralBody
    {
        /// <summary>
        /// Create a new CoRAL document from scratch
        /// </summary>
        public CoralDocument()
        {

        }

        /// <summary>
        /// Create a new CoRAL document based on a parsed CBOR object
        /// </summary>
        /// <param name="node">Encoded document to use</param>
        /// <param name="contextCori">Context to evaluate the document relative to</param>
        /// <param name="dictionary">Dictionary to be used to reverse compression</param>
        public CoralDocument(CBORObject node,  Cori contextCori, CoralDictionary dictionary) : base(node, contextCori, dictionary)
        {
        }

        /// <summary>
        /// Decode a CBOR object and create a CoRAL document from it
        /// </summary>
        /// <param name="encoded">CBOR encoded CoRAL document</param>
        /// <param name="contextCori">Context to evaluate the document relative to</param>
        /// <param name="dictionary">Dictionary to be used to reverse compression</param>
        /// <returns></returns>
        public static CoralDocument DecodeFromBytes(byte[] encoded, Cori contextCori, CoralDictionary dictionary = null)
        {
            CBORObject obj = CBORObject.DecodeFromBytes(encoded);
            return DecodeFromCbor(obj, contextCori, dictionary);
        }

        /// <summary>
        /// Create a new CoRAL document based on a parsed CBOR object
        /// </summary>
        /// <param name="node">Encoded document to use</param>
        /// <param name="contextCori">Context to evaluate the document relative to</param>
        /// <param name="dictionary">Dictionary to be used to reverse compression</param>
        /// <returns></returns>
        public static CoralDocument DecodeFromCbor(CBORObject node, Cori contextCori, CoralDictionary dictionary = null)
        {
            if (dictionary == null) {
                dictionary = CoralDictionary.Default;
            }
            return new CoralDocument(node, contextCori, dictionary);
        }

        /// <summary>
        /// Encode a CoRAL document to a binary value
        /// </summary>
        /// <param name="contextCori">Context to evaluate the document relative to</param>
        /// <param name="dictionary">Dictionary to be used for compression</param>
        /// <returns></returns>
        public byte[] EncodeToBytes(Cori contextCori, CoralDictionary dictionary = null)
        {
            if (dictionary == null) {
                dictionary = CoralDictionary.Default;
            }

            return EncodeToCBORObject(contextCori, dictionary).EncodeToBytes();
        }

        public string EncodeToString(Cori contextCori, CoralUsing usingDictionary)
        {
            StringBuilder builder = new StringBuilder();

            BuildString(builder, "", contextCori, usingDictionary);

            return builder.ToString();
        }


        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            BuildString(builder, "", null, null);
            return builder.ToString();
        }

    }
}
