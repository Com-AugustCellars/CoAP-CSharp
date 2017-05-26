/*
 * Copyright (c) 2011-2015, Longxiang He <helongxiang@smeshlink.com>,
 * SmeshLink Technology Co.
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
using System.Collections.Concurrent;
using System.Collections.Generic;

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
        private readonly IResource _resource;
        private readonly Exchange _exchange;
        private readonly String _key;
        private DateTime _interestCheckTime = DateTime.Now;
        private Int32 _interestCheckCounter = 1;

        /// <summary>
        /// The notifications that have been sent, so they can be removed from the Matcher
        /// </summary>
        private readonly ConcurrentQueue<Response> _notifications = new ConcurrentQueue<Response>();

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
            _resource = resource?? throw ThrowHelper.ArgumentNull("resource");
            _exchange = exchange?? throw ThrowHelper.ArgumentNull("exchange");
            _key = $"{Source}#{exchange.Request.TokenString}";
        }

        /// <summary>
        /// Gets the resource.
        /// </summary>
        public IResource Resource
        {
            get =>_resource;
        }

        /// <summary>
        /// Gets the exchange.
        /// </summary>
        public Exchange Exchange
        {
            get => _exchange;
        }

        public String Key
        {
            get => _key;
        }

        /// <summary>
        /// Gets the source endpoint of the observing endpoint.
        /// </summary>
        public System.Net.EndPoint Source
        {
            get => _endpoint.EndPoint;
        }

        public Response CurrentControlNotification { get; set; }

        public Response NextControlNotification { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if this relation has been established.
        /// </summary>
        public Boolean Established { get; set; }

        /// <summary>
        /// Cancel this observe relation.
        /// </summary>
        public void Cancel()
        {
            if (_Log.IsDebugEnabled) {
                _Log.Debug("Cancel observe relation from " + _key + " with " + _resource.Path);
            }
            // stop ongoing retransmissions
            if (_exchange.Response != null) {
                _exchange.Response.Cancel();
            }
            Established = false;
            _resource.RemoveObserveRelation(this);
            _endpoint.RemoveObserveRelation(this);
            _exchange.Complete = true;
        }

        /// <summary>
        /// Cancel all observer relations that this server has
        /// established with this's realtion's endpoint.
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
            _resource.HandleRequest(_exchange);
        }

        /// <summary>
        /// Do we think that we should be doing a CON check on the resource?
        /// The check is done on both a time intervolt and a number of notifications.
        /// </summary>
        /// <returns>true if should do a CON check</returns>
        public Boolean Check()
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

        /// <summary>
        /// Add the response to the notification list for this observation
        /// </summary>
        /// <param name="notification">Response to send</param>
        public void AddNotification(Response notification)
        {
            _notifications.Enqueue(notification);
        }

        /// <summary>
        /// Enumerate through all of the queued notifications
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Response> ClearNotifications()
        {
            Response resp;
            while (_notifications.TryDequeue(out resp)) {
                yield return resp;
            }
        }
    }
}
