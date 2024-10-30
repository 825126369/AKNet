/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:AKNet 网络库, 兼容 C#8.0 和 .Net Standard 2.1
*        Author:阿珂
*        CreateTime:2024/10/30 21:55:41
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using AKNet.Common;
using AKNet.Udp.POINTTOPOINT.Common;

namespace AKNet.Udp.POINTTOPOINT.Server
{
    internal class ClientPeerSocketMgr
    {
        private UdpServer mNetServer = null;
        private ClientPeer mClientPeer = null;

        readonly SocketAsyncEventArgs SendArgs = new SocketAsyncEventArgs();
        readonly ConcurrentQueue<NetUdpFixedSizePackage> mSendPackageQueue = new ConcurrentQueue<NetUdpFixedSizePackage>();
        bool bSendIOContexUsed = false;

        public ClientPeerSocketMgr(UdpServer mNetServer, ClientPeer mClientPeer)
        {
            this.mNetServer = mNetServer;
            this.mClientPeer = mClientPeer;

            SendArgs.Completed += ProcessSend;
            SendArgs.SetBuffer(new byte[Config.nUdpPackageFixedSize], 0, Config.nUdpPackageFixedSize);
            SendArgs.RemoteEndPoint = mClientPeer.GetIPEndPoint();
        }

        private void ProcessSend(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                SendNetPackage2(e);
            }
            else
            {
                NetLog.LogError(e.SocketError);
                mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
                bSendIOContexUsed = false;
            }
        }

        public void SendNetPackage(NetUdpFixedSizePackage mPackage)
        {
            MainThreadCheck.Check();
            mSendPackageQueue.Enqueue(mPackage);
            if (!bSendIOContexUsed)
            {
                bSendIOContexUsed = true;
                SendNetPackage2(SendArgs);
            }
        }

        private void SendNetPackage2(SocketAsyncEventArgs e)
        {
            NetUdpFixedSizePackage mPackage = null;
            if (mSendPackageQueue.TryDequeue(out mPackage))
            {
                Array.Copy(mPackage.buffer, e.Buffer, mPackage.Length);
                e.SetBuffer(0, mPackage.Length);
                e.RemoteEndPoint = mPackage.remoteEndPoint;
                mNetServer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);

                mNetServer.GetSocketMgr().SendNetPackage(e, ProcessSend);
            }
            else
            {
                bSendIOContexUsed = false;
            }
        }

        public void Reset()
        {
            NetUdpFixedSizePackage mPackage = null;
            while (mSendPackageQueue.TryDequeue(out mPackage))
            {
                mNetServer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
            }
        }
    }
}
