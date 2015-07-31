//----------------------------------------------------------------
// Copyright (c) Yamool Inc.  All rights reserved.
//----------------------------------------------------------------

namespace Yamool.Net.Http
{
    using System;   

    /// Represent the status of the response.
    /// </summary>
    /// <remarks>
    /// [rfc2616]    
    /// Status-Line = HTTP-Version SP Status-Code SP Reason-Phrase CRLF
    /// </remarks>
    internal struct HttpResponseStatus
    {
        /// <summary>
        /// Gets or sets the status code 
        /// see more at:http://msdn.microsoft.com/en-us/library/system.net.httpstatuscode.aspx
        /// </summary>
        public HttpStatusCode Code;

        /// <summary>
        /// Gets or sets the status description
        /// </summary>
        public string Description;

        /// <summary>
        /// Gets or sets the http version in the request
        /// </summary>
        public string HttpVersion;
    }
}
