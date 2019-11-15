using System;
using System.Collections.Generic;
using System.Text;
using Com.AugustCellars.COSE;

namespace Com.AugustCellars.CoAP.OSCOAP
{
    public class OscoreEvent
    {
        public enum EventCode
        {
            UnknownGroupIdentifier = 1,
            UnknownKeyIdentifier = 2,
            UnknownPublicKey = 3,
            PivExhaustion = 4,
            HitZoneMoved = 5,
            SenderIvSave = 6
        }

        public EventCode Code { get; }
        public byte[] GroupIdentifier { get; }
        public byte[] KeyIdentifier { get; }
        public SecurityContext SecurityContext { get; set; }
        public SecurityContext.EntityContext RecipientContext { get; set; }

        public OscoreEvent(EventCode code, byte[] groupIdentifier, byte[] keyIdentifier, SecurityContext context, SecurityContext.EntityContext recipient)
        {
            Code = code;
            GroupIdentifier = groupIdentifier;
            KeyIdentifier = keyIdentifier;
            SecurityContext = context;
            RecipientContext = recipient;
        }
    }
}
