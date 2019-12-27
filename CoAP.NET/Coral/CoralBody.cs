using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using Com.AugustCellars.CoAP.Util;
using PeterO.Cbor;

namespace Com.AugustCellars.CoAP.Coral
{
    public class CoralBody : IEnumerable
    {
        private readonly List<CoralItem> _items = new List<CoralItem>();

        public CoralBody()
        {
        }


        public CoralBody(CBORObject node, Cori baseCori, CoralDictionary dictionary)
        {
            if (node.Type != CBORType.Array) {
                throw new ArgumentException("Invalid node type");
            }

            if (baseCori == null || !baseCori.IsAbsolute()) {
                throw new ArgumentException("Must be resolved to an absolute URI", nameof(baseCori));
            }

            foreach (CBORObject child in node.Values) {
                if (child.Type != CBORType.Array) {
                    throw new ArgumentException("Invalid node type");
                }

                switch (child[0].AsInt32()) {
                    case 1:
                        CoralBaseDirective d1 = new CoralBaseDirective(child, baseCori);
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

        public void BuildString(StringBuilder builder, string pad)
        {
            foreach (CoralItem item in _items) {
                item.BuildString(builder, pad);
            }
        }
    }
}
