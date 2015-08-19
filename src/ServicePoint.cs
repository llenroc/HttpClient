// Copyright (c) 2015 Yamool. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

namespace Yamool.Net.Http
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;

    /// <summary>
    /// Provides connection management for HTTP connections.
    /// </summary>
    public class ServicePoint
    {
        private Uri _uri;
        private DateTime _idleSince;
        private int _connectionLimit;
        private bool _usesProxy;
        private int _maxIdleTime;
        private bool _useNagle;
        private int _currentConnections;
        private X509Certificate _certificate;
        private X509Certificate _clientCertificate;
        private bool _tcp_keepalive;
        private int _tcp_keepalive_time;
        private int _tcp_keepalive_interval;
        private readonly string _lookupString;
        private Timer _expiringTimer;
        private DnsResolverHelper _dnsHelper;
        private Dictionary<string, ConnectionGroup> _groups;

        internal ServicePoint(Uri uri, int connectionLimit, string lookupString, bool usesProxy)
        {
            _uri = uri;
            _connectionLimit = connectionLimit;
            _lookupString = lookupString;
            _usesProxy = usesProxy;
            _maxIdleTime = ServicePointManager.MaxServicePointIdleTime;
            _tcp_keepalive = ServicePointManager._tcp_keepalive;
            _tcp_keepalive_interval = ServicePointManager._tcp_keepalive_interval;
            _tcp_keepalive_time = ServicePointManager._tcp_keepalive_time;
            _useNagle = ServicePointManager._useNagle;
            _idleSince = DateTime.Now;
            _dnsHelper = new DnsResolverHelper(uri);
            _groups = new Dictionary<string, ConnectionGroup>(1);
        }

        /// <summary>
        /// Gets the Uniform Resource Identifier (URI) of the server that this ServicePoint object connects to.
        /// </summary>
        public Uri Address
        {
            get
            {
                return _uri;
            }
        }

        /// <summary>
        /// Gets the certificate received for this ServicePoint object.
        /// </summary>
        public X509Certificate Certificate
        {
            get
            {
                return _certificate;
            }
        }

        /// <summary>
        /// Gets the last client certificate sent to the server.
        /// </summary>
        public X509Certificate ClientCertificate
        {
            get
            {
                return _clientCertificate;
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of connections allowed on this ServicePoint object.
        /// </summary>
        public int ConnectionLimit
        {
            get
            {
                return _connectionLimit;
            }
            set
            {
                _connectionLimit = value;
            }
        }

        /// <summary>
        /// Gets the connection name.
        /// </summary>
        public string ConnectionName
        {
            get
            {
                return _uri.Scheme;
            }
        }

        /// <summary>
        /// Gets the number of open connections associated with this <see cref="ServicePoint"/> object.
        /// </summary>
        public int CurrentConnections
        {
            get
            {
                return _currentConnections;
            }
        }

        /// <summary>
        /// Gets the date and time that the ServicePoint object was last connected to a host.
        /// </summary>
        public DateTime IdleSince
        {
            get
            {
                return _idleSince;
            }
        }

        internal string LookupString
        {
            get
            {
                return _lookupString;
            }
        }

        /// <summary>
        /// Gets or sets the amount of time a connection associated with the ServicePoint object can remain idle before the connection is closed.
        /// </summary>
        public int MaxIdleTime
        {
            get
            {
                return _maxIdleTime;
            }
            set
            {
                if (value < -1 || value > int.MaxValue)
                {
                    throw new ArgumentOutOfRangeException();
                }
                if (value == _maxIdleTime)
                {
                    return;
                }
                _maxIdleTime = value;
                //need a thread lock for this operation.
                if (Interlocked.CompareExchange(ref _currentConnections, 0, 0) == 0)
                {
                    if (_expiringTimer != null)
                    {
                        _expiringTimer.Change(_maxIdleTime, Timeout.Infinite);                        
                    }
                }                
            }
        }

        internal IPEndPoint HostEndPoint
        {
            get
            {
                return _dnsHelper.GetHostEndPoint();
            }
        }
        
        /// <summary>
        /// Gets or sets a Boolean value that determines whether the Nagle algorithm is used on connections managed by this ServicePoint object.
        /// </summary>
        public bool UseNagleAlgorithm
        {
            get
            {
                return _useNagle;
            }
            set
            {
                _useNagle = value;
            }
        }

        internal bool UsesProxy
        {
            get
            {
                return _usesProxy;
            }
            set
            {
                _usesProxy = value;
            }
        }

        /// <summary>
        /// Removes the specified connection group from this ServicePoint object.
        /// </summary>
        /// <param name="connectionGroupName"></param>
        /// <returns></returns>
        public bool CloseConnectionGroup(string connectionGroupName)
        {
            return this.ReleaseConnectionGroup(connectionGroupName);
        }

        /// <summary>
        /// Enables or disables the keep-alive option on a TCP connection.
        /// </summary>
        /// <param name="enabled"></param>
        /// <param name="keepAliveTime"></param>
        /// <param name="keepAliveInterval"></param>
        public void SetTcpKeepAlive(bool enabled, int keepAliveTime, int keepAliveInterval)
        {
            if (enabled)
            {
                if (keepAliveTime <= 0)
                {
                    throw new ArgumentOutOfRangeException("keepAliveTime", "Must be greater than 0");
                }
                if (keepAliveInterval <= 0)
                {
                    throw new ArgumentOutOfRangeException("keepAliveInterval", "Must be greater than 0");
                }
            }
            _tcp_keepalive = enabled;
            _tcp_keepalive_time = keepAliveTime;
            _tcp_keepalive_interval = keepAliveInterval;
        }

        internal void SubmitRequest(HttpRequest request, string connName = null)
        {
            ConnectionGroup connGroup;            
            lock (this)
            {               
                connGroup = this.FindConnectionGroup(connName, false);
            }
            var forcedsubmit = false;
            var connection = connGroup.FindConnection(request, connName, out forcedsubmit);            
            if (connection == null)
            {
                //this request was aborted.
                return;
            }            
        }

        internal void SetCertificates(X509Certificate client, X509Certificate server)
        {
            _certificate = server;
            _clientCertificate = client;
        }
               
        /// <summary>
        /// Called this method when starting a new connection in this service point.
        /// </summary>
        internal void IncrementConnection()
        {
            if (Interlocked.Increment(ref _currentConnections) == 1)
            {
                if (_expiringTimer != null)
                {
                    _expiringTimer.Dispose();
                    _expiringTimer = null;
                }
            }
        }

        /// <summary>
        /// Called this method when removing a connection in this service point.
        /// </summary>
        internal void DecrementConnection()
        {
            if (Interlocked.Decrement(ref _currentConnections) == 0)
            {
                if (_currentConnections < 0)
                    Interlocked.Exchange(ref _currentConnections, 0);
                _idleSince = DateTime.Now;
                _expiringTimer = new Timer(ServicePointManager.IdleServicePointTimeoutDelegate, this, _maxIdleTime, Timeout.Infinite);
            }
        }

        //Sets connections in this group to not be KeepAlive.
        internal bool ReleaseConnectionGroup(string connName)
        {
            ConnectionGroup connectionGroup = null;
            lock (this)
            {
                connectionGroup = this.FindConnectionGroup(connName, true);
                if (connectionGroup == null)
                {
                    return false;
                }
                // Cancel the timer so it doesn't fire later and clean up a different 
                // connection group with the same name.
                connectionGroup.CancelIdleTimer();
                //remove ConnectionGroup from our Hashtable
                _groups.Remove(connName);
            }
            connectionGroup.DisableKeepAliveOnConnections();
            return true;
        }

        //Sets all connections in all connections groups to not be KeepAlive.
        internal void ReleaseAllConnectionGroups()
        {
            // To avoid deadlock (can't lock a ServicePoint followed by a Connection), copy out all the
            // connection groups in a lock, then release them all outside of it.
            var cgs = new List<ConnectionGroup>(_groups.Count);
            lock (this)
            {
                foreach (ConnectionGroup cg in _groups.Values)
                {
                    cgs.Add(cg);
                }
                _groups = new Dictionary<string, ConnectionGroup>(1);
            }
            foreach (var cg in cgs)
            {
                cg.CancelIdleTimer();
                cg.DisableKeepAliveOnConnections();
            }
        }

        private ConnectionGroup FindConnectionGroup(string connName, bool dontCreate)
        {
            ConnectionGroup connectionGroup;
            var lookupStr = ConnectionGroup.MakeQueryStr(connName);
            if (!_groups.TryGetValue(lookupStr, out connectionGroup) && !dontCreate)
            {
                connectionGroup = new ConnectionGroup(this, connName);
                _groups.Add(lookupStr, connectionGroup);
            }
            return connectionGroup;
        }      
    }   
}
