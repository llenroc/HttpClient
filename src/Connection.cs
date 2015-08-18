// Copyright (c) 2015 Yamool. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

namespace Yamool.Net.Http
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Represents a HTTP connection for HTTP transport. 
    /// </summary>
    internal sealed class Connection
    {
        private readonly ConnectionGroup _connectionGorup;
        private readonly PooledBuffer _buffer;
        private readonly Saea _saea;
        private readonly AsyncReadWrite _asyncReadWrite;
        private readonly Socket _socket;
        private bool _busy;

        public Connection(ConnectionGroup connectionGroup)
        {
            _connectionGorup = connectionGroup;
            _saea = SaeaPool.Default.GetSaea();
            _buffer = BufferPool.Default.GetBuffer();
        }

        public PooledBuffer Buffer
        {
            get;
            private set;
        }

        public bool Busy
        {
            get
            {
                return _busy;
            }
        }

        public ServicePoint ServicePoint
        {
            get
            {
                return _connectionGorup.ServicePoint;
            }
        }

        internal bool CanReuse
        {
            get
            {
                return _socket.Poll(0, SelectMode.SelectRead) == false;
            }
        }

        public void SetBuffer(int offset, int count)
        {
            if (offset < 0 || offset < _buffer.Offset)
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            if (count < 0 || offset + count > _buffer.Length)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            _saea.SetBuffer(_buffer.Array, offset, count);
        }

        public async Task<ArraySegment<Byte>> ReadAsync()
        {            
            await _asyncReadWrite.Read();
            var offset = _buffer.Offset;
            return new ArraySegment<byte>(_buffer.Array, _buffer.Offset, (_saea.Offset - _buffer.Offset) + _saea.BytesTransferred);
        }

        public async Task<int> WriteAsync()
        {
            await _asyncReadWrite.Write();
            return _saea.BytesTransferred;
        }

        internal void CloseOnIdle()
        {
            throw new NotImplementedException();
        }

        private class AsyncReadWrite : INotifyCompletion
        {
            private static readonly Action Sentinal = () => { };
            private Action _continuation;
            private readonly Saea _saea;
            private bool _completed;
            private Socket _socket;

            public AsyncReadWrite(Socket socket, Saea saea)
            {
                _socket = socket;
                _saea = saea;
                _saea.OnCompleted(_ =>
                {
                    var previous = _continuation ?? Interlocked.CompareExchange(ref _continuation, Sentinal, null);
                    if (previous != null)
                    {
                        previous();
                    }
                });
            }

            public bool IsCompleted
            {
                get
                {
                    return _completed;
                }
            }

            public void GetResult()
            {
                if (_saea.SocketError != SocketError.Success)
                {
                    throw new HttpRequestException("Occurring an exception during the HTTP request operations.Error:" + _saea.SocketError);
                }
            }

            public AsyncReadWrite GetAwaiter()
            {
                return this;
            }

            public AsyncReadWrite Read()
            {
                if (!_socket.ReceiveAsync(_saea))
                {
                    _completed = true;
                }
                return this;
            }

            public AsyncReadWrite Write()
            {
                if (!_socket.SendAsync(_saea))
                {
                    _completed = true;
                }
                return this;
            }

            void INotifyCompletion.OnCompleted(Action continuation)
            {
                if (_continuation == Sentinal || Interlocked.CompareExchange(ref _continuation, continuation, null) == Sentinal)
                {
                    Task.Run(continuation);
                }
            }
        }
    }
}
