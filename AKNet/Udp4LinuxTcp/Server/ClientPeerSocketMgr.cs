/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:26:53
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp4LinuxTcp.Common;
using System;
using System.Net;
using System.Net.Sockets;

namespace AKNet.Udp4LinuxTcp.Server
{
    internal class ClientPeerSocketMgr
    {
        private UdpServer mNetServer = null;
        private ClientPeerPrivate mClientPeer = null;

        FakeSocket mSocket = null;
        readonly object lock_mSocket_object =new object();

        readonly SocketAsyncEventArgs SendArgs = new SocketAsyncEventArgs();
        readonly AkCircularSpanBuffer mSendStreamList = null;
        bool bSendIOContexUsed = false;

        IPEndPoint mIPEndPoint;

        public ClientPeerSocketMgr(UdpServer mNetServer, ClientPeerPrivate mClientPeer)
        {
            this.mNetServer = mNetServer;
            this.mClientPeer = mClientPeer;

            SendArgs.Completed += ProcessSend;
            SendArgs.SetBuffer(new byte[Config.nUdpPackageFixedSize], 0, Config.nUdpPackageFixedSize);
            mSendStreamList = new AkCircularSpanBuffer();
        }

        public void HandleConnectedSocket(FakeSocket mSocket)
        {
            MainThreadCheck.Check();
            NetLog.Assert(mSocket != null, "mSocket == null");

            this.mSocket = mSocket;
            this.mIPEndPoint = mSocket.RemoteEndPoint;
            SendArgs.RemoteEndPoint = this.mIPEndPoint;
        }

        public IPEndPoint GetIPEndPoint()
        {
            if (mSocket != null)
            {
                return mSocket.RemoteEndPoint;
            }
            else
            {
                return mIPEndPoint;
            }
        }

        public sk_buff GetReceivePackage()
        {
            return mSocket.GetReceivePackage();
        }

        public bool SendToAsync(SocketAsyncEventArgs e)
        {
            bool bIOSyncCompleted = false;
            if (mSocket != null)
            {
                try
                {
                    bIOSyncCompleted = !mSocket.SendToAsync(e);
                }
                catch (Exception ex)
                {
                    bSendIOContexUsed = false;
                    if (mSocket != null)
                    {
                        NetLog.LogException(ex);
                    }
                }
            }
            return !bIOSyncCompleted;
        }

        private void ProcessSend(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                SendNetStream2(e.BytesTransferred);
            }
            else
            {
                NetLog.LogError(e.SocketError);
                mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
                bSendIOContexUsed = false;
            }
        }

        public void SendNetPackage(ReadOnlySpan<byte> mPackage)
        {
            MainThreadCheck.Check();
            lock (mSendStreamList)
            {
                mSendStreamList.WriteFrom(mPackage);
            }
            if (!bSendIOContexUsed)
            {
                bSendIOContexUsed = true;
                SendNetStream2();
            }
        }

        int nLastSendBytesCount = 0;
        private void SendNetStream2(int BytesTransferred = -1)
        {
            if (BytesTransferred >= 0)
            {
                if (BytesTransferred != nLastSendBytesCount)
                {
                    NetLog.LogError("UDP 发生短写");
                }
            }

            var mSendArgSpan = SendArgs.Buffer.AsSpan();
            int nSendBytesCount = 0;
            lock (mSendStreamList)
            {
                nSendBytesCount += mSendStreamList.WriteTo(mSendArgSpan);
            }

            if (nSendBytesCount > 0)
            {
                nLastSendBytesCount = nSendBytesCount;
                SendArgs.SetBuffer(0, nSendBytesCount);
                if (!SendToAsync(SendArgs))
                {
                    ProcessSend(null, SendArgs);
                }
            }
            else
            {
                bSendIOContexUsed = false;
            }
        }

        public void CloseSocket()
        {
            if (mSocket != null)
            {
                mSocket.Close();
                mSocket = null;
            }
        }

        public void Reset()
        {
            lock (mSendStreamList)
            {
                mSendStreamList.reset();
            }
        }

        public void Release()
        {
            lock (mSendStreamList)
            {
                mSendStreamList.reset();
            }
        }

    }
}
