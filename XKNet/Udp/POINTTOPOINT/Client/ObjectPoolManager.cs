using XKNet.Common;
using XKNet.Udp.POINTTOPOINT.Common;

namespace XKNet.Udp.POINTTOPOINT.Client
{
    internal class ObjectPoolManager : Singleton<ObjectPoolManager>
	{
		public SafeObjectPool<NetUdpFixedSizePackage> mUdpFixedSizePackagePool = null;
		public SafeObjectPool<NetCombinePackage> mCombinePackagePool = null;
		public SafeObjectPool<UdpCheckMgr.CheckPackageInfo> mCheckPackagePool = null;
		public SafeArrayGCPool<byte> nSendBufferPool = null;

		public ObjectPoolManager()
		{
			mUdpFixedSizePackagePool = new SafeObjectPool<NetUdpFixedSizePackage>();
			mCombinePackagePool = new SafeObjectPool<NetCombinePackage>();
			mCheckPackagePool = new SafeObjectPool<UdpCheckMgr.CheckPackageInfo>();
			nSendBufferPool = new SafeArrayGCPool<byte>();
		}

		public void CheckPackageCount()
		{
			NetLog.LogWarning("Client mUdpFixedSizePackagePool: " + mUdpFixedSizePackagePool.Count());
			NetLog.LogWarning("Client mCombinePackagePool: " + mCombinePackagePool.Count());
			NetLog.LogWarning("Client mCheckPackagePool: " + mCheckPackagePool.Count());
		}
	}
}