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
        private PooledBuffer _pooledBuffer;

        internal ConnectStream(Connection connection, HttpRequest request)
        {
            _connection = connection;
            _request = request;
        }

        internal ConnectStream(Connection connection, ArraySegment<byte> buffer, int offset, int bufferCount, long readCount, bool chunked, HttpRequest request)
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
            _pooledBuffer = connection.Buffer;
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
                return _readBytes == 0L || (_readBytes == -1L && _readBufferSize <= 0);
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
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (offset < 0 || offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            if (count < 0 || count > buffer.Length - offset)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            this.CheckCancelledOrDisposed();
            if (this.Eof)
            {
                return 0;
            }
            var bytesToRead = 0;
            if (_chunked)
            {
                if (!_chunkEofRecvd)
                {
                    bytesToRead = _chunkParser.Read(buffer, offset, count);
                    if (bytesToRead == 0)
                    {
                        _chunkEofRecvd = true;
                    }
                    return bytesToRead;
                }
            }
            count = Math.Min((int)_readBytes, count);
            bytesToRead = this.FillFromBufferedData(buffer, offset, count);
            if (bytesToRead > 0)
            {
                return bytesToRead;
            }
            bytesToRead = _connection.Read(buffer, offset, count);
            _readBytes -= bytesToRead;
            return bytesToRead;
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
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (offset < 0 || offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            if (count < 0 || count > buffer.Length - offset)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            this.CheckCancelledOrDisposed();
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("The request was canceled.");
            }
            if (this.Eof)
            {
                return 0;
            }
            var bytesToRead = 0;
            if (_chunked)
            {
                if (!_chunkEofRecvd)
                {
                    bytesToRead = await _chunkParser.ReadAsync(buffer, offset, count);
                    if (bytesToRead == 0)
                    {
                        _chunkEofRecvd = true;
                    }
                    return bytesToRead;
                }
            }
            count = Math.Min((int)_readBytes, count);
            bytesToRead = this.FillFromBufferedData(buffer, offset, count);
            if (bytesToRead == 0)
            {
                bytesToRead = await _connection.ReadAsync(buffer, offset, count);
            }           
            _readBytes -= bytesToRead;
            return bytesToRead;
        }

        public async Task<ArraySegment<byte>> ReadNextBuffer()
        {
            this.CheckCancelledOrDisposed();
            if (this.Eof)
            {
                return new ArraySegment<byte>();
            }
            if (_chunked)
            {
                if (!_chunkEofRecvd)
                {
                    var buffer = await _chunkParser.ReadNextBuffer();
                    if (buffer.Count == 0)
                    {
                        _chunkEofRecvd = true;
                    }
                    return buffer;
                }
            }            
            if (_readBufferSize > 0)
            {
                //return a previous cached buffer
                var bytesToRead = _readBufferSize;
                _readBytes -= bytesToRead;
                _readBufferSize -= bytesToRead;
                return new ArraySegment<byte>(_readBuffer.Array, _readOffset, bytesToRead);
            }
            var readedBuffer = await _connection.ReadPooledBufferAsync();
            _readBytes -= readedBuffer.Count;
            return readedBuffer;
        }

        private int FillFromBufferedData(byte[] buffer, int offset, int count)
        {
            if (_readBufferSize == 0)
            {
                return 0;
            }
            count = Math.Min(count, _readBufferSize);
            Buffer.BlockCopy(_readBuffer.Array, _readBuffer.Offset, buffer, offset, count);
            _readOffset += count;
            _readBufferSize -= count;
            return count;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                _connection.Close(true);
            }
            base.Dispose(disposing);
        }

        private void CheckCancelledOrDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(base.GetType().FullName);
            }
            if (_request.Aborted)
            {
                throw new OperationCanceledException("The request was canceled.");
            }
        }
    }
}
