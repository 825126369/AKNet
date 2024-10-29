using XKNet.Common;

namespace XKNet.Udp.POINTTOPOINT.Common
{
    internal static class OrderIdHelper
    {
        public static ushort AddOrderId(ushort nOrderId)
        {
            return AddOrderId(nOrderId, 1);
        }

        public static ushort AddOrderId(ushort nOrderId, ushort nAddCount)
        {
            uint n2 = (uint)nOrderId + nAddCount;
            if (n2 > Config.nUdpMaxOrderId)
            {
                n2 -= Config.nUdpMaxOrderId;
            }

            ushort n3 = (ushort)n2;
            NetLog.Assert(n3 > 0);
            return n3;
        }
    }
}
