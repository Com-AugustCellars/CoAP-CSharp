using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Com.AugustCellars.CoAP.DTLS
{
    /// <summary>
    /// QueueItems are used for items in the 
    /// </summary>
    public class QueueItem
    {


        private readonly byte[] _data;
#if LATER
        private readonly TcpSession _session;
#endif

#if LATER
        public QueueItem(TcpSession session, byte[] data)
        {
            _data = data;
            _session = session;
        }
#endif
        public QueueItem(byte[] data)
        {
            _data = data;
        }

        public byte[] Data
        {
            get => _data;
        }

        public int Length
        {
            get => Data.Length;
        }
#if LATER
        public TcpSession Session { get { return _session; } }
#endif
    }

}
