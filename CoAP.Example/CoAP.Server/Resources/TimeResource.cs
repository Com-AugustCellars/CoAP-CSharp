using System;
using System.Collections.Generic;
using System.Threading;
using Com.AugustCellars.CoAP.Server.Resources;
#if false
using PeterO.Cbor;
#endif

namespace Com.AugustCellars.CoAP.Examples.Resources
{
    class TimeResource : Resource
    {
        private Timer _timer;
        private DateTime _now;

        public TimeResource(String name)
            : base(name)
        {
            Attributes.Title = "GET the current time";
            Attributes.AddResourceType("CurrentTime");
            Observable = true;

            _timer = new Timer(Timed, null, 0, 2000*30*30);
        }

        private void Timed(Object o)
        {
            _now = DateTime.Now;
            Changed();
        }

        protected override void DoGet(CoapExchange exchange)
        {
#if true
            exchange.Respond(StatusCode.Content, _now.ToString(), MediaType.TextPlain);
#else
            Request request = exchange.Request;

            IEnumerable<Option> options =  request.GetOptions(OptionType.Accept);
            int useAccept = MediaType.Undefined;
            bool acceptFound = false;

            foreach (var acccept in options) {
                switch (acccept.IntValue) {
                case MediaType.TextPlain:
                case MediaType.ApplicationCbor:
                    useAccept = acccept.IntValue;
                    break;

                default:
                    acceptFound = true;
                    break;
                    
                }

                if (useAccept != MediaType.Undefined) break;
            }

            if (useAccept == MediaType.Undefined) {
                if (acceptFound) {
                    exchange.Respond(StatusCode.UnsupportedMediaType);
                    return;
                }
                useAccept = MediaType.TextPlain;
            }

            Response response = Response.CreateResponse(request, StatusCode.Content);

            switch (useAccept) {
                case MediaType.TextPlain:
                    string x = request.GetParameter("format");
                    if (String.IsNullOrEmpty(x)) {
                        response.PayloadString = _now.ToShortTimeString();

                    }
                    else {
                        response.PayloadString = _now.ToString(x);
                    }
                    request.ContentType = useAccept;
                    break;

                case MediaType.ApplicationCbor:
                    CBORObject obj = CBORObject.FromObject(_now);
                    request.Payload = obj.EncodeToBytes();
                    request.ContentType = useAccept;
                    break;
            }

            exchange.Respond(response);
#endif
        }
    }
}
