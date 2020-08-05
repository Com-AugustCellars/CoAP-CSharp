using System;
using System.Collections.Generic;

namespace Com.AugustCellars.CoAP
{
    public class CacheKey
    {
        private readonly long _hashCode;
        private readonly Option[] _options;


        public CacheKey(IEnumerable<Option> options)
        {
            int h = 0;
            List<Option> list = new List<Option>();

            foreach (Option o in options) {
                if (Option.IsNotCacheKey(o.Type)) {
                    continue;
                }

                h = (h * 59) + o.GetHashCode();
                list.Add(o);
            }

            _options = list.ToArray();
            _hashCode = h;
        }

        public CacheKey(IEnumerable<Option> options, OptionType[] ignoreOptionTypes)
        {
            int h = 0;
            List<Option> list = new List<Option>();

            foreach (Option o in options) {
                if (Option.IsNotCacheKey(o.Type) || (Array.IndexOf(ignoreOptionTypes, o.Type) != -1)) {
                    continue;
                }
                h = (h * 59) + o.GetHashCode();
                list.Add(o);
            }

            _options = list.ToArray();
            _hashCode = h;
    }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            CacheKey other = obj as CacheKey;
            if (other == null || other._options.Length != _options.Length) {
                return false;
            }

            for (int i=0; i<_options.Length; i++) {
                if (!_options[i].Equals(other._options[i])) {
                    return false;
                }
            }
            return true;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return (int) (_hashCode & 0xffffffff);
        }
    }
}
