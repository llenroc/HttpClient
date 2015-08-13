// Copyright (c) 2015 Yamool. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

namespace Yamool.Net.Http
{
    using System;

    /// <summary>
    ///  The set of common methods for the HTTP request.
    /// </summary>
    public sealed class HttpMethod : IEquatable<HttpMethod>
    {
        private string _name;
        private VerbOptions _options;

        /// <summary>
        /// Represents an HTTP GET method.
        /// </summary>
        public readonly static HttpMethod Get = new HttpMethod("GET", VerbOptions.AllowDefaultResponse);

        /// <summary>
        /// Represents an HTTP POST method.
        /// </summary>
        public readonly static HttpMethod Post = new HttpMethod("POST", VerbOptions.AllowRequestContent | VerbOptions.AllowDefaultResponse);

        /// <summary>
        /// Represents an HTTP HEAD method.
        /// </summary>
        public readonly static HttpMethod Head = new HttpMethod("HEAD", VerbOptions.AllowResponseHeader);

        /// <summary>
        /// Represents an HTTP PUT method.
        /// </summary>
        public readonly static HttpMethod Put = new HttpMethod("PUT", VerbOptions.AllowRequestContent | VerbOptions.AllowDefaultResponse);

        /// <summary>
        /// Represents an HTTP OPTIONS method.
        /// </summary>
        public readonly static HttpMethod Options = new HttpMethod("OPTIONS", VerbOptions.AllowResponseHeader);

        /// <summary>
        /// Represents an HTTP DELETE method.
        /// </summary>
        public readonly static HttpMethod Delete = new HttpMethod("DELETE", VerbOptions.AllowDefaultResponse);

        private HttpMethod(string name, VerbOptions options)
        {
            _name = name;
            _options = options;
        }

        public string Name
        {
            get
            {
                return _name;
            }
        }

        internal bool AllowRequestContent
        {
            get
            {
                return (_options & VerbOptions.AllowRequestContent) == VerbOptions.AllowRequestContent;
            }
        }

        internal bool AllowResponseHeader
        {
            get
            {
                return (_options & VerbOptions.AllowResponseHeader) == VerbOptions.AllowResponseHeader;
            }
        }

        internal bool AllowResponseContent
        {
            get
            {
                return (_options & VerbOptions.AllowResponseContent) == VerbOptions.AllowResponseContent;
            }
        }

        public bool Equals(HttpMethod other)
        {
            if (other == null)
            {
                return false;
            }
            return string.Compare(_name, other._name, true) == 0;
        }

        public override string ToString()
        {
            return _name;
        }

        [Flags]
        private enum VerbOptions
        {
            AllowRequestContent = 0x1,

            AllowResponseHeader = 0x2,

            AllowResponseContent = 0x4,

            AllowDefaultResponse = AllowResponseHeader | AllowResponseContent
        }
    }
}
