using System.IO;
using System.Runtime.Remoting.Channels;

namespace Com.AugustCellars.CoAP.Proxy.Http
{
    class RemotingHttpResponse : IHttpResponse
    {
        /// <summary>
        /// Stream to place body output into
        /// </summary>
        public Stream OutputStream { get; } = new MemoryStream();

        /// <summary>
        /// Headers for transport
        /// </summary>
        public ITransportHeaders Headers { get; } = new TransportHeaders();

        /// <summary>
        /// Add a header to the transport header list.
        /// It will replace the header if it already exists.
        /// </summary>
        /// <param name="name">name of parameter</param>
        /// <param name="value">value of parameter</param>
        public void AppendHeader(string name, string value)
        {
            Headers[name] = value;
        }

        /// <summary>
        /// Get/Set the __HttpStatusCode parameter
        /// </summary>
        public int StatusCode
        {
            get => (int) Headers["__HttpStatusCode"];
            set => Headers["__HttpStatusCode"] = value;
        }

        /// <summary>
        /// Get/Set the __HttpReasonPhrase parameter
        /// </summary>
        public string StatusDescription
        {
            get { return (string) Headers["__HttpReasonPhrase"]; }
            set { Headers["__HttpReasonPhrase"] = value; }
        }
    }
}