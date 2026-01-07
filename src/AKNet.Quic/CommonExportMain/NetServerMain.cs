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
            if (nNetType == NetType.Quic)
            {
                mInterface = new AKNet.Quic.Server.NetServerMain();
            }
            else if (nNetType == NetType.MSQuic)
            {
                mInterface = new AKNet.MSQuic.Server.NetServerMain();
            }
            else
            {
                NetLog.LogError("Unsupported network type: " + nNetType);
            }
        }
    }
}
