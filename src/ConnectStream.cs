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
        private readonly ConnectionReadWriteAwaitable _connection;
        private readonly HttpRequest _request;
        private volatile bool _disposed;    
        private readonly bool _chunked; 
        private ArraySegment<byte> _readBuffer;
        private int _readOffset;
        private int _readBufferSize;
        private long _readBytes;       
        private bool _chunkEofRecvd;
        private ChunkParser _chunkParser;

        public ConnectStream(ConnectionReadWriteAwaitable connection, ArraySegment<byte> buffer, int offset, int bufferCount, long readCount, bool chunked, HttpRequest request)
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
            this.CheckDisposed();
            if (_request.Cancelled)
            {
                throw new OperationCanceledException("The request was canceled.");
            }
            return this.DoReadAsync(buffer, offset, count).ConfigureAwait(false).GetAwaiter().GetResult();
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

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
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
            this.CheckDisposed();
            if (_request.Cancelled || cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("The request was canceled.");
            }
            return this.DoReadAsync(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                _connection.Dispose();
            }
        }        

        private int FillFromBufferedData(byte[] buffer, int offset, int count)
        {
            if (_readBufferSize == 0)
            {
                return 0;
            }
            var bytesTransferred = Math.Min(count, _readBufferSize);      
            Buffer.BlockCopy(_readBuffer.Array, _readBuffer.Offset, buffer, offset, count);
            _readOffset += bytesTransferred;
            _readBufferSize -= bytesTransferred;           
            return bytesTransferred;
        }

        private async Task<int> DoReadAsync(byte[] buffer, int offset, int count)
        {
            var bytesToRead = 0;
            if (_chunked)
            {
                if (!_chunkEofRecvd)
                {
                    bytesToRead = await _chunkParser.ReadAsync(buffer, offset, count).ConfigureAwait(false);
                    if (bytesToRead == 0)
                    {
                        _chunkEofRecvd = true;
                    }
                    return bytesToRead;
                }
            }
            else
            {
                if (_readBytes != 0)
                {
                    bytesToRead = (int)Math.Min(_readBytes, (long)count);
                }
                else
                {
                    bytesToRead = count;
                }
            }
            if (bytesToRead == 0 || this.Eof)
            {
                return 0;
            }
            var bytesTransferred = await this.InternalReadAsync(buffer, offset, bytesToRead).ConfigureAwait(false);
            var doneReading = false;
            if (bytesTransferred <= 0)
            {
                bytesTransferred = 0;
                doneReading = true;
            }
            if (_readBytes != -1)
            {
                _readBytes -= bytesTransferred;
                if (_readBytes < 0)
                {
                    throw new HttpOperationException();
                }
            }
            if (_readBytes == 0 || doneReading)
            {
                _readBytes = 0;
            }
            return bytesTransferred;
        }

        private async Task<int> InternalReadAsync(byte[] buffer, int offset, int count)
        {
            var bytesToRead = this.FillFromBufferedData(buffer, offset, count);
            if (bytesToRead > 0)
            {
                return bytesToRead;
            }
            await _connection.ReadAsync();
            _readBuffer = _connection.TransferredBytes;
            _readBufferSize = _connection.TransferredCount;
            _readOffset = 0;
            return this.FillFromBufferedData(buffer, offset, count);
        }        

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(this.GetType().BaseType.FullName);
            }
        }
    }
}
