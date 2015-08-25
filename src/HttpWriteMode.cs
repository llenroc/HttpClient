// Copyright (c) 2015 Yamool. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

namespace Yamool.Net.Http
{
    using System;

    /// <summary>
    /// Represents a write mode for POST.
    /// </summary>
    internal enum HttpWriteMode
    {
        Unknown = 0,
        ContentLength = 1,
        Chunked = 2,
        Buffer = 3,
        None = 4,
    }
}
