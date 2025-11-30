/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:21
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;

namespace AKNet.LinuxTcp.Common
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
