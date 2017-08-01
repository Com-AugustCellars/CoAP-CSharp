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
using System.Collections.Generic;
using System.Net;
using Com.AugustCellars.CoAP.Log;
using Com.AugustCellars.CoAP.Net;
using Com.AugustCellars.CoAP.Server.Resources;

namespace Com.AugustCellars.CoAP.Server
{
    /// <summary>
    /// Represents an execution environment for CoAP <see cref="IResource"/>s.
    /// </summary>
    public class CoapServer : IServer
    {
        private static readonly ILogger _Log = LogManager.GetLogger(typeof(CoapServer));
        private readonly IResource _root;
        private readonly List<IEndPoint> _endpoints = new List<IEndPoint>();
        private IMessageDeliverer _deliverer;

        /// <summary>
        /// Constructs a server with default configuration.
        /// </summary>
        public CoapServer()
            : this((ICoapConfig) null)
        {
        }

        /// <summary>
        /// Constructs a server that listens to the specified port(s).
        /// </summary>
        /// <param name="ports">the ports to bind to</param>
        public CoapServer(params Int32[] ports)
            : this(null, ports)
        {
        }

        /// <summary>
        /// Constructs a server with the specified configuration that
        /// listens to the given ports.
        /// </summary>
        /// <param name="config">the configuration, or <code>null</code> for default</param>
        /// <param name="ports">the ports to bind to</param>
        public CoapServer(ICoapConfig config, params Int32[] ports)
            : this(config, null, ports)
        {
        }

        /// <summary>
        /// Constructs a server with the specified configuration that
        /// listens to the given ports.
        /// </summary>
        /// <param name="config">the configuration, or <code>null</code> for default</param>
        /// <param name="rootResource">root resource object to use</param>
        /// <param name="ports">the ports to bind to</param>
        public CoapServer(ICoapConfig config, IResource rootResource, params int[] ports)
        {
            Config = config ?? CoapConfig.Default;
            _root = rootResource ?? new RootResource(this);
            _deliverer = new ServerMessageDeliverer(Config, _root);

            Resource wellKnown = new Resource(".well-known", false);
            wellKnown.Add(new DiscoveryResource(_root));
            _root.Add(wellKnown);

            foreach (int port in ports) {
                Bind(port);
            }

        }

        /// <summary>
        /// Return the configuration interface used by the server.
        /// </summary>
        public ICoapConfig Config { get; }

        private void Bind(Int32 port)
        {
            AddEndPoint(new CoAPEndPoint(port, Config));
        }

        /// <inheritdoc/>
        public IEnumerable<IEndPoint> EndPoints
        {
            get => _endpoints;
        }

        /// <summary>
        /// Get/Set the message deliverer object to use.
        /// </summary>
        public IMessageDeliverer MessageDeliverer
        {
            get => _deliverer;
            set
            {
                _deliverer = value;
                foreach (IEndPoint endpoint in _endpoints) {
                    endpoint.MessageDeliverer = value;
                }
            }
        }

        /// <inheritdoc/>
        public void AddEndPoint(IEndPoint endpoint)
        {
            endpoint.MessageDeliverer = _deliverer;
            _endpoints.Add(endpoint);
        }

        /// <inheritdoc/>
        public void AddEndPoint(IPEndPoint ep)
        {
            AddEndPoint(new CoAPEndPoint(ep, Config));
        }

        /// <inheritdoc/>
        public void AddEndPoint(IPAddress address, Int32 port)
        {
            AddEndPoint(new CoAPEndPoint(new IPEndPoint(address, port), Config));
        }

        /// <inheritdoc/>
        public IEndPoint FindEndPoint(System.Net.EndPoint ep)
        {
            foreach (IEndPoint endpoint in _endpoints) {
                if (endpoint.LocalEndPoint.Equals(ep)) {
                    return endpoint;
                }
            }
            return null;
        }

        /// <inheritdoc/>
        public IEndPoint FindEndPoint(Int32 port)
        {
            foreach (IEndPoint endpoint in _endpoints) {
                if (((IPEndPoint) endpoint.LocalEndPoint).Port == port) {
                    return endpoint;
                }
            }
            return null;
        }

        /// <inheritdoc/>
        public IServer Add(IResource resource)
        {
            _root.Add(resource);
            return this;
        }

        /// <summary>
        /// Add a resource using the path node as the parent
        /// </summary>
        /// <param name="resourcePath">path to parent node</param>
        /// <param name="resource">resource to add</param>
        /// <returns>current server</returns>
        public IServer Add(string resourcePath, IResource resource)
        {
            IResource parent = FindResource(resourcePath);
            if (parent == null) throw new ArgumentException("resource path not found", nameof(resourcePath));

            parent.Add(resource);
            return this;
        }

        /// <inheritdoc/>
        public IServer Add(params IResource[] resources)
        {
            foreach (IResource resource in resources) {
                _root.Add(resource);
            }
            return this;
        }

        /// <summary>
        /// Add resources as children of the given path
        /// </summary>
        /// <param name="resourcePath">path of parent</param>
        /// <param name="resources">Array of resources to be added</param>
        /// <returns>the server</returns>
        public IServer Add(string resourcePath, IResource[] resources)
        {
            IResource parent = FindResource(resourcePath);
            if (parent == null) {
                throw new ArgumentException("resource path not found", nameof(resourcePath));
            }

            foreach (IResource resource in resources) {
                parent.Add(resource);
            }

            return this;
        }

        /// <summary>
        /// Return a resource from the server given a path to the resource
        /// </summary>
        /// <param name="resourcePath">path to follow</param>
        /// <returns>interface to resource</returns>
        public IResource FindResource(string resourcePath)
        {
            string[] pathStrings = resourcePath.Split(',');
            IResource resource = _root;

            foreach (string path in pathStrings) {
                if (string.IsNullOrEmpty(path)) continue;
                resource = _root.GetChild(path);
                if (resource == null) return null;
            }

            return resource;
        }

        /// <inheritdoc/>
        public Boolean Remove(IResource resource)
        {
            return _root.Remove(resource);
        }

        /// <inheritdoc/>
        public void Start()
        {
            _Log.Debug("Starting CoAP server");

            if (_endpoints.Count == 0) {
                Bind(Config.DefaultPort);
            }

            Int32 started = 0;
            foreach (IEndPoint endpoint in _endpoints) {
                try {
                    endpoint.Start();
                    started++;
                }
                catch (Exception e) {
                    if (_Log.IsWarnEnabled) _Log.Warn("Could not start endpoint " + endpoint.LocalEndPoint, e);
                }
            }

            if (started == 0) {
                throw new InvalidOperationException("None of the server's endpoints could be started");
            }
        }

        /// <inheritdoc/>
        public void Stop()
        {
            _Log.Debug("Stopping CoAP server");

            _endpoints.ForEach(ep => ep.Stop());
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _endpoints.ForEach(ep => ep.Dispose());
        }

        class RootResource : Resource
        {

            public RootResource(CoapServer server)
                : base(String.Empty)
            {
            }

            protected override void DoGet(CoapExchange exchange)
            {

                exchange.Respond("Ni Hao from CoAP.NET " + Spec.Name);
            }
        }
    }
}
