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
using Common.Logging.Factory;
using FormatMessageCallback = System.Action<Com.AugustCellars.CoAP.Log.FormatMessageHandler>;

namespace Com.AugustCellars.CoAP.Log
{
    /// <summary>
    /// Logger that writes logs to a <see cref="System.IO.TextWriter"/>.
    /// </summary>
    public class TextWriterLogger : ILogger
    {
        [CLSCompliant(false)]
        protected class FormatMessageCallbackFormattedMessage
        {
            private string _cachedMessage;
            private readonly FormatMessageCallback _formatMessageCallback;

            public FormatMessageCallbackFormattedMessage(FormatMessageCallback formatMessageCallback)
            {
                _formatMessageCallback = formatMessageCallback;
            }

            public override string ToString()
            {
                if (_cachedMessage == null) {
                    if (_formatMessageCallback != null) {
                        _formatMessageCallback(FormatMessage);
                    }
                    else {
                        _cachedMessage = "";
                    }
                }

                return _cachedMessage;
            }

            [StringFormatMethod("format")]
            protected string FormatMessage(string format, params object[] args)
            {
                if (args.Length > 0) _cachedMessage = string.Format(format, args);
                else _cachedMessage = format;
                return _cachedMessage;
            }
        }

        private readonly System.IO.TextWriter _Writer;

        private readonly String _logName;

        /// <summary>
        /// Instantiates.
        /// </summary>
        public TextWriterLogger(String logName, System.IO.TextWriter writer)
        {
            _Writer = writer;
            _logName = logName;
        }

        /// <inheritdoc/>
        public Boolean IsDebugEnabled
        {
            get => LogLevel.Debug >= LogManager.Level;
        }

        /// <inheritdoc/>
        public Boolean IsInfoEnabled
        {
            get => LogLevel.Info >= LogManager.Level;
        }

        /// <inheritdoc/>
        public Boolean IsErrorEnabled
        {
            get => LogLevel.Error >= LogManager.Level;
        }

        /// <inheritdoc/>
        public Boolean IsFatalEnabled
        {
            get => LogLevel.Fatal >= LogManager.Level;
        }

        /// <inheritdoc/>
        public Boolean IsWarnEnabled
        {
            get => LogLevel.Warning >= LogManager.Level;
        }

        /// <inheritdoc/>
        public void Error(Object sender, String msg, params Object[] args)
        {
            if (!IsErrorEnabled) return;

            string text = String.Format(msg, args);

            Log("ERROR", text, null);
        }

        /// <inheritdoc/>
        public void Warning(Object sender, String msg, params Object[] args)
        {
            if (!IsWarnEnabled) return;

            string text = String.Format(msg, args);

            Log("WARNING", text, null);
        }

        /// <inheritdoc/>
        public void Info(Object sender, String msg, params Object[] args)
        {
            if (!IsInfoEnabled) return;

            string text = String.Format(msg, args);

            Log("INFO", text, null);
        }

        /// <inheritdoc/>
        public void Debug(Object sender, String msg, params Object[] args)
        {
            if (!IsDebugEnabled) return;

            string text = String.Format(msg, args);

            Log("DEBUG", text, null);
        }

        /// <inheritdoc/>
        public void Debug(Object message)
        {
            if (!IsDebugEnabled) return;
            Log("DEBUG", message, null);
        }

        /// <inheritdoc/>
        public void Debug(Object message, Exception exception)
        {
            if (!IsDebugEnabled) return;
            Log("DEBUG", message, exception);
        }

        public void Debug(FormatMessageCallback formatMessageCallback)
        {
            if (IsDebugEnabled) {
                Log("DEBUG", new FormatMessageCallbackFormattedMessage(formatMessageCallback), null);
            }
        }

        /// <inheritdoc/>
        public void Error(Object message)
        {
            Log("Error", message, null);
        }

        /// <inheritdoc/>
        public void Error(Object message, Exception exception)
        {
            Log("Error", message, exception);
        }

        /// <inheritdoc/>
        public void Error(FormatMessageCallback formatMessageCallback)
        {
            if (IsErrorEnabled) {
                Log("ERROR", new FormatMessageCallbackFormattedMessage(formatMessageCallback), null);
            }
        }

        /// <inheritdoc/>
        public void Fatal(Object message)
        {
            Log("Fatal", message, null);
        }

        /// <inheritdoc/>
        public void Fatal(Object message, Exception exception)
        {
            Log("Fatal", message, exception);
        }

        /// <inheritdoc/>
        public void Fatal(FormatMessageCallback formatMessageCallback)
        {
            if (IsFatalEnabled) {
                Log("Fatal", new FormatMessageCallbackFormattedMessage(formatMessageCallback), null);
            }
        }

        /// <inheritdoc/>
        public void Info(Object message)
        {
            Log("Info", message, null);
        }

        /// <inheritdoc/>
        public void Info(Object message, Exception exception)
        {
            Log("Info", message, exception);
        }

        /// <inheritdoc/>
        public void Info(FormatMessageCallback formatMessageCallback)
        {
            if (IsInfoEnabled) {
                Log("Info", new FormatMessageCallbackFormattedMessage(formatMessageCallback), null);
            }
        }

        /// <inheritdoc/>
        public void Warn(Object message)
        {
            Log("Warn", message, null);
        }

        /// <inheritdoc/>
        public void Warn(Object message, Exception exception)
        {
            if (IsWarnEnabled) {
                Log("Warn", message, exception);
            }
        }

        /// <inheritdoc/>
        public void Warn(FormatMessageCallback formatMessageCallback)
        {
            if (IsWarnEnabled) {
                Log("Warn", new FormatMessageCallbackFormattedMessage(formatMessageCallback), null);
            }
        }

        private void Log(String level, Object message, Exception exception)
        {
            String log = "";
            if (_logName != null) {
                log = "[" + _logName + "]";
            }

            String text = $"{DateTime.Now.ToLongTimeString()} {log} {level} - {message}";
            if (exception != null) {
                text += exception.ToString();
            }
            _Writer.WriteLine(text);
            _Writer.Flush();
        }
    }
}
