using System;
using System.Collections;
using System.Collections.Generic;
using Com.AugustCellars.CoAP.Util;
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

        private readonly Dictionary<int, object> _dictionary = new Dictionary<int, object>();

        public CoralDictionary Add(int key, string value)
        {
            if (key < 0) {
                throw new ArgumentException("Key must be non-negative value", nameof(key));
            }

            _dictionary.Add(key, value);
            return this;
        }

        public CoralDictionary Add(int key, CBORObject value)
        {
            if (key < 0) {
                throw new ArgumentException("Key must be a non-negative value", nameof(key));
            }

            if (!CoralItem.IsLiteral(value)) {
                throw new ArgumentException("Value must be a literal value");
            }

            if (value.Type == CBORType.TextString) {
                _dictionary.Add(key, value.AsString());
            }
            else {
                _dictionary.Add(key, value);
            }

            return this;
        }

        public CoralDictionary Add(int key, Cori value)
        {
            if (key < 0) {
                throw new ArgumentException("Key must be a non-negative value", nameof(key));
            }

            _dictionary.Add(key, value);
            return this;
        }

        public IEnumerator GetEnumerator()
        {
            return ((IEnumerable) _dictionary).GetEnumerator();
        }

        public CBORObject Lookup(CBORObject value, bool isIntLegal)
        {
            if (value.Type == CBORType.TextString) {
                return Lookup(value.AsString(), isIntLegal);
            }

            foreach (KeyValuePair<int, object> o in _dictionary) {
                if (value.Equals(o.Value)) {
                    if (isIntLegal) {
                        return CBORObject.FromObjectAndTag(o.Key, DictionaryTag);
                    }

                    return CBORObject.FromObject(o.Key);
                }
            }

            return value;
        }

        public CBORObject Lookup(string value, bool isIntLegal)
        {
            foreach (KeyValuePair<int, object> o in _dictionary) {
                if (value.Equals(o.Value)) {
                    if (isIntLegal) {
                        return CBORObject.FromObjectAndTag(o.Key, DictionaryTag);
                    }

                    return CBORObject.FromObject(o.Key);
                }
            }

            return CBORObject.FromObject(value);
        }

        public CBORObject Lookup(Cori value, bool isIntLegal)
        {
            foreach (KeyValuePair<int, object> o in _dictionary) {
                if (value.Equals(o.Value)) {
                    if (isIntLegal) {
                        return CBORObject.FromObjectAndTag(o.Key, DictionaryTag);
                    }
                    return CBORObject.FromObject(o.Key);
                }
            }

            return value.Data;
        }

        /// <summary>
        /// Reverse the dictionary encoding of a value.
        /// If it cannot be decoded then return null, in this case the value must be an integer.
        /// </summary>
        /// <param name="value">Value to decode</param>
        /// <param name="isIntLegal">Can an integer value be used here?</param>
        /// <returns>Original value if it can be decoded.</returns>
        public object Reverse(CBORObject value, bool isIntLegal)
        {
            if (value.IsTagged) {
                if (value.HasOneTag(DictionaryTag)) {
                    return Reverse(value.UntagOne(), false);
                }

                if (CoralItem.IsLiteral(value)) {
                    return value;
                }
                throw new ArgumentException("Value is not a literal value", nameof(value));
            }

            if (value.Type == CBORType.Integer && (isIntLegal || value.AsInt32() < 0)) {
                return value;
            }

            if (value.Type != CBORType.Integer) {
                if (value.Type == CBORType.Array || CoralItem.IsLiteral(value)) {
                    return value;
                }
                throw new ArgumentException($"The value '{value}' is not a literal value", nameof(value));
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
