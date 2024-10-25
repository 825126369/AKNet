using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;

namespace XKNet.Common
{
    internal static class IPAddressHelper
    {
        public static List<int> GetAvailableTcpPortList()
        {
            const ushort nStart = 1024;
            const ushort nEnd = ushort.MaxValue;
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
            const ushort nStart = 1024;
            const ushort nEnd = ushort.MaxValue;
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
