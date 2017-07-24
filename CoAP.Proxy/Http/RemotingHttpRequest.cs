/*
 * Copyright (c) 2011-2014, Longxiang He <helongxiang@smeshlink.com>,
 * SmeshLink Technology Co.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY.
 * 
 * This file is part of the CoAP.NET, a CoAP framework in C#.
 * Please see README for more information.
 */

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Runtime.Remoting.Channels;
using System.Text;

namespace Com.AugustCellars.CoAP.Proxy.Http
{
    /// <summary>
    /// Class representing an HTTP Request to proxy.
    /// </summary>
    class RemotingHttpRequest : IHttpRequest
    {
        private IDictionary<string, object> _parameters;
        private IDictionary<object, object> _data;
        private readonly NameValueCollection _headers = new NameValueCollection();

        /// <summary>
        /// Create the request object from the transport object
        /// </summary>
        /// <param name="headers">transport headers to copy</param>
        /// <param name="stream">stream for the body</param>
        public RemotingHttpRequest(ITransportHeaders headers, Stream stream)
        {
            Method = (string) headers["__RequestVerb"];
            Host = (string) headers["Host"];
            UserAgent = (string) headers["User-Agent"];

            string requestUri = (string) headers["__RequestUri"];
            Url = "http://" + Host + requestUri;

            int offset = requestUri.IndexOf('?');
            if (offset >= 0) {
                RequestUri = requestUri.Substring(0, offset);
                QueryString = requestUri.Substring(offset + 1);
            }
            else {
                RequestUri = requestUri;
                QueryString = null;
            }

            foreach (DictionaryEntry item in headers) {
                if (item.Value is string) _headers.Add((string) item.Key, (string) item.Value);
            }

            InputStream = stream;
        }

        /// <summary>
        /// Get/Set the URL for the request
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Get/Set the URI for the request
        /// </summary>
        public string RequestUri { get; set; }

        /// <summary>
        /// Get/Set the query string for the request
        /// </summary>
        public string QueryString { get; set; }

        /// <summary>
        /// Get/Set the method for the request
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// Get the headers from the request
        /// </summary>
        public NameValueCollection Headers
        {
            get => _headers;
        }

        /// <summary>
        /// Get/Set the body stream
        /// </summary>
        public Stream InputStream { get; set; }

        /// <summary>
        /// Get/Set the host of the URL
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Get/Set the user agent
        /// </summary>
        public string UserAgent { get; set; }

        /// <summary>
        /// Get the character encoding for the body
        /// </summary>
        public string CharacterEncoding
        {
            get
            {
                string contentType = Headers["content-type"];
                if (contentType != null) {
                    foreach (string s in contentType.Split(';')) {
                        string ct = s.Trim().ToLower();
                        if (ct.StartsWith("charset=")) return ct.Substring("charset=".Length).Trim();
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Get/Set key based value on object
        /// </summary>
        /// <param name="key">key to use for index</param>
        /// <returns>associated value</returns>
        public object this[object key]
        {
            get => _data != null && _data.ContainsKey(key) ? _data[key] : null;
            set
            {
                if (_data == null) _data = new Dictionary<object, object>();
                _data[key] = value;
            }
        }

        /// <summary>
        /// Return specific query parameter from the set
        /// If there is more than one value, only the first is returned.
        /// </summary>
        /// <param name="name">name of paraemter</param>
        /// <returns>value of parameter</returns>
        public string GetParameter(string name)
        {
            ParseParameters();
            if (_parameters.ContainsKey(name)) {
                object o = _parameters[name];
                if (o is IList<string>) o = ((IList<string>) o)[0];
                return (string) o;
            }
            else return null;
        }

        /// <summary>
        /// Return specific query parameter from the set
        /// </summary>
        /// <param name="name">name of paraemter</param>
        /// <returns>value of parameter</returns>
        public string[] GetParameters(string name)
        {
            ParseParameters();
            string[] ret;
            if (_parameters.ContainsKey(name)) {
                object o = _parameters[name];
                if (o is List<string>) ret = ((List<string>) o).ToArray();
                else ret = new string[] {(string) o};
            }
            else ret = new string[0];
            return ret;
        }

        /// <summary>
        /// Parse the query string down into a dictionary of query parameters
        /// </summary>
        private void ParseParameters()
        {
            if (_parameters != null) return;

            Encoding encoding;
            string charset = CharacterEncoding;
            if (charset == null) encoding = Encoding.UTF8;
            else encoding = Encoding.GetEncoding(charset);

            _parameters = new Dictionary<string, object>();

            if (QueryString != null) {
                ParseQueryString(_parameters, QueryString, encoding);
            }

            // TODO parse form data
        }

        private void ParseQueryString(IDictionary<string, object> parameters, string query, Encoding e)
        {
            foreach (string s in query.Split('&')) {
                ParseParameter(parameters, s, e);
            }
        }

        private void ParseParameter(IDictionary<string, object> parameters, string s, Encoding e)
        {
            if (string.IsNullOrEmpty(s)) return;
            int offset = s.IndexOf('=');
            string name, value;
            if (offset == -1) {
                name = s;
                value = string.Empty;
            }
            else {
                name = s.Substring(0, offset);
                value = s.Substring(offset + 1);
            }
            AddParameter(parameters, name, value);
        }

        private void AddParameter(IDictionary<string, object> parameters, string key, string value)
        {
            if (parameters.ContainsKey(key)) {
                object o = parameters[key];
                IList<string> list = o as IList<string>;
                if (list == null) {
                    list = new List<string> {(string) o};
                    parameters[key] = list;
                }
                list.Add(value);
            }
            else parameters[key] = value;
        }
    }
}
