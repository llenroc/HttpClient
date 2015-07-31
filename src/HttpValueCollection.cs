//----------------------------------------------------------------
// Copyright (c) Yamool Inc.  All rights reserved.
//----------------------------------------------------------------

namespace Yamool.Net.Http
{
    using System;
    using System.Collections.Specialized;
    using System.Text;

    internal class HttpValueCollection : NameValueCollection
    {
        internal HttpValueCollection(string str, bool readOnly, bool urlencoded, Encoding encoding)
            : base(StringComparer.OrdinalIgnoreCase)
        {
            if (!string.IsNullOrEmpty(str))
            {
                this.FillFromString(str, urlencoded, encoding);
            }
            base.IsReadOnly = readOnly;
        }

        internal void FillFromString(string s, bool urlencoded, Encoding encoding)
        {
            int num = (s != null) ? s.Length : 0;
            for (int i = 0; i < num; i++)
            {

                int startIndex = i;
                int num4 = -1;
                while (i < num)
                {
                    char ch = s[i];
                    if (ch == '=')
                    {
                        if (num4 < 0)
                        {
                            num4 = i;
                        }
                    }
                    else if (ch == '&')
                    {
                        break;
                    }
                    i++;
                }
                string str = null;
                string str2 = null;
                if (num4 >= 0)
                {
                    str = s.Substring(startIndex, num4 - startIndex);
                    str2 = s.Substring(num4 + 1, (i - num4) - 1);
                }
                else
                {
                    str2 = s.Substring(startIndex, i - startIndex);
                }
                if (urlencoded)
                {
                    base.Add(HttpUtils.UrlDecode(str, encoding), HttpUtils.UrlDecode(str2, encoding));
                }
                else
                {
                    base.Add(str, str2);
                }
                if ((i == (num - 1)) && (s[i] == '&'))
                {
                    base.Add(null, string.Empty);
                }
            }
        }
    }
}
