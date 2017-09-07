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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;

namespace Com.AugustCellars.CoAP
{
    /// <summary>
    /// Default implementation of <see cref="ICoapConfig"/>.
    /// </summary>
    public class CoapConfig : ICoapConfig
    {


        private readonly NameValueCollection _values = new NameValueCollection(StringComparer.OrdinalIgnoreCase);

        private static ICoapConfig _Default;

        /// <summary>
        /// Return a standard default configuration for use
        /// </summary>
        public static ICoapConfig Default
        {
            get
            {
                if (_Default == null) {
                    lock (typeof(CoapConfig)) {
                        if (_Default == null) {
                            _Default = LoadConfig();
                        }
                    }
                }
                return _Default;
            }
        }

        private const int Default_HttpPort = 8080;
        private const double Default_AckTimeoutScale = 2D;
        private const int Default_MaxMessageSize = 1024;
        private const int Default_BlockwiseStatusLifetime = 10 * 60 * 1000; // ms
        private const bool Default_UseRandomIdStart = true;
        private const int Default_TokenLength = 4;
        private const string Default_Deduplicator = Deduplication.DeduplicatorFactory.MarkAndSweepDeduplicator;
        private const int Default_CropRotationPeriod = 2000; // ms
        private const int Default_ExchangeLifetime = 247 * 1000; // ms
        private const Int64 Default_MarkAndSweepInterval = 10 * 1000; // ms
        private const Int64 Default_NotificationMaxAge = 128 * 1000; // ms
        private const Int64 Default_NotificationCheckIntervalTime = 24 * 60 * 60 * 1000; // ms
        private const int Default_NotificationCheckIntervalCount = 100;
        private const int Default_NotificationReregistrationBackoff = 2000; // ms
        private const int Default_ChannelReceivePacketSize = 2048;
#if INCLUDE_OSCOAP
        private const int Default_Oscoap_MaxMessageSize = 1024;
        private const int Default_Oscoap_DefaultBlockSize = CoapConstants.DefaultBlockSize;
        private const int Default_Oscoap_BlockwiseStatusLifetime = 10 *60 * 1000; // ms
        private const bool Default_Oscoap_ReplayWindow = true;
#endif

        /// <inheritdoc/>
        public String Version
        {
            get => Spec.Name;
        }

        /// <inheritdoc/>
        public Int32 DefaultPort
        {
            get => GetInt("DefaultPort", CoapConstants.DefaultPort);
            set => SetValue("DefaultPort", value);
        }

        /// <inheritdoc/>
        public Int32 DefaultSecurePort
        {
            get => GetInt("DefaultSecurePort", CoapConstants.DefaultSecurePort);
            set => SetValue("DefaultSecurePort", value);
        }

        /// <inheritdoc/>
        public Int32 HttpPort
        {
            get => GetInt("HttpPort", Default_HttpPort);
            set => SetValue("HttpPort", value);
        }

        /// <inheritdoc/>
        public Int32 AckTimeout
        {
            get => GetInt("AckTimeout", CoapConstants.AckTimeout);
            set => SetValue("AckTimeout", value);
        }

        /// <inheritdoc/>
        public Double AckRandomFactor
        {
            get => GetDouble("AckRandomFactor", CoapConstants.AckRandomFactor);
            set => SetValue("AckRandomFactor", value);
        }

        /// <inheritdoc/>
        public Double AckTimeoutScale
        {
            get => GetDouble("AckTimeoutScale", Default_AckTimeoutScale);
            set => SetValue("AckTimeoutScale", value);
        }

        /// <inheritdoc/>
        public Int32 MaxRetransmit
        {
            get => GetInt("MaxRetransmit", CoapConstants.MaxRetransmit);
            set => SetValue("MaxRetransmit", value);
        }

        /// <inheritdoc/>
        public Int32 MaxMessageSize
        {
            get => GetInt("MaxMessageSize", Default_MaxMessageSize);
            set => SetValue("MaxMessageSize", value);
        }

        /// <inheritdoc/>
        public Int32 DefaultBlockSize
        {

            get => GetInt("DefaultBlockSize", CoapConstants.DefaultBlockSize);
            set => SetValue("DefaultBlockSize", value);
        }

        /// <inheritdoc/>
        public Int32 BlockwiseStatusLifetime
        {

            get => GetInt("BlockwiseStatusLifetime", Default_BlockwiseStatusLifetime);
            set => SetValue("BlockwiseStatusLifetime", value);
        }

        /// <inheritdoc/>
        public Boolean UseRandomIDStart
        {
            get => GetBool("UseRandomIDStart", Default_UseRandomIdStart);
            set => SetValue("UseRandomIDStart", value);
        }

        /// <inheritdoc/>
        public Boolean UseRandomTokenStart
        {
            get => GetBool("UseRandomTokenStart", true);
            set => SetValue("UseRandomTokenStart", value);
        }

        /// <inheritdoc />
        public int TokenLength
        {
            get => GetInt("TokenLength", Default_TokenLength);
            set
            {
                if (value < 0 || value > 8) throw new ArgumentOutOfRangeException();
                SetValue("TokenLength", value);
            }
        }

        /// <inheritdoc/>
        public String Deduplicator
        {
            get => GetString("Deduplicator", Default_Deduplicator);
            set => SetValue("Deduplicator", value);
        }

        /// <inheritdoc/>
        public Int32 CropRotationPeriod
        {
            get => GetInt("CropRotationPeriod", Default_CropRotationPeriod);
            set => SetValue("CropRotationPeriod", value);
        }

        /// <inheritdoc/>
        public Int32 ExchangeLifetime
        {
            get => GetInt("ExchangeLifetime", Default_ExchangeLifetime);
            set => SetValue("ExchangeLifetime", value);
        }

        /// <inheritdoc/>
        public Int64 MarkAndSweepInterval
        {
            get => GetInt64("MarkAndSweepInterval", Default_MarkAndSweepInterval);
            set => SetValue("MarkAndSweepInterval", value);
        }

        /// <inheritdoc/>
        public Int64 NotificationMaxAge
        {
            get => GetInt64("NotificationMaxAge", Default_NotificationMaxAge);
            set => SetValue("NotificationMaxAge", value);
        }

        /// <inheritdoc/>
        public Int64 NotificationCheckIntervalTime
        {
            get => GetInt64("NotificationCheckIntervalTime", Default_NotificationCheckIntervalTime);
            set => SetValue("NotificationCheckIntervalTime", value);
        }

        /// <inheritdoc/>
        public Int32 NotificationCheckIntervalCount
        {
            get => GetInt("NotificationCheckIntervalCount", Default_NotificationCheckIntervalCount);
            set => SetValue("NotificationCheckIntervalCount", value);
        }

        /// <inheritdoc/>
        public Int32 NotificationReregistrationBackoff
        {
            get => GetInt("NotificationReregistrationBackoff", Default_NotificationReregistrationBackoff);
            set => SetValue("NotificationReregistrationBackoff", value);
        }

        /// <inheritdoc/>
        public Int32 ChannelReceiveBufferSize
        {
            get => GetInt("ChannelReceiveBufferSize", 0);
            set => SetValue("ChannelReceiveBufferSize", value);
        }

        /// <inheritdoc/>
        public Int32 ChannelSendBufferSize
        {
            get => GetInt("ChannelSendBufferSize", 0);
            set => SetValue("ChannelSendBufferSize", value);
        }

        /// <inheritdoc/>
        public Int32 ChannelReceivePacketSize
        {
            get => GetInt("ChannelReceivePacketSize", Default_ChannelReceivePacketSize);
            set => SetValue("ChannelReceivePacketSize", value);
        }

#if INCLUDE_OSCOAP
        /// <inheritdoc/>
        public Int32 OSCOAP_MaxMessageSize
        {
            get => GetInt("OSCOAP_MaxMessageSize", Default_Oscoap_MaxMessageSize);
            set => SetValue("OSCOAP_MaxMessageSize", value);
        }

        /// <inheritdoc/>
        public Int32 OSCOAP_DefaultBlockSize
        {
            get => GetInt("OSCOAP_DefaultBlockSize", Default_Oscoap_DefaultBlockSize);
            set => SetValue("OSCOAP_DefaultBlockSize", value);
                    }

        /// <inheritdoc/>
        public Int32 OSCOAP_BlockwiseStatusLifetime
        {
            get => GetInt("OSCOAP_BlockwiseStatusLifetime", Default_Oscoap_BlockwiseStatusLifetime);
            set => SetValue("OSCOAP_BlockwiseStatusLifetime", value);
        }

        /// <inheritdoc/>
        public bool OSCOAP_ReplayWindow {
            get => GetBool("OSCOAP_ReplayWindow", Default_Oscoap_ReplayWindow);
            set => SetValue("OSCOAP_ReplayWindow", value);
        }
#endif

        /// <summary>
        /// Return the configuration value for a key
        /// </summary>
        /// <param name="valueName">Key to retrive</param>
        /// <returns>Value if one can be found</returns>
        public bool GetValue(string valueName)
        {
            bool x = GetBoolean(_values, valueName, null, false);
            return x;
        }

        /// <summary>
        /// Set a value in the configuration object for a key
        /// </summary>
        /// <param name="valueName">key to use for the value</param>
        /// <param name="newValue">value to be saved</param>
        public void SetValue(string valueName, string newValue)
        {
            string oldValue = _values[valueName];
            if (oldValue == null || oldValue != newValue) {
                _values[valueName] = newValue;
                NotifyPropertyChanged(valueName);
            }
        }

        /// <summary>
        /// Set a value in the configuration object for a key
        /// </summary>
        /// <param name="valueName">key to use for the value</param>
        /// <param name="newValue">value to be saved</param>
        public void SetValue(string valueName, bool newValue)
        {
            SetValue(valueName, newValue.ToString());
        }

        /// <summary>
        /// Set a value in the configuration object for a key
        /// </summary>
        /// <param name="valueName">key to use for the value</param>
        /// <param name="newValue">value to be saved</param>
        public void SetValue(string valueName, int newValue)
        {
            SetValue(valueName, newValue.ToString());
        }

        /// <summary>
        /// Set a value in the configuration object for a key
        /// </summary>
        /// <param name="valueName">key to use for the value</param>
        /// <param name="newValue">value to be saved</param>
        public void SetValue(string valueName, Int64 newValue)
        {
            SetValue(valueName, newValue.ToString());
        }

        /// <summary>
        /// Set a value in the configuration object for a key
        /// </summary>
        /// <param name="valueName">key to use for the value</param>
        /// <param name="newValue">value to be saved</param>
        public void SetValue(string valueName, double newValue)
        {
            string value = _values[valueName];
            if (value == null) {
                _values[valueName] = newValue.ToString(CultureInfo.InvariantCulture);
                NotifyPropertyChanged(valueName);
            }
            else {
                double oldValue;
                double.TryParse(value, out oldValue);
                if (Math.Abs(  newValue - oldValue) > double.Epsilon) {
                    _values[valueName] = newValue.ToString(CultureInfo.InvariantCulture);
                    NotifyPropertyChanged(valueName);
                }
            }
        }

        /// <inheritdoc/>
        public void Load(String configFile)
        {
            string[] lines = File.ReadAllLines(configFile);
            foreach (string line in lines) {
                string[] tmp = line.Split(new char[] { '=' }, 2);
                if (tmp.Length == 2)
                    _values[tmp[0]] = tmp[1];
            }
        }

        /// <inheritdoc/>
        public void Store(String configFile)
        {
            using (StreamWriter w = new StreamWriter(configFile))
            {
                foreach (string key in _values.Keys) {
                    w.WriteLine($"{key}={_values[key]}");
                }
            }
        }

        /// <inheritdoc/>
        public string GetString(string key, string defaultValue)
        {
            return _values[key] ?? defaultValue;
        }

        private static String GetString(NameValueCollection nvc, String key1, String key2, String defaultValue)
        {
            return nvc[key1] ?? nvc[key2] ?? defaultValue;
        }

        /// <inheritdoc/>
        public int GetInt(String key, int defaultValue)
        {
            string value = GetString(key, null);
            int result;
            return !string.IsNullOrEmpty(value) && int.TryParse(value, out result) ? result : defaultValue;
        }

        /// <inheritdoc/>
        public Int64 GetInt64(String key, long defaultValue)
        {
            string value = _values[key];
            Int64 result;
            return !string.IsNullOrEmpty(value) && Int64.TryParse(value, out result) ? result : defaultValue;
        }

        /// <inheritdoc/>
        public double GetDouble(string key, double defaultValue)
        {
            string value = _values[key];
            double result;
            return !string.IsNullOrEmpty(value) && double.TryParse(value, out result) ? result : defaultValue;
        }

        private static Boolean GetBoolean(NameValueCollection nvc, String key1, String key2, Boolean defaultValue)
        {
            String value = GetString(nvc, key1, key2, null);
            Boolean result;
            return !String.IsNullOrEmpty(value) && Boolean.TryParse(value, out result) ? result : defaultValue;
        }

        /// <inheritdoc/>
        public bool GetBool(String key, bool defaultValue)
        {
            string value = GetString(key, null);
            bool result;
            return !string.IsNullOrEmpty(value) && bool.TryParse(value, out result) ? result : defaultValue;
        }

        private static ICoapConfig LoadConfig()
        {
            // TODO may have configuration file here
            return new CoapConfig();
        }
        
        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
