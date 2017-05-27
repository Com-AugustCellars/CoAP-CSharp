using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Com.AugustCellars.CoAP
{
    public interface ISession
    {
        /// <summary>
        /// Occurs when some bytes are received in this channel.
        /// </summary>
        event EventHandler<SessionEventArgs> SessionEvent;

        /// <summary>
        /// Is the session reliable?
        /// </summary>
        bool IsReliable { get; }
    }
}
