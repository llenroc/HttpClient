//----------------------------------------------------------------
// Copyright (c) Yamool Inc.  All rights reserved.
//----------------------------------------------------------------

namespace Yamool.Net.Http
{
    using System;
    using System.IO; 

    internal class ContentLengthStream : ResponseStream
    {
        private long _contentLength;
        private long _consumed;
        
        public ContentLengthStream(Stream stream, long contentLength)
            : base(stream)
        {
            _contentLength = contentLength;
        }               

        private bool TransferCompleted
        {
            get
            {
                if (_contentLength > 0)
                {
                    return (int)(_contentLength - _consumed) <= 0;
                }
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return !this.TransferCompleted;
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _consumed += count - offset;
            _stream.Write(buffer, offset, count - offset);
        }        
    }
}
