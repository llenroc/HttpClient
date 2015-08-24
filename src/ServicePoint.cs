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
        private DnsResolverHelper _dnsHelper;
        private List<Connection> _connections;
        private Timer _idleTimer;
        private int _currentConnections;

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
                if (_idleTimer != null)
                {
                    try
                    {
                        _idleTimer.Change(_maxIdleTime, _maxIdleTime);
                    }
                    catch (ObjectDisposedException) { }
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
            _idleSince = DateTime.Now;
            var created = false;
            Connection connection = this.FindConnection(request, out created);
            if (created)
            {
                Interlocked.Increment(ref _currentConnections);
                if (_idleTimer == null)
                {
                    var timer= new Timer(this.IdleTimerCallback, null, _maxIdleTime, _maxIdleTime);
                    if (Interlocked.CompareExchange(ref _idleTimer, timer, null) != null)
                    {
                        timer.Dispose();
                    }
                }               
            }
            connection.SubmitRequest(request);
            return connection;
        }

        internal void SetCertificates(X509Certificate client, X509Certificate server)
        {
            _certificate = server;
            _clientCertificate = client;
        }

        internal void ReleaseConnection(Connection connection)
        {
            lock (_connections)
            {
                if (_connections.Remove(connection))
                {
                    Interlocked.Decrement(ref _currentConnections);
                }
            }
        }

        private void IdleTimerCallback(object state)
        {
            var now = DateTime.UtcNow;
            var maxIdleTime = TimeSpan.FromMilliseconds(_maxIdleTime);
            List<Connection> connectionsToClose = null;
            lock (_connections)
            {
                foreach (var conn in _connections)
                {
                    if (conn.Busy)
                    {
                        continue;
                    }
                    if ((now - conn.IdleSince) > maxIdleTime)
                    {
                        conn.TrySetBusy();
                        if (connectionsToClose == null)
                        {
                            connectionsToClose = new List<Connection>();
                        }
                        connectionsToClose.Add(conn);
                    }
                }
            }
            if (connectionsToClose != null && connectionsToClose.Count > 0)
            {
                foreach (var conn in connectionsToClose)
                {
                    conn.Close();
                    if (Interlocked.Decrement(ref _currentConnections) <= 0)
                    {
                        ServicePointManager.IdleServicePointTimeout(this);
                        var timer = Interlocked.Exchange(ref _idleTimer, null);
                        if (timer != null)
                        {
                            timer.Dispose();
                        }
                    }
                }
            }
        }

        private Connection FindConnection(HttpRequest request, out bool created)
        {
            Connection newConnection = null;
            var freeConnectionsAvail = false;
            created = false;
            lock (_connections)
            {
                foreach (var currentConnection in _connections)
                {
                    if (currentConnection.Busy)
                    {
                        continue;
                    }
                    freeConnectionsAvail = true;
                    newConnection = currentConnection;
                }
                if (!freeConnectionsAvail && _currentConnections < _connectionLimit)
                {
                    newConnection = new Connection(this);
                    _connections.Add(newConnection);
                    created = true;
                }
                else
                {
                    if (_currentConnections > _connectionLimit)
                    {
                        throw new InvalidOperationException("The maximum number of the service point connection has been reached.");
                    }
                }
                newConnection.TrySetBusy();
            }
            return newConnection;
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
