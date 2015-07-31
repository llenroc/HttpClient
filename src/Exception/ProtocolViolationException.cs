//----------------------------------------------------------------
// Copyright (c) Yamool Inc.  All rights reserved.
//----------------------------------------------------------------

namespace Yamool.Net.Http
{
    using System;

    /// <summary>
    /// The exception that is thrown when an http protocol was violation.
    /// </summary>
    public sealed class ProtocolViolationException : InvalidOperationException
    {
        public ProtocolViolationException() { }

        public ProtocolViolationException(string message)
            : base(message) { }
    }
}
