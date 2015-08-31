// Copyright (c) 2015 Yamool. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

namespace Yamool.Net.Http
{
    using System;

    public class ByteArrayContent : HttpContent
    {
        public ByteArrayContent(byte[] content)
            : this(content, 0, content.Length)
        {
        }

        public ByteArrayContent(byte[] content, int offset, int count)
        {
        }

        protected internal override bool TryComputeLength(out long length)
        {
            throw new NotImplementedException();
        }

        protected override System.Threading.Tasks.Task SerializeToStreamAsync(System.IO.Stream stream)
        {
            throw new NotImplementedException();
        }
    }
}
