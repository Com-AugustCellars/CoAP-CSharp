using PeterO.Cbor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Com.AugustCellars.CoAP.Util;

namespace Com.AugustCellars.CoAP.Coral
{
#if false
    public class CoralBase : CoralItem
    {
        public  Uri Uri { get; }
        public CoralBase(Uri baseUri)
        {
            Uri = baseUri;
        }

        public override CBORObject EncodeToCBORObject(Cori unused, CoralDictionary dictionary)
        {
            CBORObject node = CBORObject.NewArray();

            node.Add(1);
            node.Add(Cori.ToCbor(Uri));

            return node;
        }

        /// <inheritdoc />
        public override void BuildString(StringBuilder builder, string pad, Cori contextCori, CoralUsing usingDictionary)
        {
            builder.Append(pad);
            builder.Append(Uri);
            builder.Append("\n");
        }
    }
#endif
}
