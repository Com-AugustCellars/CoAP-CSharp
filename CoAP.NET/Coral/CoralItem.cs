using PeterO.Cbor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Com.AugustCellars.CoAP.Coral
{
    public abstract class CoralItem
    {
        public abstract CBORObject EncodeToCBORObject(CoralDictionary dictionary);
    }

}
