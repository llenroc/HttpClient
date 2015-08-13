// Copyright (c) 2015 Yamool. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

namespace Yamool.Net.Http
{
    using System;

    internal static class ArraySegmentExtensions
    {
        internal static T Get<T>(this ArraySegment<T> src, int index)
        {
            if (index >= src.Count)
            {
                throw new IndexOutOfRangeException("index");
            }
            return src.Array[src.Offset + index];
        }
    }
}
