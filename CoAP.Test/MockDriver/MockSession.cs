using System;
using System.Collections.Generic;
using System.Text;
using Com.AugustCellars.CoAP;

namespace CoAP.Test.Std10.MockItems
{
    public class MockSession : ISession
    {
        /// <inheritdoc />
        public event EventHandler<SessionEventArgs> SessionEvent;

        /// <inheritdoc />
        public bool IsReliable { get; } = false;

        /// <inheritdoc />
        public bool BlockTransfer { get; set; } = false;

        /// <inheritdoc />
        public int MaxSendSize { get; set; } = 1500;
    }
}
