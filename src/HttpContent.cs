// Copyright (c) 2015 Yamool. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

namespace Yamool.Net.Http
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using System.Text;
    using Yamool.Net.Http.Headers;

    /// <summary>
    /// A base class representing an HTTP entity body and content headers.
    /// </summary>
    public abstract class HttpContent : IDisposable
    {
        internal static readonly Encoding DefaultStringEncoding = Encoding.UTF8;
        private HttpContentHeaders _headers;
        private volatile bool _disposed;
        private MemoryStream _bufferedContent;
        private bool _canCalculateLength;

        protected HttpContent()
        {
            _canCalculateLength = true;
        }

        public HttpContentHeaders Headers
        {
            get
            {
                if (_headers == null)
                {
                    _headers = new HttpContentHeaders(this.GetComputedOrBufferLength);
                }
                return _headers;
            }
        }

        private bool IsBuffered
        {
            get
            {
                return _bufferedContent != null;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public Task CopyToAsync(Stream stream)
        {
            var tcs = new TaskCompletionSource<object>();
            tcs.SetResult(null);
            return tcs.Task;
        }

        protected abstract Task SerializeToStreamAsync(Stream stream);

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;

            }
        }

        protected internal abstract bool TryComputeLength(out long length);

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(base.GetType().FullName);
            }
        }

        private long? GetComputedOrBufferLength()
        {
            this.CheckDisposed();
            if (this.IsBuffered)
            {
                return new long?(_bufferedContent.Length);
            }
            if (_canCalculateLength)
            {
                long value = 0L;
                if (this.TryComputeLength(out value))
                {
                    return new long?(value);
                }
                _canCalculateLength = false;
            }
            return null;
        }
    }
}
