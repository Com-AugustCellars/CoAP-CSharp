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

namespace Com.AugustCellars.CoAP.Log
{
    /// <summary>
    /// Create the internal console writer - which is just about ready to become a lie
    /// </summary>
    class ConsoleLogManager : ILogManager
    {
        /// <summary>
        /// Create a logger based on the type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public ILogger GetLogger(Type type)
        {
            return new TextWriterLogger(type.Name, Console.Out);
        }

        /// <summary>
        /// Create a logger w/ an arbitrary name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ILogger GetLogger(string name)
        {
            return new TextWriterLogger(name, Console.Out);
        }
    }
}
