// Copyright (c) 2015 Yamool. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

namespace Yamool.Net.Http
{
    using System;
    using System.Text;

    internal class HttpEncoder
    {
        internal static byte[] UrlEncode(byte[] bytes, int offset, int count)
        {
            if (!ValidateUrlEncodingParameters(bytes, offset, count))
            {
                return null;
            }

            var cSpaces = 0;//the number of appeared of the space char
            var cUnsafe = 0;//the number of appeared of the un-safe char.

            // count them first 
            for (var i = 0; i < count; i++)
            {
                var ch = (char)bytes[offset + i];

                if (ch == ' ')
                    cSpaces++;
                else if (!HttpEncoderUtility.IsUrlSafeChar(ch))
                    cUnsafe++;
            }

            // nothing to expand? 
            if (cSpaces == 0 && cUnsafe == 0)
                return bytes;

            // expand not 'safe' characters into %XX, spaces to +s
            var buffer = new byte[count + cUnsafe * 2];
            var pos = 0;

            for (var i = 0; i < count; i++)
            {
                var b = bytes[offset + i];
                var ch = (char)b;

                if (HttpEncoderUtility.IsUrlSafeChar(ch))
                {
                    buffer[pos++] = b;
                }
                else if (ch == ' ')
                {
                    buffer[pos++] = (byte)'+';
                }
                else
                {
                    buffer[pos++] = (byte)'%';
                    buffer[pos++] = (byte)HttpEncoderUtility.IntToHex((b >> 4) & 0xf);//(b/16)%16
                    buffer[pos++] = (byte)HttpEncoderUtility.IntToHex(b & 0x0f);//b%16
                }
            }

            return buffer;
        }

        internal static string UrlDecode(string value, Encoding encoding)
        {
            if (value == null)
            {
                return null;
            }

            var count = value.Length;
            var enncoder = new UrlDecoder(count, encoding);
            // go through the string's chars collapsing %XX and %uXXXX and
            // appending each char as char, with exception of %XX constructs 
            // that are appended as bytes
            for (var i = 0; i < count; i++)
            {
                var ch = value[i];

                if (ch == '+')
                {
                    ch = ' ';
                }
                else if (ch == '%' && i < count - 2)
                {
                    if (value[i + 1] == 'u' && i < count - 5)
                    {
                        var h1 = HttpEncoderUtility.HexToInt(value[i + 2]);
                        var h2 = HttpEncoderUtility.HexToInt(value[i + 3]);
                        var h3 = HttpEncoderUtility.HexToInt(value[i + 4]);
                        var h4 = HttpEncoderUtility.HexToInt(value[i + 5]);

                        if (h1 >= 0 && h2 >= 0 && h3 >= 0 && h4 >= 0)
                        {   // valid 4 hex chars
                            ch = (char)((h1 << 12) | (h2 << 8) | (h3 << 4) | h4);
                            i += 5;

                            // only add as char 
                            enncoder.AddChar(ch);
                            continue;
                        }
                    }
                    else
                    {
                        var h1 = HttpEncoderUtility.HexToInt(value[i + 1]);
                        var h2 = HttpEncoderUtility.HexToInt(value[i + 2]);

                        if (h1 >= 0 && h2 >= 0)
                        {     // valid 2 hex chars 
                            byte b = (byte)((h1 << 4) | h2);
                            i += 2;

                            // don't add as char
                            enncoder.AddByte(b);
                            continue;
                        }
                    }
                }
                if ((ch & 0xFF80) == 0)//unicode
                    enncoder.AddByte((byte)ch); // 7 bit have to go as bytes because of Unicode 
                else
                    enncoder.AddChar(ch);
            }

            return enncoder.GetString();
        }

        private static bool ValidateUrlEncodingParameters(byte[] bytes, int offset, int count)
        {
            if ((bytes == null) && (count == 0))
            {
                return false;
            }
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }
            if ((offset < 0) || (offset > bytes.Length))
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            if ((count < 0) || ((offset + count) > bytes.Length))
            {
                throw new ArgumentOutOfRangeException("count");
            }
            return true;
        }

        private class UrlDecoder
        {
            private int _bufferSize;
            private byte[] _byteBuffer;
            private char[] _charBuffer;
            private Encoding _encoding;
            private int _numBytes;
            private int _numChars;

            internal UrlDecoder(int bufferSize, Encoding encoding)
            {
                _bufferSize = bufferSize;
                _encoding = encoding;
                _charBuffer = new char[bufferSize];
            }

            internal void AddByte(byte b)
            {
                if (_byteBuffer == null)
                {
                    _byteBuffer = new byte[_bufferSize];
                }
                this._byteBuffer[_numBytes++] = b;
            }

            internal void AddChar(char ch)
            {
                if (_numBytes > 0)
                {
                    this.FlushBytes();
                }
                _charBuffer[this._numChars++] = ch;
            }

            private void FlushBytes()
            {
                if (_numBytes > 0)
                {
                    _numChars += this._encoding.GetChars(_byteBuffer, 0, _numBytes, _charBuffer, _numChars);
                    _numBytes = 0;
                }
            }

            internal string GetString()
            {
                if (_numBytes > 0)
                {
                    FlushBytes();
                }
                if (_numChars > 0)
                {
                    return new string(_charBuffer, 0, _numChars);
                }
                return string.Empty;
            }
        }

        private static class HttpEncoderUtility
        {
            public static bool IsUrlSafeChar(char ch)
            {
                if ((((ch >= 'a') && (ch <= 'z')) || ((ch >= 'A') && (ch <= 'Z'))) || ((ch >= '0') && (ch <= '9')))
                {
                    return true;
                }
                switch (ch)
                {
                    case '(':
                    case ')':
                    case '*':
                    case '-':
                    case '.':
                    case '_':
                    case '!':
                        return true;
                }
                return false;
            }

            public static char IntToHex(int n)
            {
                if (n <= 9)
                {
                    //0-9
                    return (char)(n + 0x30);//0x30=48
                }
                return (char)((n - 10) + 0x61);//0x61=97
            }

            public static int HexToInt(char h)
            {
                if ((h >= '0') && (h <= '9'))
                {
                    return (h - '0');
                }
                if ((h >= 'a') && (h <= 'f'))
                {
                    return ((h - 'a') + 10);
                }
                if ((h >= 'A') && (h <= 'F'))
                {
                    return ((h - 'A') + 10);
                }
                return -1;
            }
        }
    }   
}
