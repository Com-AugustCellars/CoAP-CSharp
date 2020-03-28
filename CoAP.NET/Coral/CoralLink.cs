using System;
using System.Collections.Generic;
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
        public string RelationTypeText => RelationType?.ToString(); 
        public Cori RelationType { get; }
        public int? RelationTypeInt { get; }

        /// <summary>
        /// Target of the link as a URI
        /// </summary>
        public Cori Target { get; }
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
        /// <param name="relation">Cori version of the relation - IRI</param>
        /// <param name="target">Value of the target - Not a URI!!!</param>
        /// <param name="body">attributes about the target</param>
        public CoralLink(Cori relation, CBORObject target, CoralBody body = null)
        {
            if (!IsLiteral(target)) {
                throw new ArgumentException("Value must be a literal value", nameof(target));
            }

            if (!relation.IsAbsolute()) {
                throw new ArgumentException("Relation must be an absolute IRI", nameof(relation));
            }

            RelationType = relation;
            Value = target;
            Body = body;
        }

        /// <summary>
        /// Create a CoRAL link from the parameters
        /// </summary>
        /// <param name="relation">string containing the relation - IRI</param>
        /// <param name="target">Value of the target - Not a URI!!!</param>
        /// <param name="body">attributes about the target</param>
        public CoralLink(string relation, CBORObject target, CoralBody body = null) : this(new Cori(relation), target, body) { }

        /// <summary>
        /// Create a CoRAL link from the parameters
        /// </summary>
        /// <param name="relation">Cori value containing the relation - IRI</param>
        /// <param name="uriTarget">Absolute or relative CIRI for the target</param>
        /// <param name="body">attributes about the target</param>
        public CoralLink(Cori relation, Cori uriTarget, CoralBody body = null)
        {
            if (!relation.IsAbsolute()) {
                throw new ArgumentException("Relation must be an absolute IRI", nameof(relation));
            }
 
            RelationType = relation;
            Target = uriTarget;
            Body = body;
        }

        /// <summary>
        /// Create a CoRAL link from the parameters
        /// </summary>
        /// <param name="relation">string containing the relation - IRI</param>
        /// <param name="uriTarget">Absolute or relative CIRI for the target</param>
        /// <param name="body">attributes about the target</param>
        public CoralLink(string relation, Cori uriTarget, CoralBody body = null) : this(new Cori(relation), uriTarget, body) { }

        public CoralLink(string relation, string target, CoralBody body = null) : this(relation, CBORObject.FromObject(target), body)
        {
        }

        /// <summary>
        /// Decode a CBOR encoded CoRAL link into the object for working with
        /// </summary>
        /// <param name="node">CBOR object to be decoded</param>
        /// <param name="baseCori">a URI to make things relative to</param>
        /// <param name="dictionary">dictionary to use for decoding</param>
        public CoralLink(CBORObject node, Cori baseCori, CoralDictionary dictionary)
        {
            if (node[0].AsInt32() != 2) {
                throw new ArgumentException("Not an encoded CoRAL link");
            }

            if (dictionary == null) {
                throw new ArgumentNullException(nameof(dictionary));
            }

            CBORObject o = (CBORObject) dictionary.Reverse(node[1], false);
            if (o == null) { 
                RelationTypeInt = node[1].AsInt32();
            }
            else if (o.Type == CBORType.Array) {
                RelationType = new Cori(o);
                if (node[1].Type == CBORType.Integer) {
                    RelationTypeInt = node[1].AsInt32();
                }
            }
            else {
                throw  new ArgumentException("Invalid relation in CoRAL link");
            }

            CBORObject value = (CBORObject) dictionary.Reverse(node[2], true);

            if (value == null) {
                if (!node[2].HasOneTag(CoralDictionary.DictionaryTag)) {
                    throw new ArgumentException("Invalid tagging on value");
                }
                TargetInt = node[2].Untag().AsInt32();
            }
            else if (value.Type == CBORType.Array) {
                Target = new Cori(value).ResolveTo(baseCori);
                if (node[2].Type == CBORType.Integer) {
                    TargetInt = node[2].Untag().AsInt32();
                }

                baseCori = Target;
            }
            else {
                if (node[2].IsTagged && node[2].Type == CBORType.Integer) {
                    TargetInt = node[2].Untag().AsInt32();
                }
                Value = value;
            }

            if (node.Count == 4) {
                Body = new CoralBody(node[3], baseCori, dictionary);
            }
        }

        // link = [2, relation-type, link-target, ?body]
        // relation-type = text
        // link-target = CoRI / literal
        // CoRI = <Defined in Section X of RFC XXXX>
        // literal = bool / int / float / time / bytes / text / null

        public override CBORObject EncodeToCBORObject(Cori coriBase, CoralDictionary dictionary)
        {
            CBORObject node = CBORObject.NewArray();

            node.Add(2);
            if (RelationType != null) {
                node.Add(dictionary.Lookup(RelationType, false));
            }
            else {
                node.Add(RelationTypeInt);
            }


            if (Target != null) {
                CBORObject o = dictionary.Lookup(Target, true);
                if (o.Type == CBORType.Integer) {
                    node.Add(o);
                }
                else {
                    if (coriBase != null) {
                        Cori relative = Target.MakeRelative(coriBase);
                        node.Add(relative.Data);
                    }
                    else {
                        node.Add(Target.Data);
                    }
                }
            }
            else if (Value != null) {
                node.Add(dictionary.Lookup(Value, true));
            }
            else if (TargetInt != null) {
                node.Add(CBORObject.FromObjectAndTag(TargetInt, CoralDictionary.DictionaryTag));
            }

            if (Body != null) {
                node.Add(Body.EncodeToCBORObject(coriBase, dictionary));
            }

            return node;
        }

        public override void BuildString(StringBuilder builder, string pad, Cori contextCori, CoralUsing usingDictionary)
        {
            builder.Append(pad);
            if (usingDictionary != null) {
                string t = usingDictionary.Abbreviate(RelationType.ToString()); 
                builder.Append(t);
            }
            else {
                builder.AppendFormat($"<{RelationType}>");
            }

            if (Target != null) {
                if (contextCori == null) {
                    builder.Append($" <{Target}>");
                }
                else {
                    builder.Append($" <{Target.MakeRelative(contextCori)}>");
                }
            }
            else if (Value != null) {
                builder.Append($" {Value}");
            }
            else if (TargetInt != null) {
                builder.Append($" ## {TargetInt} ##");
            }

            if (Body != null) {
                builder.Append(" [\n");
                Body.BuildString(builder, pad + "  ", contextCori, usingDictionary);
                builder.Append(pad);
                builder.Append("]");
            }

            builder.Append("\n");
        }
    }
}
