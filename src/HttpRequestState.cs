//----------------------------------------------------------------
// Copyright (c) Yamool Inc.  All rights reserved.
//----------------------------------------------------------------

namespace Yamool.Net.Http
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents the current status in the http request operation.
    /// </summary>
    internal enum HttpRequestTaskStatus : int
    {
        Init = 0,
        Processing = 1,
        Completed = 2,
        Cancelled = 3,
        Exception = 4
    }

    /// <summary>
    /// The http request asynchronous task.
    /// </summary>
    internal class HttpRequestTask : IDisposable
    {
        private TaskCompletionSource<HttpResponse> _tcs;
        private Timer _timer;
        private int _state;
        private Action<HttpRequestTaskStatus> _taskComplatedCallback;

        public HttpRequestTask(Action<HttpRequestTaskStatus> taskComplatedCallback)
        {
            _state = 1;
            _tcs = new TaskCompletionSource<HttpResponse>();
            _taskComplatedCallback = taskComplatedCallback;
        }

        public void SetTimeout(int timeout)
        {
            if (timeout == 0)
            {
                return;
            }
            if (_timer == null)
            {
                _timer = new Timer(TimeoutCallback, this, timeout, Timeout.Infinite);
            }
        }

        public void SetResult(HttpResponse response)
        {
            if (this.IsCancellationOrCompleted)
            {
                return;
            }
            if (Interlocked.CompareExchange(ref _state, 2, 1) == 1)
            {
                _tcs.TrySetResult(response);
                _taskComplatedCallback(HttpRequestTaskStatus.Completed);
            }
        }

        public void SetException(Exception exception)
        {
            if (this.IsCancellationOrCompleted)
            {
                return;
            }
            if (Interlocked.CompareExchange(ref _state, 3, 1) == 1)
            {
                _tcs.TrySetException(exception);
                _taskComplatedCallback(HttpRequestTaskStatus.Exception);
            }
        }

        public void SetCanceled()
        {
            if (this.IsCancellationOrCompleted)
            {
                return;
            }
            if (Interlocked.CompareExchange(ref _state, 3, 1) == 1)
            {
                _tcs.TrySetCanceled();
                _taskComplatedCallback(HttpRequestTaskStatus.Cancelled);
            }
        }

        private static void TimeoutCallback(object state)
        {
            var taskSource = (HttpRequestTask)state;
            taskSource.SetCanceled();
        }

        public bool IsCancellationOrCompleted
        {
            get
            {
                return _state >= 2;
            }
        }

        public Task<HttpResponse> Task
        {
            get
            {
                return _tcs.Task;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_timer != null)
                {
                    _timer.Dispose();
                    _timer = null;
                }
            }
        }
    }
}
