// Copyright (c) 2015 Yamool. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

namespace Yamool.Net.Http
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    internal class ConnectionGroup
    {
        private const int DefaultConnectionListSize = 3;
        private List<Connection> _connections;
        private int _connectionLimit;
        private ServicePoint _servicePoint;
        private string _name;
        private int _activeConnections;
        private Lazy<ManualResetEvent> _event;

        public ConnectionGroup(ServicePoint servicePoint, string connName)
        {
            _servicePoint = servicePoint;
            _connectionLimit = servicePoint.ConnectionLimit;
            _name = MakeQueryStr(connName);
            _connections = new List<Connection>(DefaultConnectionListSize);
            _event = new Lazy<ManualResetEvent>(() => new ManualResetEvent(false));
        }

        public ServicePoint ServicePoint
        {
            get
            {
                return _servicePoint;
            }
        }

        internal int ConnectionLimit
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

        internal int CurrentConnections
        {
            get
            {
                return _activeConnections;
            }
        }

        private ManualResetEvent AsyncWaitHandle
        {
            get
            {
                return _event.Value;
            }
        }

        internal void Associate(Connection connection)
        {
            lock (_connections)
            {
                _connections.Add(connection);
            }
        }

        internal void Disassociate(Connection connection)
        {
            lock (_connections)
            {
                _connections.Remove(connection);
            }
        }

        internal Connection FindConnection(HttpRequest request, out bool created)
        {
            Connection newConnection = null;
            created = false;
            lock (_connections)
            {
                foreach (var currentConnection in _connections)
                {
                    if (currentConnection.Busy)
                    {
                        continue;
                    }
                    newConnection = currentConnection;                    
                }
                if (!created)
                {

                }
            }
            return newConnection;
        }

        internal void DisableKeepAliveOnConnections()
        {
            throw new NotImplementedException();
        }

        internal void CancelIdleTimer()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Removes extra connections that are found when reducing the connection limit
        /// </summary>
        private void PruneExcesiveConnections()
        {
            var connectionsToClose = new List<Connection>();
            lock (_connections)
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
            foreach(var connection in connectionsToClose)
            {
                connection.CloseOnIdle();
            }
        }

        internal static string MakeQueryStr(string connName)
        {
            return connName ?? "";
        }
    }
}
