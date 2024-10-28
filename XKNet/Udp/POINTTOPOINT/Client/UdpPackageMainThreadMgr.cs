using System.Collections.Concurrent;
using XKNet.Common;
using XKNet.Udp.POINTTOPOINT.Common;

namespace XKNet.Udp.POINTTOPOINT.Client
{
    internal class UdpPackageMainThreadMgr
    {
        private readonly ConcurrentQueue<NetUdpFixedSizePackage> mPackageQueue = new ConcurrentQueue<NetUdpFixedSizePackage>();
        private ClientPeer mClientPeer = null;

		public UdpPackageMainThreadMgr(ClientPeer mClientPeer)
		{
			this.mClientPeer = mClientPeer;
        }

		public void Update(double elapsed)
		{
            int nCount = 0;
            NetUdpFixedSizePackage mPackage = null;
			while (mPackageQueue.TryDequeue(out mPackage))
			{
				mClientPeer.mMsgReceiveMgr.ReceiveNetPackage(mPackage);
                nCount++;

            }

            if (nCount > 0)
            {
                NetLog.LogWarning($"mPackageQueue.TryDequeue Count: {nCount}, {mPackageQueue.Count}");
            }
        }

		public void MultiThreadingReceiveNetPackage(NetUdpFixedSizePackage mPackage)
		{
            mPackageQueue.Enqueue(mPackage);
        }
    }
}