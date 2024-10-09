using XKNetCommon;

namespace XKNetTcpClient
{
    public class NetClientMain : ClientPeer
	{
        public override void Update(double elapsed)
        {
            if (elapsed >= 0.3)
            {
                NetLog.LogWarning("帧 时间 太长: " + elapsed);
            }

            base.Update(elapsed);
        }
    }
}