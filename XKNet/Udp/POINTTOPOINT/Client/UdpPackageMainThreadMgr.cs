using System.Collections.Concurrent;
using XKNet.Udp.POINTTOPOINT.Common;

namespace XKNet.Udp.POINTTOPOINT.Client
{
    internal class UdpPackageMainThreadMgr
    {
        private readonly ConcurrentStack<NetUdpFixedSizePackage> mPackageQueue = new ConcurrentStack<NetUdpFixedSizePackage>();
        private ClientPeer mClientPeer = null;

		public UdpPackageMainThreadMgr(ClientPeer mClientPeer)
		{
			this.mClientPeer = mClientPeer;
        }

		public void Update(double elapsed)
		{
			NetUdpFixedSizePackage mPackage = null;
			while (mPackageQueue.TryPop(out mPackage))
			{
				mClientPeer.mMsgReceiveMgr.ReceiveNetPackage(mPackage);
			}
		}

		public void MultiThreadingReceiveNetPackage(NetUdpFixedSizePackage mPackage)
		{
            mPackageQueue.Push(mPackage);
        }
    }
}