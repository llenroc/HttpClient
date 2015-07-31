//----------------------------------------------------------------
// Copyright (c) Yamool Inc.  All rights reserved.
//----------------------------------------------------------------

namespace Yamool.Net.Http
{
    using System;
    using System.Net;

    /// <summary>
    /// Represents a channel event arguments.
    /// </summary>
    internal class ChannelEventArgs : EventArgs
    {       
        public HttpRequest Request
        {
            get;
            set;
        }

        public int BytesTransferred
        {
            get;
            set;
        }

        public byte[] Buffer
        {
            get;
            set;
        }

        public ChannelOperation LastOperation
        {
            get;
            set;
        }

        public Exception LastException
        {
            get;
            set;
        }
    }
}
