using XKNetCommon;

namespace XKNetUdpClient
{
	public class NetClient:ClientPeer
	{
        public override void Update(double elapsed)
        {
			if (elapsed >= 0.3)
			{
				NetLog.LogWarning("NetClient 帧 时间 太长: " + elapsed);
			}

			base.Update(elapsed);
		}
	}
}

