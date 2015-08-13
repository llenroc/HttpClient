// Copyright (c) 2015 Yamool. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

namespace Yamool.Net.Http
{
    using System;
    using System.IO;

    internal enum DataParseStatus
    {
        NeedMoreData = 0,   // need more data
        ContinueParsing,    // continue parsing
        Done,               // done
        Invalid,            // bad data format
        DataTooBig,         // data exceeds the allowed size
    }

    internal struct StatusLineValues
    {
        public int MajorVersion;
        public int MinorVersion;
        public int StatusCode;
        public string StatusDescription;
        public long ContentLength;
    }

    internal class CoreResponseData
    {

        public CoreResponseData(StatusLineValues values, HttpResponseHeaders headers,Stream connectStream)
        {
            this.StatusCode = (HttpStatusCode)values.StatusCode;
            this.StatusDescription = values.StatusDescription;
            this.ContentLength = values.ContentLength;
            if (values.MajorVersion == 1 && values.MinorVersion == 1)
            {
                this.HttpVersion = HttpVersion.HTTP11;
            }
            else if (values.MajorVersion == 2 && values.MinorVersion == 0)
            {
                this.HttpVersion = HttpVersion.HTTP20;
            }
            this.ResponseHeaders = headers;
            this.ConnectStream = connectStream;
        }

        public HttpStatusCode StatusCode
        {
            get;
            private set;
        }

        public string StatusDescription
        {
            get;
            private set;
        }

        public HttpResponseHeaders ResponseHeaders
        {
            get;
            private set;
        }

        public Stream ConnectStream
        {
            get;
            private set;
        }

        public HttpVersion HttpVersion
        {
            get;
            private set;
        }

        public long ContentLength
        {
            get;
            private set;
        }
    }
}
