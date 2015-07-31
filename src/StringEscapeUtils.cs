//----------------------------------------------------------------
// Copyright (c) Yamool Inc.  All rights reserved.
//----------------------------------------------------------------

namespace Yamool.Net.Http
{
     using System;
    using System.Collections.Generic;
    using System.Text;
    using System.IO;

    /// <summary>
    /// Escapes and unescapes string.
    /// </summary>
    internal static class StringEscapeUtils
    {
        #region sequences
        private static Dictionary<char, char> _unescapeSequences = new Dictionary<char, char>()
        {
            {'\'','\''},
            {'"','"'},
            {'/','/'},
            {'a', '\a'},
            {'b', '\b'},
            {'f', '\f'},
            {'n','\n'},
            {'r','\r'},          
            {'t','\t'},           
            {'v','\v'},
            {'0','\0'}
        };

        private static Dictionary<char, string> _escapeSequences = new Dictionary<char, string>()
        {
            {'"',@"\"""},
            //{'/',@"\/"},
            {'\'',@"\'"},
            {'\a',@"\a"},
            {'\b',@"\b"},
            {'\f',@"\f"},
            {'\n',@"\n"},
            {'\r',@"\r"},
            {'\t',@"\t"},
            {'\v',@"\v"},
            {'\0',@"\0"},
        };
        #endregion

        /// <summary>
        /// Escapes the characters in a String
        /// </summary>
        /// <param name="reader">the input string reader to escaping.</param>
        /// <returns>a string that has been escaped.</returns>
        public static string Escape(TextReader reader)
        {
            var sb = new StringBuilder();
            var code = -1;
            while ((code = reader.Read()) >= 0)
            {
                var ch = (char)code;
                if (ch == '<')
                {
                    sb.Append(ch);
                    if (reader.Peek() != -1 && ((char)reader.Peek()) == '/')
                    {
                        sb.Append(@"\/");
                        reader.Read();
                    }
                }
                else
                {
                    string escapeChar = null;
                    if (_escapeSequences.TryGetValue(ch, out escapeChar))
                    {
                        sb.Append(escapeChar);
                    }
                    else
                    {
                        sb.Append(ch);
                    }
                }                            
            }
            return sb.ToString();
        }

        /// <summary>
        ///  Unescapes a string containing entity escapes to a string containing the actual Unicode characters corresponding to the escapes.
        /// </summary>
        /// <param name="reader">the input string reader to unescaping.</param>
        /// <param name="length">the length of string reader.</param>
        /// <returns>a string that has been unescaped.</returns>
        public static string Unescape(TextReader reader, int length)
        {
            var sb = new StringBuilder(length);
            var code = -1;
            while ((code = reader.Read()) >= 0)
            {
                var ch = (char)code;
                char unescapeChar;
                if ((ch == '\\' && reader.Peek() != -1) && _unescapeSequences.TryGetValue((char)reader.Peek(), out unescapeChar))
                {
                    sb.Append(unescapeChar);
                    //skip a next char
                    reader.Read();
                }
                else
                {
                    sb.Append(ch);
                }
            }
            return sb.ToString();
        }
    }
}
