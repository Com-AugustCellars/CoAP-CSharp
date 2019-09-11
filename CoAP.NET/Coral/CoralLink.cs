using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.Asn1.X509;
using PeterO.Cbor;

namespace Com.AugustCellars.CoAP.Coral
{
    public class CoralLink : CoralItem
    {
        /// <summary>
        /// Link relation Type - a text string conforming to IRI syntax
        /// </summary>
        public string RelationType { get; }
        /// <summary>
        /// Target of the link
        /// </summary>
        public CBORObject Target { get; set; }
        /// <summary>
        /// Child body
        /// </summary>
        public CoralBody Body { get; }

        public CoralLink(string relation, CBORObject target)
        {
            RelationType = relation;
            Target = target;
        }

        public CoralLink(string relation, CBORObject target, CoralBody body)
        {
            RelationType = relation;
            Target = target;
            Body = body;
        }

        public CoralLink(CBORObject node, CoralDictionary dictionary)
        {
            if (node[0].AsInt32() != 2) {
                throw new ArgumentException("Not an encoded CoRAL link");
            }

            RelationType = dictionary.Reverse(node[1]).AsString();
            Target = dictionary.Reverse(node[2]);
            if (node.Count == 4) {
                Body = new CoralBody(node[3], dictionary);
            }
        }

        public override CBORObject EncodeToCBORObject(CoralDictionary dictionary)
        {
            CBORObject node = CBORObject.NewArray();

            node.Add(2);
            node.Add(dictionary.Lookup(RelationType));
            node.Add(dictionary.Lookup(Target));
            if (Body != null) {
                node.Add(Body.EncodeToCBORObject(dictionary));
            }

            return node;
        }
    }
}
