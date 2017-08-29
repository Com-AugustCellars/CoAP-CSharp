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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
#if LOG_SWEEP_DEDUPLICATOR
using Com.AugustCellars.CoAP.Log;
#endif
using Com.AugustCellars.CoAP.Net;

namespace Com.AugustCellars.CoAP.Deduplication
{
    class SweepDeduplicator : IDeduplicator
    {
#if LOG_SWEEP_DEDUPLICATOR
        static readonly ILogger _Log = LogManager.GetLogger(typeof(SweepDeduplicator));
#endif

        private readonly ConcurrentDictionary<Exchange.KeyID, Exchange> _incommingMessages
            = new ConcurrentDictionary<Exchange.KeyID, Exchange>();
        private Timer _timer;
        private readonly ICoapConfig _config;

        public SweepDeduplicator(ICoapConfig config)
        {
            _config = config;
            Start();
        }

        private static void Sweep(Object sender)
        {
            SweepDeduplicator me = (SweepDeduplicator) sender;
#if LOG_SWEEP_DEDUPLICATOR
            log.Debug(m => m("Start Mark-And-Sweep with {0} entries", _incommingMessages.Count));
#endif

            DateTime oldestAllowed = DateTime.Now.AddMilliseconds(-me._config.ExchangeLifetime);
            List<Exchange.KeyID> keysToRemove = new List<Exchange.KeyID>();
            foreach (KeyValuePair<Exchange.KeyID, Exchange> pair in me._incommingMessages) {
                if (pair.Value.Timestamp < oldestAllowed) {
#if LOG_SWEEP_DEDUPLICATOR
                    log.Debug(m => m("Mark-And-Sweep removes {0}", pair.Key));
#endif
                    keysToRemove.Add(pair.Key);
                }
            }
            if (keysToRemove.Count > 0) {
                Exchange ex;
                foreach (Exchange.KeyID key in keysToRemove) {
                    me._incommingMessages.TryRemove(key, out ex);
                }
            }
        }

        /// <inheritdoc/>
        public void Start()
        {
            if (_timer == null) {
                _timer = new Timer(Sweep, this, _config.MarkAndSweepInterval, _config.MarkAndSweepInterval);
            }
        }

        /// <inheritdoc/>
        public void Stop()
        {
            _timer.Dispose();
            _timer = null;
            Clear();
        }

        /// <inheritdoc/>
        public void Clear()
        {
            _incommingMessages.Clear();
        }

        /// <inheritdoc/>
        public Exchange FindPrevious(Exchange.KeyID key, Exchange exchange)
        {
            Exchange prev = null;
            _incommingMessages.AddOrUpdate(key, exchange, (k, v) =>
            {
                prev = v;
                return exchange;
            });
            return prev;
        }

        /// <inheritdoc/>
        public Exchange Find(Exchange.KeyID key)
        {
            Exchange prev;
            _incommingMessages.TryGetValue(key, out prev);
            return prev;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _timer.Dispose();
            _timer = null;
        }
    }
}
