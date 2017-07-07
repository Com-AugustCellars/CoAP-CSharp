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
using System.Linq;

namespace Com.AugustCellars.CoAP.Log
{
    /// <summary>
    /// Log manager.
    /// </summary>
    public static class LogManager
    {
        static ILogManager _Manager;

        private static readonly string[] _LoggingIncludeDefault = null;
        private static readonly string[] _LoggingExcludeDefault = new string[] {
            "UDPChannel"
        };

        static LogManager()
        {
            Type test;
            try {
                test = Type.GetType("Common.Logging.LogManager, Common.Logging");
            }
            catch {
                test = null;
            }

            if (test == null) {
                _Manager = new ConsoleLogManager();
            }
            else {
                _Manager = new CommonLoggingManager();
            }
        }

        /// <summary>
        /// Gets or sets the global log level.
        /// </summary>
        public static LogLevel Level { get; set; } = LogLevel.None;

        /// <summary>
        /// Gets or sets the <see cref="ILogManager"/> to provide loggers.
        /// </summary>
        public static ILogManager Instance
        {
            get => _Manager;
            set => _Manager = value ?? NopLogManager.Instance;
        }

        /// <summary>
        /// Filter of loggers to be allocated.
        /// Any logger which is not in the filter will be given an NOP logger.
        /// </summary>
        public static string[] LoggingInclude { get; set; } = _LoggingIncludeDefault;

        /// <summary>
        /// Filter of loggers to be allocated.
        /// Any logger which is not in the filter will be given an NOP logger.
        /// If set to null, then EVERYTHING is logged.
        /// </summary>
        public static string[] LoggingExclude { get; set; } = _LoggingExcludeDefault;

        /// <summary>
        /// Gets a logger for the given type.
        /// </summary>
        public static ILogger GetLogger(Type type)
        {
            //  Explicit to include
            if ((LoggingInclude != null) && LoggingInclude.Contains(type.FullName)) {
                return _Manager.GetLogger(type);
            }
            //  Explicit to exclude
            if ((LoggingExclude != null) && LoggingExclude.Contains(type.FullName)) {
                return NopLogManager.Instance.GetLogger(type);
            }
            return LoggingInclude == null ? _Manager.GetLogger(type) : NopLogManager.Instance.GetLogger(type);
        }

        /// <summary>
        /// Gets a logger for the given type name.
        /// </summary>
        public static ILogger GetLogger(String name)
        {
            //  Explicit to include
            if ((LoggingInclude != null) && LoggingInclude.Contains(name)) {
                return _Manager.GetLogger(name);
            }
            //  Explicit to exclude
            if ((LoggingExclude != null) && LoggingExclude.Contains(name)) {
                return NopLogManager.Instance.GetLogger(name);
            }
            return LoggingInclude == null ? _Manager.GetLogger(name) : NopLogManager.Instance.GetLogger(name);
        }
    }

    /// <summary>
    /// Log levels.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// All logs.
        /// </summary>
        All,
        /// <summary>
        /// Debugs and above.
        /// </summary>
        Debug,
        /// <summary>
        /// Infos and above.
        /// </summary>
        Info,
        /// <summary>
        /// Warnings and above.
        /// </summary>
        Warning,
        /// <summary>
        /// Errors and above.
        /// </summary>
        Error,
        /// <summary>
        /// Fatals only.
        /// </summary>
        Fatal,
        /// <summary>
        /// No logs.
        /// </summary>
        None
    }
}
