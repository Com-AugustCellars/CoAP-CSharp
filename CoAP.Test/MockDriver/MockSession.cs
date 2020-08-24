using System;
using Com.AugustCellars.CoAP;

namespace CoAP.Test.Std10.MockDriver
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
