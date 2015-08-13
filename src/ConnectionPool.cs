// Copyright (c) 2015 Yamool. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

namespace Yamool.Net.Http
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;

    internal class ConnectionPool : IDisposable
    {
        private ServicePoint _servicePoint;
        private bool _isDisposed;
        private int _activeConnections;
        private List<Connection> _connections;

        public ConnectionPool(ServicePoint servicePoint)
        {
            _servicePoint = servicePoint;
            _connections = new List<Connection>(3);
        }

        public ServicePoint ServicePoint
        {
            get
            {
                return _servicePoint;
            }
        }

        public Connection FindConnection(EndPoint remoteEP)
        {
            Connection newConnection = null;

            lock (_connections)
            {
                foreach (var currentConnection in _connections)
                {
                    if (currentConnection.RemoteEndPoint == remoteEP && !currentConnection.IsActive)
                    {
                        newConnection = currentConnection;
                        break;
                    }
                }
                if (newConnection == null)
                {
                    newConnection = new Connection(this, remoteEP);
                    _connections.Add(newConnection);
                }
                newConnection.MakeAsActive();
            }
            return newConnection;
        }

        public void Dispose()
        {
            //close all connections.
            GC.SuppressFinalize(this);
        }
    }
}
