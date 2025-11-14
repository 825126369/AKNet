/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:26
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
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
        
        static int mtu_cache = 0;
        public static int GetMtu() //得到Mtu 是一个比较耗时的操作，1~2秒，甚至更长时间，所以缓存起来
        {
            if (mtu_cache <= 0)
            {
                int nMinMtu = int.MaxValue;
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        IPv4InterfaceProperties ipProps = ni.GetIPProperties().GetIPv4Properties();
                        if (ipProps != null)
                        {
                            if (ipProps.Mtu < nMinMtu)
                            {
                                nMinMtu = ipProps.Mtu;
                            }
                        }
                    }
                }

                if (nMinMtu < int.MaxValue)
                {
                    mtu_cache = nMinMtu;
                }
            }

            return mtu_cache;
        }
    }
}
