using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.Asn1.Crmf;
using PeterO.Cbor;

namespace Com.AugustCellars.CoAP.Coral
{
    public class CoralDictionary : System.Collections.IEnumerable
    {
        public const int DictionaryTag = 99999;

        public static CoralDictionary Default = new CoralDictionary() {
            {0, "http://www.w3.org/1999/02/22-rdf-syntax-ns#type"},
            {1, "http://www.iana.org/assignments/relation/item>"},
            {2, "http://www.iana.org/assignments/relation/collection"},
            {3, "http://coreapps.org/collections#create"},
            {4, "http://coreapps.org/base#update"},
            {5, "http://coreapps.org/collections#delete"},
            {6, "http://coreapps.org/base#search"},
            {7, "http://coreapps.org/coap#accept"},
            {8, "http://coreapps.org/coap#type"},
            {9, "http://coreapps.org/base#lang"},
            {10, "http://coreapps.org/coap#method"}
        };

        private readonly Dictionary<int, string> _dictionary = new Dictionary<int, string>();

        public CoralDictionary()
        {
        }

        public CoralDictionary Add(int key, string value)
        {
            _dictionary.Add(key, value);
            return this;
        }

        public IEnumerator GetEnumerator()
        {
            return ((IEnumerable) _dictionary).GetEnumerator();
        }

        public CBORObject Lookup(CBORObject value)
        {
            foreach (KeyValuePair<int, string> o in _dictionary) {
                if (value.Equals(CBORObject.FromObject(o.Value))) {
                    CBORObject newValue = CBORObject.FromObject(o.Key);
                    if (value.Type == CBORType.Integer) {
                        newValue = CBORObject.FromObjectAndTag(newValue, DictionaryTag);
                    }

                    return newValue;
                }
            }

            return value;
        }

        public CBORObject Lookup(string value)
        {
            foreach (KeyValuePair<int, string> o in _dictionary) {
                if (value.Equals(o.Value)) {
                    return CBORObject.FromObject(o.Key);
                }
            }

            return CBORObject.FromObject(value);
        }

        public CBORObject Reverse(CBORObject value)
        {
            if (value.Type != CBORType.Integer) {
                return value;
            }

            if (value.HasTag(DictionaryTag) && value.MostOuterTag.ToInt32Unchecked() == DictionaryTag) {
                return value.UntagOne();
            }

            if (!_dictionary.ContainsKey(value.AsInt32())) {
                return value;
            }

            CBORObject result = CBORObject.FromObject(_dictionary[value.AsInt32()]);

            if (result.Type == CBORType.Integer) {
                return value;
            }

            return result;
        }
    }
}
