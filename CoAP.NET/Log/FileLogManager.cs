using System;

namespace Com.AugustCellars.CoAP.Log
{
    /// <summary>
    /// Log to file to make life easier to get information
    /// </summary>
    public class FileLogManager : ILogManager
    {
        /// <summary>
        /// Create a file log manager item
        /// </summary>
        /// <param name="writeTo">File to write things to</param>
        public FileLogManager(System.IO.TextWriter writeTo)
        {
            LogStream = writeTo;
        }

        /// <summary>
        /// Create a logger based on the type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public ILogger GetLogger(Type type)
        {
            return new TextWriterLogger(type.Name, LogStream);
        }

        /// <summary>
        /// Create a logger w/ an arbitrary name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ILogger GetLogger(string name)
        {
            return new TextWriterLogger(name, LogStream);
        }

        /// <summary>
        /// Provide a way to stream to someplace else
        /// </summary>
        public System.IO.TextWriter LogStream { get; set; }
    }
}
