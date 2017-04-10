using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoAP.OSCOAP
{
    public class SecurityContextSet
    {
        List<SecurityContext> _allContexts = new List<SecurityContext>();
        public static SecurityContextSet AllContexts = new SecurityContextSet();

        public int Count { get { return _allContexts.Count; } }

        public void Add(SecurityContext ctx)
        {
            _allContexts.Add(ctx);
        }

        public List<SecurityContext> All { get { return _allContexts; } }

        public List<SecurityContext> FindByKid(byte[] kid)
        {
            List<SecurityContext> contexts = new List<SecurityContext>();
            foreach (SecurityContext ctx in _allContexts) {
                if (kid.Length == ctx.Recipient.Id.Length) {
                    bool match = true;
                    for (int i=0; i<kid.Length;i++) if (kid[i] != ctx.Recipient.Id[i]) {
                            match = false;
                            break;
                        }
                    if (match) contexts.Add(ctx);
                }
            }
            return contexts;
        }
    }
}
