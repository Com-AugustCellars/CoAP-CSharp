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


        public CoralBody(CBORObject node, Ciri baseCiri, CoralDictionary dictionary)
        {
            if (node.Type != CBORType.Array) {
                throw new ArgumentException("Invalid node type");
            }

            if (baseCiri == null || !baseCiri.IsAbsolute()) {
                throw new ArgumentException("Must be resolved to an absolute URI", nameof(baseCiri));
            }

            foreach (CBORObject child in node.Values) {
                if (child.Type != CBORType.Array) {
                    throw new ArgumentException("Invalid node type");
                }

                switch (child[0].AsInt32()) {
                    case 1:
                        CoralBaseDirective d1 = new CoralBaseDirective(child, baseCiri);
                        _items.Add(d1);
                        baseCiri = d1.BaseValue;
                        break;

                    case 2:
                        _items.Add(new CoralLink(child, baseCiri, dictionary));
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

        public CBORObject EncodeToCBORObject(Ciri ciriBase, CoralDictionary dictionary = null)
        {
            CBORObject root = CBORObject.NewArray();
            if (dictionary == null) {
                dictionary = CoralDictionary.Default;
            }

            foreach (CoralItem item in _items) {
                root.Add(item.EncodeToCBORObject(ciriBase, dictionary));
                if (item is CoralBaseDirective d) {
                    ciriBase = d.BaseValue.ResolveTo(ciriBase);
                }
            }

            return root;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        public void BuildString(StringBuilder builder)
        {
            foreach (CoralItem item in _items) {
                item.BuildString(builder);
            }
        }
    }
}
