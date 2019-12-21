using System;
using System.Collections;
using System.Collections.Generic;
using PeterO.Cbor;

namespace Com.AugustCellars.CoAP.Coral
{
    public class CoralDictionary : IEnumerable
    {
        public const int DictionaryTag = 99999;

        public static CoralDictionary Default { get; } = new CoralDictionary() {
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

        public CoralDictionary Add(int key, string value)
        {
            if (key < 0) {
                throw new ArgumentException("Key must be non-negative value", nameof(key));
            }

            _dictionary.Add(key, value);
            return this;
        }

        public IEnumerator GetEnumerator()
        {
            return ((IEnumerable) _dictionary).GetEnumerator();
        }

        public CBORObject Lookup(CBORObject value)
        {
            if (value.Type == CBORType.TextString) {
                return Lookup(value.AsString());
            }

            if (value.Type == CBORType.Integer && !value.IsTagged && value.AsInt32() >= 0) {
                return CBORObject.FromObjectAndTag(value, DictionaryTag);
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


        /// <summary>
        /// Reverse the dictionary encoding of a value.
        /// If it cannot be decoded then return null, in this case the value must be an integer.
        /// </summary>
        /// <param name="value">Value to decode</param>
        /// <returns>Original value if it can be decoded.</returns>
        public CBORObject Reverse(CBORObject value)
        {
            if (value.IsTagged) {
                if (value.HasOneTag(DictionaryTag)) {
                    return value.UntagOne();
                }
                if (value.HasTag(DictionaryTag)) {
                    throw new ArgumentException("CoRAL dictionary tag 6.TBD6 unexpectedly located");
                }

                return value;
            }

            if (value.Type != CBORType.Integer || value.AsInt32() < 0) {
                return value;
            }

            if (!_dictionary.ContainsKey(value.AsInt32())) {
                return null;
            }

            CBORObject result = CBORObject.FromObject(_dictionary[value.AsInt32()]);

            if (result.Type == CBORType.Integer) {
                return value;
            }

            return result;
        }
    }
}
