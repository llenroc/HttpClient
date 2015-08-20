// Copyright (c) 2015 Yamool. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

namespace Yamool.Net.Http
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// The connection stream with established to read data.
    /// </summary>
    internal class ConnectStream : Stream
    {
        private readonly Connection _connection;
        private readonly HttpRequest _request;
        private volatile bool _disposed;
        private readonly bool _chunked;
        private ArraySegment<byte> _readBuffer;
        private int _readOffset;
        private int _readBufferSize;
        private long _readBytes;
        private bool _chunkEofRecvd;
        private ChunkParser _chunkParser;

        public ConnectStream(Connection connection, ArraySegment<byte> buffer, int offset, int bufferCount, long readCount, bool chunked, HttpRequest request)
        {
            _connection = connection;
            _readBytes = readCount;
            _chunked = chunked;
            if (_chunked)
            {
                _chunkParser = new ChunkParser(connection, buffer, offset, bufferCount);
            }
            else
            {
                _readBuffer = buffer;
                _readOffset = offset;
                _readBufferSize = bufferCount;
            }
            _request = request;
        }

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        private bool Eof
        {
            get
            {
                if (_chunked)
                {
                    return _chunkEofRecvd;
                }
                return _readBytes == 0L;
            }
        }

        public override long Length
        {
            get
            {
                throw new NotSupportedException("This stream does not support seek operations.");
            }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException("This stream does not support seek operations.");
            }
            set
            {
                throw new NotSupportedException("This stream does not support seek operations.");
            }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException("Cannot support synchronous operations.");
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("This stream does not support seek operations.");
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("This stream does not support seek operations.");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return 0;
            //var leftBytes = _buffer.Count - _offset;
           
            //if (leftBytes > 0)
            //{
            //    count = Math.Min(count, leftBytes);
            //    Buffer.BlockCopy(_buffer.Array, _offset, buffer, 0, count);
            //    _offset += count;
            //    return count;
            //}
            //_offset = 0;
            //_buffer = await this.ReadBuffer();
            //leftBytes = _buffer.Count - offset;
            //if (leftBytes > 0)
            //{
            //    count = Math.Min(count, leftBytes);
            //    Buffer.BlockCopy(_buffer.Array, _offset, buffer, 0, count);
            //    _offset += count;
            //    return count;
            //}
            //return 0;
        }

        public async Task<ArraySegment<byte>> ReadNext()
        {
            this.CheckDisposed();
            if (_request.Aborted)
            {
                throw new OperationCanceledException("The request was canceled.");
            }
            if (this.Eof)
            {
                return new ArraySegment<byte>();
            }
            if (_chunked)
            {

            }
            var readData = new ArraySegment<byte>(_readBuffer.Array, _readOffset, _readBufferSize);
            var readBytes = readData.Count;
            if (_readBufferSize == 0)
            {
                _connection.SetBuffer(_connection.Buffer.Offset, _connection.Buffer.Length);
                readData = await _connection.ReadAsync();
                _readBufferSize = readData.Count;
            }
            _readBufferSize -= readData.Count;
            _readBytes -= readData.Count;
            return readData;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                        
            }
            base.Dispose(disposing);
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(base.GetType().FullName);
            }
        }
    }
}
