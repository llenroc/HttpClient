// Copyright (c) 2015 Yamool. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

namespace Yamool.Net.Http
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    /// A base class representing an HTTP entity body and content headers.
    /// </summary>
    public abstract class HttpContent : IDisposable
    {
        private volatile bool _disposed;

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
            }
        }
    }
}
