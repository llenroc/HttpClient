//----------------------------------------------------------------
// Copyright (c) Yamool Inc.  All rights reserved.
//----------------------------------------------------------------

namespace Yamool.Net.Http
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The standard http method for http request.
    /// </summary>
    public sealed class HttpMethod : IEquatable<HttpMethod>
    {
        private string _methodName;
        private VerbOption _verbs;

        [Flags]
        internal enum VerbOption
        {
            RequireContentBody = 0x1,
            ContentBodyNotAllowed = 0x2,
            ConnectRequest = 0x4,
            ExpectNoContentResponse = 0X8
        }

        static HttpMethod()
        {
            Get = new HttpMethod("GET", VerbOption.ContentBodyNotAllowed);
            Post = new HttpMethod("POST", VerbOption.RequireContentBody);
            Head = new HttpMethod("HEAD", VerbOption.ContentBodyNotAllowed | VerbOption.ExpectNoContentResponse);
            Options = new HttpMethod("OPTIONS", VerbOption.ContentBodyNotAllowed | VerbOption.ExpectNoContentResponse);
        }

        internal HttpMethod(string method, VerbOption verbs)
        {
            _methodName = method;
            _verbs = verbs;
        }        

        /// <summary>
        /// Represents an HTTP GET protocol method.
        /// </summary>
        public static HttpMethod Get
        {
            get;
            private set;
        }

        /// <summary>
        /// Represents an HTTP POST protocol method that is used to post a new entity as an addition to a URI.
        /// </summary>
        public static HttpMethod Post
        {
            get;
            private set;
        }

        /// <summary>
        /// Represents an HTTP HEAD protocol method.
        /// </summary>
        public static HttpMethod Head
        {
            get;
            private set;
        }

        /// <summary>
        /// Represents an HTTP OPTIONS protocol method.
        /// </summary>
        public static HttpMethod Options
        {
            get;
            private set;
        }

        /// <summary>
        /// An HTTP method.
        /// </summary>
        public string Method
        {
            get
            {
                return _methodName;
            }
        }

        internal bool ContentBodyNotAllowed
        {
            get
            {
                return (_verbs & VerbOption.ContentBodyNotAllowed) == VerbOption.ContentBodyNotAllowed;
            }
        }

        internal bool ExpectNoContentResponse
        {
            get
            {
                return (_verbs & VerbOption.ExpectNoContentResponse) == VerbOption.ExpectNoContentResponse;
            }
        }

        internal bool RequireContentBody
        {
            get
            {
                return (_verbs & VerbOption.RequireContentBody) == VerbOption.RequireContentBody;
            }
        }

        internal bool ConnectRequest
        {
            get
            {
                return (_verbs & VerbOption.ConnectRequest) == VerbOption.ConnectRequest;
            }
        }

        public override string ToString()
        {
            return this.Method;
        }

        public override int GetHashCode()
        {
            return this.Method.ToUpperInvariant().GetHashCode();
        }

        public bool Equals(HttpMethod other)
        {
            return other != null && (object.ReferenceEquals(this, other) || string.Compare(this.Method, other.Method, StringComparison.OrdinalIgnoreCase) == 0);
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as HttpMethod);
        }

        public static bool operator ==(HttpMethod left, HttpMethod right)
        {
            if (left == null || right == null)
            {
                return true;
            }
            return left.Equals(right);
        }

        public static bool operator !=(HttpMethod left, HttpMethod right)
        {
            return !(left == right);
        }
    }       
}
