//----------------------------------------------------------------
// Copyright (c) Yamool Inc.  All rights reserved.
//----------------------------------------------------------------

namespace Yamool.Net.Http
{
    using System;
    using System.Net;

    /// <summary>
    /// A channel is used to send and receive information over a socket.
    /// </summary>
    internal interface ITcpChannel
    {
        event EventHandler<ChannelEventArgs> Error;
        event EventHandler<ChannelEventArgs> Completed;

        /// <summary>
        /// Connect to remote end point.
        /// </summary>
        /// <param name="remoteEP">The remote server.</param>
        void Connect(EndPoint remoteEP);

        /// <summary>
        /// Sends data to a connected channel.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        void Send(byte[] buffer, int offset, int count);

        /// <summary>
        /// Receive a data from a connected channel.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        void Receive(byte[] buffer, int offset, int count);

        void Disconnect();

        /// <summary>
        /// Close a channel.
        /// </summary>
        void Close();
    }

    internal enum ChannelOperation
    {
        Connect,
        Receive,
        Send
    }
}
