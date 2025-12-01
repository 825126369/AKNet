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
    public class NetClientMain : NetClientMainBase
    {
        public NetClientMain(NetType nNetType)
        {
            if (nNetType == NetType.TCP)
            {
                mInterface = new AKNet.Tcp.Client.NetClientMain();
            }
#if NET9_0_OR_GREATER
            else if (nNetType == NetType.MSQuic)
            {
                mInterface = new AKNet.Quic.Client.QuicNetClientMain();
            }
#endif
            else if (nNetType == NetType.UDP)
            {
                mInterface = new AKNet.Udp.POINTTOPOINT.Client.UdpNetClientMain();
            }
            else if (nNetType == NetType.Udp2Tcp)
            {
                mInterface = new AKNet.Udp2Tcp.Client.Udp2TcpNetClientMain();
            }
            else if (nNetType == NetType.Udp3Tcp)
            {
                mInterface = new AKNet.Udp3Tcp.Client.Udp3TcpNetClientMain();
            }
            else
            {
                NetLog.LogError("Unsupported network type: " + nNetType);
            }
        }
    }
}
