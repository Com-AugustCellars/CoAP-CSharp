﻿/*
 * Copyright (c) 2011-2015, Longxiang He <helongxiang@smeshlink.com>,
 * SmeshLink Technology Co.
 *
 * Copyright (c) 2019-20, Jim Schaad <ietf@augustcellars.com>
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY.
 * 
 * This file is part of the CoAP.NET, a CoAP framework in C#.
 * Please see README for more information.
 */

using System;
using System.Diagnostics.Tracing;
using Com.AugustCellars.CoAP.Net;
using Com.AugustCellars.CoAP.Observe;
using Org.BouncyCastle.Ocsp;

namespace Com.AugustCellars.CoAP
{
    /// <summary>
    /// Represents a CoAP observe relation between a CoAP client and a resource on a server.
    /// Provides a simple API to check whether a relation has successfully established and
    /// to cancel or refresh the relation.
    /// </summary>
    public class CoapObserveRelation
    {
        readonly ICoapConfig _config;
        readonly IEndPoint _endpoint;

        public bool Reconnect { get; set; } = true;

        public CoapObserveRelation(Request request, IEndPoint endpoint, ICoapConfig config)
        {
            _config = config;
            Request = request;
            _endpoint = endpoint;
            Orderer = new ObserveNotificationOrderer(config);
            Request.ObserveRelation = this;

            request.Reregistering += OnReregister;
        }

        /// <summary>
        /// Return the original request that caused the observe relationship to be established.
        /// </summary>
        public Request Request { get; private set; }

        /// <summary>
        /// Return the most recent response that was received from the observe relationship.
        /// </summary>
        public Response Current { get; set; }

        /// <summary>
        /// Return the orderer.  This is the filter function that is used to determine if
        /// a new notification is really new or if it is a repeat or old data.
        /// </summary>
        public ObserveNotificationOrderer Orderer { get; private set; }

        /// <summary>
        /// Is the observe relationship canceled?
        /// 
        /// Setting this property does not send a request to the server to remove the observation.
        /// </summary>
        public Boolean Canceled { get; set; }

        public void ReactiveCancel()
        {
            Request.IsCancelled = true;
            Canceled = true;
        }

        /// <summary>
        /// Send a message to the resource being observed that we want to cancel
        /// the observation.
        /// </summary>
        public void ProactiveCancel()
        {
            Request cancel = Request.NewGet();
            // copy options, but set Observe to cancel
            cancel.SetOptions(Request.GetOptions());
            cancel.MarkObserveCancel();
            // use same Token
            cancel.Token = Request.Token;
            cancel.Destination = Request.Destination;

            // dispatch final response to the same message observers
            cancel.CopyEventHandler(Request);
            Reconnect = false;
            cancel.ObserveRelation = this;
            cancel.Reregistering += OnReregister;

            cancel.Send(_endpoint);
            // cancel old ongoing request
            Request.IsCancelled = true;
        }

        public void UpdateETags(byte[][] eTags)
        {
            Request newRequest = Request.NewGet();
            //  Copy over the options
            newRequest.SetOptions(Request.GetOptions());
            newRequest.ClearETags();
            foreach (byte[] tag in eTags) {
                newRequest.AddETag(tag);
            }

            newRequest.Token = Request.Token;
            newRequest.Destination = Request.Destination;
            newRequest.CopyEventHandler(Request);
            newRequest.Reregistering += OnReregister;
            newRequest.ObserveRelation = this;

            newRequest.Send(_endpoint);
            Request = newRequest;
        }

        private void OnReregister(Object sender, ReregisterEventArgs e)
        {
            // TODO: update request in observe handle for correct cancellation?
            if (Canceled) {
                e.RefreshRequest.IsCancelled = true;
                return;
            }

            Orderer.ForceRelease = true;
            //_request = e.RefreshRequest;
        }
    }
}
