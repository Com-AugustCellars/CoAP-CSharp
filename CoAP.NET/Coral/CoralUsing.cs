using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Com.AugustCellars.CoAP.Coral
{
    public class CoralUsing : IEnumerable
    {
        public static CoralUsing Default { get; } = new CoralUsing() {
            {"reef", "coap://jimsch.example.com/coreapp/reef#"}
        };

        private Dictionary<string, string> usingDictionary = new Dictionary<string, string>();

        public CoralUsing()
        {
        }

        public void Add(string key, string value)
        {
            if (usingDictionary.ContainsKey(key) || usingDictionary.ContainsValue(value)) {
                throw new ArgumentException();
            }
            usingDictionary.Add(key, value);
        }

        public IEnumerator GetEnumerator()
        {
            return ((IEnumerable) usingDictionary).GetEnumerator();
        }

        public string Abbreviate(string value)
        {
            int i = value.Length - 1;
            while (i >= 0 && (char.IsLetterOrDigit(value[i]) || value[i] == '-')) {
                i -= 1;
            }

            if (i == 0) {
                return $"<{value}>";
            }

            string l = value.Substring(i + 1);
            string k = value.Substring(0, i+1);

            foreach (KeyValuePair<string, string> x in usingDictionary) {
                if (x.Value == k) {
                    if (string.IsNullOrEmpty(x.Key)) {
                        return l;
                    }
                    else {
                        return x.Key + ":" + l;
                    }
                }
            }

            return $"<{value}>";
        }
    }
}
