// Copyright (c) 2015 Yamool. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

namespace Yamool.Net.Http
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Runtime.CompilerServices;
    using System.Net.Security;
    using System.Security.Authentication;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Represents a HTTP connection for HTTP transport. 
    /// </summary>
    internal sealed class Connection
    {    
        private readonly ServicePoint _servicePoint;
        private readonly PooledBuffer _buffer;
        private readonly Saea _saea;
        private readonly AsyncReadWrite _asyncReadWrite;
        private readonly Socket _socket;    
        private DateTime _idleSinceUtc;
        private int _state;
        private int _reuses;

        public Connection(ServicePoint servicePoint)
        {
            _state = 0;
            _servicePoint = servicePoint;
            _saea = SaeaPool.Default.GetSaea();
            _buffer = BufferPool.Default.GetBuffer();
            _socket = new Socket(servicePoint.HostEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _socket.NoDelay = !servicePoint.UseNagleAlgorithm;

            if (_servicePoint.IsSecured)
            {
                _asyncReadWrite = new SslAsyncReadWrite(servicePoint.Address.Host, _socket, _saea, servicePoint.ClientCertificate, ValidateServerCertificate);
            }
            else
            {
                _asyncReadWrite = new AsyncReadWrite(_socket, _saea);
            }
            
        }

        public bool Busy
        {
            get
            {
                return _state > 0;
            }
        }

        public PooledBuffer Buffer
        {
            get
            {
                return _buffer;
            }
        }

        public DateTime IdleSince 
        {
            get
            {
                return _idleSinceUtc;
            }
        }

        public ServicePoint ServicePoint
        {
            get
            {
                return _servicePoint;
            }
        }

        public void Close()
        {
            this.Close(false);
        }

        public void Close(bool reuse)
        {
            if (reuse)
            {
                Interlocked.Exchange(ref _state, 0);
                Interlocked.Increment(ref _reuses);
            }
            else
            {
                if (Interlocked.Exchange(ref _state, 2) == 2)
                {
                    return;
                }
                _servicePoint.ReleaseConnection(this);
                _saea.Free();
                _buffer.Dispose();
                try
                {
                    _socket.Shutdown(SocketShutdown.Both);
                }
                catch { }
                finally
                {
                    _socket.Close();
                }
                _asyncReadWrite.Dispose();
            }
        }

        public async Task<bool> ConnectAsync()
        {
            _saea.SetBuffer(0, 0);            
            await _asyncReadWrite.Connect(_servicePoint.HostEndPoint);
            if (_servicePoint.IsSecured)
            {
                await _asyncReadWrite.Authenticate();
            }
            return true;
        }

        public Task<ArraySegment<Byte>> ReadPooledBufferAsync()
        {
            return this.ReadPooledBufferAsync(_buffer.Offset, _buffer.Length); 
        }

        public async Task<ArraySegment<Byte>> ReadPooledBufferAsync(int offset, int count)
        {
            if (offset < 0 || offset < _buffer.Offset)
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            if (count < 0 || count > _buffer.Length)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            _saea.SetBuffer(_buffer.Array, offset, count);
            await _asyncReadWrite.ReadAsync();
            return new ArraySegment<byte>(_buffer.Array, _buffer.Offset, (_saea.Offset - _buffer.Offset) + _asyncReadWrite.BytesTransferred);
        }

        internal int Read(byte[] buffer, int offset, int count)
        {            
           // return _socket.Receive(buffer, offset, count, SocketFlags.None);
            _saea.SetBuffer(buffer, offset, count);
            return _asyncReadWrite.Read();
        }

        internal async Task<int> ReadAsync(byte[] buffer, int offset, int count)
        {
            _saea.SetBuffer(buffer, offset, count);
            await _asyncReadWrite.ReadAsync();
            return _asyncReadWrite.BytesTransferred;
        }

        internal async Task<int> WritePooledBufferAsync(int offset, int count)
        {
            if (offset < 0 || offset < _buffer.Offset)
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            if (count < 0 || count > _buffer.Length)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            _saea.SetBuffer(_buffer.Array, offset, count);
            await _asyncReadWrite.WriteAsync();
            return _asyncReadWrite.BytesTransferred;
        }

        internal bool SubmitRequest(HttpRequest request)
        {
            _idleSinceUtc = DateTime.UtcNow;
            return true;
        }

        internal bool TrySetBusy()
        {
            if (Interlocked.Exchange(ref _state, 1)==0)
            {             
                return true;
            }
            return false;
        }

        private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        private class SslAsyncReadWrite : AsyncReadWrite
        {
            private SslStream _sslStream;
            private string _host;
            private RemoteCertificateValidationCallback _userCertificateValidationCallback;
            private X509CertificateCollection _clientCertificates;
            private int _bytesTransferred;
            private bool _authenticated;
            public SslAsyncReadWrite(string host, Socket socket, Saea saea, X509Certificate clientCertificate, RemoteCertificateValidationCallback userCertificateValidationCallback)
                : base(socket, saea)
            {
                _host = host;
                _userCertificateValidationCallback = userCertificateValidationCallback;
                if (clientCertificate != null)
                {
                    _clientCertificates = new X509CertificateCollection(new X509Certificate[] { clientCertificate });
                }               
            }

            public override int BytesTransferred
            {
                get
                {
                    return _bytesTransferred;
                }
            }

            public override async Task<bool> Authenticate()
            {
                if (_authenticated)
                {
                    return true;
                }
                _sslStream = new SslStream(new NetworkStream(_socket), false, _userCertificateValidationCallback);
                await _sslStream.AuthenticateAsClientAsync(_host, _clientCertificates, SslProtocols.Default, false);
                _authenticated = true;
                return _authenticated;
            }

            public override int Read()
            {
                return _sslStream.Read(_saea.Buffer, _saea.Offset, _saea.Count);
            }

            public override AsyncReadWrite ReadAsync()
            {
                this.Reset();
                _sslStream.ReadAsync(_saea.Buffer, _saea.Offset, _saea.Count).ContinueWith(task =>
                {
                    _bytesTransferred = task.Result;
                    _completed = true;
                    var previous = _continuation ?? Interlocked.CompareExchange(ref _continuation, Sentinal, null);
                    if (previous != null)
                    {
                        previous();
                    }
                }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                return this;
            }

            public override AsyncReadWrite WriteAsync()
            {
                this.Reset();
                _sslStream.WriteAsync(_saea.Buffer, _saea.Offset, _saea.Count).ContinueWith(task =>
                {
                    _bytesTransferred = _saea.Count;
                    _completed = true;
                    var previous = _continuation ?? Interlocked.CompareExchange(ref _continuation, Sentinal, null);
                    if (previous != null)
                    {
                        previous();
                    }
                }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                return this;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _sslStream.Close();
                }
                base.Dispose(disposing);
            }         
        }

        private class AsyncReadWrite : INotifyCompletion, IDisposable
        {
            protected static readonly Action Sentinal = () => { };
            protected Action _continuation;
            protected readonly Saea _saea;
            protected bool _completed;
            protected readonly Socket _socket;

            public AsyncReadWrite(Socket socket, Saea saea)
            {
                _socket = socket;
                _saea = saea;
                _saea.OnCompleted(_ =>
                {
                    _completed = true;
                    var previous = _continuation ?? Interlocked.CompareExchange(ref _continuation, Sentinal, null);
                    if (previous != null)
                    {
                        previous();
                    }
                });
            }

            public bool IsCompleted
            {
                get
                {
                    return _completed;
                }
            }

            public virtual int BytesTransferred
            {
                get
                {
                    return _saea.BytesTransferred;
                }
            }

            private bool CanReuse
            {
                get
                {
                    return _socket.Connected &&
                        !(_socket.Poll(1, SelectMode.SelectRead) && _socket.Available == 0);
                }
            }

            public void Dispose()
            {
                this.Dispose(true);
                GC.SuppressFinalize(this);
            }

            public void GetResult()
            {
                if (_saea.SocketError != SocketError.Success)
                {
                    throw new HttpRequestException("Occurring an exception during the HTTP request operations.Error:" + _saea.SocketError);
                }
            }

            public AsyncReadWrite GetAwaiter()
            {
                return this;
            }

            public virtual AsyncReadWrite Connect(EndPoint remoteEP)
            {
                this.Reset();
                _saea.RemoteEndPoint = remoteEP;
                if (this.CanReuse || !_socket.ConnectAsync(_saea))
                {
                    _completed = true;
                }
                return this;
            }

            public virtual Task<bool> Authenticate()
            {
                return Task.FromResult(true);
            }

            public virtual int Read()
            {
                return _socket.Receive(_saea.Buffer, _saea.Offset, _saea.Count, SocketFlags.None);
            }

            public virtual AsyncReadWrite ReadAsync()
            {
                this.Reset();
                if (!_socket.ReceiveAsync(_saea))
                {
                    _completed = true;
                }
                return this;
            }

            public virtual AsyncReadWrite WriteAsync()
            {
                this.Reset();
                if (!_socket.SendAsync(_saea))
                {
                    _completed = true;
                }
                return this;
            }

            void INotifyCompletion.OnCompleted(Action continuation)
            {
                if (_continuation == Sentinal || Interlocked.CompareExchange(ref _continuation, continuation, null) == Sentinal)
                {
                    //Task.Run(continuation);
                    continuation();
                }
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _saea.Free();
                }
            }

            protected void Reset()
            {
                _completed = false;
                _continuation = null;
            }
        }
    }
}
