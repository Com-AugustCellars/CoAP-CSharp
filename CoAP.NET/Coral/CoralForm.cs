using PeterO.Cbor;

namespace Com.AugustCellars.CoAP.Coral
{
    public class CoralForm : CoralItem
    {
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

        public override CBORObject EncodeToCBORObject(CoralDictionary dictionary)
        {
            CBORObject node = CBORObject.NewArray();

            node.Add(3);
            node.Add(dictionary.Lookup(_operationType));
            node.Add(dictionary.Lookup(_target));
            if (Body.Length > 0)
            {
                node.Add(Body.EncodeToCBORObject(dictionary));
            }

            return node;
        }
    }
}
