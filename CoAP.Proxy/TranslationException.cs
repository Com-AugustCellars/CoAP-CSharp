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

namespace Com.AugustCellars.CoAP.Proxy
{
    /// <summary>
    /// Exception created to throw when a message to message translation error occurs
    /// </summary>
    [Serializable]
    public class TranslationException : Exception
    {
        /// <summary>
        /// Create an empty error w/ no text
        /// </summary>
        public TranslationException()
        {
        }

        /// <summary>
        /// Create an error w/ text
        /// </summary>
        /// <param name="message">message about error</param>
        public TranslationException(String message) : base(message)
        {
        }

        /// <summary>
        /// Create an error w/ text and pointer to more exception information
        /// </summary>
        /// <param name="message">message about error</param>
        /// <param name="inner">exception that caused this one</param>
        public TranslationException(String message, Exception inner) : base(message, inner)
        {
        }
    }
}
