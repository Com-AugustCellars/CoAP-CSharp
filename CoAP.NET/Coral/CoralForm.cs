using System;
using System.Collections.Generic;
using System.Text;
using Com.AugustCellars.CoAP.Util;
using PeterO.Cbor;

namespace Com.AugustCellars.CoAP.Coral
{
    public class CoralForm : CoralItem
    {
        //  A form consists of
        //  a form context - the resource on which the operation is going to be executed
        //  an operation type - identifies the semantics of the operation - an IRI
        //  a request method - either implicit or set as a link property
        //  a submission target - == What is the difference between this and a form context === the address to send to
        //  array of form fields - 

        //  Encoding in CBOR
        //  form = [3, operation_type, submission-target, ? form-fields ]
        //  form-fields = [*(form-field-type, form-field-value)]


        public string OperationType { get; }
        public int? OperationTypeInt { get; }
        public Cori Target { get; }
        public int? TargetInt { get; }

        /// <summary>
        /// Child body
        /// </summary>
        public List<CoralFormField> FormFields { get; } = new List<CoralFormField>();

        public CoralForm(string formRef, Cori target)
        {
            OperationType = formRef;
            Target = target;
        }

        public CoralForm(CBORObject form, Cori baseCori, CoralDictionary dictionary)
        {
            if (form.Type != CBORType.Array && !(form.Count == 3 || form.Count == 4)) {
                throw new ArgumentException("Invalid form descriptor", nameof(form));
            }

            if (baseCori == null || !baseCori.IsAbsolute()) {
                throw new ArgumentException("Invalid base reference", nameof(baseCori));
            }

            if (form[0].Type != CBORType.Integer || form[0].AsInt32() != 3) {
                throw new ArgumentException("Not a CoRAL form descriptor", nameof(form));
            }

            CBORObject o = (CBORObject) dictionary.Reverse(form[1], false);
            if (o == null) {
                OperationTypeInt = form[1].Untag().AsInt32();
            }
            else {
                OperationType = o.AsString();
                if (form[1].Type == CBORType.Integer) {
                    OperationTypeInt = form[1].Untag().AsInt32();
                }
            }

            o = (CBORObject) dictionary.Reverse(form[2], true);
            if (o == null) {
                TargetInt = form[2].Untag().AsInt32();
            }
            else if (o.Type != CBORType.Array) {
                throw new ArgumentException("Invalid submission target", nameof(form));
            }
            else {
                Target = new Cori(o);
                if (form[2].Type == CBORType.Integer &&  
                    form[2].HasTag(CoralDictionary.DictionaryTag)) {
                    TargetInt = form[2].Untag().AsInt32();
                }

                Target = Target.ResolveTo(baseCori);
            }

            if (form.Count == 4) {
                if (form[3].Type != CBORType.Array) {
                    throw new ArgumentException("Invalid form field array", nameof(form));
                }

                for (int i = 0; i<form[3].Count; i += 2) {
                    FormFields.Add(new CoralFormField(form[3][i], form[3][i+1], baseCori, dictionary));
                }
            }
        }

        public override CBORObject EncodeToCBORObject(Cori coriBase, CoralDictionary dictionary)
        {
            CBORObject node = CBORObject.NewArray();

            node.Add(3);
            node.Add(dictionary.Lookup(OperationType, false));

            CBORObject o = dictionary.Lookup(Target, false);
            if (o.Type == CBORType.Integer) {
                node.Add(o);
            }
            else {
                node.Add(Target.MakeRelative(coriBase).Data);
            }

            if (FormFields.Count > 0) {
                CBORObject fields = CBORObject.NewArray();
                foreach (CoralFormField f in FormFields) {
                    f.EncodeToCBOR(fields, coriBase, dictionary);
                }

                node.Add(fields);
            }

            return node;
        }

        /// <inheritdoc />
        public override void BuildString(StringBuilder builder, string pad)
        {
            throw new System.NotImplementedException();
        }
    }
}
