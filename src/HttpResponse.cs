// Copyright (c) 2015 Yamool. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

namespace Yamool.Net.Http
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Text.RegularExpressions;
    using System.Net;
    using System.Text;

    /// <summary>
    /// Represents the HTTP response message from the remote server response.
    /// </summary>
    public class HttpResponse : IDisposable
    {
        private readonly static Regex charsetRegex = new Regex(@"charset\s?=\s?([\w-]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private Uri _uri;
        private HttpMethod _method;
        private CoreResponseData _coreResponseData;
        private Stream _connectStream;
        private HttpResponseHeaders _headers;
        private long _contentLength;
        private HttpStatusCode _statusCode;
        private string _statusDescription;
        private HttpVersion _version;
        private volatile bool _disposed;
        private string _characterSet;

        internal HttpResponse(Uri responseUri, HttpMethod method, CoreResponseData coreData, DecompressionMethods decompressionMethod)
        {
            _uri = responseUri;
            _method = method;
            _coreResponseData = coreData;
            _connectStream = coreData.ConnectStream;
            _headers = coreData.ResponseHeaders;            
            _statusCode = coreData.StatusCode;
            _contentLength = coreData.ContentLength;
            _statusDescription = coreData.StatusDescription;
            _version = coreData.HttpVersion;
            //if (this.m_ContentLength == 0L && this.m_ConnectStream is ConnectStream)
            //{
            //    ((ConnectStream)this.m_ConnectStream).CallDone();
            //}
            var text = _headers[HttpHeaderNames.ContentLocation];
            if (text != null)
            {
                try
                {
                    Uri uri;
                    if (Uri.TryCreate(_uri, text, out uri))
                    {
                        _uri = uri;
                    }
                }
                catch { }
            }
            if (decompressionMethod != DecompressionMethods.None)
            {
                if ((text = _headers[HttpHeaderNames.ContentEncoding]) != null)
                {
                    if ((decompressionMethod & DecompressionMethods.GZip) == DecompressionMethods.GZip && text.IndexOf(HttpRequest.GZipHeader, 0, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _connectStream = new GZipStream(_connectStream, CompressionMode.Decompress);
                        _contentLength = -1L;
                        _headers.SetInternal(HttpHeaderNames.TransferEncoding, null);
                    }
                    else if ((decompressionMethod & DecompressionMethods.Deflate) == DecompressionMethods.Deflate && text.IndexOf(HttpRequest.DeflateHeader, 0, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _connectStream = new DeflateStream(_connectStream, CompressionMode.Decompress);
                        _contentLength = -1L;
                        _headers.SetInternal(HttpHeaderNames.TransferEncoding, null);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the character set of the response.
        /// </summary>
        public string CharacterSet
        {
            get
            {
                this.CheckDisposed();
                var contentType = _headers.ContentType;
                if (_characterSet == null)
                {
                    _characterSet = String.Empty;
                    var m = charsetRegex.Match(contentType);
                    if (m.Success)
                    {
                        _characterSet = m.Groups[1].Value;
                    }
                }
                return _characterSet;
            }
        }

        /// <summary>
        /// Gets the URI of the Internet resource that responded to the request.
        /// </summary>
        public Uri ResponseUri
        {
            get
            {
                this.CheckDisposed();
                return _uri;
            }
        }

        /// <summary>
        /// Gets the method that is used to encode the body of the response.
        /// </summary>
        public string ContentEncoding
        {
            get
            {
                this.CheckDisposed();
                return _headers.ContentEncoding ?? string.Empty;
            }
        }

        /// <summary>
        /// Gets the length of the content returned by the request.
        /// </summary>
        public long ContentLength
        {
            get
            {
                this.CheckDisposed();
                return _contentLength;
            }
        }

        /// <summary>
        /// Gets the content type of the response.
        /// </summary>
        public string ContentType
        {
            get
            {
                this.CheckDisposed();
                return _headers.ContentType ?? string.Empty;
            }
        }

        /// <summary>
        /// Gets the last date and time that the contents of the response were modified.
        /// </summary>
        public DateTime LastModified
        {
            get
            {
                this.CheckDisposed();
                var value = _headers.LastModified;
                if (value.HasValue)
                {
                    return value.Value;
                }
                return DateTime.Now;
            }
        }

        public HttpVersion ProtocolVersion
        {
            get
            {
                this.CheckDisposed();
                return _version;
            }
        }

        /// <summary>
        /// Gets the name of the server that sent the response.
        /// </summary>
        public string Server
        {
            get
            {
                this.CheckDisposed();
                return _headers.Server ?? string.Empty;
            }
        }

        /// <summary>
        /// Gets the status of the response.
        /// </summary>
        public HttpStatusCode StatusCode
        {
            get
            {
                this.CheckDisposed();
                return _statusCode;
            }
        }

        /// <summary>
        /// Gets the status description returned with the response.
        /// </summary>
        public string StatusDescription
        {
            get
            {
                this.CheckDisposed();
                return _statusDescription;
            }
        }

        /// <summary>
        /// Gets the headers that are associated with this response from the server
        /// </summary>
        public HttpResponseHeaders Headers
        {
            get
            {
                this.CheckDisposed();
                return _headers;
            }
        }

        /// <summary>
        /// Gets the method that is used to return the response.
        /// </summary>
        public HttpMethod Method
        {
            get
            {
                this.CheckDisposed();
                return _method;
            }
        }

        /// <summary>
        /// Gets the stream that is used to read the body of the response from the server
        /// </summary>
        /// <returns>A Stream containing the body of the response.</returns>
        public Stream GetResponseStream()
        {
            this.CheckDisposed();
            return _connectStream;
        }

        /// <summary>
        /// Gets a header value with specified the header name.
        /// </summary>
        /// <param name="headerName"></param>
        /// <returns></returns>
        public string GetResponseHeader(string headerName)
        {
            this.CheckDisposed();
            return _headers[headerName] ?? string.Empty;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                //we should check a `connection` value of response headers
                _connectStream.Dispose();
            }
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