/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/29 4:33:47
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Common
{
    public class NetServerMain : NetServerMainBase
    {
        public NetServerMain(NetType nNetType)
        {
            if (nNetType == NetType.Udp1MSQuic)
            {
                mInterface = new AKNet.Udp1MSQuic.Server.QuicNetServerMain();
            }
            else if (nNetType == NetType.Udp2MSQuic)
            {
                mInterface = new AKNet.Udp2MSQuic.Server.QuicNetServerMain();
            }
            else
            {
                NetLog.LogError("Unsupported network type: " + nNetType);
            }
        }
    }
}
