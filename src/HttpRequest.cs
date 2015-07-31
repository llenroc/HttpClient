//----------------------------------------------------------------
// Copyright (c) Yamool Inc.  All rights reserved.
//----------------------------------------------------------------

namespace Yamool.Net.Http
{
    using System;
    using System.IO;
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides several methods for sending HTTP requests and receiving HTTP responses from a resources by a URI.
    /// </summary>
    public class HttpRequest
    {
        private const string SP = " ";
        private const string CRLF = "\r\n";
        private const string HttpVersion = "1.1";

        #region vars
        private int _timeout;
        private long _maximumResponseContentLength;
        private int _maximumAutomaticRedirections;
        private bool _allowAutoRedirect;
        private int _buffSize;
        //can auto use decompress-mode for response stream.
        private bool _automaticDecompression;
        //proxy
        private IWebProxy _proxy;
        private X509CertificateCollection _clientCertificates;

        /// <summary>
        /// the current request uri,this possible not equal to requesturi
        /// </summary>
        internal Uri _uri;

        //Used by POST
        /// <summary>
        /// the content body of request that should sending.
        /// </summary>
        private byte[] _submitData;
        /// <summary>
        /// the entity request data that include a header data and content body data.
        /// </summary>
        private byte[] _writeBuffer;

        private RequestStream _submitWriteStream;
        private long _contentLength;

        //occures when start request
        private bool _requestSubmitted;
        //socket
        private ITcpChannel _connectChannel;
      
        private byte[] _buffer;
        private int _submitBytesTransferred;
        private int _redirectCount;        

        /// <summary>
        /// The state of the current request operation.
        /// 0:init ; 1:requesting 2:completed 3:timeout 4:canceled(aborted)       
        /// </summary>
        private int _state;
        private HttpRequestTask _task;
        private HttpResponse _response;       
        #endregion

        #region ctors
        public HttpRequest(string requestUri) : this(new Uri(requestUri)) { }

        public HttpRequest(Uri requestUri)
        {            
            this.InitRequest(requestUri);
        }
        #endregion

        /// <summary>
        /// Cancels a request operation to access an internet resource.
        /// </summary>
        public void Abort()
        {
            if (this.HaveResponse)
            {
                return;
            }
            _task.SetCanceled();
        }

        /// <summary>
        /// Gets a <see cref="Stream"/> object to use to write request data.
        /// </summary>
        /// <returns>A <see cref="Stream"/> to use to write request data.</returns>
        public Stream GetRequestStream()
        {
            this.CheckProtocol(true);
            if (this.RequestSubmitted)
            {
                throw new InvalidOperationException("This operation cannot be performed after the request has been submitted.");
            }
            _submitWriteStream = new RequestStream(this);
            return _submitWriteStream;
        }

        /// <summary>
        /// Sending a request and returns a <see cref="HttpResponse"/> object in an asynchronous operation.
        /// </summary>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public Task<HttpResponse> GetResponseAsync()
        {
            if (this.HaveResponse)
            {
                return Task.FromResult(_response);
            }
            _task = new HttpRequestTask(this.OnCompleted);            
            try
            {
                if (!this.SetRequestSubmitted())
                {
                    this.CheckProtocol(false);
                }              
                //if the request stream not close.
                if (_submitWriteStream != null && !_submitWriteStream.Closed)
                {
                    _submitWriteStream.Close();
                }
                else if (_submitWriteStream == null && this.HasEntityBody)
                {
                    throw new ProtocolViolationException("You must provide a request body if you set ContentLength>0 or SendChunked==true.");
                }
                if (Interlocked.CompareExchange(ref _state, 1, 0) == 0)
                {
                   
                    this.GetServiceEndPointAsync().ContinueWith((Task<EndPoint> task) =>
                    {
                        if (task.IsFaulted)
                        {
                            _task.SetException(task.Exception.GetBaseException());
                            return;
                        }
                        var remoteEP = task.Result;
                        _task.SetTimeout(this.Timeout);
                        //start an async operation 
                        _connectChannel = this.CreateTcpChannel(this.IsSecureRequest);
                        _connectChannel.Completed += channel_Completed;
                        _connectChannel.Error += channel_Error;
                        _connectChannel.Connect(remoteEP);

                    }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);                   
                }
                else
                {
                    _task.SetResult(_response);
                }
            }
            catch (Exception ex)
            {
                this.HandleError(ex);
            }
            return _task.Task;
        }        

        private void channel_Completed(object sender, ChannelEventArgs e)
        {
            if (this.HaveResponse)
            {
                return;
            }
            try
            {
                switch (e.LastOperation)
                {
                    case ChannelOperation.Connect:
                        {
                            this.SetRequestHeaders();
                            //build a entity message of request.
                            var requestData = this.GetRequestHeadersBytes();
                            if (this.HasEntityBody)
                            {
                                var buffer = new byte[requestData.Length + this.ContentLength];
                                Buffer.BlockCopy(requestData, 0, buffer, 0, requestData.Length);
                                Buffer.BlockCopy(this._submitData, 0, buffer, requestData.Length, (int)this.ContentLength);
                                _writeBuffer = buffer;
                            }
                            else
                            {
                                _writeBuffer = requestData;
                            }
                            var count = Math.Min(this.BuffSize, requestData.Length);
                            var offset = _submitBytesTransferred;
                            _connectChannel.Send(_writeBuffer, offset, count);
                            break;
                        }
                    case ChannelOperation.Send:
                        {
                            _submitBytesTransferred += e.BytesTransferred;
                            var remainBytesCount = _writeBuffer.Length - _submitBytesTransferred;
                            if (remainBytesCount > 0)
                            {
                                var count = Math.Min(this.BuffSize, remainBytesCount);
                                var offset = _submitBytesTransferred;
                                _connectChannel.Send(_writeBuffer, offset, count);
                            }
                            else
                            {
                                _connectChannel.Receive(_buffer, 0, this.BuffSize);
                            }
                            break;
                        }
                    case ChannelOperation.Receive:
                        {
                            //the remote server ask close connection.
                            if (e.BytesTransferred == 0)
                            {
                                _task.SetResult(_response);
                            }
                            else
                            {
                                this.ProcessReceivedBytes(e.Buffer, 0, e.BytesTransferred);
                            }
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                this.HandleError(ex);
            }
        }

        private void channel_Error(object sender, ChannelEventArgs e)
        {
            this.HandleError(e.LastException);
        }

        private void HandleError(Exception ex)
        {
            if (_state == 1)
            {
                if (_response != null)
                {
                    _response.Close();
                }
                this._task.SetException(ex);
            }
        }

        /// <summary>
        /// Set a content body of request that send with HTTP request.
        /// </summary>
        internal void SetSubmitRequestStream(byte[] requestData)
        {
            //set the content length of data.
            this._contentLength = requestData.LongLength;
            this._submitData = requestData;
        }

        /// <summary>
        /// Occures when received data from remote host.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void ProcessReceivedBytes(byte[] buffer, int offset, int count)
        {
            //check a response object whether is created in current request operation.            
            if (_response == null)
            {
                _response = new HttpResponse(_uri, this.Method.Method, this.AutomaticDecompression);
            }
            //write a content body from server to response
            _response.WriteResponse(_buffer, offset, count);
            if (_response.InternalPeekCompleted || _response.IsHeaderReady && this.Method.ExpectNoContentResponse)
            {
                _task.SetResult(_response);
                return;
            }
            if (_maximumResponseContentLength > 0 && _response.ContentBodyLength > _maximumResponseContentLength)
            {
                new HttpException(HttpExceptionStatus.MessageLengthLimitExceeded, "Maximum content length exceeded allowed maximum response length.");
            }
            //location redirection
            if (_response.StatusCode == HttpStatusCode.Found || _response.StatusCode == HttpStatusCode.MovedPermanently || _response.StatusCode == HttpStatusCode.SeeOther)
            {
                if (!_allowAutoRedirect)
                {
                    _task.SetResult(_response);
                }
                //if a redirect time greater than then the maximum number of redirect
                if ((_redirectCount++) >= _maximumAutomaticRedirections)
                {
                    new HttpException(HttpExceptionStatus.RedirectionCountExceeded, "Maximum redirection count exceeded.");
                }
                //switch to new url     
                var location = _response.Headers.Location;
                if (string.IsNullOrEmpty(location))
                {
                    new ProtocolViolationException("The value of location of header is empty.");
                }
                if (!(location.StartsWith("http")))
                {
                    location = HttpUtils.ToAbsoluteUrl(_uri, location);
                }
                //change a http verb is GET?
                this.Method = HttpMethod.Get;
                _submitBytesTransferred = 0;
                _submitData = null;
                _contentLength = 0;
                //release current response object
                _response.Close();
                _response = null;               

                this.LocationRedirect(new Uri(location));
                return;
            }
            _connectChannel.Receive(_buffer, 0, this.BuffSize);            
        }

        private void LocationRedirect(Uri newUri)
        {            
            //if http protocol is different           
            //if host or port is differnt 
            if (string.CompareOrdinal(_uri.Scheme, newUri.Scheme) != 0 || string.CompareOrdinal(_uri.Host, newUri.Host) != 0 || _uri.Port != newUri.Port)
            {
                _uri = newUri;
                this.GetServiceEndPointAsync().ContinueWith((Task<EndPoint> task) =>
                {
                    if (task.IsFaulted)
                    {
                        _task.SetException(task.Exception.GetBaseException());
                        return;
                    }
                    var remoteEP = task.Result; //this.ServiceEndPoint;
                    //close a current connection and create a new connection
                    _connectChannel.Close();
                    _connectChannel = this.CreateTcpChannel(this.IsSecureRequest);
                    //re-register event bind
                    _connectChannel.Completed += channel_Completed;
                    _connectChannel.Error += channel_Error;
                    _connectChannel.Connect(remoteEP);
                }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);               
            }
            else
            {
                _uri = newUri;
                //send a new request use current connection
                _writeBuffer = this.GetRequestHeadersBytes();
                _connectChannel.Send(_writeBuffer, 0, this.BuffSize);
            }
        }

        /// <summary>
        /// Occurs when the http request task is completed include a timeout,exception,canceled.
        /// </summary>
        /// <param name="status">The http complated status.</param>
        private void OnCompleted(HttpRequestTaskStatus status)
        {
            if (status != this.RequestStatus)
            {
                _state = (int)status;
            }
            if (_task != null)
            {
                _task.Dispose();
            }
            _connectChannel.Close();
        }

        private ITcpChannel CreateTcpChannel(bool useSSL)
        {
            if (useSSL)
            {
                return new SecureTcpChannel(this, this.ClientCertificates);
            }
            return new TcpChannel(this);
        }
     
        #region Properties
        /// <summary>
        /// Gets whether this request is secure https request.
        /// </summary>
        private bool IsSecureRequest
        {
            get
            {
                return string.CompareOrdinal(this._uri.Scheme, Uri.UriSchemeHttps) == 0;
            }
        }

        /// <summary>
        /// Gets a value that indicates whether a response has been received from an Internet resource.
        /// </summary>
        public bool HaveResponse
        {
            get
            {
                return !(this.RequestStatus == HttpRequestTaskStatus.Init || this.RequestStatus == HttpRequestTaskStatus.Processing);
            }
        }

        /// <summary>
        /// Gets or sets the time-out value in milliseconds for the get HTTP response from a remote host.
        /// </summary>
        public int Timeout
        {
            get
            {
                return _timeout;
            }
            set
            {
                if (_timeout <= 0)
                {
                    throw new ArgumentOutOfRangeException("value","The specified value must be greater than 0");
                }
                _timeout = value;
            }
        }
        
        /// <summary>
        /// Gets or sets a value that indicates whether to make a persistent connection to the Internet resource.
        /// </summary>
        public bool KeepAlive
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the type of decompression that is used.
        /// </summary>
        public bool AutomaticDecompression
        {
            get
            {
                return _automaticDecompression;
            }
            set
            {
                if (this.RequestSubmitted)
                {
                    throw new InvalidOperationException("This property cannot be set after writing has started.");
                }
                _automaticDecompression = value;
            }
        }

        /// <summary>
        /// Gets or sets the collection of security certificates that are associated with this request.
        /// </summary>
        public X509CertificateCollection ClientCertificates
        {
            get
            {
                if (this._clientCertificates == null)
                {
                    this._clientCertificates = new X509CertificateCollection();
                }
                return this._clientCertificates;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                this._clientCertificates = value;
            }
        }

        /// <summary>
        /// Gets the request whether is submitted
        /// </summary>
        public bool RequestSubmitted
        {
            get
            {
                return _requestSubmitted;
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of redirects that the request follows.
        /// </summary>
        public int MaximumAutomaticRedirections
        {
            get
            {
                return _maximumAutomaticRedirections;
            }
            set
            {
                if (_maximumAutomaticRedirections <= 0)
                {
                    throw new ArgumentOutOfRangeException("value","The specified value must be greater than 0.");
                }
                _maximumAutomaticRedirections = value;
            }
        }      

        /// <summary>        
        /// Gets or sets the maximum allowed length of the response content.
        /// </summary>
        /// <remarks>
        ///  A value of -1 means no limit is imposed on the response content; a value of 0 means that all requests fail. 
        ///  If the MaximumResponseContentLength property is not explicitly set, it defaults value is -1.
        ///  If the length of the response content received exceeds the value of the MaximumResponseContentLength property,
        ///  will throw a <see cref="HttpException"/> with the Status property set to MessageLengthLimitExceeded.
        /// </remarks>
        public long MaximumResponseContentLength
        {
            get
            {
                return _maximumResponseContentLength;
            }
            set
            {
                if (value == 0)
                {
                    throw new ArgumentOutOfRangeException("value","The specified value must be greater than 0.");
                }
                _maximumResponseContentLength = value;
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
                _allowAutoRedirect = value;
            }
        }

        /// <summary>
        /// Gets the headers which should be sent with each request.
        /// </summary>
        public HttpRequestHeaders Headers
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets a value that specifies the size of the receive buffer of the request.
        /// </summary>
        /// <value>The default value is 512.</value>
        public int BuffSize
        {
            get
            {
                return _buffSize;
            }
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException("value","The specified value must be greater than 0.");
                }
                _buffSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the Content-length HTTP header.
        /// </summary>
        public long ContentLength
        {
            get
            {
                return _contentLength;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value","The specified value must be greater than 0.");
                }
                _contentLength = value;
            }
        }

        /// <summary>
        /// Gets or sets the method for the request.
        /// </summary>
        public HttpMethod Method
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the end point of remote host of the request.
        /// </summary>
        public EndPoint RemoteEndPoint
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the original Uniform Resource Identifier (URI) of the request.
        /// </summary>
        public Uri RequestUri
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the bool vlaue that indicates whether use gzip or deflate encoding with the request.
        /// </summary>
        public bool UseGzipMode
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value of the User-agent HTTP header.
        /// </summary>
        public string UserAgent
        {
            get
            {
                return this.Headers.UserAgent;
            }
            set
            {
                this.Headers.UserAgent = value;
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
                if (this.RequestSubmitted)
                {
                    throw new InvalidOperationException("This property cannot be set after writing has started.");
                }
                _proxy = value;
            }
        }

        private bool CanGetRequestStream
        {
            get
            {
                return !this.Method.ContentBodyNotAllowed;
            }
        }

        private bool HasEntityBody
        {
            get
            {
                if ((_submitData != null && _submitData.Length > 0) || this.ContentLength > 0)
                {
                    return true;
                }
                return false;
            }
        }

        private EndPoint ServiceEndPoint
        {
            get
            {
                return FindServicePoint(_uri, this.Proxy);
            }
        }

        private HttpRequestTaskStatus RequestStatus
        {
            get
            {
                return (HttpRequestTaskStatus)_state;
            }
        }
        
        #endregion

        private void InitRequest(Uri requestUri)
        {
            _state = 0;
            this.RequestUri = _uri = requestUri;
            this.Headers = new HttpRequestHeaders();
            this.Method = HttpMethod.Get;
            //init default values.
            _allowAutoRedirect = true;
            _timeout = 45000;
            _maximumAutomaticRedirections = 50;
            _maximumResponseContentLength = -1;
            _buffSize = 1024;
            _buffer = new byte[this.BuffSize];
        }

        private bool SetRequestSubmitted()
        {
            bool ret = _requestSubmitted;
            _requestSubmitted = true;
            return ret;
        }

        private void CheckProtocol(bool onRequestStream)
        {
            if (!this.CanGetRequestStream)
            {
                if (onRequestStream)
                {
                    throw new ProtocolViolationException("Cannot send a content-body with this verb-type.");
                }
            }
            if (!(_uri.Scheme == Uri.UriSchemeHttp || _uri.Scheme == Uri.UriSchemeHttps))
            {
                throw new NotSupportProtocolException("The HttpRequest class only support a following protocol : http or https");
            }
        }

        private void SetRequestHeaders()
        {
            //host
            this.Headers.SetInternal(KnownHeaderNames.Host, _uri.Host + ((!_uri.IsDefaultPort) ? ":" + _uri.Port : string.Empty));
            if (this.UseGzipMode)
            {
                this.Headers.SetInternal(KnownHeaderNames.AcceptEncoding, "gzip, deflate");
            }
            if (this.Method.RequireContentBody)
            {
                this.Headers.SetInternal(KnownHeaderNames.Pragma, "no-cache");
                if (this.HasEntityBody)
                {
                    this.Headers.SetInternal(KnownHeaderNames.ContentLanguage, _contentLength.ToString());
                }
            }
            if (this.KeepAlive)
            {
                this.Headers.SetInternal(KnownHeaderNames.Connection, "keep-alive");
            }
        }

        private byte[] GetRequestHeadersBytes()
        {
            var sb = new StringBuilder();
            //Request-Line   = Method SP Request-URI SP HTTP-Version CRLF          
            sb.Append(this.Method.Method);
            sb.Append(SP);
            sb.Append(this.Proxy != null && !this.Proxy.IsBypassed(_uri) ? _uri.AbsoluteUri : _uri.PathAndQuery);
            sb.Append(SP);
            sb.Append("HTTP/");
            sb.Append(HttpVersion);
            sb.Append(CRLF);           

            foreach (var header_pair in this.Headers)
            {
                //ignore this header if the value of header is null or empty.
                if (string.IsNullOrEmpty(header_pair.Value))
                {
                    continue;
                }
                sb.Append(header_pair.Key);
                sb.Append(":");
                sb.Append(header_pair.Value);
                sb.Append(CRLF);
            }

            sb.Append(CRLF);
            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        #region DNS Lookup
        private static EndPoint FindServicePoint(Uri address, IWebProxy proxy)
        {
            if (proxy != null)
            {
                address = proxy.GetProxy(address);
            }
            try
            {
                var hostAddresses = Dns.GetHostAddresses(address.Host);
                if (hostAddresses.Length == 0)
                {
                    throw new HttpException(HttpExceptionStatus.NameResolutionFailure, "Can't resolve specifed host.");
                }
                return new IPEndPoint(hostAddresses[0], address.Port);
            }
            catch (System.Net.Sockets.SocketException socketEx)
            {
                throw new HttpException(HttpExceptionStatus.NameResolutionFailure, socketEx.Message);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private Task<EndPoint> GetServiceEndPointAsync()
        {
            if (this.Proxy == null && this.RemoteEndPoint != null)
            {
                return Task.FromResult(this.RemoteEndPoint);
            }
            return FindServicePointAsync(_uri, this.Proxy);
        }

        public static Task<EndPoint> FindServicePointAsync(Uri address, IWebProxy proxy)
        {
            if (proxy != null)
            {
                address = proxy.GetProxy(address);
            }
            var tcs = new TaskCompletionSource<EndPoint>();
            Dns.GetHostAddressesAsync(address.Host).ContinueWith((Task<IPAddress[]> task, object state) =>
            {
                var ts = (TaskCompletionSource<EndPoint>)state;
                if (task.IsFaulted)
                {
                    ts.SetException(task.Exception.GetBaseException());
                    return;
                }
                if (task.IsCanceled)
                {
                    ts.SetCanceled();
                    return;
                }
                ts.SetResult(new IPEndPoint(task.Result[0], address.Port));
            }, tcs, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            return tcs.Task;
        }
        #endregion
    }    
}
