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
                PackageStatistical.AddReceivePackageCount();
                mClientPeer.mUdpCheckPool.ReceiveNetPackage(mPackage);
            }
        }

        public void MultiThreadingReceiveNetPackage(NetUdpFixedSizePackage mPackage)
        {
            bool bSucccess = NetPackageEncryption.DeEncryption(mPackage);
            if (bSucccess)
            {
                mPackageQueue.Enqueue(mPackage);
            }
            else
            {
                ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mPackage);
                NetLog.LogError("解码失败 !!!");
            }
        }
    }
}