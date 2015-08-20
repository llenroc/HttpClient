// Copyright (c) 2015 Yamool. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

namespace Yamool.Net.Http
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    //https://en.wikipedia.org/wiki/Chunked_transfer_encoding

    internal class ChunkParser
    {
        private enum ReadState
        {
            ChunkLength,
            Extension,
            Payload,
            PayloadEnd,
            Trailer,
            Done,
            Error
        }
        private const int chunkLengthBuffer = 12;
        private const int noChunkLength = -1;
        private static bool[] tokenChars;

        private ReadState _readState;
        private Connection _connection;
        private ArraySegment<byte> _buffer;
        private int _bufferCurrentPos;
        private int _bufferFillLength;
        private int _currentChunkLength;
        private int _currentChunkBytesRead;

        static ChunkParser()
        {
            tokenChars = new bool[128];
            for (int i = 33; i < 127; i++)
            {
                tokenChars[i] = true;
            }
            tokenChars[40] = false;
            tokenChars[41] = false;
            tokenChars[60] = false;
            tokenChars[62] = false;
            tokenChars[64] = false;
            tokenChars[44] = false;
            tokenChars[59] = false;
            tokenChars[58] = false;
            tokenChars[92] = false;
            tokenChars[34] = false;
            tokenChars[47] = false;
            tokenChars[91] = false;
            tokenChars[93] = false;
            tokenChars[63] = false;
            tokenChars[61] = false;
            tokenChars[123] = false;
            tokenChars[125] = false;
        }

        public ChunkParser(Connection connection,ArraySegment<byte> initialBuffer, int initialBufferOffset, int initialBufferCount)
        {
            _connection = connection;
            _buffer = initialBuffer;
            _bufferCurrentPos = initialBufferOffset;
            _bufferFillLength = initialBufferOffset + initialBufferCount;
            _readState = ReadState.ChunkLength;
            _currentChunkLength = -1;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            return this.ReadAsync(buffer, offset, count).GetAwaiter().GetResult();
        }

        public async Task<int> ReadAsync(byte[] buffer, int offset, int count)
        {
            var bytesToRead = 0;
            while (_readState < ReadState.Done)
            {
                var result = DataParseStatus.DataTooBig;
                switch (_readState)
                {
                    case ReadState.ChunkLength:
                        {
                            result = this.ParseChunkLength();
                            break;
                        }
                    case ReadState.Extension:
                        {
                            result = this.ParseExtension();
                            break;
                        }
                    case ReadState.Payload:
                        {
                            result = this.HandlePayload(buffer, offset, count, ref bytesToRead);
                            break;
                        }
                    case ReadState.PayloadEnd:
                        {
                            result = this.ParsePayloadEnd();
                            break;
                        }
                    case ReadState.Trailer:
                        {
                            result = this.ParseTrailer();
                            break;
                        }
                }
                switch (result)
                {
                    case DataParseStatus.ContinueParsing:
                        {
                            // Continue with next loop iteration. Parsing was successful and we'll process the next state.
                            break;
                        }
                    case DataParseStatus.Done:
                        {
                            // Parsing was successful and we should return. We either have a result or we have a pending
                            // operation and will continue once the operation completes.
                            goto quit;
                        }
                    case DataParseStatus.Invalid:
                    case DataParseStatus.DataTooBig:
                        {
                            throw new IOException("net_io_readfailure,net_io_connectionclosed");
                        }
                    case DataParseStatus.NeedMoreData:
                        {
                            if (!await TryGetMoreDataAsync())
                            {
                                // Read operation didn't complete synchronously. Just return. The read completion
                                // callback will continue.
                                goto quit;
                            }
                            break;
                        }
                    default:
                        {
                            throw new InternalException();
                        }
                }
            }
        quit:
            return bytesToRead;
        }

        private DataParseStatus ParseChunkLength()
        {
            var chunkLength = noChunkLength;
            for (var i = _bufferCurrentPos; i < _bufferFillLength; i++)
            {
                var c = _buffer.Get(i);
                if (((c < '0') || (c > '9')) && ((c < 'A') || (c > 'F')) && ((c < 'a') || (c > 'f')))
                {
                    // Not a hex number. Check if we had at least one hex digit. If not, then this is an invalid chunk.
                    if (chunkLength == noChunkLength)
                    {
                        return DataParseStatus.Invalid;
                    }
                    // Point to the first character after the chunk length that is not part of the length value.
                    _bufferCurrentPos = i;
                    _currentChunkLength = chunkLength;

                    _readState = ReadState.Extension;
                    return DataParseStatus.ContinueParsing;
                }
                var currentDigit = (byte)((c < (byte)'A') ? (c - (byte)'0') :
                  10 + ((c < (byte)'a') ? (c - (byte)'A') : (c - (byte)'a')));

                if (chunkLength == noChunkLength)
                {
                    chunkLength = currentDigit;
                }
                else
                {
                    if (chunkLength >= 0x8000000)
                    {
                        // Shifting the value by an order of magnitude (hex) would result in a value > Int32.MaxValue.
                        // Currently only chunks up to 2GB are supported. The provided chunk length is too large.
                        return DataParseStatus.Invalid;
                    }

                    // Multiply current chunk length by 16 and add the current digit.
                    chunkLength = (chunkLength << 4) + currentDigit;
                }
            }
            // The current buffer didn't include the whole chunk length information followed by a non-hex digit char.
            return DataParseStatus.NeedMoreData;
        }

        private DataParseStatus ParseExtension()
        {
            //https://java.net/jira/browse/GRIZZLY-1684
            var currentPos = _bufferCurrentPos;
            // After the chunk length we can only have <space> or <tab> chars. A LWS with CRLF would be ambiguous since
            // CRLF also delimits the chunk length from chunk data.
            var result = this.ParseWhitespaces(ref currentPos);
            if (result != DataParseStatus.ContinueParsing)
            {
                return result;
            }
            result = this.ParseExtensionNameValuePairs(ref currentPos);
            if (result != DataParseStatus.ContinueParsing)
            {
                return result;
            }
            result = this.ParseCRLF(ref currentPos);
            if (result != DataParseStatus.ContinueParsing)
            {
                return result;
            }
            _bufferCurrentPos = currentPos;
            if (_currentChunkLength == 0)
            {
                // zero-chunk read. We're done with the response. Consume trailer and complete.
                _readState = ReadState.Trailer;
            }
            else
            {
                _readState = ReadState.Payload;
            }
            return DataParseStatus.ContinueParsing;
        }

        private DataParseStatus ParsePayloadEnd()
        {
            var crlfResult = this.ParseCRLF(ref _bufferCurrentPos);

            if (crlfResult != DataParseStatus.ContinueParsing)
            {
                return crlfResult;
            }
            _currentChunkLength = noChunkLength;
            _currentChunkBytesRead = 0;
            _readState = ReadState.ChunkLength;
            return DataParseStatus.ContinueParsing;
        }

        private DataParseStatus ParseTrailer()
        {
            if (this.ParseWhitespaces(ref _bufferCurrentPos) == DataParseStatus.NeedMoreData)
            {
                return DataParseStatus.NeedMoreData;
            }
            var currentPos = _bufferCurrentPos;

            return DataParseStatus.Done;
        }

        private DataParseStatus HandlePayload(byte[] buffer, int offset, int count, ref int bytesReadCount)
        {
            // Try to fill the user buffer with data from the internal buffer first.
            if (_bufferCurrentPos < _bufferFillLength)
            {
                // We have chunk body data in our internal buffer. Copy it to the user buffer.
                bytesReadCount = Math.Min(Math.Min(count, _bufferFillLength - _bufferCurrentPos), _currentChunkLength - _currentChunkBytesRead);
                Buffer.BlockCopy(_buffer.Array, _bufferCurrentPos, buffer, offset, bytesReadCount);
                _bufferCurrentPos += bytesReadCount;
                _currentChunkBytesRead += bytesReadCount;
                if (_currentChunkBytesRead == _currentChunkLength || bytesReadCount == count)
                {
                    //We read the whole chunk or filled the user buffer entirely. 
                    if (_currentChunkBytesRead == _currentChunkLength)
                    {
                        _readState = ReadState.PayloadEnd;
                    }
                }
                return DataParseStatus.Done;
            }
            if (_bufferCurrentPos == _bufferFillLength)
            {
                return DataParseStatus.NeedMoreData;
            }
            return DataParseStatus.Done;
        }

        private DataParseStatus ParseWhitespaces(ref int pos)
        {
            var currentPos = pos;
            while (currentPos < _bufferFillLength)
            {
                var c = _buffer.Get(currentPos);
                if (!IsWhiteSpace(c))
                {
                    // Point to the first character that is not a SP (space) or HT (horizontal tab).
                    pos = currentPos;
                    return DataParseStatus.ContinueParsing;
                }
                currentPos++;
            }
            // We only had whitespaces until the end of the buffer. Request more data to continue.
            return DataParseStatus.NeedMoreData;
        }

        private DataParseStatus ParseExtensionNameValuePairs(ref int pos)
        {
            // chunk-extension= *( ";" chunk-ext-name [ "=" chunk-ext-val ] )
            // chunk-ext-name = token
            // chunk-ext-val  = token | quoted-string
            DataParseStatus result;
            var currentPos = pos;
            while (_buffer.Get(currentPos) == ';')
            {
                currentPos++;

                result = this.ParseWhitespaces(ref currentPos);
                if (result != DataParseStatus.ContinueParsing)
                {
                    return result;
                }
                result = this.ParseToken(ref currentPos);
                if (result != DataParseStatus.ContinueParsing)
                {
                    return result;
                }
                result = this.ParseWhitespaces(ref currentPos);
                if (result != DataParseStatus.ContinueParsing)
                {
                    return result;
                }
                if (_buffer.Get(currentPos) == '=')
                {
                    currentPos++;
                    result = this.ParseWhitespaces(ref currentPos);
                    if (result != DataParseStatus.ContinueParsing)
                    {
                        return result;
                    }
                    result = this.ParseToken(ref currentPos);
                    if (result == DataParseStatus.Invalid)
                    {
                        result = this.ParseQuotedString(ref currentPos);
                    }

                    if (result != DataParseStatus.ContinueParsing)
                    {
                        return result;
                    }
                    result = this.ParseWhitespaces(ref currentPos);
                    if (result != DataParseStatus.ContinueParsing)
                    {
                        return result;
                    }
                }
            }
            pos = currentPos;

            return DataParseStatus.ContinueParsing;
        }

        private DataParseStatus ParseCRLF(ref int pos)
        {
            const int crlfLength = 2;

            if (pos + crlfLength > _bufferFillLength)
            {
                return DataParseStatus.NeedMoreData;
            }

            if ((_buffer.Get(pos) != '\r') || (_buffer.Get(pos + 1) != '\n'))
            {
                return DataParseStatus.Invalid;
            }

            pos += crlfLength;
            return DataParseStatus.ContinueParsing;
        }

        private DataParseStatus ParseToken(ref int pos)
        {
            for (var currentPos = pos; currentPos < _bufferFillLength; currentPos++)
            {
                if (!IsTokenChar(_buffer.Get(currentPos)))
                {
                    // If we found at least one token character, we have a token. If not, indicate failure since
                    // we were supposed to parse a token but we didn't find one.
                    if (currentPos > pos)
                    {
                        pos = currentPos;
                        return DataParseStatus.ContinueParsing;
                    }
                    else
                    {
                        return DataParseStatus.Invalid;
                    }
                }
            }
            return DataParseStatus.NeedMoreData;
        }

        private DataParseStatus ParseQuotedString(ref int pos)
        {
            if (pos == _bufferFillLength)
            {
                return DataParseStatus.NeedMoreData;
            }
            if (_buffer.Get(pos) != '"')
            {
                return DataParseStatus.Invalid;
            }
            var currentPos = pos + 1;
            while (currentPos < _bufferFillLength)
            {
                if ((_buffer.Get(currentPos) == '"'))
                {
                    pos = currentPos + 1; // return index pointing to char after closing quote char.
                    return DataParseStatus.ContinueParsing;
                }
                // Note that for extensions we can't support backslash before the terminating quote: E.g. if we see
                // \"\r\n we don't know if we have an escaped " followed by a LWS or if we're at the end of the quoted
                // string followed by the extension-terminating CRLF. I.e. as soon as we see \" we interpret it as
                // quoted pair.
                if (_buffer.Get(currentPos) == '\\')
                {
                    // We have a quoted pair. Make sure we have at least one more char in the buffer.
                    currentPos++;
                    if (currentPos == _bufferFillLength)
                    {
                        return DataParseStatus.NeedMoreData;
                    }

                    // Only 0-127 values are allowed in a quoted pair. If the char after \ is > 127 then \ is not part
                    // of a quoted pair but a regular char in the quoted string.
                    if (_buffer.Get(currentPos) <= 0x7F)
                    {
                        currentPos++; // skip quoted pair
                        continue;
                    }
                }

                currentPos++;
            }
            return DataParseStatus.NeedMoreData;
        }

        private async Task<bool> TryGetMoreDataAsync()
        {
            //var unreadBytesSize = _bufferFillLength - _bufferCurrentPos;
            //if (unreadBytesSize >= BufferPool.DefaultBufferLength)
            //{
            //    throw new IOException("unread bytes size exceeded the buffer length.");
            //}
            //if (unreadBytesSize > 0)
            //{
            //    _connection.MoveBufferBytesToHead(_bufferCurrentPos, unreadBytesSize);
            //    _connection.SetBuffer(unreadBytesSize, _bufferFillLength - unreadBytesSize);
            //}
            //else
            //{
            //    _connection.ResetBuffer();
            //}
            //await _connection.ReadAsync();
            // _buffer = _connection.TransferredBytes;
            // _bufferFillLength = _connection.TransferredCount;
            //_bufferCurrentPos = 0;
            return true;
        }

        private static bool IsWhiteSpace(byte c)
        {
            return c == 32 || c == 9;
        }

        private static bool IsTokenChar(byte character)
        {
            // Must be between 'space' (32) and 'DEL' (127)
            if (character > 127)
            {
                return false;
            }

            return tokenChars[character];
        }   
    }
}
