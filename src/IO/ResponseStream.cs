//----------------------------------------------------------------
// Copyright (c) Yamool Inc.  All rights reserved.
//----------------------------------------------------------------

namespace Yamool.Net.Http
{
    using System;
    using System.IO;

    internal abstract class ResponseStream : Stream
    {
        protected Stream _stream;

        public ResponseStream(Stream stream)
        {
            _stream = stream;
        }

        public override bool CanRead
        {
            get {
                return _stream.CanRead;
            }           
        }

        public override bool CanSeek
        {
            get {
                return _stream.CanSeek;
            }
        }

        public override bool CanWrite
        {
            get {
                return _stream.CanWrite;
            }
        }

        public override void Flush()
        {           
        }

        public override long Length
        {
            get {
                return _stream.Length;
            }
        }

        public override long Position
        {
            get
            {
                return _stream.Position;
            }
            set
            {
                _stream.Position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _stream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _stream.SetLength(value);
        }               

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_stream != null)
                {
                    _stream.Close();
                }
            }
        }
    }
}
