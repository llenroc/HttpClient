// Copyright (c) 2015 Yamool. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

namespace Yamool.Net.Http
{
    using System;
    using System.Net.Sockets;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    internal class ConnectionReadWriteAwaitable : IDisposable, INotifyCompletion
    {
        private static readonly Action Sentinal = () => { };
        private readonly Connection _connection;
        private readonly CancellationToken _requestCancellationToken;
        private readonly Saea _saea;
        private readonly PooledBuffer _pooledBuffer;
        private bool _completed;
        private Action _continuation;
        private int _readerIndex;
        private int _readerCount;

        public ConnectionReadWriteAwaitable(Connection connection, CancellationToken requestCancellationToken)
        {
            _connection = connection;
            _requestCancellationToken = requestCancellationToken;
            _saea = SaeaPool.Default.GetSaea();            
            _saea.AcceptSocket = connection.Socket;
            _saea.RemoteEndPoint = connection.RemoteEndPoint;
            _pooledBuffer = BufferPool.Default.GetBuffer();          
            _saea.OnCompleted((saea) =>
            {
                _connection.IOCompleted(saea);
                //continue call the next action.
                var previous = _continuation ?? Interlocked.CompareExchange(ref _continuation, Sentinal, null);
                if (previous != null)
                {
                    previous();
                }
            });
            _saea.SetBuffer(_pooledBuffer.Array, _pooledBuffer.Offset, _pooledBuffer.Length);
            this.ResetBuffer();
        }

        internal PooledBuffer InternalBuffer
        {
            get
            {
                return _pooledBuffer;
            }
        }

        public bool IsCompleted
        {
            get
            {
                return _completed;
            }
        }

        public int TransferredCount
        {
            get
            {
                return (_readerIndex - _pooledBuffer.Offset) + _saea.BytesTransferred;
            }
        }

        public SocketError LastSocketError
        {
            get
            {
                return _saea.SocketError;
            }
        }

        public ArraySegment<byte> TransferredBytes
        {
            get
            {
                return new ArraySegment<byte>(_pooledBuffer.Array, _pooledBuffer.Offset, this.TransferredCount);
            }
        }

        public ConnectionReadWriteAwaitable GetAwaiter()
        {
            return this;
        }

        public void GetResult()
        {
            _requestCancellationToken.ThrowIfCancellationRequested();
            if (_saea.SocketError != SocketError.Success)
            {
                throw new HttpRequestException("Occurring an exception during the Http request operations. Error[" + _saea.SocketError + "]");
            }
        }
        
        /// <summary>
        /// Connecting to a remote host with in asynchronous.
        /// </summary>
        /// <returns></returns>
        public ConnectionReadWriteAwaitable ConnectAsync()
        {
            this.Reset();
            _saea.SetBuffer(0, 0);
            if (!_connection.Socket.ConnectAsync(_saea))
            {
                _completed = true;
            }
            return this;
        }

        /// <summary>
        /// Write bytes of data to a connected remote host with in asynchronous.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public ConnectionReadWriteAwaitable WriteAsync(byte[] buffer, int offset, int count)
        {
            this.Reset();            
            var minCount = Math.Min(count, _pooledBuffer.Length);
            Buffer.BlockCopy(buffer, offset, _pooledBuffer.Array, _pooledBuffer.Offset, minCount);
            _saea.SetBuffer(_pooledBuffer.Offset, minCount);
            if (!_connection.Socket.SendAsync(_saea))
            {
                _completed = true;
            }
            return this;
        }

        public ConnectionReadWriteAwaitable ReadAsync()
        {
            this.Reset();
            _saea.SetBuffer(_pooledBuffer.Array, _readerIndex, _readerCount);
            if (!_connection.Socket.ReceiveAsync(_saea))
            {
                _completed = true;
            }
            return this;
        }

        public ArraySegment<byte> Read()
        {
            var bytesToRead = _connection.Socket.Receive(_pooledBuffer.Array, _readerIndex, _readerCount, SocketFlags.None);
            return new ArraySegment<byte>(_pooledBuffer.Array, _readerIndex, bytesToRead);
        }

        public void Dispose()
        {            
            SaeaPool.Default.FreeSaea(_saea);
            BufferPool.Default.FreeBuffer(_pooledBuffer);
            _connection.Close(true);
            GC.SuppressFinalize(this);
        }

        internal void Reset()
        {
            _completed = false;
            _continuation = null;
        }

        internal void SetBuffer(int offset, int count)
        {
            count = Math.Min(_pooledBuffer.Length, count);
            _readerIndex = _pooledBuffer.Offset + offset;
            _readerCount = count;
        }

        internal void ResetBuffer()
        {
            _readerIndex = _pooledBuffer.Offset;
            _readerCount = _pooledBuffer.Length;
        }

        internal int MoveBufferBytesToHead(int offset, int count)
        {
            Buffer.BlockCopy(_pooledBuffer.Array, offset, _pooledBuffer.Array, _pooledBuffer.Offset, count);
            return _pooledBuffer.Length - count;
        }

        void INotifyCompletion.OnCompleted(Action continuation)
        {
            if (_continuation == Sentinal ||
                Interlocked.CompareExchange(ref _continuation, continuation, null) == Sentinal)
            {
                Task.Run(continuation);
            }
        }
    }
}
