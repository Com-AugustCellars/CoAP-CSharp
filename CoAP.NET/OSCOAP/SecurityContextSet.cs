using System.Collections.Generic;

namespace Com.AugustCellars.CoAP.OSCOAP
{
    /// <summary>
    /// Collection of OSCOAP security contexts
    /// </summary>
    public class SecurityContextSet
    {
        /// <summary>
        /// Collection of all OSCOAP security contexts on the system.
        /// </summary>
 
        public static SecurityContextSet AllContexts = new SecurityContextSet();

        /// <summary>
        /// Get the count of all security contexts.
        /// </summary>
        public int Count { get => All.Count;}

        /// <summary>
        /// Add a new context to the set
        /// </summary>
        /// <param name="ctx">context to add</param>
        public void Add(SecurityContext ctx)
        {
            All.Add(ctx);
        }

        /// <summary>
        /// Secuirty contexts for the object
        /// </summary>
        public List<SecurityContext> All { get; } = new List<SecurityContext>();

        /// <summary>
        /// Find all security contexts that match this key identifier
        /// </summary>
        /// <param name="kid">key id to search</param>
        /// <returns>set of contexts</returns>
        public List<SecurityContext> FindByKid(byte[] kid)
        {
            List<SecurityContext> contexts = new List<SecurityContext>();
            foreach (SecurityContext ctx in All) {
                if (ctx.Recipient != null &&  kid.Length == ctx.Recipient.Id.Length) {
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

        /// <summary>
        /// Find all security contexts that match this group identifier
        /// </summary>
        /// <param name="groupId">group id to search</param>
        /// <returns>set of contexts</returns>
        public List<SecurityContext> FindByGroupId(byte[] groupId)
        {
            List<SecurityContext> contexts = new List<SecurityContext>();
            foreach (SecurityContext ctx in All) {
                if (ctx.GroupId != null &&  groupId.Length == ctx.GroupId.Length) {
                    bool match = true;
                    for (int i = 0; i < groupId.Length; i++) {
                        if (groupId[i] != ctx.GroupId[i]) {
                            match = false;
                            break;
                        }
                    }
                    if (match) contexts.Add(ctx);
                }
            }
            return contexts;
        }
    }
}
