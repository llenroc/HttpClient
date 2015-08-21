// Copyright (c) 2015 Yamool. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

namespace Yamool.Net.Http
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class HttpRequest
    {
        private enum ReadState
        {
            Start,
            StatusLine, // about to parse status line
            Headers,    // reading headers
            Data        // now read data
        }

        private const string SP = " ";
        private const int RequestLineConstantSize = 12;
        internal const string GZipHeader = "gzip";
        internal const string DeflateHeader = "deflate";
        internal const string ChunkedHeader = "chunked";
        private const int BeforeVersionNumbers = 0;
        private const int MajorVersionNumber = 1;
        private const int MinorVersionNumber = 2;
        private const int StatusCodeNumber = 3;
        private const int AfterStatusCode = 4;
        private const int AfterCarriageReturn = 5;
        private const string BeforeVersionNumberBytes = "HTTP/";

        private static readonly byte[] HttpBytes = new byte[] { 72, 84, 84, 80, 47 };// HTTP/
        private bool _allowAutoRedirect;
        private int _autoRedirects;
        private DecompressionMethods _automaticDecompression;
        private X509CertificateCollection _clientCertificates;
        private long _contentLength;
        private ICredentials _credentials;
        private CookieContainer _cookieContainer;        
        private HttpWriteMode _httpWriteMode;
        private bool _keepAlive;
        private int _maxAutomaticRedirections;
        private int _maximumResponseHeadersLength;
        private int _requestSubmitted;
        private IWebProxy _proxy;
        private Uri _originUri;
        private HttpMethod _originMethod;
        private bool _sendChunked;
        private int _timeout;
        private Uri _uri;
        private HttpRequestHeaders _headers;
        private Uri _hostUri;
        private HttpMethod _method;
        private HttpVersion _version;
        private long _startTimestamp;
        private ServicePoint _servicePoint;
        private bool _redirectedToDifferentHost;
        private int _totalResponseHeadersLength;
        private int _statusState;
        private ReadState _readState;
        private StatusLineValues _statusLineValues;
        private HttpResponseHeaders _responseHeaders;        
        private CancellationToken _requestCancellationToken;
        private HttpContent _submitContent;

        public HttpRequest(Uri uri) : this(HttpMethod.Get, uri, HttpVersion.HTTP11) { }

        public HttpRequest(HttpMethod method, Uri uri) : this(method, uri,HttpVersion.HTTP11) { }

        public HttpRequest(HttpMethod method, Uri uri, HttpVersion version)
        {
            _allowAutoRedirect = true;
            _automaticDecompression = DecompressionMethods.None;
            _contentLength = -1L;
            _httpWriteMode = HttpWriteMode.Unknown;
            _keepAlive = true;
            _maximumResponseHeadersLength = 8190;
            _maxAutomaticRedirections = 50;
            _originUri = uri;
            _originMethod = method;
            _timeout = 100000;
            _uri = _originUri;
            _method = _originMethod;
            _version = version;
            _sendChunked = false;
            _headers = new HttpRequestHeaders();
            _startTimestamp = DateTime.UtcNow.Ticks;          
        }

        #region Properties
        /// <summary>
        /// Gets or sets the value of the <c>Accept</c> HTTP header.
        /// </summary>
        public string Accept
        {
            get
            {
                return _headers.Accept;
            }
            set
            {
                _headers.Accept = value;
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the request should follow redirection responses.
        /// </summary>
        public bool AllowAutoRedirect
        {
            get
            {
                return _allowAutoRedirect;
            }
            set
            {
                this.CheckRequestSubmitted();
                _allowAutoRedirect = value;
            }
        }

        /// <summary>
        /// Gets or sets the type of decompression that is used.
        /// </summary>
        public DecompressionMethods AutomaticDecompression
        {
            get
            {
                return _automaticDecompression;
            }
            set
            {
                this.CheckRequestSubmitted();
                _automaticDecompression = value;
            }
        }

        /// <summary>
        /// Gets the boolean value that indicates this request whether is cancelled.
        /// </summary>
        internal bool Aborted
        {
            get
            {
                return _requestCancellationToken.IsCancellationRequested;
            }
        }

        /// <summary>
        /// Gets or sets the collection of security certificates that are associated with this request.
        /// </summary>
        public X509CertificateCollection ClientCertificates
        {
            get
            {
                if (_clientCertificates == null)
                {
                    _clientCertificates = new X509CertificateCollection();
                }
                return _clientCertificates;
            }
            set
            {
                this.CheckRequestSubmitted();
                _clientCertificates = value;
            }
        }

        /// <summary>
        /// Gets or sets the value of the <c>Connection</c> HTTP header.
        /// </summary>
        public string Connection
        {
            get
            {
                return _headers.Connection;
            }
            set
            {
                _headers.Connection = value;
            }
        }

        /// <summary>
        /// Gets or sets the value of the <c>Content-type</c> HTTP header. 
        /// </summary>
        public string ContentType
        {
            get
            {
                return _headers.ContentType;
            }
            set
            {
                _headers.ContentType = value;
            }
        }

        /// <summary>
        /// Gets or sets the content length for the request-body.
        /// </summary>
        public long ContentLength
        {
            get
            {
                return _contentLength;
            }
            set
            { 
                this.CheckRequestSubmitted();
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value is negative.");
                }               
                _contentLength = value;
            }
        }

        /// <summary>
        /// Gets or sets authentication information for the request.
        /// </summary>
        public ICredentials Credentials
        {
            get
            {
                return _credentials;
            }
            set
            {
                this.CheckRequestSubmitted();
                _credentials = value;
            }
        }

        /// <summary>
        /// Gets or sets the cookies associated with the request.
        /// </summary>
        public CookieContainer CookieContainer
        {
            get
            {
                return _cookieContainer;
            }
            set
            {
                this.CheckRequestSubmitted();
                _cookieContainer = value;
            }
        }

        internal HttpMethod CurrentMethod
        {
            get
            {
                return _method;
            }
        }

        /// <summary>
        /// Get or set the <c>Date</c> HTTP header value to use in an HTTP request.
        /// </summary>
        public DateTime Date
        {
            get
            {
                return _headers.Date;
            }
            set
            {
                _headers.Date = value;
            }
        }

        /// <summary>
        /// Gets the collection of HTTP request headers.
        /// </summary>
        public HttpRequestHeaders Headers
        {
            get
            {
                return _headers;
            }
        }

        /// <summary>
        /// Get or set the <c>Host</c> header value to use in an HTTP request independent from the request URI.
        /// </summary>
        public string Host
        {
            get
            {
                if (this.UseCustomHost)
                {
                    return GetHostAndPortString(_hostUri.Host, _hostUri.Port, _hostUri.IsDefaultPort);
                }
                return GetHostAndPortString(_uri.Host, _uri.Port, _uri.IsDefaultPort);
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                this.CheckRequestSubmitted();
                Uri uri;
                if (value.IndexOf('/') >= 0 || (!TryGetHostUri(value, out uri)))
                {
                    throw new ArgumentException("The specified value is not a valid Host header string.");
                }
                _hostUri = uri;
            }
        }

        /// <summary>
        /// Gets or sets the value of the <c>If-Modified-Since</c> HTTP header.
        /// </summary>
        public DateTime? IfModifiedSince
        {
            get
            {
                return _headers.IfModifiedSince;
            }
            set
            {
                _headers.IfModifiedSince = value;
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether to make a persistent connection to the remote server.
        /// </summary>
        public bool KeepAlive
        {
            get
            {
                return _keepAlive;
            }
            set
            {
                this.CheckRequestSubmitted();
                _keepAlive = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of redirects that the request follows.
        /// </summary>
        public int MaxAutomaticRedirections
        {
            get
            {
                return _maxAutomaticRedirections;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value");
                }
                this.CheckRequestSubmitted();
                _maxAutomaticRedirections = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum allowed length of the response headers.
        /// </summary>
        public int MaxResponseHeadersLength
        {
            get
            {
                return _maximumResponseHeadersLength;
            }
            set
            {
                if (value < 0 && value != -1)
                {
                    throw new ArgumentOutOfRangeException("value");
                }
                this.CheckRequestSubmitted();
                _maximumResponseHeadersLength = value;
            }
        }

        /// <summary>
        /// Gets or sets proxy information for the request
        /// </summary>
        public IWebProxy Proxy
        {
            get
            {
                return _proxy;
            }
            set
            {
                this.CheckRequestSubmitted();
                _proxy = value;
            }
        }

        /// <summary>
        /// Gets or sets the value of the <c>Referer</c> HTTP header.
        /// </summary>
        public string Referer
        {
            get
            {
                return _headers.Referer;
            }
            set
            {
                _headers.Referer = value;
            }
        }

        /// <summary>
        /// Gets the original Uniform Resource Identifier (URI) of the request. 
        /// </summary>
        public Uri RequestUri
        {
            get
            {
                return _originUri;
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether to send data in segments to the Internet resource.
        /// </summary>
        public bool SendChunked
        {
            get
            {
                return _sendChunked;
            }
            set
            {
                this.CheckRequestSubmitted();
                _sendChunked = value;
            }
        }

        /// <summary>
        /// Gets the service point to use for the request.
        /// </summary>
        public ServicePoint ServicePoint
        {
            get
            {
                return this.FindServicePoint(false);
            }
        }

        /// <summary>
        /// Gets or sets the number of milliseconds to wait before the HTTP response completed.
        /// </summary>
        public int Timeout
        {
            get
            {
                return _timeout;
            }
            set
            {
                this.CheckRequestSubmitted();
                _timeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the value of the <c>User-agent</c> HTTP header.
        /// </summary>
        public string UserAgent
        {
            get
            {
                return _headers.UserAgent;
            }
            set
            {
                this.CheckRequestSubmitted();
                _headers.UserAgent = value;
            }
        }

        private bool UseCustomHost
        {
            get
            {
                return _hostUri != null && !_redirectedToDifferentHost;
            }
        }

        internal bool UsesProxySemantics
        {
            get
            {
                return _servicePoint.UsesProxy && _uri.Scheme != Uri.UriSchemeHttps;
            }
        }
        #endregion

        /// <summary>
        /// Sends an HTTP request and return an HTTP response as asynchronous operation.
        /// </summary>
        /// <returns></returns>
        public Task<HttpResponse> SendAsync()
        {
            return this.SendAsync(null, CancellationToken.None);
        }

        /// <summary>
        ///  Sends an HTTP request and return an HTTP response as asynchronous operation.
        /// </summary>
        /// <param name="requestCancellationToken"></param>
        /// <returns></returns>
        public Task<HttpResponse> SendAsync(CancellationToken requestCancellationToken)
        {
            return this.SendAsync(null, requestCancellationToken);
        }

        /// <summary>
        /// Sends an HTTP request and return an HTTP response as asynchronous operation.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="requestCancellationToken"></param>
        /// <returns></returns>
        public async Task<HttpResponse> SendAsync(HttpContent content, CancellationToken requestCancellationToken)
        {
            this.CheckProtocol(content != null);
            if (this.SetRequestSubmitted())
            {
                throw new InvalidOperationException("This HTTP request has been submitted.");
            }

            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(requestCancellationToken);
            if (this.SetTimeout(linkedCts))
            {
                _requestCancellationToken = linkedCts.Token;
            }
            _submitContent = content;
            var connection = this.ServicePoint.SubmitRequest(this);
            var responseData = await this.SendRequestAsync(connection).ConfigureAwait(false);
            return new HttpResponse(_uri, _method, responseData, _automaticDecompression);
        }

        /// <summary>
        /// Send request to the service point and get the response.
        /// </summary>
        /// <returns></returns>
        private async Task<CoreResponseData> SendRequestAsync(Connection connection)
        {           
           try
           {
               await connection.ConnectAsync();
               //write a request header to the connection that established connected.    
               await this.WriteRequestAsync(connection);
               return await this.ReadResponseAsync(connection);
           }
           catch
           {
               connection.CloseOnIdle();
               throw;
           }
        }

        private async Task<CoreResponseData> ReadResponseAsync(Connection connection)
        {            
            _readState = ReadState.Start;
            var requestDone = false;        
            var bytesScanned = 0;
            var readBuffer = new ArraySegment<byte>();
            var buffer_offset = connection.Buffer.Offset;
            var buffer_length = connection.Buffer.Length;
            while (!requestDone)
            {
                if (this.Aborted)
                {
                    throw new OperationCanceledException("The request was canceled.");
                }
                readBuffer = await connection.ReadPooledBufferAsync(buffer_offset, buffer_length);
                var bytesRead = readBuffer.Count;
                if (bytesRead == 0)
                {
                    //connection is closed by the remote host.
                    break;
                }
                bytesScanned = 0;
                var parseStatus = this.ParseResponseData(readBuffer, ref bytesScanned, ref requestDone);
                if (parseStatus == DataParseStatus.Invalid || parseStatus == DataParseStatus.DataTooBig)
                {
                    if (parseStatus == DataParseStatus.Invalid)
                    {
                        throw new HttpResponseException("Cannot correct to parse the server response.",WebExceptionStatus.ServerProtocolViolation);
                    }
                    else
                    {
                        throw new HttpResponseException("The server response buffer limit exceeded.", WebExceptionStatus.MessageLengthLimitExceeded);
                    }
                }
                else if (parseStatus == DataParseStatus.NeedMoreData)
                {
                    var unparsedDataSize = bytesRead - bytesScanned;
                    if (unparsedDataSize > 0)
                    {
                        if (unparsedDataSize >= BufferPool.DefaultBufferLength)
                        {
                            throw new IOException("unparsed size exceeded the buffer length.");
                        }
                        Buffer.BlockCopy(connection.Buffer.Array, bytesScanned, connection.Buffer.Array, connection.Buffer.Offset, unparsedDataSize);
                        buffer_offset = unparsedDataSize;
                        buffer_length -= unparsedDataSize;
                    }
                }
                else
                {
                    buffer_length = connection.Buffer.Length;
                    buffer_offset = connection.Buffer.Offset;
                }
            }
            if (!requestDone)
            {
                //not finished.
                throw new HttpRequestException("The request not finished.");
            }
            if (this.Redirect((HttpStatusCode)_statusLineValues.StatusCode))
            {               
                if (_redirectedToDifferentHost)
                {
                    var previous_connection = connection;
                    connection = this.FindServicePoint(true).SubmitRequest(this);
                    previous_connection.CloseOnIdle();
                }
                return await this.SendRequestAsync(connection);
            }
            var connectStream = this.CreateResponseStream(connection, readBuffer, bytesScanned);
            return new CoreResponseData(_statusLineValues, _responseHeaders, connectStream);
        }

        private DataParseStatus ParseResponseData(ArraySegment<byte> data, ref int bytesScanned, ref bool requestDone)
        {
            var result = DataParseStatus.NeedMoreData;
            requestDone = false;
            switch (_readState)
            {
                case ReadState.Start:
                    {                       
                        _readState = ReadState.StatusLine;
                        _statusState = BeforeVersionNumbers;
                        _totalResponseHeadersLength = 0;
                        _statusLineValues = new StatusLineValues()
                        {
                            MajorVersion = 0,
                            MinorVersion = 0,
                            StatusCode = 0,
                            StatusDescription = ""
                        };
                        goto case ReadState.StatusLine;
                    }
                case ReadState.StatusLine:
                    {
                        var parseStatus = this.ParseStatusLine(data, ref bytesScanned);
                        if (parseStatus == DataParseStatus.Done)
                        {
                            _readState = ReadState.Headers;
                            _responseHeaders = new HttpResponseHeaders();
                            goto case ReadState.Headers;
                        }
                        else if (parseStatus != DataParseStatus.NeedMoreData)
                        {
                            result = parseStatus;
                            break;
                        }
                        break;
                    }
                case ReadState.Headers:
                    {
                        if (bytesScanned >= data.Count)
                        {
                            //need more data
                            break;
                        }
                        var parseStatus = _responseHeaders.ParseHeaders(data, ref bytesScanned, ref _totalResponseHeadersLength, _maximumResponseHeadersLength);
                        if (parseStatus == DataParseStatus.Invalid || parseStatus == DataParseStatus.DataTooBig)
                        {
                            result = parseStatus;
                            break;
                        }                            
                        else if (parseStatus == DataParseStatus.Done)
                        {
                            //if StatusCode  is BadRequest or Continue                            
                            goto case ReadState.Data;
                        }
                        break;
                    }
                case ReadState.Data:
                    {
                        requestDone = true;
                        result = DataParseStatus.Done;
                        break;
                    }
            }
            return result;
        }

        private DataParseStatus ParseStatusLine(ArraySegment<byte> data, ref int bytesParsed)
        {
            //HTTP/1.1 301 Moved Permanently\r\n
            //HTTP/1.1 200 OK\r\n
            var parseStatus = DataParseStatus.DataTooBig;
            var statusLineLength = data.Count;
            var initialBytesParsed = bytesParsed;
            var effectiveMax = _maximumResponseHeadersLength <= 0 ? int.MaxValue : (_maximumResponseHeadersLength - _totalResponseHeadersLength + bytesParsed);
            if (statusLineLength < effectiveMax)
            {
                parseStatus = DataParseStatus.NeedMoreData;
                effectiveMax = statusLineLength;
            }
            if (bytesParsed >= effectiveMax)
            {
                goto quit;
            }

            switch (_statusState)
            {
                case BeforeVersionNumbers:
                    {
                        while (_totalResponseHeadersLength - initialBytesParsed + bytesParsed < BeforeVersionNumberBytes.Length)
                        {
                            if (BeforeVersionNumberBytes[_totalResponseHeadersLength - initialBytesParsed + bytesParsed] != data.Get(bytesParsed))
                            {
                                parseStatus = DataParseStatus.Invalid;
                                goto quit;
                            }
                            if (++bytesParsed == effectiveMax)
                            {
                                goto quit;
                            }
                        }
                        if (data.Get(bytesParsed) == '.')
                        {
                            parseStatus = DataParseStatus.Invalid;
                            goto quit;
                        }
                        _statusState = MajorVersionNumber;
                        goto case MajorVersionNumber;
                    }
                case MajorVersionNumber:
                    {
                        while (data.Get(bytesParsed) != '.')
                        {
                            if (data.Get(bytesParsed) < '0' || data.Get(bytesParsed) > '9')
                            {
                                parseStatus = DataParseStatus.Invalid;
                                goto quit;
                            }
                            _statusLineValues.MajorVersion = _statusLineValues.MajorVersion * 10 + data.Get(bytesParsed) - '0';//1,10
                            if (++bytesParsed == effectiveMax)
                            {
                                goto quit;
                            }
                        }
                        // Need visibility past the dot.
                        if (bytesParsed + 1 == effectiveMax)
                        {
                            goto quit;
                        }
                        bytesParsed++;
                        if (data.Get(bytesParsed) == ' ')
                        {
                            parseStatus = DataParseStatus.Invalid;
                            goto quit;
                        }
                        _statusState = MinorVersionNumber;
                        goto case MinorVersionNumber;
                    }
                case MinorVersionNumber:
                    {
                        while (data.Get(bytesParsed) != ' ')
                        {
                            if (data.Get(bytesParsed) < '0' || data.Get(bytesParsed) > '9')
                            {
                                parseStatus = DataParseStatus.Invalid;
                                goto quit;
                            }

                            _statusLineValues.MinorVersion = _statusLineValues.MinorVersion * 10 + data.Get(bytesParsed) - '0';

                            if (++bytesParsed == effectiveMax)
                            {
                                goto quit;
                            }
                        }
                        _statusState = StatusCodeNumber;
                        _statusLineValues.StatusCode = 1;

                        // Move past the space.
                        if (++bytesParsed == effectiveMax)
                        {
                            goto quit;
                        }
                        goto case StatusCodeNumber;
                    }
                case StatusCodeNumber:
                    {
                        while (data.Get(bytesParsed) >= '0' && data.Get(bytesParsed) <= '9')
                        {
                            if (_statusLineValues.StatusCode >= 1000)
                            {
                                parseStatus = DataParseStatus.Invalid;
                                goto quit;
                            }

                            _statusLineValues.StatusCode = _statusLineValues.StatusCode * 10 + data.Get(bytesParsed) - '0';

                            if (++bytesParsed == effectiveMax)
                            {
                                goto quit;
                            }
                        }
                        if (data.Get(bytesParsed) != ' ' || _statusLineValues.StatusCode < 1000)
                        {
                            if (data.Get(bytesParsed) == '\r' && _statusLineValues.StatusCode >= 1000)
                            {
                                _statusLineValues.StatusCode -= 1000;
                                _statusState = AfterCarriageReturn;
                                if (++bytesParsed == effectiveMax)
                                {
                                    goto quit;
                                }
                                goto case AfterCarriageReturn;
                            }
                            parseStatus = DataParseStatus.Invalid;
                            goto quit;
                        }
                        _statusLineValues.StatusCode -= 1000;

                        _statusState = AfterStatusCode;

                        // Move past the space.
                        if (++bytesParsed == effectiveMax)
                        {
                            goto quit;
                        }
                        goto case AfterStatusCode;
                    }
                case AfterStatusCode:
                    {
                        var beginning = bytesParsed;
                        while (data.Get(bytesParsed) != '\r')
                        {
                            if (data.Get(bytesParsed) < ' ' || data.Get(bytesParsed) == 127)
                            {
                                parseStatus = DataParseStatus.Invalid;
                                goto quit;
                            }
                            if (++bytesParsed == effectiveMax)
                            {
                                var s = Encoding.ASCII.GetString(data.Array, beginning, bytesParsed - beginning);
                                if (_statusLineValues.StatusDescription == null)
                                {
                                    _statusLineValues.StatusDescription = s;
                                }
                                else
                                {
                                    _statusLineValues.StatusDescription += s;
                                }
                                goto quit;
                            }
                        }
                        if (bytesParsed > beginning)
                        {
                            var s = Encoding.ASCII.GetString(data.Array, data.Offset + beginning, bytesParsed - beginning);
                            if (_statusLineValues.StatusDescription == null)
                            {
                                _statusLineValues.StatusDescription = s;
                            }
                            else
                            {
                                _statusLineValues.StatusDescription += s;
                            }
                        }
                        else if (_statusLineValues.StatusDescription == null)
                        {
                            _statusLineValues.StatusDescription = "";
                        }
                        _statusState = AfterCarriageReturn;

                        // Move past the CR.
                        if (++bytesParsed == effectiveMax)
                        {
                            goto quit;
                        }
                        goto case AfterCarriageReturn;
                    }
                case AfterCarriageReturn:
                    {
                        if (data.Get(bytesParsed) != '\n')
                        {
                            parseStatus = DataParseStatus.Invalid;
                            goto quit;
                        }
                        parseStatus = DataParseStatus.Done;
                        bytesParsed++;
                        break;
                    }
            }
        quit:
            _totalResponseHeadersLength += bytesParsed - initialBytesParsed;

            return parseStatus;
        }

        private ConnectStream CreateResponseStream(Connection connection, ArraySegment<byte> data, int bytesParsed)
        {
            var fHaveChunked = false;
            var dummyResponseStream = false;
            var contentLength = this.ProcessHeaderData(ref fHaveChunked, out dummyResponseStream);
            if (contentLength == -2)
            {
                throw new HttpResponseException("Cannot correct to parse the server response.", WebExceptionStatus.ServerProtocolViolation);
            }
            _statusLineValues.ContentLength = contentLength;
            int bufferLeft = data.Count - bytesParsed;
            var bytesToCopy = 0;
            if (dummyResponseStream)
            {
                bytesToCopy = 0;
                fHaveChunked = false;
            }
            else
            {
                bytesToCopy = -1;
                if (!fHaveChunked && (contentLength <= (long)int.MaxValue))
                {
                    bytesToCopy = (int)contentLength;
                }
            }           
            if (bytesToCopy != -1 && bytesToCopy <= bufferLeft)
            {
                return new ConnectStream(connection, data, bytesParsed, bytesToCopy, dummyResponseStream ? 0 : contentLength, fHaveChunked, this);
            }
            else
            {
                return new ConnectStream(connection, data, bytesParsed, bufferLeft, dummyResponseStream ? 0 : contentLength, fHaveChunked, this);
            }
        }

        private bool Redirect(HttpStatusCode code)
        {
            if (code == HttpStatusCode.MultipleChoices || // 300
                code == HttpStatusCode.MovedPermanently || // 301
                code == HttpStatusCode.Found || // 302
                code == HttpStatusCode.SeeOther || // 303
                code == HttpStatusCode.TemporaryRedirect)  // 307
            {
                if (!_allowAutoRedirect)
                {
                    throw new RedirectException(_originUri);
                }
                _method = HttpMethod.Get;
                _autoRedirects++;
                if (_autoRedirects > _maxAutomaticRedirections)
                {
                    throw new CircularRedirectException(_maxAutomaticRedirections, _originUri);
                }
                var location = _responseHeaders.Location;
                if (location == null)
                {
                    throw new HttpResponseException("No Location header value.", WebExceptionStatus.ServerProtocolViolation);
                }
                var newUri = new Uri(_uri, location);
                if (!HttpUtils.IsHttpUri(newUri))
                {
                    throw new ProtocolException("Supports HTTP or HTTPS scheme.");
                }
                var oldUri = _uri;
                _uri = newUri;
                _redirectedToDifferentHost = Uri.Compare(_originUri, _uri, UriComponents.HostAndPort, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) != 0;
                if (this.UseCustomHost)
                {
                    // if the scheme/path changed, update _HostUri to reflect the new scheme/path
                    var hostString = GetHostAndPortString(_hostUri.Host, _hostUri.Port, true);
                    Uri hostUri;
                    var hostUriSuccess = TryGetHostUri(hostString, out hostUri);
                    _hostUri = hostUri;
                }
                return true;
            }
            return false;
        }

        private long ProcessHeaderData(ref bool fHaveChunked, out bool dummyResponseStream)
        {
            fHaveChunked = false;          
            var contentLength = -1L;
            var transferEncodingString = _responseHeaders.TransferEncoding;
            if (transferEncodingString != null)
            {
                fHaveChunked = transferEncodingString.IndexOf(ChunkedHeader, StringComparison.OrdinalIgnoreCase) >= 0;
            }
            if (!fHaveChunked)
            {
                var contentLengthValue = _responseHeaders.ContentLength;
                if (contentLengthValue.HasValue)
                {
                    contentLength = contentLengthValue.Value;
                }
                if (contentLength < -1)
                {
                    contentLength = -2;
                }
            }
            dummyResponseStream = !_method.AllowResponseContent || _statusLineValues.StatusCode < (int)HttpStatusCode.OK ||
                                 _statusLineValues.StatusCode == (int)HttpStatusCode.NoContent || (_statusLineValues.StatusCode == (int)HttpStatusCode.NotModified && contentLength < 0);
            return contentLength;
        }

        private void CheckRequestSubmitted()
        {
            if (_requestSubmitted == 1)
            {
                throw new InvalidOperationException("This operation cannot be performed after the request has been submitted.");
            }          
        }

        private ServicePoint FindServicePoint(bool forceFind)
        {
            var servicePoint = _servicePoint;
            if (servicePoint == null || forceFind)
            {
                lock (this)
                {
                    if (_servicePoint == null || forceFind)
                    {
                        _servicePoint = ServicePointManager.FindServicePoint(_uri, _proxy);
                    }
                    servicePoint = _servicePoint;
                }
            }
            return servicePoint;
        }

        private void CheckProtocol(bool requestedContent)
        {
            if (!HttpUtils.IsHttpUri(_uri))
            {
                throw new ProtocolException("Supports HTTP or HTTPS scheme.");
            }
            if (!_method.AllowRequestContent)
            {
                if (requestedContent)
                {
                    throw new ProtocolViolationException("Cannot send a content-body with this method[" + _method + "]");
                }
                _httpWriteMode = HttpWriteMode.None;
            }
            else
            {
                if (_sendChunked)
                {                  
                    _httpWriteMode = HttpWriteMode.Chunked;
                }
                else
                {
                    _httpWriteMode = (_contentLength >= 0L) ? HttpWriteMode.ContentLength : (requestedContent ? HttpWriteMode.Buffer : HttpWriteMode.None);
                }
            }
            if (_httpWriteMode != HttpWriteMode.Chunked)
            {
                if (requestedContent && _contentLength == -1L && _keepAlive)
                {
                    throw new ProtocolViolationException("Must either set ContentLength to a non-negative number or set SendChunked to true with this method[" + _originMethod + "]");
                }
            }
        }

        private bool SetRequestSubmitted()
        {
            if (Interlocked.Exchange(ref _requestSubmitted, 1) == 1)
            {
                return true;
            }
            return false;
        }

        private async Task WriteRequestAsync(Connection connection)
        {
            this.UpdateHeaders();
            if (_httpWriteMode != HttpWriteMode.None)
            {
                if (_httpWriteMode == HttpWriteMode.Chunked)
                {
                    _headers.AddInternal(HttpHeaderNames.TransferEncoding, ChunkedHeader);
                }
                else
                {
                    if (_contentLength >= 0L)
                    {
                        _headers.SetInternal(HttpHeaderNames.ContentLength, _contentLength.ToString());
                    }
                }
                //100-continue ??
            }
            //Accept-Encoding header
            var acceptEncodingValues = this.Accept ?? string.Empty;
            if ((_automaticDecompression & DecompressionMethods.GZip) != DecompressionMethods.None && acceptEncodingValues.IndexOf(GZipHeader, StringComparison.OrdinalIgnoreCase) < 0)
            {
                if ((_automaticDecompression & DecompressionMethods.Deflate) != 0
                    && acceptEncodingValues.IndexOf(DeflateHeader, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    _headers.AddInternal(HttpHeaderNames.AcceptEncoding, GZipHeader + ", " + DeflateHeader);
                }
                else
                {
                    _headers.AddInternal(HttpHeaderNames.AcceptEncoding, GZipHeader);
                }
            }
            else
            {
                if ((_automaticDecompression & DecompressionMethods.Deflate) != DecompressionMethods.None && acceptEncodingValues.IndexOf("deflate", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    _headers.AddInternal(HttpHeaderNames.AcceptEncoding, DeflateHeader);
                }
            }
            if (_keepAlive)
            {
                _headers.SetInternal(HttpHeaderNames.Connection, "Keep-Alive");
            }
            else
            {
                _headers.SetInternal(HttpHeaderNames.Connection, "Close");
            }
            var requestHeadersString = _headers.ToString();
            string requestLine = null;
            if (this.UsesProxySemantics)
            {
                requestLine = this.GenerateProxyRequestLine();
            }
            else
            {
                requestLine = this.GenerateRequestLine();
            }
            // Request-Line   = Method SP Request-URI SP HTTP-Version CRLF
            // i.e. GET http://www.w3.org/pub/WWW/TheProject.html HTTP/1.1\r\n

            var writeBytesCount = requestHeadersString.Length + requestLine.Length + RequestLineConstantSize;
            byte[] writeBuffer = null;
            var usePooledBuffer = false;
            if (connection.Buffer.Length >= writeBytesCount)
            {
                writeBuffer = connection.Buffer.Array;
                usePooledBuffer = true;
            }
            else
            {
                writeBuffer = new byte[writeBytesCount];
            }
            var offset = 0;
            offset += Encoding.ASCII.GetBytes(requestLine, 0, requestLine.Length, writeBuffer, offset);
            Buffer.BlockCopy(HttpBytes, 0, writeBuffer, offset, HttpBytes.Length);
            offset += HttpBytes.Length;
            writeBuffer[offset++] = (byte)(_version == HttpVersion.HTTP20 ? '2' : '1');
            writeBuffer[offset++] = (byte)'.';
            writeBuffer[offset++] = (byte)(_version == HttpVersion.HTTP20 ? '0' : '1');
            writeBuffer[offset++] = (byte)'\r';
            writeBuffer[offset++] = (byte)'\n';
            Encoding.UTF8.GetBytes(requestHeadersString, 0, requestHeadersString.Length, writeBuffer, offset);
            //transfer data
            var beginReadIndex = 0;
            var leftWriteBytes = writeBytesCount;
            while (leftWriteBytes > 0)
            {
                var count = Math.Min(writeBytesCount - beginReadIndex, connection.Buffer.Length);
                if (!usePooledBuffer)
                {
                    Buffer.BlockCopy(writeBuffer, beginReadIndex, connection.Buffer.Array, connection.Buffer.Offset, count); 
                }
                var writeBytes = await connection.WritePooledBufferAsync(connection.Buffer.Offset + beginReadIndex, count);          
                leftWriteBytes -= writeBytes;
                beginReadIndex += writeBytes;
            }
            if (_method.AllowRequestContent && _submitContent != null)
            {
                using (_submitContent)
                {
                    await _submitContent.CopyToAsync(null);
                    _submitContent = null;
                }
            }
        }

        private void UpdateHeaders()
        {
            string safeHostAndPort;
            if (this.UseCustomHost)
            {
                safeHostAndPort = GetSafeHostAndPort(_hostUri);
            }
            else
            {
                safeHostAndPort = GetSafeHostAndPort(_uri);
            }
            _headers.SetInternal(HttpHeaderNames.Host, safeHostAndPort);
            if (_cookieContainer != null)
            {
                _headers.RemoveInternal(HttpHeaderNames.Cookie);
                var cookieHeader = _cookieContainer.GetCookieHeader(this.UseCustomHost ? _hostUri : _uri);
                if (cookieHeader.Length > 0)
                {
                    _headers.SetInternal(HttpHeaderNames.Cookie, cookieHeader);
                }
            }
        }

        private bool SetTimeout(CancellationTokenSource cts)
        {
            if (_timeout != -1)
            {
                cts.CancelAfter(_timeout);
                return true;
            }
            return false;
        }

        private static string GetSafeHostAndPort(Uri sourceUri)
        {
            return GetHostAndPortString(sourceUri.DnsSafeHost, sourceUri.Port, !sourceUri.IsDefaultPort);
        }

        private static string GetHostAndPortString(string hostName, int port, bool addPort)
        {
            if (addPort)
            {
                return hostName + ":" + port;
            }
            return hostName;
        }

        private string GenerateProxyRequestLine()
        {
            //
            // Handle Proxy Case, i.e. "GET http://hostname-outside-of-proxy.somedomain.edu:999"
            //
            var scheme = _uri.GetComponents(UriComponents.Scheme | UriComponents.KeepDelimiter, UriFormat.UriEscaped);
            var host = GetSafeHostAndPort(_uri);
            var path = _uri.GetComponents(UriComponents.Path | UriComponents.Query, UriFormat.UriEscaped);
            //method + SP + scheme + host + path + SP
            return string.Concat(_method.Name, SP, scheme, host, path, SP);
        }

        private string GenerateRequestLine()
        {
            var pathAndQuery = _uri.PathAndQuery;
            return string.Concat(_method.Name, SP, pathAndQuery, SP);
        }

        private bool TryGetHostUri(string hostName, out Uri hostUri)
        {
            StringBuilder sb = new StringBuilder(_uri.Scheme);
            sb.Append("://");
            sb.Append(hostName);
            sb.Append(_uri.PathAndQuery);

            return Uri.TryCreate(sb.ToString(), UriKind.Absolute, out hostUri);
        }
    }
}
