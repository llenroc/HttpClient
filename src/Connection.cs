// Copyright (c) 2015 Yamool. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

namespace Yamool.Net.Http
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;

    /// <summary>
    /// Represents a HTTP connection for HTTP transport. 
    /// </summary>
    internal sealed class Connection : IDisposable
    {
        private volatile bool _disposed;
        private int _state;
        private EndPoint _connectEndPoint;
        private Socket _socket;
        private ConnectionPool _connectionPool;

        public Connection(ConnectionPool connectionPool, EndPoint remoteEP)
        {
            _connectionPool = connectionPool;
            _connectEndPoint = remoteEP;
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
        }

        /// <summary>
        /// Gets the remote endpoint which client is connected to this.
        /// </summary>
        public EndPoint RemoteEndPoint
        {
            get
            {
                return _connectEndPoint;
            }
        }

        /// <summary>
        /// Indicates this connection is active now.
        /// </summary>
        public bool IsActive
        {
            get
            {
                return _state == 1;
            }
        }

        /// <summary>
        /// The socket object to communication.
        /// </summary>
        public Socket Socket
        {
            get
            {
                return _socket;
            }
        }

        public bool MakeAsActive()
        {
            return Interlocked.Exchange(ref _state, 1) == 0;
        }

        public bool MakeInActive()
        {
            return Interlocked.Exchange(ref _state, 0) == 1;
        }

        internal void IOCompleted(Saea saea)
        {
            if (saea.SocketError != SocketError.Success)
            {
                this.Close(false);
                return;
            }
            if (saea.LastOperation == SocketAsyncOperation.Connect)
            {
                _connectionPool.ServicePoint.CompletedConnection(_socket);
            }
        }

        public void Close(bool reuse)
        {
            if (reuse)
            {

            }
            else
            {
                if (_socket.Connected)
                {
                    _socket.Shutdown(SocketShutdown.Both);
                    _socket.Close();
                }
            }
        }

        public void Dispose()
        {
            this.Close(false);
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
