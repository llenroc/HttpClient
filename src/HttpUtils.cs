// Copyright (c) 2015 Yamool. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

namespace Yamool.Net.Http
{
    using System;
    using System.Collections.Specialized;    
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Net;

    /// <summary>
    /// The HttpUtils class provides a set of utility methods for HTTP request/response processing.
    /// </summary>
    public static class HttpUtils
    {
        private class HttpQSCollection : NameValueCollection
        {
            public override string ToString()
            {
                if (this.Count == 0)
                {
                    return string.Empty;
                }
                return string.Join("&", this.AllKeys.Select(k => k + "=" + UrlEncode(this[k])));
            }
        }

        /// <summary>
        /// Parses a query string into a <see cref="NameValueCollection"/> using UTF8 encoding.
        /// </summary>
        /// <param name="query">The query string to parse.</param>
        /// <returns>A <see cref="NameValueCollection"/> of query parameters and values.</returns>
        public static NameValueCollection ParseQueryString(string query)
        {
            return ParseQueryString(query, Encoding.UTF8);
        }

        /// <summary>
        /// Parses a query string into a <see cref="NameValueCollection"/> using specify encoding.
        /// </summary>
        /// <param name="query">The query string to parse.</param>
        /// <param name="encoding">The encoding to encoding.</param>
        /// <returns></returns>
        public static NameValueCollection ParseQueryString(string query, Encoding encoding)
        {
            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }
            if (string.IsNullOrEmpty(query) || (query.Length == 1 && query[0] == '?'))
            {
                return new HttpQSCollection();
            }
            var result = new HttpQSCollection();
            ParseQueryString(query, encoding, result);
            return result;
        }

        internal static void ParseQueryString(string query, Encoding encoding, NameValueCollection result)
        {
            var decoded = HtmlDecode(query);
            var decodedLength = decoded.Length;

            var namePos = 0;
            var first = true;
            while (namePos <= decodedLength)
            {
                var valuePos = -1;
                var valueEnd = -1;
                for (var q = namePos; q < decodedLength; q++)
                {
                    if (valuePos == -1 && decoded[q] == '=')
                    {
                        valuePos = q + 1;
                    }
                    else if (decoded[q] == '&')
                    {
                        valueEnd = q;
                        break;
                    }
                }

                if (first)
                {
                    first = false;
                    if (decoded[namePos] == '?')
                    {
                        namePos++;
                    }
                }

                string name, value;
                if (valuePos == -1)
                {
                    name = null;
                    valuePos = namePos;
                }
                else
                {
                    name = UrlDecode(decoded.Substring(namePos, valuePos - namePos - 1), encoding);
                }
                if (valueEnd < 0)
                {
                    namePos = -1;
                    valueEnd = decoded.Length;
                }
                else
                {
                    namePos = valueEnd + 1;
                }
                value = UrlDecode(decoded.Substring(valuePos, valueEnd - valuePos), encoding);

                result.Add(name, value);
                if (namePos == -1)
                    break;
            }
        }

        /// <summary>
        /// HTML-encodes a string and returns the encoded string.
        /// </summary>
        /// <param name="value">the html string to encode</param>
        /// <returns>he encoded text.</returns>
        public static string HtmlEncode(string value)
        {
            return WebUtility.HtmlEncode(value);
        }

        /// <summary>
        /// Decodes an HTML-encoded string and returns the decoded string.
        /// </summary>
        /// <param name="value">the html string to decode.</param>
        /// <returns>he decoded text.</returns>
        public static string HtmlDecode(string value)
        {
           return WebUtility.HtmlDecode(value);
        }

        /// <summary>
        /// Encodes a URL string.
        /// </summary>
        /// <param name="value">The text to encode.</param>
        /// <returns>An encoded string.</returns>
        public static string UrlEncode(string value)
        {
            return UrlEncode(value, Encoding.UTF8);
        }

        /// <summary>
        /// Encodes a URL string using the specified encoding object.
        /// </summary>
        /// <param name="value">The text to encode.</param>
        /// <param name="encoding">The Encoding object that specifies the encoding scheme.</param>
        /// <returns>An encoded string.</returns>
        public static string UrlEncode(string value, Encoding encoding)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            var bytes = encoding.GetBytes(value);
            return Encoding.ASCII.GetString(HttpEncoder.UrlEncode(bytes, 0, bytes.Length));
        }


        /// <summary>
        /// Converts a string that has been encoded for transmission in a URL into a decoded string.
        /// </summary>
        /// <param name="value">The string to decode.</param>
        /// <returns>A decoded string.</returns>
        public static string UrlDecode(string value)
        {           
            return UrlDecode(value, Encoding.UTF8);
        }

        /// <summary>
        /// Converts a URL-encoded string into a decoded string, using the specified encoding object.
        /// </summary>
        /// <param name="str">The string to decode.</param>
        /// <param name="encoding">The Encoding that specifies the decoding scheme.</param>
        /// <returns>A decoded string.</returns>
        public static string UrlDecode(string value, Encoding encoding)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            return HttpEncoder.UrlDecode(value, encoding);
        }

        public static string EncodingWebFormData(NameValueCollection formData)
        {
            if (formData == null)
                throw new ArgumentNullException("formData");
            if (formData.Count == 0)
                return string.Empty;
            var sb = new StringBuilder();
            foreach (string key in formData)
            {
                sb.Append(UrlEncode(key));
                sb.Append("=");
                sb.Append(UrlEncode(formData[key]));
            }
            return sb.ToString();
        }

        /// <summary>
        /// escape a specified string.
        /// </summary>
        /// <param name="stringToEscape">the input string to escape</param>
        /// <returns>a new escape string.</returns>
        public static string EscapeDataString(string stringToEscape)
        {
            if (string.IsNullOrEmpty(stringToEscape))
            {
                return stringToEscape;
            }
            using (var reader = new StringReader(stringToEscape))
            {
                return StringEscapeUtils.Escape(reader);
            }
        }

        /// <summary>
        /// unescape a specified string.
        /// </summary>
        /// <param name="stringToUnescape">the input string to unescape.</param>
        /// <returns>a new unescape string.</returns>
        public static string UnescapeDataString(string stringToUnescape)
        {
            if (string.IsNullOrEmpty(stringToUnescape))
            {
                return stringToUnescape;
            }
            using (var reader = new StringReader(stringToUnescape))
            {
                return StringEscapeUtils.Unescape(reader, stringToUnescape.Length);
            }
        }

        internal static string Date2String(DateTime D)
        {
            DateTimeFormatInfo provider = new DateTimeFormatInfo();
            return D.ToUniversalTime().ToString("R", provider);
        }

        internal static DateTime String2Date(string S)
        {
            DateTime time;
            if (!DateTime.TryParse(S, new DateTimeFormatInfo(), DateTimeStyles.AssumeUniversal, out time))
            {
                throw new ProtocolViolationException("Parser Datetime error.");
            }
            return time;
        }        

        internal static bool IsHttpUri(Uri uri)
        {
            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
        }
    }
}
