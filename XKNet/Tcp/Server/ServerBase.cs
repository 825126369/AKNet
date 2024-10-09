using XKNet.Common;
using XKNet.Tcp.Common;

namespace XKNet.Tcp.Server
{
    public abstract class ServerBase
	{
        internal readonly ClientPeerManager mClientPeerManager = null;
        public readonly PackageManager mPackageManager = null;
		private readonly BufferManager mBufferManager = null;
        internal readonly NetPackage mNetPackage = null;
        internal readonly SafeIdManager mClientIdManager = null;
		internal readonly ClientPeerPool mClientPeerPool = null;
        internal readonly ReadWriteIOContextPool mReadWriteIOContextPool = null;

		protected ServerBase()
		{
			mPackageManager = new PackageManager();
			mNetPackage = new NetPackage();

			mBufferManager = new BufferManager(ServerConfig.nIOContexBufferLength, 2 * ServerConfig.numConnections);
			mReadWriteIOContextPool = new ReadWriteIOContextPool(ServerConfig.numConnections * 2, mBufferManager);

			mClientIdManager = new SafeIdManager();
			mClientPeerManager = new ClientPeerManager(this);
			mClientPeerPool = new ClientPeerPool(ServerConfig.numConnections, this);
		}
	}
}
