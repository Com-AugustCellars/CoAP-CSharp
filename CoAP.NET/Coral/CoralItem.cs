using PeterO.Cbor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Com.AugustCellars.CoAP.Util;

namespace Com.AugustCellars.CoAP.Coral
{
    public abstract class CoralItem
    {
        public abstract CBORObject EncodeToCBORObject(Ciri baseCiri, CoralDictionary dictionary);
        public abstract void BuildString(StringBuilder builder);

        public static bool IsLiteral(CBORObject value)
        {
            if (value.IsTagged)
            {
                return value.HasOneTag(1) && value.Type == CBORType.Integer;
            }

            switch (value.Type)
            {
            case CBORType.Integer:
            case CBORType.Boolean:
            case CBORType.FloatingPoint:
            case CBORType.ByteString:
            case CBORType.TextString:
                return true;

            case CBORType.SimpleValue:
                return value.IsNull;

            default:
                return false;
            }
        }
    }

}
