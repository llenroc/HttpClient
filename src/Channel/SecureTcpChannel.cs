//----------------------------------------------------------------
// Copyright (c) Yamool Inc.  All rights reserved.
//----------------------------------------------------------------

namespace Yamool.Net.Http
{
    using System;
    using System.Net;
    using System.Net.Sockets;  
    using System.Net.Security;
    using System.Security.Authentication;
    using System.Security.Cryptography.X509Certificates;

    internal class SecureTcpChannel : ITcpChannel
    {
        private HttpRequest _request;
        private Socket _socket;        
        private ChannelEventArgs _args;
        private SslStream _sslStream;

        public SecureTcpChannel(HttpRequest request, X509CertificateCollection clientCertificates)
        {
            _request = request;
            _args = new ChannelEventArgs();
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            if (clientCertificates != null && clientCertificates.Count > 0)
            {
                this.Certificate = clientCertificates[0];
            }
        }

        public event EventHandler<ChannelEventArgs> Error;

        public event EventHandler<ChannelEventArgs> Completed;

        public void Connect(EndPoint remoteEP)
        {
            try
            {
                _socket.BeginConnect(remoteEP, (ar) =>
                {
                    try
                    {
                        _socket.EndConnect(ar);
                        _args.LastOperation = ChannelOperation.Connect;
                        //create a ssl-stream
                        _sslStream = new SslStream(new NetworkStream(_socket), false, OnRemoteCertifiateValidation);
                        X509CertificateCollection certificates = null;
                        if (Certificate != null)
                        {
                            certificates = new X509CertificateCollection(new[] { Certificate });
                        }
                        _sslStream.AuthenticateAsClient(_request._uri.Host, certificates, SslProtocols.Default, false);
                        this.OnCompleted(_args);
                    }
                    catch (AuthenticationException)
                    {
                        this.OnError(new HttpException(HttpExceptionStatus.SecureChannelFailure, "ssl stream authentication failure."));
                    }
                    catch (Exception ex)
                    {
                        this.OnError(ex);
                    }
                }, null);
            }
            catch (Exception ex)
            {
                this.OnError(ex);
            }
        }

        public void Send(byte[] buffer, int offset, int count)
        {
            try
            {
                _sslStream.BeginWrite(buffer, offset, count, (ar) =>
                {
                    try
                    {
                        _sslStream.EndWrite(ar);
                        _args.BytesTransferred = count - offset;
                        _args.LastOperation = ChannelOperation.Send;
                        this.OnCompleted(_args);
                    }
                    catch (Exception ex)
                    {
                        this.OnError(ex);
                    }
                }, null);
            }
            catch (Exception ex)
            {
                this.OnError(ex);
            }
        }

        public void Receive(byte[] buffer, int offset, int count)
        {
            try
            {
                _sslStream.BeginRead(buffer, offset, count, (ar) =>
                {
                    try
                    {
                        var readCount = _sslStream.EndRead(ar);
                        _args.BytesTransferred = readCount;
                        _args.LastOperation = ChannelOperation.Receive;
                        this.OnCompleted(_args);
                    }
                    catch (Exception ex)
                    {
                        this.OnError(ex);
                    }
                }, null);
            }
            catch (Exception ex)
            {
                this.OnError(ex);
            }
        }

        public void Disconnect()
        {
            this.Close();
        }

        public void Close()
        {
            if (_sslStream != null)
            {
                try
                {
                    _sslStream.Close();
                }
                catch { }
            }
            if (_socket != null)
            {
                try
                {
                    if (_socket.Connected)
                    {
                        _socket.Shutdown(SocketShutdown.Both);
                    }

                }
                finally
                {
                    _socket.Close();
                }
            }
            
        }

        /// <summary>
        /// The following method is invoked by the RemoteCertificateValidationDelegate.
        /// This allows you to check the certificate and accept or reject it
        /// </summary>
        /// <param name="sender">An object that contains state information for this validation.</param>
        /// <param name="certificate">The certificate used to authenticate the remote party.</param>
        /// <param name="chain">The chain of certificate authorities associated with the remote certificate.</param>
        /// <param name="sslPolicyErrors">One or more errors associated with the remote certificate.</param>
        /// <returns>return true will accept the certificate</returns>
        private bool OnRemoteCertifiateValidation(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return (this.Certificate != null && certificate == null) || (this.Certificate == null && certificate != null);
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

        public X509Certificate Certificate
        {
            get;
            set;
        }
    }
}
