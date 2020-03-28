using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Com.AugustCellars.CoAP.Util;
using PeterO.Cbor;

namespace Com.AugustCellars.CoAP.Coral
{
    public class CoralBody : IEnumerable
    {
        private readonly List<CoralItem> _items = new List<CoralItem>();

        /// <summary>
        /// Create a new coral body from scratch.
        /// </summary>
        public CoralBody()
        {
        }

        /// <summary>
        /// Decode a CBOR object into a coral body.
        /// </summary>
        /// <param name="node">CBOR node to be decoded</param>
        /// <param name="contextCori">context to decode URIs</param>
        /// <param name="dictionary">Dictionary used for decompression</param>
        public CoralBody(CBORObject node, Cori contextCori, CoralDictionary dictionary)
        {
            Cori baseCori = contextCori;

            if (node.Type != CBORType.Array) {
                throw new ArgumentException("Invalid node type");
            }

            if (contextCori == null || !contextCori.IsAbsolute()) {
                throw new ArgumentException("Must be resolved to an absolute URI", nameof(contextCori));
            }

            foreach (CBORObject child in node.Values) {
                if (child.Type != CBORType.Array) {
                    throw new ArgumentException("Invalid node type");
                }

                switch (child[0].AsInt32()) {
                    case 1:
                        CoralBaseDirective d1 = new CoralBaseDirective(child, contextCori);
                        _items.Add(d1);
                        baseCori = d1.BaseValue;
                        break;

                    case 2:
                        _items.Add(new CoralLink(child, baseCori, dictionary));
                        break;

                    case 3:
                        _items.Add(new CoralForm(child, baseCori, dictionary));
                        break;

                    default:
                        throw new ArgumentException("Unrecognized CoRAL node type");
                }
            }
        }

        public int Length => _items.Count;
        public CoralItem this[int i] => _items[i];

        public CoralBody Add(CoralItem item)
        {
            _items.Add(item);
            return this;
        }

        public CBORObject EncodeToCBORObject(Cori coriBase, CoralDictionary dictionary = null)
        {
            CBORObject root = CBORObject.NewArray();
            if (dictionary == null) {
                dictionary = CoralDictionary.Default;
            }

            foreach (CoralItem item in _items) {
                root.Add(item.EncodeToCBORObject(coriBase, dictionary));
                if (item is CoralBaseDirective d) {
                    coriBase = d.BaseValue.ResolveTo(coriBase);
                }
            }

            return root;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        public void BuildString(StringBuilder builder, string pad, Cori contextCori, CoralUsing usingDictionary)
        {
            Cori baseCori = contextCori;
            foreach (CoralItem item in _items) {
                if (item is CoralBaseDirective) {
                    baseCori = ((CoralBaseDirective) item).BaseValue;
                    item.BuildString(builder, pad, contextCori, usingDictionary);
                }
                else {
                    item.BuildString(builder, pad, baseCori, usingDictionary);
                }
            }
        }
    }
}
