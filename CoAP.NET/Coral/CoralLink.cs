using System;
using System.Text;
using Com.AugustCellars.CoAP.Util;
using PeterO.Cbor;

namespace Com.AugustCellars.CoAP.Coral
{
    public class CoralLink : CoralItem
    {
        // link context - base URI for the link
        // link relation type - IRI text string or dictionary # - may not un-resolve on read
        // link target - relative URI or literal value of the relation
        // link attributes - array of links, forms and so forth about this relation

        /// <summary>
        /// Link relation Type - a text string conforming to IRI syntax
        /// </summary>
        public string RelationType { get; }
        public int? RelationTypeInt { get; }

        /// <summary>
        /// Target of the link as a URI
        /// </summary>
        public Ciri Target { get; }
        public int? TargetInt { get; }

        /// <summary>
        /// Target of the link as a URI resolved against last given context
        /// </summary>
        public string ResolvedTarget { get;  }

        /// <summary>
        /// Target of the link as a single value
        /// </summary>
        public CBORObject Value { get;  }

        /// <summary>
        /// Target of the link as a collection of values
        /// </summary>
        public CoralBody Body { get; }

        /// <summary>
        /// Create a CoRAL link from the parameters
        /// </summary>
        /// <param name="relation">string containing the relation - IRI</param>
        /// <param name="target">Value of the target - Not a URI!!!</param>
        /// <param name="body">attributes about the target</param>
        public CoralLink(string relation, CBORObject target, CoralBody body = null)
        {
            if (!IsLiteral(target)) {
                throw new ArgumentException("Value must be a literal value", nameof(target));
            }

            RelationType = relation;
            Value = target;
            Body = body;
        }

        /// <summary>
        /// Create a CoRAL link from the parameters
        /// </summary>
        /// <param name="relation">string containing the relation - IRI</param>
        /// <param name="uriTarget">Absolute or relative CIRI for the target</param>
        /// <param name="body">attributes about the target</param>
        public CoralLink(string relation, Ciri uriTarget, CoralBody body = null)
        {
            if (!uriTarget.IsWellFormed()) throw new ArgumentException("must be well formed", nameof(uriTarget));
 
            RelationType = relation;
            Target = uriTarget;
            Body = body;
        }

        public CoralLink(string relation, string target, CoralBody body = null) : this(relation, CBORObject.FromObject(target), body)
        {
        }

        /// <summary>
        /// Decode a CBOR encoded CoRAL link into the object for working with
        /// </summary>
        /// <param name="node">CBOR object to be decoded</param>
        /// <param name="dictionary">dictionary to use for decoding</param>
        public CoralLink(CBORObject node, Ciri baseCiri, CoralDictionary dictionary)
        {
            if (node[0].AsInt32() != 2) {
                throw new ArgumentException("Not an encoded CoRAL link");
            }

            if (dictionary == null) {
                throw new ArgumentNullException(nameof(dictionary));
            }

            CBORObject o = dictionary.Reverse(node[1]);
            if (o == null) { 
                RelationTypeInt = node[1].AsInt32();
            }
            else if (o.Type == CBORType.TextString) {
                RelationType = o.AsString();
                if (!node[1].IsTagged && node[1].Type == CBORType.Integer) {
                    RelationTypeInt = node[1].AsInt32();
                }
            }
            else {
                throw  new ArgumentException("Invalid relation in CoRAL link");
            }

            CBORObject value = dictionary.Reverse(node[2]);

            if (value == null) {
                TargetInt = node[2].AsInt32();
            }
            else if (value.Type == CBORType.Array) {
                Target = new Ciri(value).ResolveTo(baseCiri);
                if (node[2].Type == CBORType.Integer) {
                    TargetInt = node[2].AsInt32();
                }

                baseCiri = Target;
            }
            else {
                if (!node[2].IsTagged && node[2].Type == CBORType.Integer) {
                    TargetInt = node[2].AsInt32();
                }
                Value = value;
            }

            if (node.Count == 4) {
                Body = new CoralBody(node[3], baseCiri, dictionary);
            }
        }

        public override CBORObject EncodeToCBORObject(Ciri ciriBase, CoralDictionary dictionary)
        {
            CBORObject node = CBORObject.NewArray();

            node.Add(2);
            node.Add(dictionary.Lookup(RelationType));
            if (TargetInt != null) {
                node.Add(TargetInt);
            }
            else if (Target != null) {
                CBORObject o = dictionary.Lookup(Target.ToString());
                if (o.Type == CBORType.Integer) {
                    node.Add(o);
                }
                else {
                    if (ciriBase != null) {
                        Ciri relative = Target.MakeRelative(ciriBase);
                        node.Add(relative.Data);
                    }
                    else {
                        node.Add(Target.Data);
                    }
                }
            }
            else if (Value != null) {
                node.Add(dictionary.Lookup(Value));
            }

            if (Body != null) {
                node.Add(Body.EncodeToCBORObject(ciriBase, dictionary));
            }

            return node;
        }

        public override void BuildString(StringBuilder builder)
        {
            builder.Append(RelationType);
            if (Target != null) {
                builder.Append($" <{Target}>");
            }
            else if (Value != null) {
                builder.Append($" {Value}");
            }
            else if (TargetInt != null) {
                builder.Append($" ## {TargetInt} ##");
            }

            if (Body != null) {
                builder.Append(" [\n");
                Body.BuildString(builder);
                builder.Append("]");
            }

            builder.Append("\n");
        }
    }
}
