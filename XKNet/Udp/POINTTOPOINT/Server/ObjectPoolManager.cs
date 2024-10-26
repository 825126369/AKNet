using XKNet.Common;
using XKNet.Udp.POINTTOPOINT.Common;

namespace XKNet.Udp.POINTTOPOINT.Server
{
    internal class ObjectPoolManager : Singleton<ObjectPoolManager>
	{
		public readonly SafeObjectPool<NetUdpFixedSizePackage> mUdpFixedSizePackagePool;
		public readonly SafeObjectPool<NetCombinePackage> mCombinePackagePool;
		public readonly SafeArrayGCPool<byte> nSendBufferPool = null;

		public ObjectPoolManager()
		{
			mUdpFixedSizePackagePool = new SafeObjectPool<NetUdpFixedSizePackage>();
			mCombinePackagePool = new SafeObjectPool<NetCombinePackage>();
			nSendBufferPool = new SafeArrayGCPool<byte>();
		}

		public void CheckPackageCount()
		{
			NetLog.LogWarning("Server mUdpFixedSizePackagePool: " + mUdpFixedSizePackagePool.Count());
			NetLog.LogWarning("Server mCombinePackagePool: " + mCombinePackagePool.Count());
		}
	}
}