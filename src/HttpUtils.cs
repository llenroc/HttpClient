//----------------------------------------------------------------
// Copyright (c) Yamool Inc.  All rights reserved.
//----------------------------------------------------------------

namespace Yamool.Net.Http
{
    using System;
    using System.Collections.Specialized;    
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Net;

    /// <summary>
    /// The HttpUtils class provides a set of utility methods for HTTP request/response processing.
    /// </summary>
    public static class HttpUtils
    {
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
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }
            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }
            if ((query.Length > 0) && (query[0] == '?'))
            {
                query = query.Substring(1);
            }
            return new HttpValueCollection(query, false, true, encoding);
        }

        internal static string Date2String(DateTime D)
        {
            DateTimeFormatInfo provider = new DateTimeFormatInfo();
            return D.ToUniversalTime().ToString("R", provider);
        }

        /// <summary>
        /// HTML-encodes a string and returns the encoded string.
        /// </summary>
        /// <param name="value">the html string to encode</param>
        /// <returns>he encoded text.</returns>
        public static string HtmlEncode(string value)
        {
            //if (string.IsNullOrEmpty(value))
            //    return value;
            //var output = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
            //WebUtility.HtmlEncode(value, output);
            //return output.ToString();
            return WebUtility.HtmlEncode(value);
        }

        /// <summary>
        /// Decodes an HTML-encoded string and returns the decoded string.
        /// </summary>
        /// <param name="value">the html string to decode.</param>
        /// <returns>he decoded text.</returns>
        public static string HtmlDecode(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;
            //check a Character Entities
            var index = value.IndexOf('&');
            if (index < 0)
            {
                return value;
            }
            //check a again 
            if (value.IndexOf(';', index) <= 0)
            {
                return value;
            }
            var output = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
            WebUtility.HtmlDecode(value, output);
            return output.ToString();
        }

        /// <summary>
        /// Encodes a URL string.
        /// </summary>
        /// <param name="str">The text to encode.</param>
        /// <returns>An encoded string.</returns>
        public static string UrlEncode(string str)
        {
            if (str == null)
            {
                return null;
            }
            return UrlEncode(str, Encoding.UTF8);
        }

        /// <summary>
        /// Encodes a URL string using the specified encoding object.
        /// </summary>
        /// <param name="str">The text to encode.</param>
        /// <param name="e">The Encoding object that specifies the encoding scheme.</param>
        /// <returns>An encoded string.</returns>
        public static string UrlEncode(string str, Encoding e)
        {
            if (str == null)
            {
                return null;
            }
            return Encoding.ASCII.GetString(UrlEncodeToBytes(str, e));
        }

        /// <summary>
        /// Converts a string into a URL-encoded array of bytes using the specified encoding object.
        /// </summary>
        /// <param name="str">The string to encode</param>
        /// <param name="e">The Encoding that specifies the encoding scheme.</param>
        /// <returns>An encoded array of bytes.</returns>
        public static byte[] UrlEncodeToBytes(string str, Encoding e)
        {
            if (str == null)
            {
                return null;
            }
            byte[] bytes = e.GetBytes(str);
            return HttpEncoder.UrlEncode(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Converts a string that has been encoded for transmission in a URL into a decoded string.
        /// </summary>
        /// <param name="str">The string to decode.</param>
        /// <returns>A decoded string.</returns>
        public static string UrlDecode(string str)
        {
            if (str == null)
            {
                return null;
            }
            return UrlDecode(str, Encoding.UTF8);
        }

        /// <summary>
        /// Converts a URL-encoded string into a decoded string, using the specified encoding object.
        /// </summary>
        /// <param name="str">The string to decode.</param>
        /// <param name="e">The Encoding that specifies the decoding scheme.</param>
        /// <returns>A decoded string.</returns>
        public static string UrlDecode(string str, Encoding e)
        {
            return HttpEncoder.UrlDecode(str, e);
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
        /// unescape a specified string.
        /// </summary>
        /// <param name="input">the input string to unescape.</param>
        /// <returns>a new unescape string.</returns>
        public static string Unescape(string input)
        {
            if (input == null)
                return null;
            string result = null;
            using (var reader = new StringReader(input))
            {
                result = StringEscapeUtils.Unescape(reader, input.Length);
                reader.Close();
            }
            return result;
        }

        /// <summary>
        /// escape a specified string.
        /// </summary>
        /// <param name="input">the input string to escape</param>
        /// <returns>a new escape string.</returns>
        public static string Escape(string input)
        {
            if (input == null)
                return null;
            string result = null;
            using (var reader = new StringReader(input))
            {
                result = StringEscapeUtils.Escape(reader);
                reader.Close();
            }
            return result;
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

        /// <summary>
        /// Convert a relative path to the absolute url.
        /// </summary>
        /// <param name="url">the original request url.</param>
        /// <param name="relativeUrl">the relative url.</param>
        /// <returns></returns>
        internal static string ToAbsoluteUrl(Uri url, string relativeUrl)
        {
            //is a relative path    
            if (relativeUrl[0] == '/')
            {
                return url.GetComponents(UriComponents.Scheme | UriComponents.Host, UriFormat.UriEscaped) + relativeUrl;
            }
            else
            {
                var index = url.LocalPath.IndexOf(".");
                var requestUrl = url.GetComponents(UriComponents.Scheme | UriComponents.Host | UriComponents.Path, UriFormat.UriEscaped);
                //if the reques url with file name.
                if (index > 0)
                {
                    requestUrl = requestUrl.Substring(0, requestUrl.LastIndexOf("/"));
                }
                return requestUrl + "/" + relativeUrl;
            }
        }
    }
}
