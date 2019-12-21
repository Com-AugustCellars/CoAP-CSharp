using System;
using System.Collections.Generic;
using System.Text;
using Com.AugustCellars.CoAP.Util;
using PeterO.Cbor;

namespace Com.AugustCellars.CoAP.Coral
{
    public class CoralBaseDirective : CoralItem
    {
        public Ciri BaseValue { get; }

        public CoralBaseDirective(Ciri value)
        {
            if (!value.IsWellFormed()) {
                throw new ArgumentException("URI is not well formed", nameof(value));
            }
            BaseValue = value;
        }

        public CoralBaseDirective(CBORObject value, Ciri baseCiri)
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

            Ciri temp = new Ciri(value[1]);
            if (!temp.IsWellFormed()) {
                throw new ArgumentException("base value is not well formed", nameof(value));
            }

            temp = temp.ResolveTo(baseCiri);
            if (!temp.IsAbsolute()) {
                throw new ArgumentException("new base URI must be an absolute URI", nameof(value));
            }

            BaseValue = temp;
        }

        /// <inheritdoc />
        public override CBORObject EncodeToCBORObject(Ciri baseCiri, CoralDictionary dictionary)
        {
            CBORObject result = CBORObject.NewArray();
            result.Add(1);
            if (baseCiri != null) {
                Ciri relative = BaseValue.MakeRelative(baseCiri);
                result.Add(relative.Data);
            }
            else {
                result.Add(BaseValue.Data);
            }

            return result;
        }

        /// <inheritdoc />
        public override void BuildString(StringBuilder builder)
        {
            builder.AppendFormat($"#base <{BaseValue.Data}>\n");
        }
    }
}
