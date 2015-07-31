//----------------------------------------------------------------
// Copyright (c) Yamool Inc.  All rights reserved.
//----------------------------------------------------------------

namespace Yamool.Net.Http
{
    using System;

    /// <summary>
    /// Provides extension methods for the <see cref="HttpHeaders"/> class.
    /// </summary>
    public static class HttpHeaderExtensions
    {
        /// <summary>
        /// Set an HTTP header.
        /// </summary>
        /// <param name="headers">Tht Http header collection.</param>
        /// <param name="name">The name of the HTTP header.</param>
        /// <param name="value">The value of the HTTP header.</param>
        /// <returns><c>HttpHeaders</c></returns>
        public static HttpHeaders WithHeader(this HttpHeaders headers, string name, string value)
        {
            headers.Set(name, value);
            return headers;
        }
    }
}
