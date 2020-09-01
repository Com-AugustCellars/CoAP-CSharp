﻿/*
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
#if NETSTANDARD1_3 == false
using System.Diagnostics.Contracts;
#endif
using Com.AugustCellars.CoAP.Deduplication;
using Com.AugustCellars.CoAP.Log;
using Com.AugustCellars.CoAP.Observe;
using Com.AugustCellars.CoAP.Util;

using Org.BouncyCastle.Security;

namespace Com.AugustCellars.CoAP.Net
{
    public class Matcher : IMatcher, IDisposable
    {
        static readonly ILogger _Log = LogManager.GetLogger(typeof(Matcher));

        /// <summary>
        /// for all
        /// </summary>
        readonly IDictionary<Exchange.KeyID, Exchange> _exchangesByID
            = new ConcurrentDictionary<Exchange.KeyID, Exchange>();

        /// <summary>
        /// for outgoing
        /// </summary>
        readonly IDictionary<Exchange.KeyToken, Exchange> _exchangesByToken
            = new ConcurrentDictionary<Exchange.KeyToken, Exchange>();

        /// <summary>
        /// for blockwise
        /// </summary>
        readonly ConcurrentDictionary<Exchange.KeyUri, Exchange> _ongoingExchanges
            = new ConcurrentDictionary<Exchange.KeyUri, Exchange>();

        private Int32 _running;
        private Int32 _currentID;
        private IDeduplicator _deduplicator;
        private int _tokenLength;
        private SecureRandom _random = new SecureRandom();

        public Matcher(ICoapConfig config)
        {
            _deduplicator = DeduplicatorFactory.CreateDeduplicator(config);
            if (config.UseRandomIDStart) {
                _currentID = new Random().Next(1 << 16);
            }

            _tokenLength = config.TokenLength;

            config.PropertyChanged += PropertyChanged;
        }

        private void PropertyChanged(object obj, PropertyChangedEventArgs eventArgs)
        {
            if (eventArgs.PropertyName == "TokenLength") {
                ICoapConfig config = (ICoapConfig) obj;
                _tokenLength = config.TokenLength;
            }
        }

        /// <inheritdoc/>
        public void Start()
        {
            if (System.Threading.Interlocked.CompareExchange(ref _running, 1, 0) > 0) {
                return;
            }

            _deduplicator.Start();
        }

        /// <inheritdoc/>
        public void Stop()
        {
            if (System.Threading.Interlocked.Exchange(ref _running, 0) == 0) {
                return;
            }

            _deduplicator.Stop();
            Clear();
        }

        /// <inheritdoc/>
        public void Clear()
        {
            _exchangesByID.Clear();
            _exchangesByToken.Clear();
            _ongoingExchanges.Clear();
            _deduplicator.Clear();
        }

        /// <inheritdoc/>
        public void SendRequest(Exchange exchange, Request request)
        {
            if (request.ID == Message.None) {
                request.ID = System.Threading.Interlocked.Increment(ref _currentID) % (1 << 16);
            }

            /*
             * The request is a CON or NON and must be prepared for these responses
             * - CON => ACK / RST / ACK+response / CON+response / NON+response
             * - NON => RST / CON+response / NON+response
             * If this request goes lost, we do not get anything back.
             */

            // the MID is from the local namespace -- use blank address
            Exchange.KeyID keyID = new Exchange.KeyID(request.ID, null, request.Session);

            //  If we do not have a token, then create one
            Exchange.KeyToken keyToken;
            if (request.Token == null) {
                int length = _tokenLength;
                if (_tokenLength < 0) {
                    length = _random.Next(8);
                }

                byte[] token = new byte[length];
                int tries = 0;

                do {
                    if ((length < 8) && (tries > length * 5 + 1)) {
                        length += 1;
                        tries = 0;
                        token = new byte[length];
                    }

                    _random.NextBytes(token);
                    keyToken = new Exchange.KeyToken(token);
                } while (_exchangesByToken.ContainsKey(keyToken));

                request.Token = token;
            }
            else {
                keyToken = new Exchange.KeyToken(request.Token);
            }

            exchange.Completed += OnExchangeCompleted;

            _Log.Debug(m => m("Stored open request by {0}, {1}", keyID, keyToken));

            _exchangesByID[keyID] = exchange;
            _exchangesByToken[keyToken] = exchange;
        }

        /// <inheritdoc/>
        public void SendResponse(Exchange exchange, Response response)
        {
            if (response.ID == Message.None)
                response.ID = System.Threading.Interlocked.Increment(ref _currentID) % (1 << 16);

            /*
             * The response is a CON or NON or ACK and must be prepared for these
             * - CON => ACK / RST // we only care to stop retransmission
             * - NON => RST // we only care for observe
             * - ACK => nothing!
             * If this response goes lost, we must be prepared to get the same 
             * CON/NON request with same MID again. We then find the corresponding
             * exchange and the ReliabilityLayer resends this response.
             */

            // Blockwise transfers are identified by URI and remote endpoint
            if (response.HasOption(OptionType.Block2)) {
                Request request = exchange.CurrentRequest;

                Exchange.KeyUri keyUri = new Exchange.KeyUri(request, response.Destination);

                // Observe notifications only send the first block, hence do not store them as ongoing
                if (exchange.ResponseBlockStatus != null && !response.HasOption(OptionType.Observe)) {
                    // Remember ongoing blockwise GET requests
                    if (Utils.Put(_ongoingExchanges, keyUri, exchange) == null) {
                        if (_Log.IsDebugEnabled)
                            _Log.Debug("Ongoing Block2 started late, storing " + keyUri + " for " + request);
                    }
                    else {
                        if (_Log.IsDebugEnabled)
                            _Log.Debug("Ongoing Block2 continued, storing " + keyUri + " for " + request);
                    }
                }
                else {
                    if (_Log.IsDebugEnabled)
                        _Log.Debug("Ongoing Block2 completed, cleaning up " + keyUri + " for " + request);
                    Exchange exc;
                    _ongoingExchanges.TryRemove(keyUri, out exc);
                }
            }

            // Insert CON and NON to match ACKs and RSTs to the exchange
            // Do not insert ACKs and RSTs.
            if (response.Type == MessageType.CON || response.Type == MessageType.NON) {
                Exchange.KeyID keyID = new Exchange.KeyID(response.ID, null, response.Session);
                _exchangesByID[keyID] = exchange;
            }

            // Only CONs and Observe keep the exchange active
            if (response.Type != MessageType.CON && response.Last) {
                exchange.Complete = true;
            }
        }

        /// <inheritdoc/>
        public void SendEmptyMessage(Exchange exchange, EmptyMessage message)
        {
            if (message.Type == MessageType.RST && exchange != null) {
                // We have rejected the request or response
                exchange.Complete = true;
            }
        }

        /// <inheritdoc/>
        public Exchange ReceiveRequest(Request request)
        {
            /*
		     * This request could be
		     *  - Complete origin request => deliver with new exchange
		     *  - One origin block        => deliver with ongoing exchange
		     *  - Complete duplicate request or one duplicate block (because client got no ACK) 
		     *      =>
		     * 		if ACK got lost => resend ACK
		     * 		if ACK+response got lost => resend ACK+response
		     * 		if nothing has been sent yet => do nothing
		     * (Retransmission is supposed to be done by the retransm. layer)
		     */

            Exchange.KeyID keyId = new Exchange.KeyID(request.ID, request.Source, request.Session);

            /*
             * The differentiation between the case where there is a Block1 or
             * Block2 option and the case where there is none has the advantage that
             * all exchanges that do not need blockwise transfer have simpler and
             * faster code than exchanges with blockwise transfer.
             */
            if (!request.HasOption(OptionType.Block1) && !request.HasOption(OptionType.Block2)) {
                Exchange exchange = new Exchange(request, Origin.Remote);
                Exchange previous = _deduplicator.FindPrevious(keyId, exchange);
                if (previous == null) {
                    exchange.Completed += OnExchangeCompleted;
                    return exchange;
                }
                else {
                    if (_Log.IsInfoEnabled) {
                        _Log.Info("Duplicate request: " + request);
                    }

                    request.Duplicate = true;
                    return previous;
                }
            }
            else {
                Exchange.KeyUri keyUri = new Exchange.KeyUri(request, request.Source);

                if (_Log.IsDebugEnabled) {
                    _Log.Debug("Looking up ongoing exchange for " + keyUri);
                }

                Exchange ongoing;
                if (_ongoingExchanges.TryGetValue(keyUri, out ongoing)) {
                    Exchange prev = _deduplicator.FindPrevious(keyId, ongoing);
                    if (prev != null) {
                        if (_Log.IsInfoEnabled) {
                            _Log.Info("Duplicate ongoing request: " + request);
                        }

                        request.Duplicate = true;
                    }
                    else {
                        // the exchange is continuing, we can (i.e., must) clean up the previous response
                        if (ongoing.CurrentResponse.Type != MessageType.ACK && !ongoing.CurrentResponse.HasOption(OptionType.Observe)) {
                            keyId = new Exchange.KeyID(ongoing.CurrentResponse.ID, null, ongoing.CurrentResponse.Session);
                            if (_Log.IsDebugEnabled) {
                                _Log.Debug("Ongoing exchange got new request, cleaning up " + keyId);
                            }

                            _exchangesByID.Remove(keyId);
                        }
                    }

                    return ongoing;
                }
                else {
                    // We have no ongoing exchange for that request block. 
                    /*
                     * Note the difficulty of the following code: The first message
                     * of a blockwise transfer might arrive twice due to a
                     * retransmission. The new Exchange must be inserted in both the
                     * hash map 'ongoing' and the deduplicator. They must agree on
                     * which exchange they store!
                     */

                    Exchange exchange = new Exchange(request, Origin.Remote);
                    Exchange previous = _deduplicator.FindPrevious(keyId, exchange);
                    if (previous == null) {
                        if (_Log.IsDebugEnabled) {
                            _Log.Debug("New ongoing request, storing " + keyUri + " for " + request);
                        }

                        exchange.Completed += OnExchangeCompleted;
                        _ongoingExchanges[keyUri] = exchange;
                        return exchange;
                    }
                    else {
                        if (_Log.IsInfoEnabled) {
                            _Log.Info("Duplicate initial request: " + request);
                        }

                        request.Duplicate = true;
                        return previous;
                    }
                } // if ongoing
            } // if blockwise
        }

        /// <inheritdoc/>
        public Exchange ReceiveResponse(Response response)
        {
            /*
		     * This response could be
		     * - The first CON/NON/ACK+response => deliver
		     * - Retransmitted CON (because client got no ACK)
		     * 		=> resend ACK
		     */

            Exchange.KeyID keyId;
            if (response.Type == MessageType.ACK) {
                // own namespace
                keyId = new Exchange.KeyID(response.ID, null, response.Session);
            }
            else {
                // remote namespace
                keyId = new Exchange.KeyID(response.ID, response.Source, response.Session);
            }

            Exchange.KeyToken keyToken = new Exchange.KeyToken(response.Token);

            Exchange exchange;
            if (_exchangesByToken.TryGetValue(keyToken, out exchange)) {
                //  We need to play games if this is multicast
                if (exchange.CurrentRequest.IsMulticast) {
                    Exchange newExchange = new Exchange(exchange) {
                        Request = exchange.Request
                    };
                    exchange = newExchange;
                }

                // There is an exchange with the given token
                Exchange prev = _deduplicator.FindPrevious(keyId, exchange);
                if (prev != null) {
                    // (and thus it holds: prev == exchange)
                    _Log.Info(m => m($"Duplicate response for open exchange: {response}"));
                    response.Duplicate = true;
                }
                else {
                    keyId = new Exchange.KeyID(exchange.CurrentRequest.ID, null, response.Session);
                    _Log.Debug(m => m($"Exchange got response: Cleaning up {keyId}"));
                    _exchangesByID.Remove(keyId);
                }

                if (response.Type == MessageType.ACK && exchange.CurrentRequest.ID != response.ID) {
                    // The token matches but not the MID. This is a response for an older exchange
                    _Log.Warn(m => m($"Possible MID reuse before lifetime end: {response.TokenString} expected MID {exchange.CurrentRequest.ID} but received {response.ID}"));
                }

                return exchange;
            }
            else {
                // There is no exchange with the given token.
                if (response.Type != MessageType.ACK) {
                    // only act upon separate responses
                    Exchange prev = _deduplicator.Find(keyId);
                    if (prev != null) {
                        _Log.Info(m => m($"Duplicate response for completed exchange: {response}"));
                        response.Duplicate = true;
                        return prev;
                    }
                }
                else {
                    _Log.Info(m => m($"Ignoring unmatchable piggy-backed response from {response.Source}: {response}"));
                }

                // ignore response
                return null;
            }
        }

        /// <inheritdoc/>
        public Exchange ReceiveEmptyMessage(EmptyMessage message)
        {
            // local namespace
            Exchange.KeyID keyID = new Exchange.KeyID(message.ID, null, null);
            Exchange exchange;
            if (_exchangesByID.TryGetValue(keyID, out exchange)) {
                if (_Log.IsDebugEnabled)
                    _Log.Debug("Exchange got reply: Cleaning up " + keyID);
                _exchangesByID.Remove(keyID);
                return exchange;
            }
            else {
                if (_Log.IsInfoEnabled)
                    _Log.Info("Ignoring unmatchable empty message from " + message.Source + ": " + message);
                return null;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            IDisposable d = _deduplicator as IDisposable;
            if (d != null)
                d.Dispose();
        }

        private void OnExchangeCompleted(Object sender, EventArgs e)
        {
            Exchange exchange = (Exchange) sender;

            /* 
			 * Logging in this method leads to significant performance loss.
			 * Uncomment logging code only for debugging purposes.
			 */

            if (exchange.Origin == Origin.Local) {
                // this endpoint created the Exchange by issuing a request
                Exchange.KeyID keyID = new Exchange.KeyID(exchange.CurrentRequest.ID, null, null);
                Exchange.KeyToken keyToken = new Exchange.KeyToken(exchange.CurrentRequest.Token);

                if (_Log.IsDebugEnabled)
                    _Log.Debug("Exchange completed: Cleaning up " + keyToken);

                _exchangesByToken.Remove(keyToken);
                // in case an empty ACK was lost
                _exchangesByID.Remove(keyID);
            }
            else // Origin.Remote
            {
                // this endpoint created the Exchange to respond a request

                Response response = exchange.CurrentResponse;
                if (response == null) {
                    response = exchange.Response;
                }
                if (response != null && response.Type != MessageType.ACK) {
                    // only response MIDs are stored for ACK and RST, no reponse Tokens
                    Exchange.KeyID midKey = new Exchange.KeyID(response.ID, null, response.Session);
                    //if (log.IsDebugEnabled)
                    //    log.Debug("Remote ongoing completed, cleaning up " + midKey);
                    _exchangesByID.Remove(midKey);
                }

                Request request = exchange.CurrentRequest;
                if (request != null && (request.HasOption(OptionType.Block1) || response.HasOption(OptionType.Block2))) {

                    Exchange.KeyUri uriKey = new Exchange.KeyUri(request, request.Source);

                    _Log.Debug(m => m($"Remote ongoing completed, cleaning up {uriKey}"));

                    Exchange exc;
                    _ongoingExchanges.TryRemove(uriKey, out exc);
                }
            }
        }
    }
}
