/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/7 21:38:44
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

        public static ushort AddOrderId(ushort nOrderId, int nAddCount)
        {
            int n2 = nOrderId + nAddCount;
            if (n2 > Config.nUdpMaxOrderId)
            {
                n2 -= Config.nUdpMaxOrderId;
            }
            else if (n2 < Config.nUdpMinOrderId)
            {
                int nDistance = Config.nUdpMinOrderId - n2;
                n2 = (int)Config.nUdpMaxOrderId + 1 - nDistance;
            }
            NetLog.Assert(n2 >= (int)Config.nUdpMinOrderId && n2 <= (int)Config.nUdpMaxOrderId, n2);

            ushort n3 = (ushort)n2;
            return n3;
        }

        public static ushort MinusOrderId(ushort nOrderId)
        {
            return AddOrderId(nOrderId, -1);
        }

        public static bool orInOrderIdFront(ushort nOrderId_Back, ushort nOrderId_Front, uint nCount)
        {
            for (int i = 1; i <= nCount; i++)
            {
                if (AddOrderId(nOrderId_Back, i) == nOrderId_Front)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
