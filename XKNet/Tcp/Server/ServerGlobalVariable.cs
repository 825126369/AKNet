using XKNet.Common;
using XKNet.Tcp.Common;

namespace XKNet.Tcp.Server
{
    internal class ServerGlobalVariable : Singleton<ServerGlobalVariable>
	{
        internal readonly ClientPeerManager mClientPeerManager = new ClientPeerManager();
        internal readonly PackageManager mPackageManager = new PackageManager();
        internal readonly NetPackage mNetPackage = new TcpNetPackage();
        internal readonly SafeIdManager mClientIdManager = new SafeIdManager();

        internal byte[] cacheSendProtobufBuffer = new byte[Config.nMsgPackageBufferMaxLength];

        internal ClientPeerPool mClientPeerPool = null;
        internal BufferManager mBufferManager = null;
        internal ReadWriteIOContextPool mReadWriteIOContextPool = null;

		public void Init()
		{
			mBufferManager = new BufferManager(Config.nIOContexBufferLength, 2 * Config.numConnections);
			mReadWriteIOContextPool = new ReadWriteIOContextPool(Config.numConnections * 2, mBufferManager);
			mClientPeerPool = new ClientPeerPool(Config.numConnections);
		}
	}
}
