//----------------------------------------------------------------
// Copyright (c) Yamool Inc.  All rights reserved.
//----------------------------------------------------------------

namespace Yamool.Net.Http
{
    using System;

    public class NotSupportProtocolException:NotSupportedException
    {
        public NotSupportProtocolException(string message) : base(message) { }
    }
}
