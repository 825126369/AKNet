using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using XKNet.Common;
using XKNet.Udp.POINTTOPOINT.Common;

namespace XKNet.Udp.POINTTOPOINT.Server
{
    internal class ClientPeerSocketMgr
    {
        private UdpServer mNetServer = null;
        private ClientPeer mClientPeer = null;

        readonly SocketAsyncEventArgs SendArgs = new SocketAsyncEventArgs();
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

        readonly ConcurrentQueue<NetUdpFixedSizePackage> mSendPackageQueue = new ConcurrentQueue<NetUdpFixedSizePackage>();
        readonly object bSendIOContexUsedObj = new object();
        bool bSendIOContexUsed = false;
        public void SendNetPackage(NetUdpFixedSizePackage mPackage)
        {
            var mPackage2 = ObjectPoolManager.Instance.mUdpFixedSizePackagePool.Pop();
            mPackage2.CopyFrom(mPackage);
            mPackage2.remoteEndPoint = mPackage.remoteEndPoint;
            mSendPackageQueue.Enqueue(mPackage2);

            bool bCanGoNext = false;
            lock (bSendIOContexUsedObj)
            {
                bCanGoNext = bSendIOContexUsed == false;
                if (!bSendIOContexUsed)
                {
                    bSendIOContexUsed = true;
                }
            }

            if (bCanGoNext)
            {
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
                ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mPackage);

                mNetServer.mSocketMgr.SendNetPackage(e, ProcessSend);
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
                ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mPackage);
            }
        }
    }
}
