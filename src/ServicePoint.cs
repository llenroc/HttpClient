// Copyright (c) 2015 Yamool. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

namespace Yamool.Net.Http
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.Net;
    using System.Net.Sockets;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;

    /// <summary>
    /// Provides connection management for HTTP connections.
    /// </summary>
    public class ServicePoint
    {
        private const int DefaultConnectionListSize = 3;
        private Uri _uri;
        private DateTime _idleSince;
        private int _connectionLimit;
        private bool _usesProxy;
        private int _maxIdleTime;
        private bool _useNagle;
        private int _activeConnections;
        private X509Certificate _certificate;
        private X509Certificate _clientCertificate;
        private bool _tcp_keepalive;
        private int _tcp_keepalive_time;
        private int _tcp_keepalive_interval;
        private readonly string _lookupString;
        private Timer _expiringTimer;
        private DnsResolverHelper _dnsHelper;
        private List<Connection> _connections;

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
            _connections = new List<Connection>(DefaultConnectionListSize);
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
                this.PruneExcesiveConnections();
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
                return _connections.Count;
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
                if (Interlocked.CompareExchange(ref _activeConnections, 0, 0) == 0)
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

        internal void KeepAliveSetup(Socket socket)
        {
            if (!_tcp_keepalive)
            {
                return;
            }
            var bytes = new byte[12];
            PutBytes(bytes, (uint)(_tcp_keepalive ? 1 : 0), 0);
            PutBytes(bytes, (uint)_tcp_keepalive_time, 4);
            PutBytes(bytes, (uint)_tcp_keepalive_interval, 8);
            socket.IOControl(IOControlCode.KeepAliveValues, bytes, null);
        }

        internal Connection SubmitRequest(HttpRequest request)
        {
            var created = false;
            var connection = this.CreateOrReuseConnection(request, out created);
            connection.SubmitRequest(request);
            return connection;
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
            if (Interlocked.Increment(ref _activeConnections) == 1)
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
            if (Interlocked.Decrement(ref _activeConnections) == 0)
            {
                if (_activeConnections < 0)
                {
                    Interlocked.Exchange(ref _activeConnections, 0);
                }
                _idleSince = DateTime.Now;
                _expiringTimer = new Timer(ServicePointManager.IdleServicePointTimeoutDelegate, this, _maxIdleTime, Timeout.Infinite);
            }
        }

        /// <summary>
        /// Sets all connections to not be KeepAlive
        /// </summary>
        internal void ReleaseAllConnections()
        {
            var removedConnections = new List<Connection>(_connections.Count);
            lock (this)
            {
                foreach (var connection in _connections)
                {
                    removedConnections.Add(connection);
                }
                _connections = new List<Connection>();
            }
            foreach (var connection in removedConnections)
            {
                connection.CloseOnIdle();
            }
        }

        private Connection CreateOrReuseConnection(HttpRequest request, out bool created)
        {
            Connection newConnection = null;
            var freeConnectionsAvail = false;
            created = false;
            lock (this)
            {
                foreach (var currentConnection in _connections)
                {
                    if (currentConnection.BusyCount > 0)
                    {
                        continue;
                    }
                    freeConnectionsAvail = true;
                    newConnection = currentConnection;
                    created = true;
                }
                if (!freeConnectionsAvail && this.CurrentConnections < _connectionLimit)
                {
                    newConnection = new Connection(this);
                    _connections.Add(newConnection);
                }
                else
                {
                    if (this.CurrentConnections > _connectionLimit)
                    {
                        //waiting when get an available connection or throw an exception?
                        throw new InvalidOperationException("The maximum number of the service point connection has been reached.");
                    }
                }
                newConnection.MarkAsReserved();
            }
            return newConnection;
        }

        /// <summary>
        /// Removes extra connections that are found when reducing the connection limit
        /// </summary>
        private void PruneExcesiveConnections()
        {
            var connectionsToClose = new List<Connection>();
            lock (this)
            {
                var connectionLimit = this.ConnectionLimit;
                if (this.CurrentConnections > connectionLimit)
                {
                    var numberToPrune = this.CurrentConnections - connectionLimit;
                    for (var i = 0; i < numberToPrune; i++)
                    {
                        connectionsToClose.Add(_connections[i]);
                    }
                    _connections.RemoveRange(0, numberToPrune);
                }
            }
            foreach (var connection in connectionsToClose)
            {
                connection.CloseOnIdle();
            }
        }

        private static void PutBytes(byte[] bytes, uint v, int offset)
        {
            if (BitConverter.IsLittleEndian)
            {
                bytes[offset] = (byte)(v & 0x000000ff);
                bytes[offset + 1] = (byte)((v & 0x0000ff00) >> 8);
                bytes[offset + 2] = (byte)((v & 0x00ff0000) >> 16);
                bytes[offset + 3] = (byte)((v & 0xff000000) >> 24);
            }
            else
            {
                bytes[offset + 3] = (byte)(v & 0x000000ff);
                bytes[offset + 2] = (byte)((v & 0x0000ff00) >> 8);
                bytes[offset + 1] = (byte)((v & 0x00ff0000) >> 16);
                bytes[offset] = (byte)((v & 0xff000000) >> 24);
            }
        }
    }   
}
