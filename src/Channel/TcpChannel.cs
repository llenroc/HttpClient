//----------------------------------------------------------------
// Copyright (c) Yamool Inc.  All rights reserved.
//----------------------------------------------------------------

namespace Yamool.Net.Http
{
    using System;
    using System.Net;
    using System.Net.Sockets;

    internal class TcpChannel : ITcpChannel
    {
        private bool _closed;
        private SocketAsyncEventArgs _socketEventArgs;
        private ChannelEventArgs _args;
        private HttpRequest _request;

        public TcpChannel(HttpRequest request)
        {
            _request = request;
            _socketEventArgs = new SocketAsyncEventArgs();
            _args = new ChannelEventArgs();
            _socketEventArgs.Completed += this.IO_Completed;
        }        

        public event EventHandler<ChannelEventArgs> Error;

        public event EventHandler<ChannelEventArgs> Completed;

        public void Connect(EndPoint remoteEP)
        {
            _socketEventArgs.RemoteEndPoint = remoteEP;
            _socketEventArgs.AcceptSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                _socketEventArgs.SetBuffer(null, 0, 0);
                var willRaiseEvent = _socketEventArgs.AcceptSocket.ConnectAsync(_socketEventArgs);
                if (!willRaiseEvent)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                this.OnError(ex);
            }
        }

        public void Send(byte[] buffer, int offset, int count)
        {
            _socketEventArgs.SetBuffer(buffer, offset, count);
            try
            {
                var willRaiseEvent = _socketEventArgs.AcceptSocket.SendAsync(_socketEventArgs);
                if (!willRaiseEvent)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                this.OnError(ex);
            }
        }

        public void Receive(byte[] buffer, int offset, int count)
        {
            _socketEventArgs.SetBuffer(buffer, offset, count);
            try
            {
                var willRaiseEvent = _socketEventArgs.AcceptSocket.ReceiveAsync(_socketEventArgs);
                if (!willRaiseEvent)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                this.OnError(ex);
            }
        }

        public void Disconnect()
        {
            CloseSocket(_socketEventArgs.AcceptSocket);
        }

        public void Close()
        {
            try
            {
                CloseSocket(_socketEventArgs.AcceptSocket);
            }
            finally
            {
                //release a socketeventargs object
                _socketEventArgs.Dispose();
            }
        }

        private void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                this.SocketErrorHandler(e.SocketError);
                return;
            }
            //set a event args          
            _args.BytesTransferred = e.BytesTransferred;            
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Connect:
                    {
                        _args.LastOperation = ChannelOperation.Connect;
                        this.OnCompleted(_args);
                        break;
                    }
                case SocketAsyncOperation.Send:
                    {
                        _args.LastOperation = ChannelOperation.Send;
                        this.OnCompleted(_args);
                        break;
                    }
                case SocketAsyncOperation.Receive:
                    {
                        _args.Buffer = e.Buffer;
                        _args.LastOperation = ChannelOperation.Receive;
                        this.OnCompleted(_args);
                        break;
                    }
            }
        }

        private static void CloseSocket(Socket socket)
        {
            if (socket == null)
            {
                return;
            }
            try
            {
                if (socket.Connected)
                {
                    socket.Shutdown(SocketShutdown.Both);
                }
            }
            finally
            {
                socket.Close();
            }
        }

        private void SocketErrorHandler(SocketError error)
        {
            this.OnError(new HttpException(HttpExceptionStatus.UnknownError, error.ToString()));
        }

        private void OnError(Exception ex)
        {
            var handler = this.Error;
            if (handler != null)
            {
                _args.LastException = ex;
                handler(this, _args);
            }
        }

        private void OnCompleted(ChannelEventArgs e)
        {
            var handler = this.Completed;
            if (handler != null)
            {
                handler(this, e);
            }
        }
    }
}
