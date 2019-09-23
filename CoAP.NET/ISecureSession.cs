using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Com.AugustCellars.COSE;
using Org.BouncyCastle.Crypto.Tls;

namespace Com.AugustCellars.CoAP
{
    public interface ISecureSession : ISession
    {
        OneKey AuthenticationKey { get; }
        Certificate AuthenticationCertificate { get; }
    }
}
