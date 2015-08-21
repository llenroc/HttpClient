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
    internal sealed class Connection : IDisposable
    {
        private readonly ServicePoint _servicePoint;
        private readonly PooledBuffer _buffer;
        private readonly Saea _saea;
        private readonly AsyncReadWrite _asyncReadWrite;
        private readonly Socket _socket;
        private bool _idle = true;
        private int _reservedCount;
        private DateTime _idleSinceUtc;

        public Connection(ServicePoint servicePoint)
        {
            _servicePoint = servicePoint;
            _saea = SaeaPool.Default.GetSaea();
            _buffer = BufferPool.Default.GetBuffer();
            _socket = new Socket(servicePoint.HostEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _socket.NoDelay = !servicePoint.UseNagleAlgorithm;
            _asyncReadWrite = new AsyncReadWrite(_socket, _saea);

        }

        public int BusyCount
        {
            get
            {
                return _reservedCount;
            }
        }

        public PooledBuffer Buffer
        {
            get
            {
                return _buffer;
            }
        }

        public ServicePoint ServicePoint
        {
            get
            {
                return _servicePoint;
            }
        }

        public async Task<bool> ConnectAsync()
        {
            _saea.SetBuffer(0, 0);
            await _asyncReadWrite.Connect(_servicePoint.HostEndPoint);
            return true;
        }

        public void Dispose()
        {
            try
            {
                this.CloseSocket(0);
            }
            finally
            {
                _saea.Free();
                _buffer.Dispose();
            }
            GC.SuppressFinalize(this);
        }

        public Task<ArraySegment<Byte>> ReadPooledBufferAsync()
        {
            return this.ReadPooledBufferAsync(_buffer.Offset, _buffer.Length); 
        }

        public async Task<ArraySegment<Byte>> ReadPooledBufferAsync(int offset, int count)
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
            await _asyncReadWrite.Read();
            return new ArraySegment<byte>(_buffer.Array, _buffer.Offset, (_saea.Offset - _buffer.Offset) + _saea.BytesTransferred);
        }

        public void CloseSocket(int timeout = 0)
        {
            var abortSocket = _socket;
            if (abortSocket != null)
            {
                abortSocket.Close(timeout);
            }
        }

        internal int Read(byte[] buffer, int offset, int count)
        {
            return _socket.Receive(buffer, offset, count, SocketFlags.None);
        }

        internal async Task<int> ReadAsync(byte[] buffer, int offset, int count)
        {
            _saea.SetBuffer(buffer, offset, count);
            await _asyncReadWrite.Read();
            return _saea.BytesTransferred;
        }

        internal async Task<int> WritePooledBufferAsync(int offset, int count)
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
            await _asyncReadWrite.Write();
            return _saea.BytesTransferred;
        }

        /// <summary>
        /// Cause the Connection to Close and Abort its socket,after the next request is completed. 
        /// If the Connection is already idle, then Aborts the socket immediately.
        /// </summary>
        internal void CloseOnIdle()
        {
            lock (this)
            {
                if (!_idle)
                {
                    this.CheckIdle();
                }               
                else
                {
                    //If the Connection is already idle, then Aborts the socket immediately.
                    _saea.Free();
                    this.CloseSocket(0);
                    _buffer.Dispose();
                    GC.SuppressFinalize(this);
                }
            }
        }

        internal bool SubmitRequest(HttpRequest request)
        {
            lock (this)
            {
                this.CheckNonIdle();
            }
            return true;
        }       

        private void CheckIdle()
        {
            if (!_idle)
            {
                _idle = true;
                this.ServicePoint.DecrementConnection();
                _idleSinceUtc = DateTime.UtcNow;
            }
        }

        private void CheckNonIdle()
        {
            if (_idle)
            {
                _idle = false;
                this.ServicePoint.IncrementConnection();
            }
        }

        internal void MarkAsReserved()
        {
            Interlocked.Increment(ref _reservedCount);          
        }

        internal void Free()
        {
            Interlocked.Decrement(ref _reservedCount);
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

            private bool CanReuse
            {
                get
                {
                    return _socket.Connected && _socket.Poll(0, SelectMode.SelectRead) == true;
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

            public AsyncReadWrite Connect(EndPoint remoteEP)
            {
                this.Reset();
                _saea.RemoteEndPoint = remoteEP;
                if (this.CanReuse || !_socket.ConnectAsync(_saea))
                {
                    _completed = true;
                }
                return this;
            }

            public AsyncReadWrite Read()
            {
                this.Reset();
                if (!_socket.ReceiveAsync(_saea))
                {
                    _completed = true;
                }
                return this;
            }

            public AsyncReadWrite Write()
            {
                this.Reset();
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

            private void Reset()
            {
                _completed = false;
                _continuation = null;
            }
        }
    }
}
