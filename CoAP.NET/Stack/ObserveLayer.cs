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
using System.Timers;
using Com.AugustCellars.CoAP.Log;
using Com.AugustCellars.CoAP.Net;
using Com.AugustCellars.CoAP.Observe;

namespace Com.AugustCellars.CoAP.Stack
{
    public class ObserveLayer : AbstractLayer
    {
        private static readonly ILogger log = LogManager.GetLogger(typeof(ObserveLayer));
        private static readonly object reregistrationContextKey = "ReregistrationContext";
        private static readonly Random random = new Random();

        /// <summary>
        /// Additional time to wait until re-registration
        /// </summary>
        private readonly int _backOff;

        /// <summary>
        /// Constructs a new observe layer.
        /// </summary>
        public ObserveLayer(ICoapConfig config)
        {
            _backOff = config.NotificationReregistrationBackoff;
        }

        /// <inheritdoc/>
        public override void SendResponse(INextLayer nextLayer, Exchange exchange, Response response)
        {
            ObserveRelation relation = exchange.Relation;
            if (relation != null && relation.Established)  {
                if (exchange.Request.IsAcknowledged || exchange.Request.Type == MessageType.NON) {
                    // Transmit errors as CON
                    if (!Code.IsSuccess(response.Code)) {
                        log.Debug(m => m($"Response has error code {response.Code} and must be sent as CON"));
                        response.Type = MessageType.CON;
                        relation.Cancel();
                    }
                    else { 
                        // Make sure that every now and than a CON is mixed within
                        if (relation.Check()) { 
                            log.Debug("The observe relation check requires the notification to be sent as CON");
                            response.Type = MessageType.CON;
                        }
                        else {
                            // By default use NON, but do not override resource decision
                            if (response.Type == MessageType.Unknown) {
                                response.Type = MessageType.NON;
                            }
                        }
                    }
                }

                // This is a notification
                response.Last = false;

                /*
                 * The matcher must be able to find the NON notifications to remove
                 * them from the exchangesByID map
                 */
                if (response.Type == MessageType.NON) {
                    // relation.AddNotification(response);
                    PrepareTimeout(nextLayer, exchange, response);
                }

                /*
                 * Only one Confirmable message is allowed to be in transit. A CON
                 * is in transit as long as it has not been acknowledged, rejected,
                 * or timed out. All further notifications are postponed here. If a
                 * former CON is acknowledged or timeouts, it starts the freshest
                 * notification (In case of a timeout, it keeps the retransmission
                 * counter). When a fresh/younger notification arrives but must be
                 * postponed we forget any former notification.
                 */
                if (response.Type == MessageType.CON) {
                    PrepareSelfReplacement(nextLayer, exchange, response);
                }

                // The decision whether to postpone this notification or not and the
                // decision which notification is the freshest to send next must be
                // synchronized
                lock (exchange) {
                    Response current = relation.CurrentControlNotification;
                    if (current != null && IsInTransit(current)) { 
                        log.Debug(m => m($"A former notification is still in transit. Postpone {response}"));
                        if (relation.NextControlNotification != null && relation.NextControlNotification.Type == MessageType.CON) {
                            response.Type = MessageType.CON;
                        }

                        if (response.Type == MessageType.CON) {
                            // use the same ID
                            response.ID = current.ID;
                        }

                        relation.NextControlNotification = response;
                        return;
                    }
                    else { 
                        relation.CurrentControlNotification = response;
                        relation.NextControlNotification = null;
                    }
                }
            }

            // else no observe was requested or the resource does not allow it
            base.SendResponse(nextLayer, exchange, response);
        }

        /// <inheritdoc/>
        public override void ReceiveResponse(INextLayer nextLayer, Exchange exchange, Response response)
        {
            if (response.HasOption(OptionType.Observe)) {
                CoapObserveRelation relation = exchange.Request.ObserveRelation;
                if (relation == null || relation.Canceled) {
                    // The request was canceled and we no longer want notifications
                    log.Debug("ObserveLayer rejecting notification for canceled Exchange");

                    EmptyMessage rst = EmptyMessage.NewRST(response);
                    SendEmptyMessage(nextLayer, exchange, rst);
                    // Matcher sets exchange as complete when RST is sent

                    return;
                }

                if (relation.Reconnect) {
                    PrepareReregistration(exchange, response, msg => SendRequest(nextLayer, exchange, msg));
                }
            }
            base.ReceiveResponse(nextLayer, exchange, response);
        }

        /// <inheritdoc/>
        public override void ReceiveEmptyMessage(INextLayer nextLayer, Exchange exchange, EmptyMessage message)
        {
            // NOTE: We could also move this into the MessageObserverAdapter from
            // sendResponse into the method rejected().

            if (message.Type == MessageType.RST && exchange.Origin == Origin.Remote) {
                // The response has been rejected
                ObserveRelation relation = exchange.Relation;
                if (relation != null) {
                    relation.Cancel();
                } // else there was no observe relation ship and this layer ignores the rst
            }
            base.ReceiveEmptyMessage(nextLayer, exchange, message);
        }

        private static bool IsInTransit(Response response)
        {
            MessageType type = response.Type;
            bool acked = response.IsAcknowledged;
            bool timeout = response.IsTimedOut;
            bool result = ((type == MessageType.CON) || (type == MessageType.NON)) && !acked && !timeout;
            return result;
        }

        private void PrepareSelfReplacement(INextLayer nextLayer, Exchange exchange, Response response)
        {
            response.Acknowledged += (o, e) => {
                lock (exchange) {
                    ObserveRelation relation = exchange.Relation;
                    Response next = relation.NextControlNotification;
                    relation.CurrentControlNotification = next; // next may be null
                    relation.NextControlNotification = null;
                    if (next != null) { 
                        log.Debug("Notification has been acknowledged, send the next one");

                        // this is not a self replacement, hence a new ID
                        next.ID = Message.None;

                        // Create a new task for sending next response so that we can leave the sync-block
                        Executor.Start(() => base.SendResponse(nextLayer, exchange, next));
                    }
                }
            };

            response.Retransmitting += (o, e) => {
                lock (exchange) {
                    ObserveRelation relation = exchange.Relation;
                    Response next = relation.NextControlNotification;
                    if (next != null) {
                        log.Debug("The notification has timed out and there is a fresher notification for the retransmission.");
                        
                        // Cancel the original retransmission and send the fresh notification here
                        response.IsCancelled = true;

                        if (relation.CurrentControlNotification.Type == MessageType.CON) {
                            // use the same ID if continuing from CON to CON
                            next.ID = response.ID;
                        }

                        // Convert all notification retransmissions to CON
                        if (next.Type != MessageType.CON) {
                            next.Type = MessageType.CON;
                            PrepareSelfReplacement(nextLayer, exchange, next);
                        }
                        relation.CurrentControlNotification = next;
                        relation.NextControlNotification = null;
                        
                        // Create a new task for sending next response so that we can leave the sync-block
                        Executor.Start(() => base.SendResponse(nextLayer, exchange, next));
                    }
                }
            };

            response.TimedOut += (o, e) => {
                ObserveRelation relation = exchange.Relation; 
                log.Debug(m => m($"Notification {relation.Exchange.Request.TokenString} timed out. Cancel all relations with source {relation.Source}"));
                relation.CancelAll();
            };
        }

        private void PrepareTimeout(INextLayer nextLayer, Exchange exchange, Response response)
        {
            log.Debug(m => m($"PrepareTimeout - for response {response}"));
            response.TimedOut += (o, e) => {
                lock (exchange) {
                    ObserveRelation relation = exchange.Relation;
                    log.Debug(m => m($"Notification {relation.Exchange.Request.TokenString} timed out."));

                    Response next = relation.NextControlNotification;
                    if (next != null) {
                        log.Debug("The notification has timed out and there is a fresher notification for the retransmission.");

                        // don't use the same ID
                        // next.ID = response.ID;
                        next.ID = Message.None;

                        relation.CurrentControlNotification = next;
                        relation.NextControlNotification = null;

                        // Create a new task for sending next response so that we can leave the sync-block
                        Executor.Start(() => base.SendResponse(nextLayer, exchange, next));
                    }
                }
            };
        }

        private void PrepareReregistration(Exchange exchange, Response response, Action<Request> reregister)
        {

            long timeout = response.MaxAge * 1000 + _backOff + random.Next(2, 15) * 1000;
            ReregistrationContext ctx = exchange.GetOrAdd<ReregistrationContext>(
                reregistrationContextKey, _ => new ReregistrationContext(exchange, timeout, reregister));

            log.Debug(m => m("Scheduling re-registration in " + timeout + "ms for " + exchange.Request));

            ctx.Restart();
        }

        class ReregistrationContext : IDisposable
        {
            private readonly Exchange _exchange;
            private readonly Action<Request> _reregister;
            private readonly Timer _timer;

            public ReregistrationContext(Exchange exchange, long timeout, Action<Request> reregister)
            {
                _exchange = exchange;
                _reregister = reregister;
                _timer = new Timer(timeout) {
                    AutoReset = false
                };
                _timer.Elapsed += timer_Elapsed;
            }

            public void Start()
            {
                _timer.Start();
            }

            public void Restart()
            {
                Stop();
                Start();
            }

            public void Stop()
            {
                _timer.Stop();
            }

            public void Cancel()
            {
                Stop();
                Dispose();
            }

            public void Dispose()
            {
                _timer.Dispose();
            }

            void timer_Elapsed(object sender, ElapsedEventArgs e)
            {
                Request request = _exchange.Request;
                //  Make sure that we really want to do the re-registration
                if (request.ObserveRelation != null && !request.ObserveRelation.Canceled && request.ObserveRelation.Reconnect) {
                    Request refresh = new Request(request.Method);

                    refresh.SetOptions(request.GetOptions());
                    // make sure Observe is set and zero
                    refresh.MarkObserve();

                    // use same Token
                    refresh.Token = request.Token;
                    refresh.Destination = request.Destination;
                    refresh.CopyEventHandler(request);
                    refresh.ObserveRelation = request.ObserveRelation;
                    log.Debug(m => m( "Re-registering for " + request));
                    request.FireReregister(refresh);
                    if (!refresh.IsCancelled) {
                        _reregister(refresh);
                    }
                }
                else { 
                    log.Debug(m => m("Dropping re-registration for canceled " + request));
                }
            }
        }
    }
}
