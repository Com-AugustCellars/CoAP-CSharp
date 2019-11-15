using System;
using System.Collections;
using System.Collections.Generic;

namespace Com.AugustCellars.CoAP.OSCOAP
{
    /// <summary>
    /// Collection of OSCOAP security contexts
    /// </summary>
    public class SecurityContextSet : IEnumerable<SecurityContext>
    {
        /// <summary>
        /// Get the count of all security contexts.
        /// </summary>
        public int Count => All.Count;

        /// <summary>
        /// Add a new context to the set
        /// </summary>
        /// <param name="ctx">context to add</param>
        public void Add(SecurityContext ctx)
        {
            All.Add(ctx);
        }

        public void Add(SecurityContextSet set)
        {
            foreach (SecurityContext c in set) {
                All.Add(c);
            }
        }

        /// <summary>
        /// Security contexts for the object
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
                    for (int i=0; i<kid.Length;i++) {
                        if (kid[i] != ctx.Recipient.Id[i]) {
                            match = false;
                            break;
                        }
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


        public event EventHandler<OscoreEvent> OscoreEvents;

        public void OnEvent(OscoreEvent e)
        {
            EventHandler<OscoreEvent> eventHandler = OscoreEvents;
            eventHandler?.Invoke(null, e);
        }

        public IEnumerator<SecurityContext> GetEnumerator()
        {
            return All.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return All.GetEnumerator();
        }
    }
}
