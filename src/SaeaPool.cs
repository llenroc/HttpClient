// Copyright (c) 2015 Yamool. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

namespace Yamool.Net.Http
{
    using System;
    using System.Collections.Concurrent;
    using System.Net.Sockets;
    using System.Threading;

    internal class SaeaPool
    {
        private static Lazy<SaeaPool> defaultSaeaPool = new Lazy<SaeaPool>(() => { return new SaeaPool(Environment.ProcessorCount << 1); });
        private readonly ConcurrentStack<Saea> _pool = new ConcurrentStack<Saea>();
        private int _currentSaeaCount;

        public SaeaPool(int numSaea)
        {
            for (var i = 0; i < numSaea; i++)
            {
                _pool.Push(this.NewSaea());
            }
        }

        public static SaeaPool Default
        {
            get
            {
                return defaultSaeaPool.Value;
            }
        }

        public Saea GetSaea()
        {
            Saea saea = null;
            if (!_pool.TryPop(out saea))
            {
                saea = this.NewSaea();
            }
            return saea;
        }

        public void FreeSaea(Saea saea)
        {
            saea.Free();
        }

        private Saea NewSaea()
        {
            Interlocked.Increment(ref _currentSaeaCount);
            return new Saea(this);
        }
    }

    internal class Saea : SocketAsyncEventArgs
    {
        private SaeaPool _pool;
        private volatile Action<Saea> _completedCallback;

        public Saea(SaeaPool pool)
        {
            _pool = pool;
            _completedCallback = null;
        }       

        public void OnCompleted(Action<Saea> completedCallback)
        {           
            _completedCallback = completedCallback;
        }

        public void Free()
        {
            this.AcceptSocket = null;
            this.DisconnectReuseSocket = false;
            if (this.UserToken != null && this.UserToken is IDisposable)
            {
                ((IDisposable)this.UserToken).Dispose();
                this.UserToken = null;
            }
            _completedCallback = null;
        }

        protected override void OnCompleted(SocketAsyncEventArgs e)
        {
            if (_completedCallback != null)
            {
                _completedCallback(this);
            }
        }
    }
}
