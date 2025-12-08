/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:15
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Common
{
    public class NetServerMain : NetServerMainBase
    {
        public NetServerMain(NetType nNetType)
        {
            if (nNetType == NetType.TCP)
            {
                mInterface = new AKNet.Tcp.Server.NetServerMain();
            }
#if NET9_0_OR_GREATER
            else if (nNetType == NetType.MSQuic)
            {
                mInterface = new AKNet.Quic.Server.NetServerMain();
            }
#endif
            else if (nNetType == NetType.Udp1Tcp)
            {
                mInterface = new AKNet.Udp1Tcp.Server.UdpNetServerMain();
            }
            else if (nNetType == NetType.Udp2Tcp)
            {
                mInterface = new AKNet.Udp2Tcp.Server.Udp2TcpNetServerMain();
            }
            else if (nNetType == NetType.Udp3Tcp)
            {
                mInterface = new AKNet.Udp3Tcp.Server.Udp3TcpNetServerMain();
            }
            else
            {
                NetLog.LogError("Unsupported network type: " + nNetType);
            }
        }
    }
}
