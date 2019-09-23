using System;
using System.Collections.Generic;
using System.Text;

namespace Com.AugustCellars.CoAP
{
    public class CoAPException : Exception
    {
        public CoAPException()
        {
        }

        public CoAPException(string message)
            : base(message)
        {
        }

        public CoAPException(string message, Exception inner)
            : base(message, inner)
        {
        }

    }
}
