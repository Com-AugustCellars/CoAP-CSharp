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


        private readonly string _operationType;
        private readonly string _target;
        /// <summary>
        /// Child body
        /// </summary>
        public CoralBody Body { get; } = new CoralBody();

        public CoralForm(string formRef, string target)
        {
            _operationType = formRef;
            _target = target;
        }

        public CoralForm(string formRef, string target, CoralBody body)
        {
            _operationType = formRef;
            _target = target;
            Body = body;
        }

        public override CBORObject EncodeToCBORObject(Ciri ciriBase, CoralDictionary dictionary)
        {
            CBORObject node = CBORObject.NewArray();

            node.Add(3);
            node.Add(dictionary.Lookup(_operationType));
            node.Add(dictionary.Lookup(_target));
            if (Body.Length > 0)
            {
                node.Add(Body.EncodeToCBORObject(ciriBase, dictionary));
            }

            return node;
        }

        /// <inheritdoc />
        public override void BuildString(StringBuilder builder)
        {
            throw new System.NotImplementedException();
        }
    }
}
