// Copyright (c) 2015 Yamool. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

namespace Yamool.Net.Http
{
    using System;
    using System.Collections.Specialized;

    //http://en.wikipedia.org/wiki/List_of_HTTP_header_fields

    /// <summary>
    /// Represents the collection of Request Headers.
    /// </summary>
    public sealed class HttpRequestHeaders : HttpHeaders
    {
        public HttpRequestHeaders() { }

        /// <summary>
        /// Content-Types that are acceptable for the response
        /// </summary>
        public string Accept
        {
            get
            {
                return this[HttpHeaderNames.Accept];
            }
            set
            {
                this.SetSpecialHeaders(HttpHeaderNames.Accept, value);
            }
        }

        /// <summary>
        /// Character sets that are acceptable
        /// </summary>
        public string AcceptCharset
        {
            get
            {
                return this[HttpHeaderNames.AcceptCharset];
            }
            set
            {
                this.SetSpecialHeaders(HttpHeaderNames.AcceptCharset, value);
            }
        }

        /// <summary>
        /// List of acceptable encodings. See HTTP compression.
        /// </summary>
        public string AcceptEncoding
        {
            get
            {
                return this[HttpHeaderNames.AcceptEncoding];
            }
            set
            {
                this.SetSpecialHeaders(HttpHeaderNames.AcceptEncoding, value);
            }
        }

        /// <summary>
        /// List of acceptable human languages for response
        /// </summary>
        public string AcceptLanguage
        {
            get
            {
                return this[HttpHeaderNames.AcceptLanguage];
            }
            set
            {
                this.SetSpecialHeaders(HttpHeaderNames.AcceptLanguage, value);
            }
        }

        /// <summary>
        /// Authentication credentials for HTTP authentication
        /// </summary>
        public string Authorization
        {
            get
            {
                return this[HttpHeaderNames.Authorization];
            }
            set
            {
                this.SetSpecialHeaders(HttpHeaderNames.Authorization, value);
            }
        }

        /// <summary>
        /// Used to specify directives that MUST be obeyed by all caching mechanisms along the request/response chain
        /// </summary>
        public string CacheControl
        {
            get
            {
                return this[HttpHeaderNames.CacheControl];
            }
            set
            {
                this.SetSpecialHeaders(HttpHeaderNames.CacheControl, value);
            }
        }

        /// <summary>
        /// The MIME type of the body of the request (used with POST and PUT requests)
        /// </summary>
        public string ContentType
        {
            get
            {
                return this[HttpHeaderNames.ContentType];
            }
            set
            {
                this.SetSpecialHeaders(HttpHeaderNames.ContentType, value);
            }
        }
        /// <summary>
        /// What type of connection the user-agent would prefer
        /// </summary>
        public string Connection
        {
            get
            {
                return this[HttpHeaderNames.Connection];
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                if (value.IndexOf("keep-alive", StringComparison.OrdinalIgnoreCase) != -1 || value.IndexOf("close", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    throw new ArgumentException("connection value is invalid.");
                }
                this.SetSpecialHeaders(HttpHeaderNames.Connection, value);
            }
        }

        /// <summary>
        /// The date and time that the message was sent (in "HTTP-date" format as defined by RFC 2616)
        /// </summary>
        public DateTime Date
        {
            get
            {
                return this.GetDateHeaderHelper(HttpHeaderNames.Date);
            }
            set
            {
                this.SetDateHeaderHelper(HttpHeaderNames.Date, value);
            }
        }

        /// <summary>
        /// Indicates that particular server behaviors are required by the client
        /// </summary>
        public string Expect
        {
            get
            {
                return this[HttpHeaderNames.Expect];
            }
            set
            {
                this.SetSpecialHeaders(HttpHeaderNames.Expect, value);
            }
        }

        /// <summary>
        /// The email address of the user making the request
        /// </summary>
        public string From
        {
            get
            {
                return this[HttpHeaderNames.From];
            }
            set
            {
                this.SetSpecialHeaders(HttpHeaderNames.From, value);
            }
        }

        /// <summary>
        /// The domain name of the server (for virtual hosting), and the TCP port number on which the server is listening.
        /// </summary>
        public string Host
        {
            get
            {
                return this[HttpHeaderNames.Host];
            }
            set
            {
                this.SetSpecialHeaders(HttpHeaderNames.Host, value);
            }
        }

        /// <summary>
        /// Only perform the action if the client supplied entity matches the same entity on the server. 
        /// </summary>
        /// <remarks>
        /// This is mainly for methods like PUT to only update a resource if it has not been modified since the user last updated it.
        /// </remarks>
        public string IfMatch
        {
            get
            {
                return this[HttpHeaderNames.IfMatch];
            }
            set
            {
                this.SetSpecialHeaders(HttpHeaderNames.IfMatch, value);
            }
        }

        /// <summary>
        /// Allows a 304 Not Modified to be returned if content is unchanged
        /// </summary>
        public DateTime? IfModifiedSince
        {
            get
            {
                var value = this.GetDateHeaderHelper(HttpHeaderNames.IfModifiedSince);
                if (value == DateTime.MinValue)
                    return null;
                return value;
            }
            set
            {
                this.SetDateHeaderHelper(HttpHeaderNames.IfModifiedSince, value.Value);
            }
        }

        /// <summary>
        /// Allows a 304 Not Modified to be returned if content is unchanged, see HTTP ETag
        /// </summary>
        public string IfNoneMatch
        {
            get
            {
                return this[HttpHeaderNames.IfNoneMatch];
            }
            set
            {
                this.SetSpecialHeaders(HttpHeaderNames.IfNoneMatch, value);
            }
        }

        /// <summary>
        /// If the entity is unchanged, send me the part(s) that I am missing; otherwise, send me the entire new entity
        /// </summary>
        public string IfRange
        {
            get
            {
                return this[HttpHeaderNames.IfRange];
            }
            set
            {
                this.SetSpecialHeaders(HttpHeaderNames.IfRange, value);
            }
        }

        /// <summary>
        /// Only send the response if the entity has not been modified since a specific time.
        /// </summary>
        public string IfUnmodifiedSince
        {
            get
            {
                return this[HttpHeaderNames.IfUnmodifiedSince];
            }
            set
            {
                this.SetSpecialHeaders(HttpHeaderNames.IfUnmodifiedSince, value);
            }
        }

        /// <summary>
        /// Limit the number of times the message can be forwarded through proxies or gateways.
        /// </summary>
        public string MaxForwards
        {
            get
            {
                return this[HttpHeaderNames.MaxForwards];
            }
            set
            {
                this.SetSpecialHeaders(HttpHeaderNames.MaxForwards, value);
            }
        }

        /// <summary>
        /// Implementation-specific headers that may have various effects anywhere along the request-response chain.
        /// </summary>
        public string Pragma
        {
            get
            {
                return this[HttpHeaderNames.Pragma];
            }
            set
            {
                this.SetSpecialHeaders(HttpHeaderNames.Pragma, value);
            }
        }

        /// <summary>
        /// Authorization credentials for connecting to a proxy.
        /// </summary>
        public string ProxyAuthorization
        {
            get
            {
                return this[HttpHeaderNames.ProxyAuthorization];
            }
            set
            {
                this.SetSpecialHeaders(HttpHeaderNames.ProxyAuthorization, value);
            }
        }

        /// <summary>
        /// Request only part of an entity. Bytes are numbered from 0.
        /// </summary>
        public string Range
        {
            get
            {
                return this[HttpHeaderNames.Range];
            }
            set
            {
                this.SetSpecialHeaders(HttpHeaderNames.Range, value);
            }
        }

        /// <summary>
        /// This is the address of the previous web page from which a link to the currently requested page was followed.
        /// </summary>
        public string Referer
        {
            get
            {
                return this[HttpHeaderNames.Referer];
            }
            set
            {
                this.SetSpecialHeaders(HttpHeaderNames.Referer, value);
            }
        }

        /// <summary>
        /// The transfer encodings the user agent is willing to accept: the same values as for the response header 
        /// Transfer-Encoding can be used, plus the "trailers" value (related to the "chunked" transfer method) 
        /// to notify the server it expects to receive additional headers (the trailers) after the last, zero-sized, chunk.
        /// </summary>
        public string TE
        {
            get
            {
                return this[HttpHeaderNames.TE];
            }
            set
            {
                this.SetSpecialHeaders(HttpHeaderNames.TE, value);
            }
        }

        public string TransferEncoding
        {
            get
            {
                return this[HttpHeaderNames.TransferEncoding];
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                this.SetSpecialHeaders(HttpHeaderNames.TransferEncoding, value);
            }
        }

        /// <summary>
        /// Ask the server to upgrade to another protocol.
        /// </summary>
        public string Upgrade
        {
            get
            {
                return this[HttpHeaderNames.Upgrade];
            }
            set
            {
                this.SetSpecialHeaders(HttpHeaderNames.Upgrade, value);
            }
        }

        /// <summary>
        /// The user agent string of the user agent
        /// </summary>
        public string UserAgent
        {
            get
            {
                return this[HttpHeaderNames.UserAgent];
            }
            set
            {
                this.SetSpecialHeaders(HttpHeaderNames.UserAgent, value);
            }
        }

        /// <summary>
        /// Informs the server of proxies through which the request was sent.
        /// </summary>
        public string Via
        {
            get
            {
                return this[HttpHeaderNames.Via];
            }
            set
            {
                this.SetSpecialHeaders(HttpHeaderNames.Via, value);
            }
        }

        /// <summary>
        /// A general warning about possible problems with the entity body
        /// </summary>
        public string Warning
        {
            get
            {
                return this[HttpHeaderNames.Warning];
            }
            set
            {
                this.SetSpecialHeaders(HttpHeaderNames.Warning, value);
            }
        }

        public override string ToString()
        {
            return this.GetAsString(false, false);
        }
    }
}
