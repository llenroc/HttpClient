// Copyright (c) 2015 Yamool. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

namespace Yamool.Net.Http
{
    using System;
    using System.Net;

    /// <summary>
    /// Describes an exception that occurred during the processing of HTTP requests/response.
    /// </summary>
    public abstract class HttpException : Exception
    {
        public HttpException() { }

        public HttpException(string message) : base(message) { }

        public HttpException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>    
    /// Represents the Http protocol exception.
    /// </summary>
    public class ProtocolException : HttpException
    {
        public ProtocolException() { }

        public ProtocolException(string message) : base(message) { }

        public ProtocolException(string message, Exception innerException) : base(message, innerException) { }
    }
    
    /// <summary>
    /// Describes an exception of Http redirection during the HTTP Request.
    /// </summary>
    public class RedirectException : ProtocolException
    {
        public RedirectException(Uri uri)
        {
            this.RequestUri = uri;
        }

        public RedirectException(Uri uri, string message)
            : base(message)
        {
            this.RequestUri = uri;
        }

        public RedirectException(Uri uri, string message, Exception innerException)
            : base(message, innerException)
        {
            this.RequestUri = uri;
        }

        /// <summary>
        /// The original request uri which cause this exception.
        /// </summary>
        public Uri RequestUri { get; private set; }
    }

    /// <summary>
    /// Describes an exception of circular redirection during the Http Request.
    /// </summary>
    public class CircularRedirectException : RedirectException
    {
        public CircularRedirectException(int redirectedCount, Uri uri)
            : base(uri)
        {
            this.RedirectedCount = redirectedCount;
        }

        public CircularRedirectException(int redirectedCount, Uri uri, string message)
            : base(uri, message)
        {
            this.RedirectedCount = redirectedCount;
        }

        public CircularRedirectException(int redirectedCount, Uri uri, string message, Exception innerException)
            : base(uri, message, innerException)
        {
            this.RedirectedCount = redirectedCount;
        }

        /// <summary>
        /// The redirected count in this request.
        /// </summary>
        public int RedirectedCount { get; private set; }
    }

    /// <summary>
    /// Represents the HTTP protocol violation exception.
    /// </summary>
    public class ProtocolViolationException : ProtocolException
    {
        public ProtocolViolationException() { }

        public ProtocolViolationException(string message) : base(message) { }

        public ProtocolViolationException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Describes an exception that occurred during the processing of HTTP request.
    /// </summary>
    public class HttpRequestException : HttpException
    {
         public HttpRequestException() { }

        public HttpRequestException(string message) : base(message) { }

        public HttpRequestException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Describes an exception that occurred during the processing of HTTP response.
    /// </summary>
    public class HttpResponseException : HttpException
    {
        public HttpResponseException(string message, WebExceptionStatus status)
            : base(message)
        {
            this.Status = status;
        }

        /// <summary>
        /// Gets the status of the response.
        /// </summary>
        public WebExceptionStatus Status
        {
            get;
            private set;
        }
    }

    /// <summary>
    /// Descripbes an exception that occures during the IO transferring data.
    /// </summary>
    public class HttpOperationException : HttpException
    {
        public HttpOperationException() : base() { }

        public HttpOperationException(string message) : base(message) { }
    }

    internal class InternalException : HttpException
    {
    }
}
