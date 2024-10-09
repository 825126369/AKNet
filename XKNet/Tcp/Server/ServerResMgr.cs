using XKNet.Common;
using XKNet.Tcp.Common;
using XKNet.Udp.Server;

namespace XKNet.Tcp.Server
{
    internal class ServerResMgr : Singleton<ServerResMgr>
	{
        internal readonly ClientPeerManager mClientPeerManager = null;
        internal readonly PackageManager mPackageManager = null;
		internal readonly BufferManager mBufferManager = null;
        internal readonly NetPackage mNetPackage = null;
        internal readonly SafeIdManager mClientIdManager = null;
		internal readonly ClientPeerPool mClientPeerPool = null;
        internal readonly ReadWriteIOContextPool mReadWriteIOContextPool = null;

        internal byte[] cacheSendProtobufBuffer = new byte[1024];

		public ServerResMgr()
		{
			mClientPeerManager = new ClientPeerManager();
            mPackageManager = new PackageManager();
			mNetPackage = new NetPackage();

			mBufferManager = new BufferManager(ServerConfig.nIOContexBufferLength, 2 * ServerConfig.numConnections);
			mReadWriteIOContextPool = new ReadWriteIOContextPool(ServerConfig.numConnections * 2, mBufferManager);

			mClientIdManager = new SafeIdManager();
			mClientPeerPool = new ClientPeerPool(ServerConfig.numConnections);
		}
		
		public void Init()
		{

		}
	}
}
