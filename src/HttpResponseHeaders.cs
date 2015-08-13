// Copyright (c) 2015 Yamool. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

namespace Yamool.Net.Http
{
    using System;

    // http://en.wikipedia.org/wiki/List_of_HTTP_header_fields

    /// <summary>
    ///  Represents the collection of Response Headers.
    /// </summary>
    public sealed class HttpResponseHeaders : HttpHeaders
    {
        /// <summary>
        /// What partial content range types this server supports
        /// </summary>
        public string AcceptRanges
        {
            get
            {
                return this[HttpHeaderNames.AcceptRanges];
            }
        }

        /// <summary>
        /// The age the object has been in a proxy cache in seconds
        /// </summary>
        public string Age
        {
            get
            {
                return this[HttpHeaderNames.Age];
            }
        }

        /// <summary>
        /// Tells all caching mechanisms from server to client whether they may cache this object. 
        /// It is measured in seconds
        /// </summary>
        public string CacheControl
        {
            get
            {
                return this[HttpHeaderNames.CacheControl];
            }
        }

        /// <summary>
        /// The MIME type of this content
        /// </summary>
        public string ContentType
        {
            get
            {
                return this[HttpHeaderNames.ContentType];
            }
        }

        /// <summary>
        /// Options that are desired for the connection
        /// </summary>
        public string Connection
        {
            get
            {
                return this[HttpHeaderNames.Connection];
            }
        }

        /// <summary>
        /// The type of encoding used on the data. See HTTP compression.
        /// </summary>
        public string ContentEncoding
        {
            get
            {
                return this[HttpHeaderNames.ContentEncoding];
            }
        }

        /// <summary>
        /// The length of the response body in octets (8-bit bytes)
        /// </summary>
        /// <remarks>
        /// If the Transfer-Encoding of response is chunked then the content-length value is null.
        /// </remarks>
        public long? ContentLength
        {
            get
            {
                var content_length = this[HttpHeaderNames.ContentLength];
                if (string.IsNullOrEmpty(content_length))
                {
                    return null;
                }
                return long.Parse(content_length);
            }
        }

        /// <summary>
        /// The character encoding of the document.
        /// </summary>
        public string Charset
        {
            get
            {
                string characterSet = null;
                string contentType = this.ContentType;
                string str2 = contentType.ToLower(System.Globalization.CultureInfo.InvariantCulture);
                if (str2.Trim().StartsWith("text/"))
                {
                    characterSet = "ISO-8859-1";
                }
                int index = str2.IndexOf(";");
                if (index > 0)
                {
                    while ((index = str2.IndexOf("charset", index)) >= 0)
                    {
                        index += 7;
                        if ((str2[index - 8] == ';') || (str2[index - 8] == ' '))
                        {
                            while ((index < str2.Length) && (str2[index] == ' '))
                            {
                                index++;
                            }
                            if ((index < (str2.Length - 1)) && (str2[index] == '='))
                            {
                                index++;
                                int num2 = str2.IndexOf(';', index);
                                if (num2 > index)
                                {
                                    characterSet = contentType.Substring(index, num2 - index).Trim();
                                }
                                else
                                {
                                    characterSet = contentType.Substring(index).Trim();
                                }
                                break;
                            }

                        }
                    }
                }
                return characterSet;
            }
        }

        /// <summary>
        /// The date and time that the message was sent (in "HTTP-date" format as defined by RFC 2616)
        /// </summary>
        public DateTime? Date
        {
            get
            {
                var value = this.GetDateHeaderHelper(HttpHeaderNames.Date);
                if (value == DateTime.MinValue)
                {
                    return null;
                }
                return value;
            }
        }

        /// <summary>
        /// An identifier for a specific version of a resource, often a message digest
        /// </summary>
        public string ETag
        {
            get
            {
                return this[HttpHeaderNames.ETag];
            }
        }

        /// <summary>
        /// Gives the date/time after which the response is considered stale
        /// </summary>
        public DateTime? Expires
        {
            get
            {
                var value = this.GetDateHeaderHelper(this[HttpHeaderNames.Expires]);
                if (value == DateTime.MinValue)
                {
                    return null;
                }
                return value;
            }
        }

        /// <summary>
        /// The last modified date for the requested object (in "HTTP-date" format as defined by RFC 2616
        /// </summary>
        public DateTime? LastModified
        {
            get
            {
                var value = this.GetDateHeaderHelper(this[HttpHeaderNames.LastModified]);
                if (value == DateTime.MinValue)
                {
                    return null;
                }
                return value;
            }
        }

        /// <summary>
        /// Used in redirection, or when a new resource has been created.
        /// </summary>
        public string Location
        {
            get
            {
                return this[HttpHeaderNames.Location];
            }
        }

        /// <summary>
        /// Implementation-specific headers that may have various effects anywhere along the request-response chain
        /// </summary>
        public string Pragma
        {
            get
            {
                return this[HttpHeaderNames.Pragma];
            }
        }

        /// <summary>
        /// Request authentication to access the proxy.
        /// </summary>
        public string ProxyAuthenticate
        {
            get
            {
                return this[HttpHeaderNames.ProxyAuthenticate];
            }
        }

        /// <summary>
        /// If an entity is temporarily unavailable, this instructs the client to try again later. 
        /// Value could be a specified period of time (in seconds) or a HTTP-date
        /// </summary>
        public string RetryAfter
        {
            get
            {
                return this[HttpHeaderNames.RetryAfter];
            }
        }

        /// <summary>
        /// A name for the server
        /// </summary>
        public string Server
        {
            get
            {
                return this[HttpHeaderNames.Server];
            }
        }

        /// <summary>
        /// An HTTP cookie
        /// </summary>
        public string SetCookie
        {
            get
            {
                return this[HttpHeaderNames.SetCookie];
            }
        }

        /// <summary>
        /// The Trailer general field value indicates that the given set of header 
        /// fields is present in the trailer of a message encoded with chunked transfer-coding.
        /// </summary>
        public string Trailer
        {
            get
            {
                return this[HttpHeaderNames.Trailer];
            }
        }

        /// <summary>
        /// The form of encoding used to safely transfer the entity to the user. 
        /// Currently defined methods are: chunked, compress, deflate, gzip, identity.
        /// </summary>
        public string TransferEncoding
        {
            get
            {
                return this[HttpHeaderNames.TransferEncoding];
            }
        }

        /// <summary>
        /// Ask the client to upgrade to another protocol.
        /// </summary>
        public string Upgrade
        {
            get
            {
                return this[HttpHeaderNames.Upgrade];
            }
        }

        /// <summary>
        /// Tells downstream proxies how to match future request headers to decide whether the 
        /// cached response can be used rather than requesting a fresh one from the origin server.
        /// </summary>
        public string Vary
        {
            get
            {
                return this[HttpHeaderNames.Vary];
            }
        }

        /// <summary>
        /// Informs the client of proxies through which the response was sent.
        /// </summary>
        public string Via
        {
            get
            {
                return this[HttpHeaderNames.Via];
            }
        }

        /// <summary>
        /// A general warning about possible problems with the entity body.
        /// </summary>
        public string Warning
        {
            get
            {
                return this[HttpHeaderNames.Warning];
            }
        }

        /// <summary>
        /// Indicates the authentication scheme that should be used to access the requested entity.
        /// </summary>
        public string WwwAuthenticate
        {
            get
            {
                return this[HttpHeaderNames.WWWAuthenticate];
            }
        }

        public override string ToString()
        {
            return this.GetAsString(false, true);
        }
    }
}
