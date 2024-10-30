/************************************Copyright*****************************************
*        ProjectName:XKNet
*        Web:https://github.com/825126369/XKNet
*        Description:XKNet 网络库, 兼容 C#8.0 和 .Net Standard 2.1
*        Author:阿珂
*        CreateTime:2024/10/30 12:14:19
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;

namespace XKNet.Common
{
    internal static class IPAddressHelper
    {
        public static List<int> GetAvailableTcpPortList()
        {
            const ushort nStart = 4000;
            const ushort nEnd = 9000;
            List<int> usedPorts = new List<int>();
            List<int> availablePorts = new List<int>();

            IPEndPoint[] ConnInfoArray = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
            foreach (var conn in ConnInfoArray)
            {
                if (conn.Port != 0)
                {
                    usedPorts.Add(conn.Port);
                }
            }

            for (int i = nStart; i <= nEnd; i++)
            {
                if (!usedPorts.Contains(i))
                {
                    availablePorts.Add(i);
                }
            }
            return availablePorts;
        }

        public static List<int> GetAvailableUdpPortList()
        {
            const ushort nStart = 4000;
            const ushort nEnd = 9000;
            List<int> usedPorts = new List<int>();
            List<int> availablePorts = new List<int>();

            IPEndPoint[] ConnInfoArray = IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners();
            foreach (var conn in ConnInfoArray)
            {
                if (conn.Port != 0)
                {
                    usedPorts.Add(conn.Port);
                }
            }

            for (int i = nStart; i <= nEnd; i++)
            {
                if (!usedPorts.Contains(i))
                {
                    availablePorts.Add(i);
                }
            }
            return availablePorts;
        }
    }
}
