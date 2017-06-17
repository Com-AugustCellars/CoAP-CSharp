/*
 * Copyright (c) 2011-2014, Longxiang He <helongxiang@smeshlink.com>,
 * SmeshLink Technology Co.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY.
 * 
 * This file is part of the CoAP.NET, a CoAP framework in C#.
 * Please see README for more information.
 */

using System;
using System.Net;
using System.Net.Sockets;

namespace Com.AugustCellars.CoAP.Channel
{
    public partial class UDPChannel
    {
        private UDPSocket NewUDPSocket(AddressFamily addressFamily, Int32 bufferSize)
        {
            return new UDPSocket(addressFamily, bufferSize, SocketAsyncEventArgs_Completed);
        }

        private void BeginReceive(UDPSocket socket)
        {
#if LOG_UDP_CHANNEL
            _Log.Debug(m => m("BeginReceive: socket={0}  _running={1}", socket.ToString(), _running));
#endif
            if (_running == 0) {
                return;
            }

            if (socket.ReadBuffer.RemoteEndPoint == null) {
                socket.ReadBuffer.RemoteEndPoint = socket.Socket.Connected ?
                    socket.Socket.RemoteEndPoint :
                    new IPEndPoint(socket.Socket.AddressFamily == AddressFamily.InterNetwork ?
                        IPAddress.Any : IPAddress.IPv6Any, 0);
#if LOG_UDP_CHANNEL
                _Log.Debug( m => m("BeginReceive: Setup the remote endpoint {0}", socket.ReadBuffer.RemoteEndPoint.ToString()));
#endif
            }
            
            Boolean willRaiseEvent;
            try {
#if LOG_UDP_CHANNEL
                _Log.Debug("BeginReceive:  Start async read");
#endif
                willRaiseEvent = socket.Socket.ReceiveFromAsync(socket.ReadBuffer);
            }
            catch (ObjectDisposedException) {
#if LOG_UDP_CHANNEL
                _Log.Debug(m => m("BeginRecieve:  Socket {0} is disposed", socket.ToString()));
#endif
                // do nothing
                return;
            }
            catch (Exception ex) {
#if LOG_UDP_CHANNEL
                _Log.Debug(m =>m("BeginReceive: Socket {0} has exception", socket.ToString()));
#endif
                EndReceive(socket, ex);
                return;
            }

#if LOG_UDP_CHANNEL
            _Log.Debug(m => m("BeginReceive: willRaiseEvent={0}", willRaiseEvent));
#endif
            if (!willRaiseEvent) {
                ProcessReceive(socket.ReadBuffer);
            }
        }

        private void BeginSend(UDPSocket socket, Byte[] data, System.Net.EndPoint destination)
        {
            socket.SetWriteBuffer(data, 0, data.Length);
            socket.WriteBuffer.RemoteEndPoint = destination;

            Boolean willRaiseEvent;
            try {
                willRaiseEvent = socket.Socket.SendToAsync(socket.WriteBuffer);
            }
            catch (ObjectDisposedException) {
                // do nothing
                return;
            }
            catch (Exception ex) {
                EndSend(socket, ex);
                return;
            }

            if (!willRaiseEvent) {
                ProcessSend(socket.WriteBuffer);
            }
        }

        private void ProcessReceive(SocketAsyncEventArgs e)
        {
#if LOG_UDP_CHANNEL
            _Log.Debug("ProcessReceive");
#endif
            UDPSocket socket = (UDPSocket)e.UserToken;

            if (e.SocketError == SocketError.Success) {
#if LOG_UDP_CHANNEL
                _Log.Debug("ProcessReceive: ==> EndReceive");
#endif
                EndReceive(socket, e.Buffer, e.Offset, e.BytesTransferred, e.RemoteEndPoint);
            }
            else if (e.SocketError != SocketError.OperationAborted
                && e.SocketError != SocketError.Interrupted) {
#if LOG_UDP_CHANNEL
                _Log.Debug(m => m("ProcessRecieve: ==> exception handler {0}", e.SocketError.ToString()));
#endif
                EndReceive(socket, new SocketException((Int32)e.SocketError));
            }
        }

        private void ProcessSend(SocketAsyncEventArgs e)
        {
            UDPSocket socket = (UDPSocket)e.UserToken;

            if (e.SocketError == SocketError.Success) {
                EndSend(socket, e.BytesTransferred);
            }
            else {
                EndSend(socket, new SocketException((Int32)e.SocketError));
            }
        }

        void SocketAsyncEventArgs_Completed(Object sender, SocketAsyncEventArgs e)
        {
#if LOG_UDP_CHANNEL
            _Log.Debug( m=> m("SocketAsyncEventArgs: operation = {0}", e.LastOperation.ToString()));
#endif
            switch (e.LastOperation) {
                case SocketAsyncOperation.ReceiveFrom:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.SendTo:
                    ProcessSend(e);
                    break;
            }
        }

        partial class UDPSocket
        {
            public readonly SocketAsyncEventArgs ReadBuffer;
            public readonly SocketAsyncEventArgs WriteBuffer;
            readonly Byte[] _writeBuffer;
            private Boolean _isOuterBuffer;

            public UDPSocket(AddressFamily addressFamily, Int32 bufferSize,
                EventHandler<SocketAsyncEventArgs> completed)
            {
                Socket = new Socket(addressFamily, SocketType.Dgram, ProtocolType.Udp);
                ReadBuffer = new SocketAsyncEventArgs();
                ReadBuffer.SetBuffer(new Byte[bufferSize], 0, bufferSize);
                ReadBuffer.Completed += completed;
                ReadBuffer.UserToken = this;

                _writeBuffer = new Byte[bufferSize];
                WriteBuffer = new SocketAsyncEventArgs();
                WriteBuffer.SetBuffer(_writeBuffer, 0, bufferSize);
                WriteBuffer.Completed += completed;
                WriteBuffer.UserToken = this;
            }

            public void SetWriteBuffer(Byte[] data, Int32 offset, Int32 count)
            {
                if (count > _writeBuffer.Length) {
                    WriteBuffer.SetBuffer(data, offset, count);
                    _isOuterBuffer = true;
                }
                else {
                    if (_isOuterBuffer) {
                        WriteBuffer.SetBuffer(_writeBuffer, 0, _writeBuffer.Length);
                        _isOuterBuffer = false;
                    }
                    Buffer.BlockCopy(data, offset, _writeBuffer, 0, count);
                    WriteBuffer.SetBuffer(0, count);
                }
            }

            public void Dispose()
            {
                Socket.Close();
                ReadBuffer.Dispose();
                WriteBuffer.Dispose();
            }
        }
    }
}
