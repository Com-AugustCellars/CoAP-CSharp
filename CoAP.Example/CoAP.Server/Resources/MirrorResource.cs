using System;
using System.Text;
using Com.AugustCellars.CoAP.Net;
using Com.AugustCellars.CoAP.Server.Resources;

namespace Com.AugustCellars.CoAP.Examples.Resources
{
    /// <summary>
    /// This resource responds with the data from a request in its payload. This
    /// resource responds to GET, POST, PUT and DELETE requests.
    /// </summary>
    class MirrorResource : Resource
    {
        public MirrorResource(String name)
            : base(name)
        { }

        public override void HandleRequest(Exchange exchange)
        {
            Request request = exchange.Request;
            StringBuilder buffer = new StringBuilder();
            buffer.Append("resource ").Append(Uri).Append(" received request")
                .Append("\n").Append("Code: ").Append(request.Code)
                .Append("\n").Append("Source: ").Append(request.Source)
                .Append("\n").Append("Type: ").Append(request.Type)
                .Append("\n").Append("MID: ").Append(request.ID)
                .Append("\n").Append("Token: ").Append(request.TokenString)
                //.Append("\n").Append(request.Options)
                ;
            Response response = new Response(StatusCode.Content);
            response.PayloadString = buffer.ToString();
            exchange.SendResponse(response);
        }
    }
}
