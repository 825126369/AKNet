/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:22
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;

namespace AKNet.Common
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

        public static int GetMtu()
        {
            List<int> mtuList = new List<int>();
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    IPv4InterfaceProperties ipProps = ni.GetIPProperties().GetIPv4Properties();
                    if (ipProps != null)
                    {
                        Console.WriteLine($"NetworkInterface: {ni.Id} {ni.Name} {ipProps.Mtu}");
                        mtuList.Add(ipProps.Mtu);
                    }
                }
            }
            
            if (mtuList.Count > 0)
            {
                return mtuList[0];
            }
            else
            {
                return 0;
            }
        }
    }
}
