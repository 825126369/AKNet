/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/14 8:56:46
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Common
{
    public class NetClientMain : NetClientMainBase
    {
        public NetClientMain(NetType nNetType)
        {
            if (nNetType == NetType.LinuxTCP)
            {
                mInterface = new AKNet.LinuxTcp.Client.Udp4LinuxTcpNetClientMain();
            }
            else
            {
                NetLog.LogError("Unsupported network type: " + nNetType);
            }
        }
    }
}
