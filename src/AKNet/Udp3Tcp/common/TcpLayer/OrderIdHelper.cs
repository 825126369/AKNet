/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:54
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System.Runtime.CompilerServices;

namespace AKNet.Udp3Tcp.Common
{
    internal static class OrderIdHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint AddOrderId(uint nOrderId)
        {
            return AddOrderId(nOrderId, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint MinusOrderId(uint nOrderId)
        {
            return AddOrderId(nOrderId, -1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint AddOrderId(uint nOrderId, int nAddCount)
        {
            long n2 = nOrderId + nAddCount;
            if (n2 > Config.nUdpMaxOrderId)
            {
                n2 = n2 - Config.nUdpMaxOrderId + Config.nUdpMinOrderId - 1;
            }
            else if (n2 < Config.nUdpMinOrderId)
            {
                n2 = n2 + Config.nUdpMaxOrderId - Config.nUdpMinOrderId + 1;
            }

            NetLog.Assert(n2 >= Config.nUdpMinOrderId && n2 <= Config.nUdpMaxOrderId, n2);
            uint n3 = (uint)n2;
            return n3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool orInOrderIdFront(uint nOrderId_Back, uint nOrderId, int nCount)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetOrderIdLength(uint nOrderId, uint nRequestOrderId)
        {
            if (nRequestOrderId >= nOrderId)
            {
                return (int)(nRequestOrderId - nOrderId);
            }
            else
            {
                return (int)(Config.nUdpMaxOrderId - nOrderId + nRequestOrderId - Config.nUdpMinOrderId + 1);
            }
        }
    }
}
