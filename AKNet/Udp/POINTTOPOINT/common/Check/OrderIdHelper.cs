/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:29
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;

namespace AKNet.Udp.POINTTOPOINT.Common
{
    internal static class OrderIdHelper
    {
        public static ushort AddOrderId(ushort nOrderId)
        {
            return AddOrderId(nOrderId, 1);
        }

        public static ushort MinusOrderId(ushort nOrderId)
        {
            return AddOrderId(nOrderId, -1);
        }

        public static ushort AddOrderId(ushort nOrderId, int nAddCount)
        {
            int n2 = nOrderId + nAddCount;
            if (n2 > Config.nUdpMaxOrderId)
            {
                n2 = n2 - Config.nUdpMaxOrderId + Config.nUdpMinOrderId - 1;
            }
            else if (n2 < Config.nUdpMinOrderId)
            {
                n2 = n2 + Config.nUdpMaxOrderId - Config.nUdpMinOrderId + 1;
            }

            NetLog.Assert(n2 >= Config.nUdpMinOrderId && n2 <= Config.nUdpMaxOrderId, n2);
            ushort n3 = (ushort)n2;
            return n3;
        }

        public static bool orInOrderIdFront(ushort nOrderId_Back, ushort nOrderId, int nCount)
        {
            if (nOrderId_Back + nCount <= Config.nUdpMaxOrderId)
            {
                return nOrderId > nOrderId_Back && nOrderId <= nOrderId_Back + nCount;
            }
            else
            {
                if (nOrderId > nOrderId_Back)
                {
                    return nOrderId > nOrderId_Back && nOrderId <= Config.nUdpMaxOrderId;
                }
                else
                {
                    return nOrderId >= Config.nUdpMinOrderId && nOrderId <= AddOrderId(nOrderId_Back, nCount);
                }
            }
        }

    }
}
