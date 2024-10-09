using XKNetCommon;

namespace XKNetTcpServer
{
    public class NetServerMain : TCPSocket_Server
	{
		public void Update(double elapsed)
		{
			if (elapsed >= 0.3)
			{
				NetLog.LogWarning("XKNetTcpServer 帧 时间 太长: " + elapsed);
			}

			mClientPeerManager.Update(elapsed);
		}
	}
}