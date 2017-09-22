/*
 * Copyright (c) 2011-2012, Longxiang He <helongxiang@smeshlink.com>,
 * SmeshLink Technology Co.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY.
 * 
 * This file is part of the CoAP.NET, a CoAP framework in C#.
 * Please see README for more information.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Com.AugustCellars.CoAP.Log;
using Com.AugustCellars.CoAP.Observe;
using Com.AugustCellars.CoAP.Util;
using Com.AugustCellars.CoAP.Server.Resources;
using Com.AugustCellars.CoAP.Net;
using Com.AugustCellars.CoAP.Threading;

namespace Com.AugustCellars.CoAP.EndPoint.Resources
{
    /// <summary>
    /// This class describes the functionality of a CoAP resource.
    /// </summary>
    public partial class RemoteResource : IComparable<RemoteResource>, IResource
    {
        private static ILogger log = LogManager.GetLogger(typeof(Resource));

        private Int32 _totalSubResourceCount;
        private HashSet<LinkAttribute> _attributes;
        private RemoteResource _parent;
        private SortedDictionary<String, RemoteResource> _subResources;
        private Boolean _hidden;

        /// <summary>
        /// Initialize a resource.
        /// </summary>
        /// <param name="resourceIdentifier">The identifier of this resource</param>
        public RemoteResource(String resourceIdentifier) : this(resourceIdentifier, false)
        {
        }

        /// <summary>
        /// Initialize a resource.
        /// </summary>
        /// <param name="resourceIdentifier">The identifier of this resource</param>
        /// <param name="hidden">True if this resource is hidden</param>
        public RemoteResource(String resourceIdentifier, Boolean hidden)
        {
            Name = resourceIdentifier;
            _hidden = hidden;
            _attributes = new HashSet<LinkAttribute>();
        }

        /// <inheritdoc/>
        public String Uri
        {
            get => Path + Name;
        }

        /// <summary>
        /// Gets the URI of this resource.
        /// </summary>
        public String Path
        {
            get => String.Empty;
            set => throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public IResource Parent
        {
            get => _parent;
            set => throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public String Name { get; set; }

        /// <inheritdoc/>
        public ResourceAttributes Attributes { get; } = new ResourceAttributes();

        [Obsolete("Use Attributes")]
        public ICollection<LinkAttribute> LinkAttributes
        {
            get { return _attributes; }
        }

        [Obsolete("use Attributes.Get()")]
        public IList<LinkAttribute> GetAttributes(String name)
        {
            IEnumerable<string> oldList = Attributes.GetValues(name);
            List<LinkAttribute> newList = new List<LinkAttribute>();
            foreach (string item in oldList) {
                newList.Add(new LinkAttribute(name, item));
            }
            return newList.AsReadOnly();
        }

        [Obsolete("use Attributes.Add()")]
        public Boolean SetAttribute(LinkAttribute attr)
        {
            Attributes.Add(attr.Name, attr.Value.ToString());
            return true;
        }

        [Obsolete("use Attributes.Clear()")]
        public Boolean ClearAttribute(String name)
        {
            Attributes.Clear(name);
            return true;
        }

        /// <inheritdoc/>
        public Boolean Visible
        {
            get => !_hidden;
        }

        public Boolean Hidden
        {
            get { return _hidden; }
            set { _hidden = value; }
        }

        public IList<string> ResourceTypes
        {
            get { return GetStringValues(GetAttributes(LinkFormat.ResourceType)); }
        }

        /// <summary>
        /// Gets or sets the type attribute of this resource.
        /// </summary>
        public string ResourceType
        {
            get
            {
                IList<string> attrs = Attributes.GetResourceTypes().ToList();
                return attrs.Count == 0 ? null : attrs[0];
            }
            set { SetAttribute(new LinkAttribute(LinkFormat.ResourceType, value)); }
        }

        /// <summary>
        /// Gets or sets the title attribute of this resource.
        /// </summary>
        public String Title
        {
            get
            {
                IList<LinkAttribute> attrs = GetAttributes(LinkFormat.Title);
                return attrs.Count == 0 ? null : attrs[0].StringValue;
            }
            set
            {
                ClearAttribute(LinkFormat.Title);
                SetAttribute(new LinkAttribute(LinkFormat.Title, value));
            }
        }

        public IList<String> InterfaceDescriptions
        {
            get { return GetStringValues(GetAttributes(LinkFormat.InterfaceDescription)); }
        }

        /// <summary>
        /// Gets or sets the interface description attribute of this resource.
        /// </summary>
        public String InterfaceDescription
        {
            get
            {
                IList<LinkAttribute> attrs = GetAttributes(LinkFormat.InterfaceDescription);
                return attrs.Count == 0 ? null : attrs[0].StringValue;
            }
            set { SetAttribute(new LinkAttribute(LinkFormat.InterfaceDescription, value)); }
        }

        public IList<Int32> GetContentTypeCodes
        {
            get { return GetIntValues(GetAttributes(LinkFormat.ContentType)); }
        }

        /// <summary>
        /// Gets or sets the content type code attribute of this resource.
        /// </summary>
        [Obsolete("Use Attributes.GetContentTypes()")]
        public Int32 ContentTypeCode
        {
            get
            {
                IList<LinkAttribute> attrs = GetAttributes(LinkFormat.ContentType);
                return attrs.Count == 0 ? 0 : attrs[0].IntValue;
            }
            set { SetAttribute(new LinkAttribute(LinkFormat.ContentType, value)); }
        }

        /// <summary>
        /// Gets or sets the maximum size estimate attribute of this resource.
        /// </summary>
        public Int32 MaximumSizeEstimate
        {
            get
            {
                IList<LinkAttribute> attrs = GetAttributes(LinkFormat.MaxSizeEstimate);
                return attrs.Count == 0 ? -1 : attrs[0].IntValue;
            }
            set { SetAttribute(new LinkAttribute(LinkFormat.MaxSizeEstimate, value)); }
        }

        /// <summary>
        /// Gets or sets the observable attribute of this resource.
        /// </summary>
        public Boolean Observable
        {
            get { return GetAttributes(LinkFormat.Observable).Count > 0; }
            set
            {
                if (value) SetAttribute(new LinkAttribute(LinkFormat.Observable, value));
                else ClearAttribute(LinkFormat.Observable);
            }
        }

        /// <summary>
        /// Gets the total count of sub-resources, including children and children's children...
        /// </summary>
        public Int32 TotalSubResourceCount
        {
            get { return _totalSubResourceCount; }
        }

        /// <summary>
        /// Gets the count of sub-resources of this resource.
        /// </summary>
        public Int32 SubResourceCount
        {
            get { return null == _subResources ? 0 : _subResources.Count; }
        }


        /// <summary>
        /// Removes this resource from its parent.
        /// </summary>
        public void Remove()
        {
            if (_parent != null) _parent.RemoveSubResource(this);
        }

        /// <inheritdoc/>
        public bool Remove(IResource child)
        {
            ((RemoteResource) child).Remove();
            return true;
        }


        /// <inheritdoc/>
        public IResource GetChild(string name)
        {
            return GetResource(name);
        }

        /// <summary>
        /// Gets sub-resources of this resource.
        /// </summary>
        /// <returns></returns>
        public RemoteResource[] GetSubResources()
        {
            if (null == _subResources) return new RemoteResource[0];

            RemoteResource[] resources = new RemoteResource[_subResources.Count];
            this._subResources.Values.CopyTo(resources, 0);
            return resources;
        }

        public RemoteResource GetResource(String path)
        {
            return GetResource(path, false);
        }

        public RemoteResource GetResource(String path, Boolean last)
        {
            if (String.IsNullOrEmpty(path)) return this;

            return _subResources[path];

#if false // find root for absolute path
            if (path.StartsWith("/")) {
                RemoteResource root = this;
                while (root._parent != null) root = root._parent;
                path = path.Equals("/") ? null : path.Substring(1);
                return root.GetResource(path);
            }

            Int32 pos = path.IndexOf('/');
            String head = null, tail = null;

            // note: "some/resource/" addresses a resource "" under "resource"
            if (pos == -1) {
                head = path;
            }
            else {
                head = path.Substring(0, pos);
                tail = path.Substring(pos + 1);
            }

            if (SubResources.ContainsKey(head)) return SubResources[head].GetResource(tail, last);
            else if (last) return this;
            else return null;
#endif
        }

        /// <inheritdoc/>
        public IEnumerable<IResource> Children
        {
            get
            {
                if (_subResources == null) return null;
                return SubResources.Values;
            }
        }


        private SortedDictionary<String, RemoteResource> SubResources
        {
            get
            {
                if (_subResources == null) _subResources = new SortedDictionary<String, RemoteResource>();
                return _subResources;
            }
        }

        /// <inheritdoc/>
        public void Add(IResource resource)
        {
            AddSubResource((RemoteResource) resource);
        }

        /// <summary>
        /// Adds a resource as a sub-resource of this resource.
        /// </summary>
        /// <param name="resource">The sub-resource to be added</param>
        public void AddSubResource(RemoteResource resource)
        {
            if (null == resource) throw new ArgumentNullException(nameof(resource));

            //  For simplicity purposes we keep this as a flat list rather than as a
            //  tree structure.  The reason for this is that people can feed us back 
            //  a full URI as the name, so trying to rebuild the tree structure just
            //  is not going to make alot of sense.


            resource._parent = this;
            SubResources[resource.Name] = resource;

            if (log.IsDebugEnabled) log.Debug("Add resource " + resource.Name);
        }

        /// <summary>
        /// Removes a sub-resource from this resource by its identifier.
        /// </summary>
        /// <param name="resourcePath">the path of the sub-resource to remove</param>
        public void RemoveSubResource(String resourcePath)
        {
            RemoveSubResource(GetResource(resourcePath));
        }

        /// <summary>
        /// Removes a sub-resource from this resource.
        /// </summary>
        /// <param name="resource">the sub-resource to remove</param>
        public void RemoveSubResource(RemoteResource resource)
        {
            if (null == resource) return;

            if (SubResources.Remove(resource.Name)) {
                RemoteResource p = resource._parent;
                while (p != null) {
                    p._totalSubResourceCount--;
                    p = p._parent;
                }

                resource._parent = null;
            }
        }

        public void CreateSubResource(Request request, String newIdentifier)
        {
            DoCreateSubResource(request, newIdentifier);
        }

        public Int32 CompareTo(RemoteResource other)
        {
            return Path.CompareTo(other.Path);
        }

        /// <inheritdoc/>
        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();
            Print(sb, 0);
            return sb.ToString();
        }

        private void Print(StringBuilder sb, Int32 indent)
        {
            for (Int32 i = 0; i < indent; i++) sb.Append(" ");
            sb.AppendFormat("+[{0}]", Name);

            String title = Title;
            if (title != null) sb.AppendFormat(" {0}", title);
            sb.AppendLine();

            foreach (LinkAttribute attr in LinkAttributes) {
                if (attr.Name.Equals(LinkFormat.Title)) continue;
                for (Int32 i = 0; i < indent + 3; i++) sb.Append(" ");
                sb.AppendFormat("- ");
                attr.Serialize(sb);
                sb.AppendLine();
            }

            if (_subResources != null)
                foreach (RemoteResource sub in _subResources.Values) {
                    sub.Print(sb, indent + 2);
                }
        }

        /// <summary>
        /// Creates a resouce instance with proper subtype.
        /// </summary>
        /// <returns></returns>
//        protected abstract RemoteResource CreateInstance(String name);
//        protected abstract void DoCreateSubResource(Request request, String newIdentifier);

        private static IList<String> GetStringValues(IEnumerable<LinkAttribute> attributes)
        {
            List<String> list = new List<String>();
            foreach (LinkAttribute attr in attributes) {
                list.Add(attr.StringValue);
            }
            return list;
        }

        private static IList<Int32> GetIntValues(IEnumerable<LinkAttribute> attributes)
        {
            List<Int32> list = new List<Int32>();
            foreach (LinkAttribute attr in attributes) {
                list.Add(attr.IntValue);
            }
            return list;
        }

        /// <inheritdoc/>
        public void AddObserveRelation(ObserveRelation o)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public void RemoveObserveRelation(ObserveRelation o)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public bool Cachable
        {
            get => false;
        }

        /// <inheritdoc/>
        public IEnumerable<IEndPoint> EndPoints
        {
            get => throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public IExecutor Executor
        {
            get => throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public void HandleRequest(Exchange exchange)
        {
            throw new NotSupportedException();
        }

    }
}

