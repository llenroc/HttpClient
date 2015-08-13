// Copyright (c) 2015 Yamool. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

namespace Yamool.Net.Http
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides connection management for HTTP connections.
    /// </summary>
    public class ServicePoint
    {        
        private SemaphoreSlim _semaphoreConnections;
        private string _host;
        private int _port;
        private string _lookupString;
        private DateTime _idleSince;
        private bool _isProxyServicePoint;
        private bool _useTcpKeepAlive;
        private int _tcpKeepAliveTime;
        private int _tcpKeepAliveInterval;
        private ConnectionPool _connectionPool;

        internal ServicePoint(Uri address, int defaultConnectionLimit, string lookupString, bool proxyServicePoint)
        {
            _host = address.DnsSafeHost;
            _port = address.Port;
            _idleSince = DateTime.Now;
            _lookupString = lookupString;
            _isProxyServicePoint = proxyServicePoint;
            _semaphoreConnections = new SemaphoreSlim(defaultConnectionLimit);
            _connectionPool = new ConnectionPool(this);
        }

        internal ConnectionReadWriteAwaitable SubmitRequest(CancellationToken requestCancellationToken)
        {
            var allAddress = Dns.GetHostAddresses(_host);
            if (allAddress == null || allAddress.Length == 0)
            {
                throw new HttpRequestException("Cannot resolve host." + _host);
            }
            var ipAddress = allAddress.Where(k => k.AddressFamily == AddressFamily.InterNetwork).First();
            var connectedEP = new IPEndPoint(ipAddress, _port);
            var connection = _connectionPool.FindConnection(connectedEP);
            return new ConnectionReadWriteAwaitable(connection, requestCancellationToken);
        }

        public bool InternalProxyServicePoint
        {
            get
            {
                return _isProxyServicePoint;
            }
        }

        public void SetKeepAlives(bool enable)
        {
            this.SetKeepAlives(enable, 900000, 1000);
        }

        public void SetKeepAlives(bool enabled, int keepAliveTime, int keepAliveInterval)
        {
            if (!enabled)
            {
                _useTcpKeepAlive = false;
                _tcpKeepAliveTime = 0;
                _tcpKeepAliveInterval = 0;
                return;
            }
            if (keepAliveTime <= 0)
            {
                throw new ArgumentOutOfRangeException("keepAliveTime");
            }
            if (keepAliveInterval <= 0)
            {
                throw new ArgumentOutOfRangeException("keepAliveInterval");
            }
            _tcpKeepAliveTime = keepAliveTime;
            _tcpKeepAliveInterval = keepAliveInterval;
            _useTcpKeepAlive = true;
        }

        internal void CompletedConnection(Socket connectedSocket)
        {
            if (_useTcpKeepAlive)
            {
                var optionInValue = new byte[] { 1, 0, 0, 0, 
                    (byte)(_tcpKeepAliveTime & 255), (byte)(_tcpKeepAliveTime >> 8 & 255), (byte)(_tcpKeepAliveTime >> 16 & 255), (byte)(_tcpKeepAliveTime >> 24 & 255), 
                    (byte)(_tcpKeepAliveInterval & 255), (byte)(_tcpKeepAliveInterval >> 8 & 255), (byte)(_tcpKeepAliveInterval >> 16 & 255), (byte)(_tcpKeepAliveInterval >> 24 & 255) };
                connectedSocket.IOControl(IOControlCode.KeepAliveValues, optionInValue, null);
            }
        }
    }
}
