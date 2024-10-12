using XKNet.Common;
using XKNet.Udp.POINTTOPOINT.Common;

namespace XKNet.Udp.POINTTOPOINT.Client
{
    internal class ObjectPoolManager : Singleton<ObjectPoolManager>
	{
		public SafeObjectPool<NetUdpFixedSizePackage> mUdpFixedSizePackagePool = null;
		public SafeObjectPool<NetCombinePackage> mCombinePackagePool = null;
		public SafeArrayGCPool<byte> nSendBufferPool = null;

		public ObjectPoolManager()
		{
			mUdpFixedSizePackagePool = new SafeObjectPool<NetUdpFixedSizePackage>();
			mCombinePackagePool = new SafeObjectPool<NetCombinePackage>();
			nSendBufferPool = new SafeArrayGCPool<byte>();
		}

		public void CheckPackageCount()
		{
			NetLog.LogWarning("Client mUdpFixedSizePackagePool: " + mUdpFixedSizePackagePool.Count());
			NetLog.LogWarning("Client mCombinePackagePool: " + mCombinePackagePool.Count());
		}
	}
}