using XKNet.Common;

namespace XKNet.Tcp.Server
{
    public class TcpNetServerMain : TCPSocket_Server
	{
		public void Update(double elapsed)
		{
			if (elapsed >= 0.3)
			{
				NetLog.LogWarning("XKNet.Tcp.Server 帧 时间 太长: " + elapsed);
			}

			mClientPeerManager.Update(elapsed);
		}
	}
}