using AKNet.Common;
using AKNet.Udp.POINTTOPOINT.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace AKNet.Udp.POINTTOPOINT.Server
{
    internal class FakeSocket : IPoolItemInterface
    {
        private readonly UdpServer mNetServer;
        private readonly Queue<NetUdpFixedSizePackage> mWaitCheckPackageQueue = new Queue<NetUdpFixedSizePackage>();

        public FakeSocket(UdpServer mNetServer)
        {
            this.mNetServer = mNetServer;
        }

        public IPEndPoint RemoteEndPoint { get; set; }

        public void ReceivePackage(NetUdpFixedSizePackage mPackage)
        {
            lock (mWaitCheckPackageQueue)
            {
                mWaitCheckPackageQueue.Enqueue(mPackage);
            }
        }

        public bool GetReceivePackage(out NetUdpFixedSizePackage mPackage)
        {
            lock (mWaitCheckPackageQueue)
            {
                return mWaitCheckPackageQueue.TryDequeue(out mPackage);
            }
        }

        public bool SendToAsync(SocketAsyncEventArgs mArg)
        {
            return this.mNetServer.GetSocketMgr().SendToAsync(mArg);
        }

        public void Reset()
        {
            lock (mWaitCheckPackageQueue)
            {
                while (mWaitCheckPackageQueue.TryDequeue(out var mPackage))
                {
                    mNetServer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
                }
                mWaitCheckPackageQueue.Clear();
            }
        }

        public void Close()
        {
            this.mNetServer.GetFakeSocketManager().RemoveFakeSocket(this);
        }
    }
}
