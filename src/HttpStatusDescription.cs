// Copyright (c) 2015 Yamool. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

namespace Yamool.Net.Http
{
    using System;
    using System.Collections.Generic;

    internal static class HttpStatusDescription
    {
        private static readonly Dictionary<int, string> httpStatusDescriptions = new Dictionary<int, string>()
        {
            {100,"Continue"},
            {101,"Switching Protocols"},
            {102,"Processing"},
            {103,"CheckPoint"},
            {200,"OK"},
            {201,"Created"},
            {202,"Accepted"},
            {203,"Non-Authoritative Information"},
            {204,"No Content"},
            {205,"Reset Content"},
            {206,"Partial Content"},
            {207,"Multi-Status"},
            {300,"Multiple Choices"},
            {301,"Moved Permanently"},
            {302,"Found"},
            {303,"See Other"},
            {304,"Not Modified"},
            {305,"Use Proxy"},
            {306,"Switch Proxy"},
            {307,"Temporary Redirect"},
            {308,"Resume Incomplete"},
            {400,"Bad Request"},
            {401,"Unauthorized"},
            {402,"Payment Required"},
            {403,"Forbidden"},
            {404,"Not Found"},
            {405,"Method Not Allowed"},
            {406,"Not Acceptable"},
            {407,"Proxy Authentication Required"},
            {408,"Request Timeout"},
            {409,"Conflict"},
            {410,"Gone"},
            {411,"Length Required"},
            {412,"Precondition Failed"},
		    {413,"Request Entity Too Large"},
            {414,"Request-Uri Too Long"},
		    {415,"Unsupported Media Type"},
            {416,"Requested Range Not Satisfiable"},
            {417,"Expectation Failed"},
            {418,"Im a Teapot"},
            {420,"Enhance Your Calm"},
            {422,"Unprocessable Entity"},
            {423,"Locked"},
            {424,"Failed Dependency"},
            {425,"Unordered Collection"},
            {426,"Upgrade Required"},
            {429,"Too Many Requests"},
            {444,"No Response"},
            {449,"RetryWith"},
            {450,"Blocked By Windows Parental Controls"},
            {499,"ClientClosedRequest"},
            {500,"Internal Server Error"},
            {501,"Not Implemented"},
            {502,"Bad Gateway"},
            {503,"Service Unavailable"},
            {504,"Gateway Timeout"},
            {505,"Http Version Not Supported"},
            {506,"Variant Also Negotiates"},
            {507,"Insufficient Storage"},
            {509,"Band width LimitExceeded"},
            {510,"Not Extended"}
        };

        internal static string Get(HttpStatusCode code)
        {
            return HttpStatusDescription.Get((int)code);
        }

        internal static string Get(int code)
        {
            string desc = null;
            if (httpStatusDescriptions.TryGetValue(code, out desc))
            {
                return desc;
            }
            return null;
        }
    }
}
