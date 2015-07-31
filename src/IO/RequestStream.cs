//----------------------------------------------------------------
// Copyright (c) Yamool Inc.  All rights reserved.
//----------------------------------------------------------------

namespace Yamool.Net.Http
{
    using System;
    using System.IO;

    internal class RequestStream : Stream
    {
        private bool _isOpen;
        private MemoryStream _bufferData;
        private HttpRequest _request;

        private RequestStream() { }

        internal RequestStream(HttpRequest request)
        {
            _request = request;         
            _isOpen = true;
            _bufferData = new MemoryStream();
        }

        public override bool CanRead
        {
            get
            {
                return false;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return _isOpen;
            }
        }

        public override long Length
        {
            get
            {
                return _bufferData.Length;
            }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException("This stream does not support Position operations.");
            }
            set
            {
                throw new NotSupportedException("This stream does not support Position operations.");
            }
        }

        public override void Flush()
        {           
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("This stream does not support read operation.");
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("This stream does not support seek operation.");
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("This stream does not support setLength operation.");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {           
            _bufferData.Write(buffer, offset, count);
        }

        private void CloseInternal()
        {           
            _bufferData.Position = 0;
            _request.SetSubmitRequestStream(_bufferData.ToArray());
            _bufferData.Close();            
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && _isOpen)
                {
                    this.CloseInternal();
                    _isOpen = false;
                    _bufferData = null;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        internal bool Closed
        {
            get
            {
                return !_isOpen;
            }
        }
    }
}
