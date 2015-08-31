// Copyright (c) 2015 Yamool. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

namespace Yamool.Net.Http.Headers
{
    using System;

    public class MediaTypeHeaderValue
    {
        public MediaTypeHeaderValue(string mediaType)
        {
            this.MediaType = mediaType;
        }

        public string CharSet
        {
            get;
            set;
        }

        public string MediaType
        {
            get;
            set;
        }
    }
}
