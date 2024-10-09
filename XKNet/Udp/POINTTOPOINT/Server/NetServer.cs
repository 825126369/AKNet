using XKNetCommon;

namespace XKNetUdpServer
{
    public class NetServer: SocketUdp_Server
	{
		public NetServer()
		{
			mClientPeerManager = new ClientPeerManager (this);
			mPackageManager = new PackageManager ();
		}

		public override void InitNet (string ip, ushort ServerPort)
		{
			base.InitNet (ip, ServerPort);
		}

		public void Update(double elapsed)
		{
			if (elapsed >= 0.3)
			{
				NetLog.LogWarning("NetServer 帧 时间 太长: " + elapsed);
			}

			mClientPeerManager.Update (elapsed);
		}

		public override void Release ()
		{
			ObjectPoolManager.Instance.CheckPackageCount();
			base.Release ();
		}

		public PackageManager GetPackageManager()
		{
			return mPackageManager;
		}

		public ClientPeerManager GetClientPeerManager ()
		{
			return mClientPeerManager;
		}
	}

}