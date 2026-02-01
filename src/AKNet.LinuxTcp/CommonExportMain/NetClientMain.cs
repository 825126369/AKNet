/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:27:06
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Common
{
    public class NetClientMain : NetClientMainBase
    {
        public NetClientMain(NetType nNetType)
        {
            mInterface = new AKNet.LinuxTcp.Client.NetClientMain();
        }
    }
}
