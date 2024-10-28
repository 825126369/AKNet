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
            NetUdpFixedSizePackage mPackage = null;
			while (mPackageQueue.TryDequeue(out mPackage))
			{
				mClientPeer.mMsgReceiveMgr.ReceiveNetPackage(mPackage);
            }
        }

		public void MultiThreadingReceiveNetPackage(NetUdpFixedSizePackage mPackage)
		{
            mPackageQueue.Enqueue(mPackage);
        }
    }
}