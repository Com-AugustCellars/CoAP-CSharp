using System;
using Com.AugustCellars.CoAP;
using Com.AugustCellars.CoAP.Codec;
using Com.AugustCellars.CoAP.Net;
using Com.AugustCellars.CoAP.Stack;

namespace CoAP.Test.Std10.MockDriver
{
    public class MockStack : LayerStack
    {
        public string StackName { get; set; }
        public MockEndpoint MyEndPoint { get; set; }
        public IMessageDeliverer MyDeliverer { get; set; }

        public MockStack(Type[] layers, ICoapConfig config = null)
        {
            if (config == null) {
                config = CoapConfig.Default;
            }

            int i = 0;
            foreach (Type l in layers) {
                ILayer ll = (ILayer) Activator.CreateInstance(l, new object[]{config}, null);

                this.AddLast(i.ToString(), ll);
                i++;
            }
        }

        public void SendResponseBytes(byte[] encodedRequest)
        {
            MessageDecoder decoder = new Spec.MessageDecoder18(encodedRequest);
            Response serverRequest = decoder.DecodeResponse();

            // Exchange exchange = new Exchange(serverRequest, Origin.Remote);

            SendResponse(null, serverRequest);

        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Stack name = {StackName}";
        }
    }
}
