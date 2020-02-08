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
// ReSharper disable InconsistentNaming

namespace Com.AugustCellars.CoAP
{
    /// <summary>
    /// Provides configuration for CoAP communication.
    /// </summary>
    public interface ICoapConfig : System.ComponentModel.INotifyPropertyChanged
    {
        /// <summary>
        /// Gets the version of CoAP protocol.
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Gets the default CoAP port for normal CoAP communication (not secure).
        /// </summary>
        int DefaultPort { get; }

        /// <summary>
        /// Gets the default CoAP port for secure CoAP communication (coaps).
        /// </summary>
        int DefaultSecurePort { get; }

        /// <summary>
        /// Gets the port which HTTP proxy is on.
        /// </summary>
        int HttpPort { get; }

        /// <summary>
        /// Input to computing the resend intervolt
        /// </summary>
        int AckTimeout { get; }

        /// <summary>
        /// Input to computing the resend intervolt
        /// </summary>
        double AckRandomFactor { get; }

        /// <summary>
        /// Input to cmputing the resend intervolt
        /// </summary>
        double AckTimeoutScale { get; }

        /// <summary>
        /// Maximum number of times that a message is resent
        /// </summary>
        int MaxRetransmit { get; }

        /// <summary>
        /// Size of message to start blocking at
        /// </summary>
        int MaxMessageSize { get; }

        /// <summary>
        /// Gets the default preferred size of block in blockwise transfer.
        /// </summary>
        int DefaultBlockSize { get; }

        /// <summary>
        /// Time to key a blockwise transfer active
        /// </summary>
        int BlockwiseStatusLifetime { get; }

        /// <summary>
        /// Use random number for ID starting point
        /// </summary>
        // ReSharper disable once InconsistentNaming
        bool UseRandomIDStart { get; }

        /// <summary>
        /// Obsolete item
        /// </summary>
        [Obsolete("Tokens are always generated randommly.  Will be removed in the future")]
        bool UseRandomTokenStart { get; }

        /// <summary>
        /// What should the token length be?
        /// </summary>
        int TokenLength { get; }
        
        /// <summary>
        /// Input to determining if a notification is fresh
        /// </summary>
        long NotificationMaxAge { get; }

        /// <summary>
        /// Input on when to refresh a stale notification
        /// </summary>
        long NotificationCheckIntervalTime { get; }

        /// <summary>
        /// Input on when to refresh a stale notification
        /// </summary>
        int NotificationCheckIntervalCount { get; }

        /// <summary>
        /// Input on when to refresh a stale notification
        /// </summary>
        int NotificationReregistrationBackoff { get; }

        /// <summary>
        /// Which deduplicator is used
        /// </summary>
        string Deduplicator { get; }

        /// <summary>
        /// Time that buckets are rotated when using the crop rotation deduplicator
        /// </summary>
        int CropRotationPeriod { get; }

        /// <summary>
        /// Length to keep an exchange around for the sweep deduplicator
        /// </summary>
        int ExchangeLifetime { get; }

        /// <summary>
        /// Frequency to sweep for the sweep deduplicator
        /// </summary>
        long MarkAndSweepInterval { get; }

        /// <summary>
        /// Size of receive buffer if greater than 0
        /// For UDP based buffers
        /// </summary>
        int ChannelReceiveBufferSize { get; }

        /// <summary>
        /// Size of send buffer if greater than 0
        /// For UDP based buffers
        /// </summary>
        int ChannelSendBufferSize { get; }

        /// <summary>
        /// Size of packet receive buffer
        /// For UDP based buffers
        /// </summary>
        int ChannelReceivePacketSize { get; }

        /// <summary>
        /// Size of message to start blocking at
        /// </summary>
        Int32 OSCOAP_MaxMessageSize { get; }

        /// <summary>
        /// Gets the default preferred size of block in blockwise transfer.
        /// </summary>
        Int32 OSCOAP_DefaultBlockSize { get; }

        /// <summary>
        /// Time to key a blockwise transfer active
        /// </summary>
        Int32 OSCOAP_BlockwiseStatusLifetime { get; }

        /// <summary>
        /// Size of the window for preventing replays
        /// </summary>
        bool OSCOAP_ReplayWindow { get; }

        /// <summary>
        /// Loads configuration from a config properties file.
        /// </summary>
        void Load(string configFile);

        /// <summary>
        /// Stores the configuration in a config properties file.
        /// </summary>
        void Store(string configFile);

        /// <summary>
        /// Get a value from the configuration object
        /// </summary>
        /// <param name="key">key for the value</param>
        /// <param name="defaultValue">default if no value present</param>
        /// <returns>configuration value</returns>
        bool GetBool(string key, bool defaultValue);

        /// <summary>
        /// Get a value from the configuration object
        /// </summary>
        /// <param name="key">key for the value</param>
        /// <param name="defaultValue">default if no value present</param>
        /// <returns>configuration value</returns>
        double GetDouble(string key, double defaultValue);

        /// <summary>
        /// Get a value from the configuration object
        /// </summary>
        /// <param name="key">key for the value</param>
        /// <param name="defaultValue">default if no value present</param>
        /// <returns>configuration value</returns>
        int GetInt(string key, int defaultValue);

        /// <summary>
        /// Get a value from the configuration object
        /// </summary>
        /// <param name="key">key for the value</param>
        /// <param name="defaultValue">default if no value present</param>
        /// <returns>configuration value</returns>
        long GetInt64(string key, long defaultValue);

        /// <summary>
        /// Get a value from the configuration object
        /// </summary>
        /// <param name="key">key for the value</param>
        /// <param name="defaultValue">default if no value present</param>
        /// <returns>configuration value</returns>
        string GetString(string key, string defaultValue);

    }
}
