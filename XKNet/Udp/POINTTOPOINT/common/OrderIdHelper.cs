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
            nOrderId += nAddCount;
            if (nOrderId > Config.nUdpMaxOrderId)
            {
                nOrderId -= Config.nUdpMinOrderId;
            }
            return nOrderId;
        }
    }
}
