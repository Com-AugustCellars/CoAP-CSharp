using System;
using System.Collections.Generic;
using System.Text;
using Com.AugustCellars.CoAP.Util;
using Org.BouncyCastle.Operators;
using PeterO.Cbor;

namespace Com.AugustCellars.CoAP.Coral
{
    public class CoralFormField
    {
        public string FieldTypeText => FieldType?.ToString();
        public Cori FieldType { get; }
        public int? FieldTypeInt { get; }
        public Cori Url { get; }
        public CBORObject Literal { get; }
        public int? LiteralInt { get; }


        public CoralFormField(Cori fieldType, Cori value)
        {
            FieldType = fieldType;
            if (!value.IsAbsolute()) {
                throw new ArgumentException("Must be an absolute CoRI value", nameof(value));
            }

            Url = value;
        }

        public CoralFormField(string fieldType, Cori value) : this(new Cori(fieldType), value) { }

        public CoralFormField(Cori fieldType, CBORObject value)
        {
            FieldType = fieldType;
            Literal = value;
        }

        public CoralFormField(string fieldType, CBORObject value) : this(new Cori(fieldType), value) { }

        public CoralFormField(CBORObject type, CBORObject value, Cori baseCori, CoralDictionary dictionary)
        {

            CBORObject o = (CBORObject) dictionary.Reverse(type, false);
            if (o == null) {
                FieldTypeInt = type.AsInt32();
            }
            else if (o.Type == CBORType.Array) {
                FieldType = new Cori(o);
                if (type.Type == CBORType.Integer) {
                    FieldTypeInt = type.AsInt32();
                }
            }
            else {
                throw new ArgumentException("Not a valid form field type");
            }
            
            o = (CBORObject) dictionary.Reverse(value, true);

            if (o == null) {
                LiteralInt = value.AsInt32();
            }
            else if (o.Type == CBORType.Array) {
                Url = new Cori(o);
                if (baseCori != null) {
                    Url = Url.ResolveTo(baseCori);
                }

                if (value.Type == CBORType.Integer) {
                    LiteralInt = value.Untag().AsInt32();
                }
            }
            else {
                Literal = o;
                if (value.IsTagged && value.HasOneTag(CoralDictionary.DictionaryTag) && value.Type == CBORType.Integer) {
                    LiteralInt = value.Untag().AsInt32();
                }
            }
        }

        public CBORObject EncodeToCBOR(CBORObject fieldArray, Cori baseCori, CoralDictionary dictionary)
        {

            fieldArray.Add(dictionary.Lookup(FieldType, false));

            if (Literal != null) {
                fieldArray.Add(dictionary.Lookup(Literal, true));
            }
            else {
                CBORObject x = dictionary.Lookup(Url, true);
                if (x.Type == CBORType.Integer) {
                    fieldArray.Add(x);
                }
                else {
                    fieldArray.Add(Url.MakeRelative(baseCori).Data);
                }
            }

            return fieldArray;
        }
    }
}
