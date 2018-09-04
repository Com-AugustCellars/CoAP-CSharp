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
#if NETSTANDARD1_3 == false
using System.Timers;
#else
using System.Threading;
#endif
using Com.AugustCellars.CoAP.Log;
using Com.AugustCellars.CoAP.Net;

namespace Com.AugustCellars.CoAP.Deduplication
{

    /// <summary>
    /// Crop Rotation De-duplicator: 
    /// </summary>
    internal class CropRotation : IDeduplicator, IDisposable
    {
        private readonly ConcurrentDictionary<Exchange.KeyID, Exchange>[] _maps;
        private Int32 _first;
        private Int32 _second;
        private Timer _timer;
#if NETSTANDARD1_3
        private int _period;
#endif

        public CropRotation(ICoapConfig config)
        {
            _maps = new ConcurrentDictionary<Exchange.KeyID, Exchange>[3];
            _maps[0] = new ConcurrentDictionary<Exchange.KeyID, Exchange>();
            _maps[1] = new ConcurrentDictionary<Exchange.KeyID, Exchange>();
            _maps[2] = new ConcurrentDictionary<Exchange.KeyID, Exchange>();
            _first = 0;
            _second = 1;
#if NETSTANDARD1_3
            _period = config.CropRotationPeriod;
#else
            _timer = new Timer(config.CropRotationPeriod);
            _timer.Elapsed += Rotation;
#endif
        }

#if NETSTANDARD1_3
        private static void Rotation(Object obj)
        {
            CropRotation sender = obj as CropRotation;
            Int32 third = sender._first;
            sender._first = sender._second;
            sender._second = (sender._second + 1) % 3;
            sender._maps[third].Clear();
        }
#else
        private void Rotation(Object sender, ElapsedEventArgs e)
        {
            Int32 third = _first;
            _first = _second;
            _second = (_second + 1) % 3;
            _maps[third].Clear();
        }
#endif

        /// <inheritdoc/>
        public void Start()
        {
#if NETSTANDARD1_3
            _timer = new Timer(Rotation, this, _period, _period);
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
            _maps[0].Clear();
            _maps[1].Clear();
            _maps[2].Clear();
        }

        /// <inheritdoc/>
        public Exchange FindPrevious(Exchange.KeyID key, Exchange exchange)
        {
            Int32 f = _first, s = _second;
            Exchange prev = null;
            
            _maps[f].AddOrUpdate(key, exchange, (k, v) =>
            {
                prev = v;
                return exchange;
            });
            if (prev != null || f == s)
                return prev;

            prev = _maps[s].AddOrUpdate(key, exchange, (k, v) =>
            {
                prev = v;
                return exchange;
            });
            return prev;
        }

        /// <inheritdoc/>
        public Exchange Find(Exchange.KeyID key)
        {
            Int32 f = _first, s = _second;
            Exchange prev;
            if (_maps[f].TryGetValue(key, out prev) || f == s) {
                return prev;
            }

            _maps[s].TryGetValue(key, out prev);
            return prev;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}
