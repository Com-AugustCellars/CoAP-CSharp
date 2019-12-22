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
#if NETSTANDARD1_3
using System.Threading;
#else
using System.Timers;
#endif
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
        // private int _period;

        public SweepDeduplicator(ICoapConfig config)
        {
            _config = config;
#if NETSTANDARD1_3
            _period = (int) config.MarkAndSweepInterval;
#else
            _timer = new Timer(config.MarkAndSweepInterval);
            _timer.Elapsed += Sweep;
#endif
        }

#if NETSTANDARD1_3
        private static void Sweep(Object obj)
#else
        private void Sweep(Object obj, ElapsedEventArgs e)
#endif
        {
#if NETSTANDARD1_3
            SweepDeduplicator sender = obj as SweepDeduplicator;
#else
            SweepDeduplicator sender = this;
#endif
#if LOG_SWEEP_DEDUPLICATOR
            log.Debug(m => m("Start Mark-And-Sweep with {0} entries", _incommingMessages.Count));
#endif

            DateTime oldestAllowed = DateTime.Now.AddMilliseconds(-sender._config.ExchangeLifetime);
            List<Exchange.KeyID> keysToRemove = new List<Exchange.KeyID>();
            foreach (KeyValuePair<Exchange.KeyID, Exchange> pair in sender._incommingMessages) {
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
                    sender._incommingMessages.TryRemove(key, out ex);
                }
            }
        }

        /// <inheritdoc/>
        public void Start()
        {
#if NETSTANDARD1_3
            _timer = new Timer(Sweep, this, _period, _period);
#else
            _timer.Start();
#endif
        }

        /// <inheritdoc/>
        public void Stop()
        {
#if NETSTANDARD1_3
            _timer.Dispose();
            _timer = null;
#else
            _timer.Stop();
#endif
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
