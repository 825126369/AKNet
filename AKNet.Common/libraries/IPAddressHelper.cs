/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/14 8:56:46
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("AKNet")]
[assembly: InternalsVisibleTo("AKNet.LinuxTcp")]
[assembly: InternalsVisibleTo("AKNet.MSQuic")]
[assembly: InternalsVisibleTo("AKNet2")]
[assembly: InternalsVisibleTo("AKNet.Other")]
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

        public static int GetMtu(IPPacketInformation pktInfo)
        {
            // 用 LINQ 一句话找到对应网卡
            NetworkInterface nic = NetworkInterface.GetAllNetworkInterfaces()
                        .FirstOrDefault(n => n.GetIPProperties()
                                              .GetIPv4Properties()?.Index == pktInfo.Interface
                                           || n.GetIPProperties()
                                              .GetIPv6Properties()?.Index == pktInfo.Interface);

            if (nic == null)
                throw new InvalidOperationException("找不到索引为 " + pktInfo.Interface + " 的接口");

            // 根据协议版本把 MTU 读出来
            if (pktInfo.Address.AddressFamily == AddressFamily.InterNetwork && nic.Supports(NetworkInterfaceComponent.IPv4))
                return nic.GetIPProperties().GetIPv4Properties().Mtu;

            if (pktInfo.Address.AddressFamily == AddressFamily.InterNetworkV6 && nic.Supports(NetworkInterfaceComponent.IPv6))
                return nic.GetIPProperties().GetIPv6Properties().Mtu;

            throw new NotSupportedException("该接口不支持 " + pktInfo.Address.AddressFamily);
        }
       
    }
}
