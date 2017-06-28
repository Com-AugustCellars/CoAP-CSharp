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
        String Version { get; }

        /// <summary>
        /// Gets the default CoAP port for normal CoAP communication (not secure).
        /// </summary>
        Int32 DefaultPort { get; }

        /// <summary>
        /// Gets the default CoAP port for secure CoAP communication (coaps).
        /// </summary>
        Int32 DefaultSecurePort { get; }

        /// <summary>
        /// Gets the port which HTTP proxy is on.
        /// </summary>
        Int32 HttpPort { get; }

        /// <summary>
        /// Input to computing the resend intervolt
        /// </summary>
        Int32 AckTimeout { get; }

        /// <summary>
        /// Input to computing the resend intervolt
        /// </summary>
        Double AckRandomFactor { get; }

        /// <summary>
        /// Input to cmputing the resend intervolt
        /// </summary>
        Double AckTimeoutScale { get; }

        /// <summary>
        /// Maximum number of times that a message is resent
        /// </summary>
        Int32 MaxRetransmit { get; }

        /// <summary>
        /// Size of message to start blocking at
        /// </summary>
        Int32 MaxMessageSize { get; }

        /// <summary>
        /// Gets the default preferred size of block in blockwise transfer.
        /// </summary>
        Int32 DefaultBlockSize { get; }

        /// <summary>
        /// Time to key a blockwise transfer active
        /// </summary>
        Int32 BlockwiseStatusLifetime { get; }

        /// <summary>
        /// Use random number for ID starting point
        /// </summary>
        // ReSharper disable once InconsistentNaming
        Boolean UseRandomIDStart { get; }

        /// <summary>
        /// Obsolete item
        /// </summary>
        [Obsolete("Tokens are always generated randommly.  Will be removed in the future")]
        Boolean UseRandomTokenStart { get; }

        /// <summary>
        /// What should the token length be?
        /// </summary>
        int TokenLength { get; }
        
        /// <summary>
        /// Input to determining if a notification is fresh
        /// </summary>
        Int64 NotificationMaxAge { get; }

        /// <summary>
        /// Input on when to refresh a stale notification
        /// </summary>
        Int64 NotificationCheckIntervalTime { get; }

        /// <summary>
        /// Input on when to refresh a stale notification
        /// </summary>
        Int32 NotificationCheckIntervalCount { get; }

        /// <summary>
        /// Input on when to refresh a stale notification
        /// </summary>
        Int32 NotificationReregistrationBackoff { get; }

        /// <summary>
        /// Which deduplicator is used
        /// </summary>
        String Deduplicator { get; }

        /// <summary>
        /// Time that buckets are rotated when using the crop rotation deduplicator
        /// </summary>
        Int32 CropRotationPeriod { get; }

        /// <summary>
        /// Length to keep an exchange around for the sweep deduplicator
        /// </summary>
        Int32 ExchangeLifetime { get; }

        /// <summary>
        /// Frequency to sweep for the sweep deduplicator
        /// </summary>
        Int64 MarkAndSweepInterval { get; }

        /// <summary>
        /// Size of receive buffer if greater than 0
        /// For UDP based buffers
        /// </summary>
        Int32 ChannelReceiveBufferSize { get; }

        /// <summary>
        /// Size of send buffer if greater than 0
        /// For UDP based buffers
        /// </summary>
        Int32 ChannelSendBufferSize { get; }

        /// <summary>
        /// Size of packet receive buffer
        /// For UDP based buffers
        /// </summary>
        Int32 ChannelReceivePacketSize { get; }

#if INCLUDE_OSCOAP

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
#endif

        /// <summary>
        /// Loads configuration from a config properties file.
        /// </summary>
        void Load(String configFile);

        /// <summary>
        /// Stores the configuration in a config properties file.
        /// </summary>
        void Store(String configFile);

        /// <summary>
        /// Get a value from the configuration object
        /// </summary>
        /// <param name="key">key for the value</param>
        /// <param name="defaultValue">default if no value present</param>
        /// <returns>configuration value</returns>
        bool GetBool(String key, bool defaultValue);

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
        int GetInt(String key, int defaultValue);

        /// <summary>
        /// Get a value from the configuration object
        /// </summary>
        /// <param name="key">key for the value</param>
        /// <param name="defaultValue">default if no value present</param>
        /// <returns>configuration value</returns>
        Int64 GetInt64(String key, long defaultValue);

        /// <summary>
        /// Get a value from the configuration object
        /// </summary>
        /// <param name="key">key for the value</param>
        /// <param name="defaultValue">default if no value present</param>
        /// <returns>configuration value</returns>
        string GetString(string key, string defaultValue);

    }
}
