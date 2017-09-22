/*
 * Copyright (c) 2011-2014, Longxiang He <helongxiang@smeshlink.com>,
 * SmeshLink Technology Co.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY.
 * 
 * This file is part of the CoAP.NET, a CoAP framework in C#.
 * Please see README for more information.
 */

using System;
using Com.AugustCellars.CoAP.Server.Resources;
using Com.AugustCellars.CoAP.Util;

namespace Com.AugustCellars.CoAP
{
    /// <summary>
    /// This class can be used to programmatically browse a remote CoAP endoint.
    /// </summary>
    public class WebLink : IComparable<WebLink>
    {
        /// <summary>
        /// Instantiates.
        /// </summary>
        /// <param name="uri">the uri of this resource.</param>
        public WebLink(String uri)
        {
            Uri = uri;
        }

        /// <summary>
        /// Gets the uri of this resource.
        /// </summary>
        public String Uri { get; }

        /// <summary>
        /// Gets the attributes of this resource.
        /// </summary>
        public ResourceAttributes Attributes { get; } = new ResourceAttributes();

        /// <inheritdoc/>
        public Int32 CompareTo(WebLink other)
        {
            if (other == null) {
                throw ThrowHelper.ArgumentNull("other");
            }

            return string.Compare(Uri, other.Uri, StringComparison.Ordinal);
        }

        /// <inheritdoc/>
        public override String ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append('<').Append(Uri).Append('>')
                .Append(' ').Append(Attributes.Title);
            if (Attributes.Contains(LinkFormat.ResourceType)) {
                sb.Append("\n\t").Append(LinkFormat.ResourceType)
                    .Append(":\t");
                foreach (string s in Attributes.GetResourceTypes()) {
                    sb.Append(s).Append(' ');
                }
            }

            if (Attributes.Contains(LinkFormat.InterfaceDescription)) {
                sb.Append("\n\t").Append(LinkFormat.InterfaceDescription).Append(":\t");
                foreach (string s in Attributes.GetInterfaceDescriptions()) {
                    sb.Append(s).Append(' ');
                }
            }

            if (Attributes.Contains(LinkFormat.ContentType)) {
                sb.Append("\n\t").Append(LinkFormat.ContentType)
                    .Append(":\t");
                foreach (string s in Attributes.GetContentTypes()) {
                    sb.Append(s).Append(' ');
                }
            }

            if (Attributes.Contains(LinkFormat.MaxSizeEstimate)) {
                sb.Append("\n\t").Append(LinkFormat.MaxSizeEstimate)
                    .Append(":\t").Append(Attributes.MaximumSizeEstimate);
            }

            if (Attributes.Observable) {
                sb.Append("\n\t").Append(LinkFormat.Observable);
            }

            return sb.ToString();
        }
    }
}
