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
using Com.AugustCellars.CoAP.Net;

namespace Com.AugustCellars.CoAP.Server.Resources
{
    /// <summary>
    /// Represents an exchange of a CoAP request and response and
    /// provides a user-friendly API to subclasses of <see cref="Resource"/>
    /// for responding to requests.
    /// </summary>
    public class CoapExchange
    {
        private readonly Exchange _exchange;
        private readonly Resource _resource;

        /// <summary>
        /// Constructs a new CoAP Exchange object representing
        /// the specified exchange and resource.
        /// </summary>
        internal CoapExchange(Exchange exchange, Resource resource)
        {
            _exchange = exchange;
            _resource = resource;
        }

        /// <summary>
        /// Gets the request.
        /// </summary>
        public Request Request
        {
            get => _exchange.Request;
        }

        /// <summary>
        /// Gets or sets the Location-Path for the response.
        /// </summary>
        public String LocationPath { get; set; }

        /// <summary>
        /// Gets or sets the Location-Query for the response.
        /// </summary>
        public String LocationQuery { get; set; }

        /// <summary>
        /// Gets or sets the Max-Age for the response body.
        /// </summary>
        public Int32 MaxAge { get; set; } = 60;

        /// <summary>
        /// Gets or sets the ETag for the response.
        /// </summary>
        public Byte[] ETag { get; set; }

        /// <summary>
        /// Accepts the exchange.
        /// </summary>
        public void Accept()
        {
            _exchange.SendAccept();
        }

        /// <summary>
        /// Rejects the exchange.
        /// </summary>
        public void Reject()
        {
            _exchange.SendReject();
        }

        /// <summary>
        /// Responds the specified response code and no payload.
        /// </summary>
        public void Respond(StatusCode code)
        {
            Respond(new Response(code));
        }

        /// <summary>
        /// Responds with code 2.05 (Content) and the specified payload.
        /// </summary>
        public void Respond(String payload)
        {
            Respond(StatusCode.Content, payload);
        }

        /// <summary>
        /// Responds with the specified response code and payload.
        /// </summary>
        public void Respond(StatusCode code, String payload)
        {
            Response response = new Response(code);
            response.SetPayload(payload, MediaType.TextPlain);
            Respond(response);
        }

        /// <summary>
        /// Responds with the specified response code and payload.
        /// </summary>
        public void Respond(StatusCode code, Byte[] payload)
        {
            Response response = new Response(code) {
                Payload = payload
            };
            Respond(response);
        }

        /// <summary>
        /// Responds with the specified response code, payload and content-type.
        /// </summary>
        public void Respond(StatusCode code, Byte[] payload, Int32 contentType)
        {
            Response response = new Response(code) {
                Payload = payload,
                ContentType = contentType
            };
            Respond(response);
        }

        /// <summary>
        /// Responds with the specified response code, payload and content-type.
        /// </summary>
        public void Respond(StatusCode code, String payload, Int32 contentType)
        {
            Response response = new Response(code);
            response.SetPayload(payload, contentType);
            Respond(response);
        }

        /// <summary>
        /// Responds Respond with the specified response.
        /// </summary>
        public void Respond(Response response)
        {
            if (response == null) {
                throw new ArgumentNullException(nameof(response));
            }

            // set the response options configured through the CoapExchange API
            if (LocationPath != null) {
                response.LocationPath = LocationPath;
            }

            if (LocationQuery != null) {
                response.LocationQuery = LocationQuery;
            }

            if (MaxAge != 60) {
                response.MaxAge = MaxAge;
            }

            if (ETag != null) {
                response.SetOption(Option.Create(OptionType.ETag, ETag));
            }

            response.Session = _exchange.Request.Session;

            _resource.CheckObserveRelation(_exchange, response);

            _exchange.SendResponse(response);
        }
    }
}
