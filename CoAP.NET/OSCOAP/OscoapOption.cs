using System;
using System.Collections.Generic;
using System.Text;

using Com.AugustCellars.CoAP;

namespace Com.AugustCellars.CoAP.OSCOAP
{
#if INCLUDE_OSCOAP
    public class OscoapOption : Option
    {
        public OscoapOption() : base(OptionType.Oscoap)
        {

        }

        public OscoapOption(OptionType o) : base(o)
        {
        }

        public void Set(byte[] value) { RawValue = value; }

        public override string ToString()
        {
            if (this.RawValue == null) return "** InPayload";
            return String.Format("** Length={0}", this.RawValue.Length);
        }
    }

 #endif
}
