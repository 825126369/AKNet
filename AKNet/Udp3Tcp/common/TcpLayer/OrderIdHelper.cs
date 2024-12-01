/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/28 7:14:07
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
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
            if (nAddCount >= 0)
            {
                nOrderId += (uint)nAddCount;
            }
            else
            {
                nOrderId -= (uint)Math.Abs(nAddCount);
            }
            return nOrderId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool orInOrderIdFront(uint nOrderId_Back, uint nOrderId, int nCount)
        {
            return nOrderId - nOrderId_Back <= (uint)nCount;
        }
    }
}
