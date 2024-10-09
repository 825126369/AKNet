using XKNetCommon;
using XKNetTcpCommon;

namespace XKNetTcpServer
{
    public class ServerBase
	{
		public readonly ClientPeerManager mClientPeerManager = null;
		public readonly PackageManager mPackageManager = null;
		private readonly BufferManager mBufferManager = null;
		public readonly NetPackage mNetPackage = null;
		public readonly SafeIdManager mClientIdManager = null;
		public readonly ClientPeerPool mClientPeerPool = null;
		public readonly ReadWriteIOContextPool mReadWriteIOContextPool = null;

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
