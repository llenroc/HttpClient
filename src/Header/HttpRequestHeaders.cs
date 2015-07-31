//----------------------------------------------------------------
// Copyright (c) Yamool Inc.  All rights reserved.
//----------------------------------------------------------------

namespace Yamool.Net.Http
{
    using System;
    using System.Collections.Specialized;

    /// <summary>
    /// Represents the collection of Request Headers.
    /// </summary>
    /// <remarks>
    /// http://en.wikipedia.org/wiki/List_of_HTTP_header_fields
    /// </remarks>
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
                return this[KnownHeaderNames.Accept];
            }
            set
            {
                this[KnownHeaderNames.Accept] = value;
            }
        }

        /// <summary>
        /// Character sets that are acceptable
        /// </summary>
        public string AcceptCharset
        {
            get
            {
                return this[KnownHeaderNames.AcceptCharset];
            }
            set
            {
                this[KnownHeaderNames.AcceptCharset] = value;
            }
        }

        /// <summary>
        /// List of acceptable encodings. See HTTP compression.
        /// </summary>
        public string AcceptEncoding
        {
            get
            {
                return this[KnownHeaderNames.AcceptEncoding];
            }
            set
            {
                this[KnownHeaderNames.AcceptEncoding] = value;
            }
        }

        /// <summary>
        /// List of acceptable human languages for response
        /// </summary>
        public string AcceptLanguage
        {
            get
            {
                return this[KnownHeaderNames.AcceptLanguage];
            }
            set
            {
                this[KnownHeaderNames.AcceptLanguage] = value;
            }
        }

        /// <summary>
        /// Authentication credentials for HTTP authentication
        /// </summary>
        public string Authorization
        {
            get
            {
                return this[KnownHeaderNames.Authorization];
            }
            set
            {
                this[KnownHeaderNames.Authorization] = value;
            }
        }

        /// <summary>
        /// Used to specify directives that MUST be obeyed by all caching mechanisms along the request/response chain
        /// </summary>
        public string CacheControl
        {
            get
            {
                return this[KnownHeaderNames.CacheControl];
            }
            set
            {
                this[KnownHeaderNames.CacheControl] = value;
            }
        }

        /// <summary>
        /// The MIME type of the body of the request (used with POST and PUT requests)
        /// </summary>
        public string ContentType
        {
            get
            {
                return this[KnownHeaderNames.ContentType];
            }
            set
            {
                this[KnownHeaderNames.ContentType] = value;
            }
        }
        /// <summary>
        /// What type of connection the user-agent would prefer
        /// </summary>
        public string Connection
        {
            get
            {
                return this[KnownHeaderNames.Connection];
            }
            set
            {
                this[KnownHeaderNames.Connection] = value;
            }
        }

        /// <summary>
        /// The date and time that the message was sent (in "HTTP-date" format as defined by RFC 2616)
        /// </summary>
        public DateTime Date
        {
            get
            {
                return this.GetDateHeaderHelper(KnownHeaderNames.Date);
            }
            set
            {
                this.SetDateHeaderHelper(KnownHeaderNames.Date, value);
            }
        }

        /// <summary>
        /// Indicates that particular server behaviors are required by the client
        /// </summary>
        public string Expect
        {
            get
            {
                return this[KnownHeaderNames.Expect];
            }
            set
            {
                this[KnownHeaderNames.Expect] = value;
            }
        }

        /// <summary>
        /// The email address of the user making the request
        /// </summary>
        public string From
        {
            get
            {
                return this[KnownHeaderNames.From];
            }
            set
            {
                this[KnownHeaderNames.From] = value;
            }
        }

        /// <summary>
        /// The domain name of the server (for virtual hosting), and the TCP port number on which the server is listening.
        /// </summary>
        public string Host
        {
            get
            {
                return this[KnownHeaderNames.Host];
            }
            set
            {
                this[KnownHeaderNames.Host] = value;
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
                return this[KnownHeaderNames.IfMatch];
            }
            set
            {
                this[KnownHeaderNames.IfMatch] = value;
            }
        }

        /// <summary>
        /// Allows a 304 Not Modified to be returned if content is unchanged
        /// </summary>
        public DateTime IfModifiedSince
        {
            get
            {
                return this.GetDateHeaderHelper(KnownHeaderNames.IfModifiedSince);
            }
            set
            {
                this.SetDateHeaderHelper(KnownHeaderNames.IfModifiedSince, value);
            }
        }

        /// <summary>
        /// Allows a 304 Not Modified to be returned if content is unchanged, see HTTP ETag
        /// </summary>
        public string IfNoneMatch
        {
            get
            {
                return this[KnownHeaderNames.IfNoneMatch];
            }
            set
            {
                this[KnownHeaderNames.IfNoneMatch] = value;
            }
        }

        /// <summary>
        /// If the entity is unchanged, send me the part(s) that I am missing; otherwise, send me the entire new entity
        /// </summary>
        public string IfRange
        {
            get
            {
                return this[KnownHeaderNames.IfRange];
            }
            set
            {
                this[KnownHeaderNames.IfRange] = value;
            }
        }

        /// <summary>
        /// Only send the response if the entity has not been modified since a specific time.
        /// </summary>
        public string IfUnmodifiedSince
        {
            get
            {
                return this[KnownHeaderNames.IfUnmodifiedSince];
            }
            set
            {
                this[KnownHeaderNames.IfUnmodifiedSince] = value;
            }
        }

        /// <summary>
        /// Limit the number of times the message can be forwarded through proxies or gateways.
        /// </summary>
        public string MaxForwards
        {
            get
            {
                return this[KnownHeaderNames.MaxForwards];
            }
            set
            {
                this[KnownHeaderNames.MaxForwards] = value;
            }
        }

        /// <summary>
        /// Implementation-specific headers that may have various effects anywhere along the request-response chain.
        /// </summary>
        public string Pragma
        {
            get
            {
                return this[KnownHeaderNames.Pragma];
            }
            set
            {
                this[KnownHeaderNames.Pragma] = value;
            }
        }

        /// <summary>
        /// Authorization credentials for connecting to a proxy.
        /// </summary>
        public string ProxyAuthorization
        {
            get
            {
                return this[KnownHeaderNames.ProxyAuthorization];
            }
            set
            {
                this[KnownHeaderNames.ProxyAuthorization] = value;
            }
        }

        /// <summary>
        /// Request only part of an entity. Bytes are numbered from 0.
        /// </summary>
        public string Range
        {
            get
            {
                return this[KnownHeaderNames.Range];
            }
            set
            {
                this[KnownHeaderNames.Range] = value;
            }
        }

        /// <summary>
        /// This is the address of the previous web page from which a link to the currently requested page was followed.
        /// </summary>
        public string Referrer
        {
            get
            {
                return this[KnownHeaderNames.Referer];
            }
            set
            {
                this[KnownHeaderNames.Referer] = value;
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
                return this[KnownHeaderNames.TE];
            }
            set
            {
                this[KnownHeaderNames.TE] = value;
            }
        }


        /// <summary>
        /// Ask the server to upgrade to another protocol.
        /// </summary>
        public string Upgrade
        {
            get
            {
                return this[KnownHeaderNames.Upgrade];
            }
            set
            {
                this[KnownHeaderNames.Upgrade] = value;
            }
        }

        /// <summary>
        /// The user agent string of the user agent
        /// </summary>
        public string UserAgent
        {
            get
            {
                return this[KnownHeaderNames.UserAgent];
            }
            set
            {
                this[KnownHeaderNames.UserAgent] = value;
            }
        }

        /// <summary>
        /// Informs the server of proxies through which the request was sent.
        /// </summary>
        public string Via
        {
            get
            {
                return this[KnownHeaderNames.Via];
            }
            set
            {
                this[KnownHeaderNames.Via] = value;
            }
        }

        /// <summary>
        /// A general warning about possible problems with the entity body
        /// </summary>
        public string Warning
        {
            get
            {
                return this[KnownHeaderNames.Warning];
            }
            set
            {
                this[KnownHeaderNames.Warning] = value;
            }
        }     
    }
}
