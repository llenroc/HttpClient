//----------------------------------------------------------------
// Copyright (c) Yamool Inc.  All rights reserved.
//----------------------------------------------------------------

namespace Yamool.Net.Http
{
    using System;
    using System.IO;

    internal class ResponseLeaveStream : Stream
    {
        private HttpResponse _response;
        private Stream _respStream;
        private bool _isOpen;

        public ResponseLeaveStream(HttpResponse response, Stream respStream)
        {
            _response = response;
            _respStream = respStream;
            _isOpen = true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_isOpen)
                {                    
                    _respStream.Close();
                    _isOpen = false;
                }
            }
        }

        public override bool CanRead
        {
            get
            {
                return _isOpen;
            }
        }

        public override bool CanSeek
        {
            get {
                return false;
            }
        }

        public override bool CanWrite
        {
            get {
                return false;
            }
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Length
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override long Position
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _respStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}
