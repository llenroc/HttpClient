//----------------------------------------------------------------
// Copyright (c) Yamool Inc.  All rights reserved.
//----------------------------------------------------------------

namespace Yamool.Net.Http
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Text;

    /// <summary>
    /// Represents the HTTP response message from the remote server response.
    /// </summary>
    public class HttpResponse : IDisposable
    {
        private int _readState;
        private Uri _uri;
        private HttpResponseHeaders _headers;
        private bool _propertiesDisposed;
        private Stream _responseStream;
        private HttpResponseStatus _responseStatus;
        private string _httpVerb;
        private byte[] _beWriteBytes;
        private bool _automaticDecompression;
        private bool _decompressed;
      
        internal HttpResponse(Uri responseUri, string httpVerb,bool automaticDecompression)
        {
            _readState = 0;
            _uri = responseUri;
            _httpVerb = httpVerb;
            _automaticDecompression = automaticDecompression;
            _headers = new HttpResponseHeaders();
            _responseStatus = new HttpResponseStatus();
        }

        
        /// <summary>
        /// Gets the stream that is used to read the body of the response from the server
        /// </summary>
        /// <returns>A Stream containing the body of the response.</returns>
        public Stream GetResponseStream()
        {
            this.CheckDisposed();
            if (_responseStream != null)
            {
                _responseStream.Position = 0;
                if (!_decompressed && _automaticDecompression && this.Headers.ContentEncoding != null)
                {
                    if (string.Compare("gzip", this.Headers.ContentEncoding) == 0)
                    {
                        _responseStream = new GZipStream(_responseStream, CompressionMode.Decompress);
                    }
                    else if (string.Compare("deflate", this.Headers.ContentEncoding) == 0)
                    {
                        _responseStream = new DeflateStream(_responseStream, CompressionMode.Decompress);
                    }
                    //possible is a media stream
                    _decompressed = true;
                }               
            }
            return _responseStream;
        }

        /// <summary>
        /// Write a buffer to response.
        /// </summary>
        /// <param name="buffer">The bytes of response.</param>
        /// <param name="offset">The offset value that which start write in buffer.</param>
        /// <param name="count">The count of buffer which how number of bytes can be writeable.</param>
        /// <remarks>If this method is called and writeHead has not been called, it will switch to implicit header mode and flush the implicit headers.</remarks>
        internal void WriteResponse(byte[] buffer, int offset, int count)
        {
            if (_beWriteBytes != null)
            {
                count = count - offset;
                var newBuffer = new byte[count + _beWriteBytes.Length];
                Buffer.BlockCopy(_beWriteBytes, 0, newBuffer, 0, _beWriteBytes.Length);
                Buffer.BlockCopy(buffer, offset, newBuffer, _beWriteBytes.Length, count);
                buffer = newBuffer;               
                count = newBuffer.Length;
                offset = 0;
                //set null for keep-stream in response.
                _beWriteBytes = null;
            }
            while (offset < count)
            {
                switch (_readState)
                {
                    case 0://http status code
                        {
                            this.WriteStatusCode(buffer, ref offset, count);
                            break;
                        }
                    case 1://header of response
                        {
                            this.WriteHeader(buffer, ref offset, count);
                            break;
                        }
                    case 2://content body of response
                        {
                            this.WriteContentBody(buffer, offset, count);
                            return;
                        }
                }
            }
        }        

        /// <summary>
        /// Closes the response stream and release the resource.
        /// </summary>
        public void Close()
        {
            this.Dispose(true);
        }

        #region Properties
        /// <summary>
        /// Gets whether this response has completed.
        /// </summary>
        internal bool InternalPeekCompleted
        {
            get
            {
                return _responseStream != null && !_responseStream.CanWrite;
            }
        }

        /// <summary>
        /// Gets the final Uniform Resource Identifier (URI) of the response.
        /// </summary>
        public Uri ResponseUri
        {
            get
            {
                return _uri;
            }
        }

        /// <summary>
        /// Gets the status of the response.
        /// </summary>
        public HttpStatusCode StatusCode
        {
            get
            {
                return _responseStatus.Code;
            }
        }

        /// <summary>
        /// Gets the version of the http protocol.
        /// </summary>
        public string HttpVersion
        {
            get
            {
                return _responseStatus.HttpVersion;
            }
        }

        /// <summary>
        /// Gets the status description returned with the response.
        /// </summary>
        public string StatusDescript
        {
            get
            {
                return _responseStatus.Description;
            }
        }

        /// <summary>
        /// Gets the headers that are associated with this response from the server. 
        /// </summary>
        public HttpResponseHeaders Headers
        {
            get
            {
                return _headers;
            }
        }

        internal long ContentBodyLength
        {
            get
            {
                return _responseStream.Length;
            }
        }

        internal bool IsHeaderReady
        {
            get
            {
                return _readState > 1;
            }
        }

        public bool IsSuccessStatusCode
        {
            get
            {
                return this.StatusCode >= HttpStatusCode.OK && this.StatusCode <= (HttpStatusCode)299;
            }
        }
        #endregion

        #region IDisposable
        /// <summary>
        /// Releases the resources used by the <see cref="HttpResponse"/> object. 
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing && !_propertiesDisposed)
            {
                _propertiesDisposed = true;
                if (_responseStream != null)
                {
                    _responseStream.Close();
                }               
            }
        }
        #endregion

        private void CheckDisposed()
        {
            if (_propertiesDisposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }

        private void WriteStatusCode(byte[] buffer, ref int offset, int count)
        {
            //HTTP/1.1 301 
            //HTTP/1.1 301 Moved Permanently
            //HTTP/1.1 200 OK
            var i = 0;
            var position = offset;
            var newline = false;
            while (offset < count)
            {
                var code = buffer[offset++];
                if (code == 13)
                {
                    //code or desc?
                    var nextSegment = Encoding.UTF8.GetString(buffer, position, offset - position - 1);
                    if (i == 1)
                    {
                        _responseStatus.Code = (HttpStatusCode)int.Parse(nextSegment);
                    }
                    else
                    {
                        _responseStatus.Description = nextSegment;
                    }
                    position = offset;
                    i++;
                    //\r\n
                    if (offset < count && buffer[offset] == 10)
                    {
                        offset++;
                        newline = true;                     
                    }
                    break;
                }
                else if (code == 32)
                {
                    var nextSegment= Encoding.UTF8.GetString(buffer, position, offset - position - 1);
                    if (i == 0)
                    {
                        _responseStatus.HttpVersion = nextSegment;                    
                    }
                    else if (i == 1)
                    {
                        _responseStatus.Code = (HttpStatusCode)int.Parse(nextSegment);                        
                    }
                    position = offset;
                    i++;
                }
            }
            //copy a buffer if not arrived at status code line.
            if (!newline)
            {
                _beWriteBytes = new byte[count];
                Buffer.BlockCopy(buffer, offset, _beWriteBytes, 0, count);
            }
            else
            {
                _readState = 1;
            }
        }

        private void WriteHeader(byte[] buffer, ref int offset, int count)
        {
            //header_key:header_value
            //\r\n
            //\r\n [endof]
            var newline = false;
            var endof = false;
            var colon=0;
            var position = offset;
            while (offset < count)
            {
                var code = buffer[offset++];
                //Set-Cookie:_FS=NU=1; domain=.bing.com; path=/
                if (colon == 0 && code == ':')
                {
                    colon = offset - 1;
                    continue;
                }
                if (code == 13)
                {
                    if (offset < count && buffer[offset] == 10)
                    {
                        //\r\n\r\n
                        if (newline)
                        {
                            offset++;
                            endof = true;
                            break;
                        }
                        newline = true;
                    }
                    if (colon > 0)
                    {
                        var name = Encoding.UTF8.GetString(buffer, position, colon - position);
                        var value = Encoding.UTF8.GetString(buffer, colon + 1, offset - colon - 2).Trim();
                        this.Headers.SetInternal(name, value);
                        colon = 0;
                    }
                    if (buffer[offset] == 10)
                    {
                        offset++;
                    }
                    position = offset;
                }
                else
                {
                    newline = false;
                }
            }
            if (!endof)
            {
                //copy a remain bytes to save.
                var remainBytes = count - position;
                _beWriteBytes = new byte[remainBytes];
                Buffer.BlockCopy(buffer, position, _beWriteBytes, 0, remainBytes);
            }
            else
            {
                _readState = 2;
            }
        }

        private void WriteContentBody(byte[] buffer, int offset, int count)
        {
            //check whether create a response stream for request.
            if (_responseStream == null)
            {
                var transferEncoding = this.Headers.TransferEncoding;
                if (!string.IsNullOrEmpty(transferEncoding) && string.CompareOrdinal(transferEncoding, "chunked") == 0)
                {
                    _responseStream = new ChunkedStream(new MemoryStream());
                }
                else
                {
                    var contentLength = this.Headers.ContentLength.HasValue ? this.Headers.ContentLength.Value : 0L;
                    _responseStream = new ContentLengthStream(new MemoryStream((int)contentLength), contentLength);
                }
            }
            _responseStream.Write(buffer, offset, count);
        }
    }
}
