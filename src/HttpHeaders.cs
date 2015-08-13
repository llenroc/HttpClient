// Copyright (c) 2015 Yamool. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

namespace Yamool.Net.Http
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// A collection of headers and their values as defined in RFC 2616.
    /// </summary>
    public abstract class HttpHeaders : IEnumerable<KeyValuePair<string, string>>, IEnumerable
    {
        private static readonly char[] _invalidChars = new char[] { ' ', '\r', '\n', '\t' };
        private static readonly char[] _headerTrimChars = new char[] { '\t', '\n', '\v', '\f', '\r', ' ' };

        private Dictionary<string, string> _headerStore;
        private string[] _allKeys;

        protected HttpHeaders()
        {
            _headerStore = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets or sets the entry with the specified key in the Collection.
        /// </summary>
        /// <param name="name">The String key of the entry to locate. </param>
        /// <returns>Return null if not found.otherwise header value.</returns>
        public string this[string name]
        {
            get
            {
                return this.Get(name);
            }
            set
            {
                this.Set(name, value);
            }
        }

        /// <summary>
        /// Gets the entry at the specified index of the NameValueCollection.
        /// </summary>
        /// <param name="index">the index of the collection</param>
        /// <returns></returns>
        public string this[int index]
        {
            get
            {
                return this.Get(index);
            }
        }

        public bool IsReadOnly
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the number of header contained in the Collection.
        /// </summary>
        public int Count
        {
            get
            {
                return _headerStore.Count;
            }
        }

        public string[] AllKeys
        {
            get
            {
                if (_allKeys == null)
                {
                    _allKeys = _headerStore.Keys.ToArray();
                }
                return _allKeys;
            }
        }

        /// <summary>
        /// Inserts a header with the specified name and value into the collection
        /// </summary>
        /// <param name="name">The name of the header add to the collection.</param>
        /// <param name="value">The content of the header to set.</param>
        /// <remarks>If the specified the header already exists in the header then will add new header value to the end of header with same header keys.</remarks>
        public void Add(string name, string value)
        {
            if (this.IsReadOnly)
            {
                throw new NotSupportedException("The http header collection is read-only.");
            }
            name = CheckBadChars(name, false);
            value = CheckBadChars(value, true);
            this.AddInternal(name, value);
        }

        /// <summary>
        /// Sets the specified header to the specified value.
        /// </summary>
        /// <param name="name">The header to set.</param>
        /// <param name="value">The content of the header to set.</param>
        /// <remarks>
        /// If the header specified in header is already present, value replaces the existing value
        /// </remarks>
        public void Set(string name, string value)
        {
            if (this.IsReadOnly)
            {
                throw new NotSupportedException("The http header collection is read-only.");
            }
            name = CheckBadChars(name, false);
            value = CheckBadChars(value, true);
            this.SetInternal(name, value);
        }

        /// <summary>
        /// Removes the specified header from the HttpHeaders collection.
        /// </summary>
        /// <param name="name">The name of the header to remove from the collection.</param>
        public bool Remove(string name)
        {
            if (this.IsReadOnly)
            {
                throw new NotSupportedException("The http header collection is read-only.");
            }
            name = CheckBadChars(name, false);
            return this.RemoveInternal(name);
        }

        internal void SetReadOnly()
        {
            this.IsReadOnly = true;
        }

        /// <summary>
        /// Get the value of a particular header in the collection, specified by an index into the collection
        /// </summary>
        /// <param name="index">The zero-based index of the key to get from the collection.</param>
        /// <returns>return a value of the particular header at the specified index</returns>  
        /// <remarks>
        /// If collection have a multi-values in particular header based on the specified name of the header,will
        /// return a string that use ',' to separated multi-values.
        /// </remarks>
        public string Get(int index)
        {
            if (index >= this.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            var name = this.AllKeys[index];
            return this.Get(name);
        }

        /// <summary>        
        /// Get the value of a particular header in the collection.
        /// </summary>
        /// <param name="name">the name of web header.</param>
        /// <returns>return null if not found in the collection.</returns>
        /// <remarks>
        /// If collection have a multi-values in particular header based on the specified name of the header,will
        /// return a string that use ',' to separated multi-values.
        /// </remarks>
        public string Get(string name)
        {
            string value = null;
            _headerStore.TryGetValue(name, out value);
            return value;
        }

        /// <summary>
        /// Removes all headers from the collection.
        /// </summary>
        public void Clear()
        {
            if (this.IsReadOnly)
            {
                throw new NotSupportedException("Collection is read-only");
            }
            this.InvalidateCachedArrays();
            _headerStore.Clear();
        }

        internal void AddInternal(string name, string value)
        {
            this.InvalidateCachedArrays();
            if (_headerStore.ContainsKey(name))
            {
                _headerStore[name] += "," + value;
            }
            else
            {
                _headerStore[name] = value;
            }
        }

        internal void SetInternal(string name, string value)
        {
            this.InvalidateCachedArrays();
            _headerStore[name] = value;
        }

        internal bool RemoveInternal(string name)
        {
            return _headerStore.Remove(name);
        }

        internal void AddHeaders(HttpHeaders headers)
        {
            if (headers.Count == 0)
            {
                return;
            }
            foreach (var pair in headers)
            {
                this.SetInternal(pair.Key, pair.Value);
            }
        }

        internal static string CheckBadChars(string name, bool isHeaderValue)
        {
            if (string.IsNullOrEmpty(name))
            {
                if (!isHeaderValue)
                {
                    throw new ArgumentException("The name of header is empty.");
                }
                return string.Empty;
            }
            //VALUE check
            if (isHeaderValue)
            {
                //Trim spaces from both ends 
                name = name.Trim(_headerTrimChars);
                int crlf = 0;
                for (int i = 0; i < name.Length; i++)
                {
                    char c = (char)(0x000000ff & (uint)name[i]);
                    //if the value contains a \r\n chars then will ignore the next an chars that with SPACE or \t
                    switch (crlf)
                    {
                        case 0:
                            if (c == '\r')
                            {
                                crlf = 1;
                            }
                            else if (c == '\n')
                            {
                                // Technically this is bad HTTP.  But it would be a breaking change to throw here.
                                // Is there an exploit? 
                                crlf = 2;
                            }
                            else if (c == 127 || (c < ' ' && c != '\t'))
                            {
                                throw new ArgumentException("Specified value has invalid HTTP Header characters.", "value");
                            }
                            break;

                        case 1:
                            if (c == '\n')
                            {
                                crlf = 2;
                                break;
                            }
                            throw new ArgumentException("Specified value has invalid HTTP Header characters.", "value");

                        case 2:
                            if (c == ' ' || c == '\t')
                            {
                                crlf = 0;
                                break;
                            }
                            throw new ArgumentException("Specified value has invalid HTTP Header characters.", "value");
                    }
                }
                if (crlf != 0)
                {
                    throw new ArgumentException("Specified value has invalid HTTP Header characters.", "value");
                }
                return name;
            }
            //NAME check

            //First, check for absence of separators and spaces
            if (name.IndexOfAny(_invalidChars) != -1)
            {
                throw new ArgumentException("Specified value has invalid HTTP Header characters.", "name");
            }
            //Second, check for non CTL ASCII-7 characters (32-126) 
            if (ContainsNonAsciiChars(name))
            {
                throw new ArgumentException("Specified value has invalid Http Header characters.", "name");
            }
            return name;
        }

        internal static bool ContainsNonAsciiChars(string token)
        {
            for (var i = 0; i < token.Length; i++)
            {
                if ((token[i] < ' ') || (token[i] > '~'))
                {
                    return true;
                }
            }
            return false;
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            foreach (var pair in _headerStore)
            {
                yield return pair;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public override string ToString()
        {
            return this.GetAsString(false, true);
        }

        internal string GetAsString(bool winInetCompat, bool forTrace)
        {
            if (_headerStore.Count == 0)
            {
                return "\r\n";
            }
            var sb = new StringBuilder(30 * _headerStore.Count);
            foreach (var pair in _headerStore)
            {
                if (string.IsNullOrEmpty(pair.Value))
                {
                    continue;
                }
                sb.Append(pair.Key);
                sb.Append(winInetCompat ? ":" : ": ");
                sb.Append(pair.Value);
                sb.Append("\r\n");
            }
            if (!forTrace)
            {
                sb.Append("\r\n");
            }
            return sb.ToString();
        }

        internal DataParseStatus ParseHeaders(ArraySegment<byte> data, ref int bytesParsed, ref int totalResponseHeadersLength, int maximumResponseHeadersLength)
        {
            var parseStatus = DataParseStatus.DataTooBig;
            var length = data.Count;
            var i = bytesParsed;
            var effectiveMax = maximumResponseHeadersLength <= 0 ? int.MaxValue : (maximumResponseHeadersLength - totalResponseHeadersLength + i);
            if (length < effectiveMax)
            {
                effectiveMax = length;
                parseStatus = DataParseStatus.NeedMoreData;
            }
            if (i >= effectiveMax)
            {
                return parseStatus;
            }
            while (i < length)
            {
                if (data.Get(i) == '\r')
                {
                    if (++i == effectiveMax)
                    {
                        break;
                    }
                    if (data.Get(i++) == '\n')
                    {
                        totalResponseHeadersLength += i - bytesParsed;
                        bytesParsed = i;
                        parseStatus = DataParseStatus.Done;
                        break;
                    }
                    parseStatus = DataParseStatus.Invalid;
                    break;
                }
                var iBeginName = i;
                //Read Header name.
                for (; i < effectiveMax && data.Get(i++) != ':'; ) { }
                if (i == effectiveMax)
                {
                    break;
                }
                if (i == iBeginName)
                {
                    parseStatus = DataParseStatus.Invalid;
                    //invalid header name
                    break;
                }
                var iEndName = i - 1;
                var crlf = 0;
                var iBeginValue = -1;
                var iEndValue = -1;
                //Read Header value.
                for (; i < effectiveMax && crlf != 2; i++)
                {
                    var ch = (char)data.Get(i);
                    switch (ch)
                    {
                        case ' ':
                            {
                                continue;
                            }
                        case '\r':
                            {
                                if (crlf == 0)
                                {
                                    crlf = 1;
                                }
                                break;
                            }
                        case '\n':
                            {
                                if (crlf == 1)
                                {
                                    crlf = 2;
                                }
                                break;
                            }
                    }
                    if (iBeginValue == -1)
                    {
                        iBeginValue = i;
                    }
                    if (crlf == 0)
                    {
                        iEndValue = i;
                    }
                }
                if (i == iBeginValue)
                {
                    parseStatus = DataParseStatus.Invalid;
                    break;
                }   
                if (crlf != 2)
                {
                    break;
                }
                var headerName = Encoding.UTF8.GetString(data.Array, data.Offset + iBeginName, iEndName - iBeginName);
                var headerValue = Encoding.UTF8.GetString(data.Array, data.Offset + iBeginValue, iEndValue - iBeginValue + 1);
                this.AddInternal(headerName, headerValue);
                totalResponseHeadersLength += i - bytesParsed;
                bytesParsed = i;               
            }
            return parseStatus;
        }

        private void InvalidateCachedArrays()
        {
            _allKeys = null;
        }

        /// <summary>
        /// Set the specialed datetime to the header with the header name.
        /// </summary>
        /// <param name="headerName">the name of the header.</param>
        /// <param name="dateTime">the datetime to set.</param>
        protected void SetDateHeaderHelper(string headerName, DateTime dateTime)
        {
            if (dateTime == DateTime.MinValue)
            {
                this.SetInternal(headerName, null);
            }
            else
            {
                this.SetInternal(headerName, HttpUtils.Date2String(dateTime));
            }
        }

        /// <summary>
        /// Get the datetime with specialed the name of the header.
        /// </summary>
        /// <param name="headerName">the name of the header.</param>
        /// <returns>DateTime.</returns>
        protected DateTime GetDateHeaderHelper(string headerName)
        {
            string s = this[headerName];
            if (s == null)
            {
                return DateTime.MinValue;
            }
            return HttpUtils.String2Date(s);
        }

        protected void SetSpecialHeaders(string name, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }
            this.SetInternal(name, CheckBadChars(value, true));
        }
    }
}