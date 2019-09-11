using PeterO.Cbor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Com.AugustCellars.CoAP.Util;

namespace Com.AugustCellars.CoAP.Coral
{
    public class CoralBase : CoralItem
    {
        public  Uri Uri { get; }
        public CoralBase(Uri baseUri)
        {
            Uri = baseUri;
        }

        public override CBORObject EncodeToCBORObject(CoralDictionary dictionary)
        {
            CBORObject node = CBORObject.NewArray();

            node.Add(1);
            node.Add(Ciri.ToCbor(Uri));

            return node;
        }

    }
}
