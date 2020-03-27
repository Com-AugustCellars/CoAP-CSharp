using System;
using System.Collections.Generic;
using System.Text;
using Com.AugustCellars.CoAP.Util;
using PeterO.Cbor;

namespace Com.AugustCellars.CoAP.Coral
{
    public class CoralBaseDirective : CoralItem
    {
        public Cori BaseValue { get; }

        public CoralBaseDirective(Cori value)
        {
            if (!value.IsWellFormed()) {
                throw new ArgumentException("URI is not well formed", nameof(value));
            }

            if (!value.IsAbsolute()) {
                throw new ArgumentException("URI is not absolute", nameof(value));
            }
            BaseValue = value;
        }

        public CoralBaseDirective(CBORObject value, Cori baseCori)
        {
            if (value.Type != CBORType.Array || value.Count != 2) {
                throw new ArgumentException();
            }

            if (value[0].Type != CBORType.Integer || value[0].IsTagged || value[0].AsInt32() != 1) {
                throw new ArgumentException();
            }

            if (value[1].Type != CBORType.Array) {
                throw new ArgumentException();
            }

            Cori temp = new Cori(value[1]);
            if (!temp.IsWellFormed()) {
                throw new ArgumentException("base value is not well formed", nameof(value));
            }

            temp = temp.ResolveTo(baseCori);
            if (!temp.IsAbsolute()) {
                throw new ArgumentException("new base URI must be an absolute URI", nameof(value));
            }

            BaseValue = temp;
        }

        /// <inheritdoc />
        public override CBORObject EncodeToCBORObject(Cori baseCori, CoralDictionary dictionary)
        {
            CBORObject result = CBORObject.NewArray();
            result.Add(1);
            if (baseCori != null) {
                Cori relative = BaseValue.MakeRelative(baseCori);
                result.Add(relative.Data);
            }
            else {
                result.Add(BaseValue.Data);
            }

            return result;
        }

        /// <inheritdoc />
        public override void BuildString(StringBuilder builder, string pad, Cori contextCori, CoralUsing usingDictionary)
        {
            builder.Append(pad);
            if (contextCori != null) {
                builder.AppendFormat($"#base <{BaseValue.MakeRelative(contextCori)}>\n");
            }
            else {
                builder.AppendFormat($"#base <{BaseValue}>\n");
            }
        }
    }
}
