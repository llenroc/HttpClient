// Copyright (c) 2015 Yamool. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

namespace Yamool.Net.Http
{
    using System;
    using System.Threading;
    using System.Collections.Concurrent;

    internal class BufferPool
    {
        public const int DefaultBufferLength = 4096;
        private static Lazy<BufferPool> defaultBufferPool = new Lazy<BufferPool>(() => new BufferPool(1));

        private byte[] _largeBuffer;
        private ConcurrentStack<PooledBuffer> _queues = new ConcurrentStack<PooledBuffer>();
        private int _activeCount;

        public BufferPool(int numBuffer)
        {
            var bufferLength = DefaultBufferLength;
            _largeBuffer = new byte[bufferLength * numBuffer];
            for (var i = 0; i < numBuffer; i++)
            {
                _queues.Push(new PooledBuffer(_largeBuffer, i * bufferLength, bufferLength, true));
            }
        }

        public static BufferPool Default
        {
            get
            {
                return defaultBufferPool.Value;
            }
        }

        public PooledBuffer GetBuffer()
        {
            PooledBuffer buffer = null;
            if (!_queues.TryPop(out buffer))
            {
                //no pooled
                buffer = new PooledBuffer(new byte[4096], 0, 4096, false);
            }
            Interlocked.Increment(ref _activeCount);
            return buffer;
        }

        public void FreeBuffer(PooledBuffer buffer)
        {
            _queues.Push(buffer);
            Interlocked.Decrement(ref _activeCount);
        }
    }

    internal class PooledBuffer
    {
        public PooledBuffer(byte[] buffer, int offset, int length, bool pooled = true)
        {
            this.Array = buffer;
            this.Offset = offset;
            this.Length = length;
            this.IsPooled = pooled;
        }

        /// <summary>
        /// The byte array.
        /// </summary>
        public byte[] Array
        {
            get;
            private set;
        }

        /// <summary>
        /// The offset which begin to read.
        /// </summary>
        public int Offset
        {
            get;
            private set;
        }

        /// <summary>
        /// The buffer length.
        /// </summary>
        public int Length
        {
            get;
            private set;
        }

        internal bool IsPooled
        {
            get;
            private set;
        }
    }
}
