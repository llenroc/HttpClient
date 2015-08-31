// Copyright (c) 2015 Yamool. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

namespace Yamool.Net.Http.Headers
{
    using System;

    public sealed class HttpContentHeaders : HttpHeaders
    {
        private Func<long?> _calculateLengthFunc;

        internal HttpContentHeaders(Func<long?> calculateLengthFunc)
        {
            _calculateLengthFunc = calculateLengthFunc;
        }

        public MediaTypeHeaderValue ContentType
        {
            get;
            set;
        }
    }
}
