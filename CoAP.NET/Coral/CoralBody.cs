using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PeterO.Cbor;

namespace Com.AugustCellars.CoAP.Coral
{
    public class CoralBody : IEnumerable
    {
        private readonly List<CoralItem> _items = new List<CoralItem>();

        public CoralBody()
        {
        }


        public CoralBody(CBORObject node, CoralDictionary dictionary)
        {
            if (node.Type != CBORType.Array) {
                throw new ArgumentException("Invalid node type");
            }

            foreach (CBORObject child in node.Values) {
                if (node.Type != CBORType.Array)
                {
                    throw new ArgumentException("Invalid node type");
                }

                switch (node[0].AsInt32()) {
                    case 1:
                        _items.Add(new CoralLink(node, dictionary));
                        break;

                    default:
                        throw new ArgumentException("Unrecognized CoRAL node type");
                }
            }
        }

        public int Length => _items.Count;

        public CoralBody Add(CoralItem item)
        {
            _items.Add(item);
            return this;
        }

        static CoralBody DecodeFromBytes(byte[] encoded, CoralDictionary dictionary = null)
        {
            CBORObject obj = CBORObject.DecodeFromBytes(encoded);
            if (dictionary == null) {
                dictionary = CoralDictionary.Default;
            }
            return new CoralBody(obj, dictionary);
        }

        public byte[] EncodeToBytes(CoralDictionary dictionary = null)
        {
            return EncodeToCBORObject(dictionary).EncodeToBytes();
        }

        public CBORObject EncodeToCBORObject(CoralDictionary dictionary = null)
        {
            CBORObject root = CBORObject.NewArray();
            if (dictionary == null) {
                dictionary = CoralDictionary.Default;
            }

            foreach (CoralItem item in _items) {
                root.Add(item.EncodeToCBORObject(dictionary));
            }

            return root;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }
    }
}
