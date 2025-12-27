/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:16
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/

using AKNet.Common;
using System;
using System.Net;
using System.Net.Sockets;

namespace AKNet.Udp4Tcp.Common
{
    public class SocketItem : IDisposable
    {
        public Socket mSocket;
        public readonly SocketAsyncEventArgs ReceiveArgs = new SocketAsyncEventArgs();
        public readonly SocketAsyncEventArgs SendArgs = new SocketAsyncEventArgs();
        private static readonly IPEndPoint mEndPointEmpty = new IPEndPoint(IPAddress.Any, 0);

        public SocketItem()
        {
            mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            mSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            mSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, int.MaxValue);

            ReceiveArgs.Completed += ProcessReceive;
            ReceiveArgs.SetBuffer(new byte[Config.nUdpPackageFixedSize], 0, Config.nUdpPackageFixedSize);
            ReceiveArgs.RemoteEndPoint = mEndPointEmpty;
        }

        public void StartReceiveFromAsync()
        {
            bool bIOPending = false;
            if (mSocket != null)
            {
                try
                {
                    bIOPending = mSocket.ReceiveFromAsync(ReceiveArgs);
                    if (!bIOPending)
                    {
                        ProcessReceive(null, ReceiveArgs);
                    }
                }
                catch (Exception e)
                {
                    if (mSocket != null)
                    {
                        NetLog.LogException(e);
                    }
                }
            }
        }

        private void ProcessReceive(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
            {
                NetLog.Assert(e.RemoteEndPoint != mEndPointEmpty);
                MultiThreadingReceiveNetPackage(e);
                e.RemoteEndPoint = mEndPointEmpty;
            }
            StartReceiveFromAsync();
        }

        public bool SendToAsync(SocketAsyncEventArgs e)
        {
            bool bIOPending = false;

            try
            {
                bIOPending = this.mSocket.SendToAsync(e);
                if (!bIOPending)
                {
                    ProcessSend(null, e);
                }
            }
            catch (Exception ex)
            {
                NetLog.LogException(ex);
            }

            return bIOPending;
        }

        private void ProcessSend(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                
            }
            else
            {
                NetLog.LogError(e.SocketError);
            }
        }

        public void Dispose()
        {
            if (mSocket != null)
            {
                Socket mSocket2 = mSocket;
                mSocket = null;

                try
                {
                    mSocket2.Close();
                }
                catch (Exception) { }
            }
        }
    }

}









