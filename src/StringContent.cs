// Copyright (c) 2015 Yamool. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

namespace Yamool.Net.Http
{
    using System;
    using System.Text;
    using Yamool.Net.Http.Headers;

    public class StringContent : ByteArrayContent
    {
        public StringContent(string content, Encoding encoding, string mediaType)
            : base(GetContentByteArray(content, encoding))
        {
            var mediaTypeHeaderValue = new MediaTypeHeaderValue((mediaType == null) ? "text/plain" : mediaType);
            mediaTypeHeaderValue.CharSet = encoding.WebName;
            this.Headers.ContentType = mediaTypeHeaderValue;
        }

        private static byte[] GetContentByteArray(string content, Encoding encoding)
        {
            if (content == null)
            {
                throw new ArgumentNullException("content");
            }
            if (encoding == null)
            {
                encoding = HttpContent.DefaultStringEncoding;
            }
            return encoding.GetBytes(content);
        }
    }
}
