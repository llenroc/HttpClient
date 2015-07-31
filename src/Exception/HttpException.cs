//----------------------------------------------------------------
// Copyright (c) Yamool Inc.  All rights reserved.
//----------------------------------------------------------------

namespace Yamool.Net.Http
{
    using System;    

    /// <summary>
    /// Describes an exception that occurred during the processing of HTTP requests/response.
    /// </summary>
    public class HttpException : Exception
    {
        public HttpException(string message)
            : this(HttpExceptionStatus.UnknownError, message)
        { }

        public HttpException(HttpExceptionStatus statusCode, string message)
            : this(statusCode, message, null)
        { }

        public HttpException(HttpExceptionStatus statusCode, string message, Exception innerException)
            : base(message, innerException)
        {
            this.StatusCode = statusCode;
        }

        public HttpExceptionStatus StatusCode
        {
            get;
            private set;
        }
    }
}
