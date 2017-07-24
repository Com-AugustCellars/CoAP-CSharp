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

using System;
using Com.AugustCellars.CoAP.Util;

namespace Com.AugustCellars.CoAP.Proxy
{
    /// <summary>
    /// Provides the translations between the messages from the internal CoAP nodes and external ones.
    /// </summary>
    public static class CoapTranslator
    {
        /// <summary>
        /// Starting from an external CoAP request, the method fills a new request
        /// for the internal CoAP nodes. Translates the proxy-uri option in the uri
        /// of the new request and simply copies the options and the payload from the
        /// original request to the new one.
        /// </summary>
        /// <param name="incomingRequest">the original request</param>
        /// <returns>request</returns>
        /// <exception cref="ArgumentNullException">the <paramref name="incomingRequest"/> is null</exception>
        /// <exception cref="TranslationException"></exception>
        public static Request GetRequest(Request incomingRequest)
        {
            if (incomingRequest == null) {
                throw ThrowHelper.ArgumentNull("incomingRequest");
            }

            Request outgoingRequest = new Request(incomingRequest.Method, incomingRequest.Type == MessageType.CON);

            // copy payload
            Byte[] payload = incomingRequest.Payload;
            outgoingRequest.Payload = payload;

            // get the uri address from the proxy-uri option
            Uri serverUri;
            try {
                /*
                 * The new draft (14) only allows one proxy-uri option. Thus, this
                 * code segment has changed.
                 */
                serverUri = incomingRequest.ProxyUri;
            }
            catch (UriFormatException e) {
                throw new TranslationException("Cannot translate the server uri", e);
            }

            // copy every option from the original message
            foreach (Option opt in incomingRequest.GetOptions()) {


                switch (opt.Type) {
                    // do not copy the proxy-uri option because it is not necessary in the new message
                    case OptionType.ProxyUri:

                    // do not copy the uri-* options because they are already filled in
                    // the new message
                    case OptionType.UriHost:
                    case OptionType.UriPath:
                    case OptionType.UriPort:
                    case OptionType.UriQuery:

                    // do not copy the block* option because it is a local option and
                    // have to be assigned by the proper layer
                    case OptionType.Block1:
                    case OptionType.Block2:
                        break;

                    default:
                        outgoingRequest.AddOption(opt);
                        break;
                }
            }

            //  New URI for the request came from the proxy request.

            if (serverUri != null) outgoingRequest.URI = serverUri;

            return outgoingRequest;
        }

        /// <summary>
        /// Fills the new response with the response received from the internal CoAP
        /// node. Simply copies the options and the payload from the forwarded
        /// response to the new one.
        /// </summary>
        /// <param name="incomingResponse">the forwarded request</param>
        /// <exception cref="ArgumentNullException">the <paramref name="incomingResponse"/> is null</exception>
        /// <returns></returns>
        public static Response GetResponse(Response incomingResponse)
        {
            if (incomingResponse == null) throw ThrowHelper.ArgumentNull("incomingResponse");

            Response outgoingResponse = new Response(incomingResponse.StatusCode);

            // copy payload
            Byte[] payload = incomingResponse.Payload;
            outgoingResponse.Payload = payload;

            // copy the timestamp
            outgoingResponse.Timestamp = incomingResponse.Timestamp;


            // copy options from the original message
            foreach (Option opt in incomingResponse.GetOptions()) {
                switch (opt.Type) {
                    // do not copy the block* option because it is a local option and
                    // have to be assigned by the proper layer
                    case OptionType.Block1:
                    case OptionType.Block2:
                        break;

                    default:
                        outgoingResponse.AddOption(opt);
                        break;
                }
            }

            return outgoingResponse;
        }
    }
}
