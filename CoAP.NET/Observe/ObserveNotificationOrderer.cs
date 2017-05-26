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
using System.Threading;

namespace Com.AugustCellars.CoAP.Observe
{
    /// <summary>
    /// This class holds the state of an observe relation such
    /// as the timeout of the last notification and the current number.
    /// </summary>
    public class ObserveNotificationOrderer
    {
        private readonly ICoapConfig _config;
        private Int32 _number;

        public ObserveNotificationOrderer()
            : this(null)
        { }

        public ObserveNotificationOrderer(ICoapConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Gets a new observe option number.
        /// </summary>
        /// <returns>a new observe option number</returns>
        public Int32 GetNextObserveNumber()
        {
            Int32 next = Interlocked.Increment(ref _number);
            while (next >= 1 << 24) {
                Interlocked.CompareExchange(ref _number, 0, next);
                next = Interlocked.Increment(ref _number);
            }
            return next;
        }

        /// <summary>
        /// Gets the current notification number.
        /// </summary>
        public Int32 Current
        {
            get => _number;
        }

        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Is this the most recent response that we have seen for this observe relation?
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public Boolean IsNew(Response response)
        {
            Int32? obs = response.Observe;
            if (!obs.HasValue) {
                // this is a final response, e.g., error or proactive cancellation
                return true;
            }

            // Multiple responses with different notification numbers might
            // arrive and be processed by different threads. We have to
            // ensure that only the most fresh one is being delivered.
            // We use the notation from the observe draft-08.
            DateTime t1 = Timestamp;
            DateTime t2 = DateTime.Now;
            Int32 v1 = Current;
            Int32 v2 = obs.Value;
            Int64 notifMaxAge = (_config ?? CoapConfig.Default).NotificationMaxAge;
            if ((v1 < v2) && (v2 - v1 < 1 << 23)
                    || (v1 > v2) && (v1 - v2 > 1 << 23)
                    || (t2 > t1.AddMilliseconds(notifMaxAge))) {
                Timestamp = t2;
                _number = v2;
                return true;
            }
            else {
                return false;
            }
        }
    }
}
