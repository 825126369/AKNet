using XKNet.Common;
using XKNet.Tcp.Common;

namespace XKNet.Tcp.Server
{
    internal class ServerGlobalVariable : Singleton<ServerGlobalVariable>
	{
        internal readonly ClientPeerManager mClientPeerManager = new ClientPeerManager();
        internal readonly PackageManager mPackageManager = new PackageManager();
        internal readonly NetPackage mNetPackage = new NetPackage();
        internal readonly SafeIdManager mClientIdManager = new SafeIdManager();

		internal ClientPeerPool mClientPeerPool = null;
        internal BufferManager mBufferManager = null;
        internal ReadWriteIOContextPool mReadWriteIOContextPool = null;

        internal byte[] cacheSendProtobufBuffer = new byte[1024];

		public void Init()
		{
			mBufferManager = new BufferManager(ServerConfig.nIOContexBufferLength, 2 * ServerConfig.numConnections);
			mReadWriteIOContextPool = new ReadWriteIOContextPool(ServerConfig.numConnections * 2, mBufferManager);
			mClientPeerPool = new ClientPeerPool(ServerConfig.numConnections);
		}
	}
}
