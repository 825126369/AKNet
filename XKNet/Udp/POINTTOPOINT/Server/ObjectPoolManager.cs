using XKNet.Common;
using XKNet.Udp.Common;

namespace XKNet.Udp.Server
{
    internal class ObjectPoolManager : Singleton<ObjectPoolManager>
	{
		public SafeObjectPool<NetUdpFixedSizePackage> mUdpFixedSizePackagePool;
		public SafeObjectPool<NetCombinePackage> mCombinePackagePool;
		public SafeObjectPool<ClientPeer> mClientPeerPool;
		public SafeObjectPool<NetEndPointPackage> mNetEndPointPackagePool;
		public SafeObjectPool<UdpCheck3Pool.CheckPackageInfo> mCheckPackagePool = null;
		public SafeArrayGCPool<byte> nSendBufferPool = null;

		public ObjectPoolManager()
		{
			mUdpFixedSizePackagePool = new SafeObjectPool<NetUdpFixedSizePackage>();
			mCombinePackagePool = new SafeObjectPool<NetCombinePackage>();
			mClientPeerPool = new SafeObjectPool<ClientPeer>();
			mNetEndPointPackagePool = new SafeObjectPool<NetEndPointPackage>();

			mCheckPackagePool = new SafeObjectPool<UdpCheck3Pool.CheckPackageInfo>();
			nSendBufferPool = new SafeArrayGCPool<byte>();
		}

		public void CheckPackageCount()
		{
			NetLog.LogWarning("Server mUdpFixedSizePackagePool: " + mUdpFixedSizePackagePool.Count());
			NetLog.LogWarning("Server mCombinePackagePool: " + mCombinePackagePool.Count());
			NetLog.LogWarning("Server mClientPeerPool: " + mClientPeerPool.Count());
			NetLog.LogWarning("Server mNetEndPointPackagePool: " + mNetEndPointPackagePool.Count());
			NetLog.LogWarning("Server mCheckPackagePool: " + mCheckPackagePool.Count());
		}
	}
}