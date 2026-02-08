/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:47
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
            else if (nNetType == NetType.Udp1Tcp)
            {
                mInterface = new AKNet.Udp1Tcp.Server.NetServerMain();
            }
            else if (nNetType == NetType.Udp2Tcp)
            {
                mInterface = new AKNet.Udp2Tcp.Server.NetServerMain();
            }
            else if (nNetType == NetType.Udp3Tcp)
            {
                mInterface = new AKNet.Udp3Tcp.Server.NetServerMain();
            }
            else if (nNetType == NetType.Udp4Tcp)
            {
                mInterface = new AKNet.Udp4Tcp.Server.NetServerMain();
            }
            else
            {
                NetLog.LogError("Unsupported network type: " + nNetType);
            }
        }
    }
}
