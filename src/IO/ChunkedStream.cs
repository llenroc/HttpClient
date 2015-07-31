//----------------------------------------------------------------
// Copyright (c) Yamool Inc.  All rights reserved.
//----------------------------------------------------------------

namespace Yamool.Net.Http
{
    using System;
    using System.IO;
    using System.Globalization;
    using System.Text;

    class ChunkedStream : ResponseStream
    {
        /// <summary>
        /// MUST be read specified count from buffer before to process.
        /// </summary>
        private int _beReadCount;
        /// <summary>
        /// The stage bytes and MUST be write to buffer before to process.
        /// </summary>
        private byte[] _beWriteBytes;

        private bool _transferCompleted;

        public ChunkedStream(Stream stream)
            : base(stream)
        {
            _beReadCount = 0;
        }      
      
        public override void Write(byte[] buffer, int offset, int count)
        {           
            //MUST be write to buffer before to process.
            if (_beWriteBytes != null)
            {
                count = count - offset;
                var newBuffer = new byte[count + _beWriteBytes.Length];
                Buffer.BlockCopy(_beWriteBytes, 0, newBuffer, 0, _beWriteBytes.Length);
                Buffer.BlockCopy(buffer, offset, newBuffer, _beWriteBytes.Length, count);
                buffer = newBuffer;
                count = newBuffer.Length;              
                offset = 0;
                _beWriteBytes = null;
            }
            //MUST be read specified count from buffer before to process.
            if (_beReadCount > 0)
            {
                var readCount = Math.Min(_beReadCount, count);
                _stream.Write(buffer, offset, readCount);
                _beReadCount -= readCount;
                offset += readCount;
            }
            while (offset < count)
            {
                var chunkSize = this.ReadChunkSize(buffer, count, ref offset);
                if (chunkSize == 0)
                {
                    _transferCompleted = true;
                    return;
                }
                else if (chunkSize == -1)
                {
                    break;
                }
                var length = Math.Min(chunkSize, count - offset);
                _stream.Write(buffer, offset, length);
                if (chunkSize > length)
                {
                    _beReadCount = chunkSize - length;
                    break;
                }
                offset += length;
            }
        }        

        public override bool CanWrite
        {
            get
            {
                return !_transferCompleted;
            }
        }

        private int ReadChunkSize(byte[] buffer, int count, ref int offset)
        {
            //2013-05-28
            if (offset >= count)
                return -1;
            bool sawCR = false;
            int startIndex = offset;
            while (offset < count)
            {
                if (offset - startIndex >= 30)
                {
                    throw new HttpException(HttpExceptionStatus.ProtocolError, "chunk size too long.");
                }
                int code = buffer[offset++];
                //\r\n chunk-size \r\n
                if ((offset - startIndex > 1) && code == 13 && (offset + 1 < count && buffer[offset++] == 10))
                {
                    sawCR = true;
                    break;
                }
            }
            if (sawCR)
            {
                return int.Parse(Encoding.ASCII.GetString(buffer, startIndex, offset - startIndex - 2), NumberStyles.HexNumber);
            }
            else if (offset == count)
            {
                if (buffer[startIndex] == 13 && startIndex + 1 < count && buffer[startIndex + 1] == 10)
                {
                    startIndex += 2;
                }
                var length = count - startIndex;
                if (length > 0)
                {
                    _beWriteBytes = new byte[length];
                    Buffer.BlockCopy(buffer, startIndex, _beWriteBytes, 0, _beWriteBytes.Length);
                }
            }
            return -1;
        }       

    }
}
