/*
 * Copyright (c) 2011-2015, Longxiang He <helongxiang@smeshlink.com>,
 * SmeshLink Technology Co.
 *
 * Copyright (c) 2019-2020, Jim Schaad <ietf@augustcellars.com>
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY.
 * 
 * This file is part of the CoAP.NET, a CoAP framework in C#.
 * Please see README for more information.
 */

using System;
using Com.AugustCellars.CoAP.Log;
using Com.AugustCellars.CoAP.Net;
using Com.AugustCellars.CoAP.Server.Resources;
using Com.AugustCellars.CoAP.Util;

namespace Com.AugustCellars.CoAP.Observe
{
    /// <summary>
    /// Represents a relation between a client endpoint and a resource on this server.
    /// </summary>
    public class ObserveRelation
    {
        private static readonly ILogger _Log = LogManager.GetLogger(typeof(ObserveRelation));
        private readonly ICoapConfig _config;
        private readonly ObservingEndpoint _endpoint;
        private DateTime _interestCheckTime = DateTime.Now;
        private int _interestCheckCounter = 0;

        /// <summary>
        /// Constructs a new observe relation.
        /// </summary>
        /// <param name="config">the config</param>
        /// <param name="endpoint">the observing endpoint</param>
        /// <param name="resource">the observed resource</param>
        /// <param name="exchange">the exchange that tries to establish the observe relation</param>
        public ObserveRelation(ICoapConfig config, ObservingEndpoint endpoint, IResource resource, Exchange exchange)
        {
            _config = config?? throw ThrowHelper.ArgumentNull("config");
            _endpoint = endpoint?? throw ThrowHelper.ArgumentNull("endpoint");
            Resource = resource?? throw ThrowHelper.ArgumentNull("resource");
            Exchange = exchange?? throw ThrowHelper.ArgumentNull("exchange");
            Key = $"{Source}#{exchange.Request.TokenString}";
        }

        /// <summary>
        /// Gets the resource.
        /// </summary>
        public IResource Resource { get; }

        /// <summary>
        /// Gets the exchange.
        /// </summary>
        public Exchange Exchange { get; }

        public string Key { get; }

        /// <summary>
        /// Gets the source endpoint of the observing endpoint.
        /// </summary>
        public System.Net.EndPoint Source => _endpoint.EndPoint;

        public Response CurrentControlNotification { get; set; }

        public Response NextControlNotification { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if this relation has been established.
        /// </summary>
        public bool Established { get; set; }

        /// <summary>
        /// Cancel this observe relation.
        /// </summary>
        public void Cancel()
        { 
            _Log.Debug(m => m($"Cancel observe relation from {Key} with {Resource.Path}"));

            // stop ongoing retransmissions
            if (Exchange.Response != null) {
                Exchange.Response.Cancel();
            }
            Established = false;
            Resource.RemoveObserveRelation(this);
            _endpoint.RemoveObserveRelation(this);
            Exchange.Complete = true;
        }

        /// <summary>
        /// Cancel all observer relations that this server has
        /// established with this relation's endpoint.
        /// </summary>
        public void CancelAll()
        {
            _endpoint.CancelAll();
        }

        /// <summary>
        /// Notifies the observing endpoint that the resource has been changed.
        /// </summary>
        public void NotifyObservers()
        {
            // makes the resource process the same request again
            Resource.HandleRequest(Exchange);
        }

        /// <summary>
        /// Do we think that we should be doing a CON check on the resource?
        /// The check is done on both a time interval and a number of notifications.
        /// </summary>
        /// <returns>true if should do a CON check</returns>
        public bool Check()
        {
            bool check = false;
            DateTime now = DateTime.Now;
            check |= _interestCheckTime.AddMilliseconds(_config.NotificationCheckIntervalTime) < now;
            check |= (++_interestCheckCounter >= _config.NotificationCheckIntervalCount);
            if (check) {
                _interestCheckTime = now;
                _interestCheckCounter = 0;
            }
            return check;
        }
    }
}
