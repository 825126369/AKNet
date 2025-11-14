/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:26:52
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;

namespace AKNet.Udp4LinuxTcp.Common
{
    public static class UdpStatistical
    {
        public static void PrintLog()
        {
            NetLog.Log($"Udp PackageStatistical:");
            TcpMibMgr.PRINT_NET_STATS();
        }
    }
}
