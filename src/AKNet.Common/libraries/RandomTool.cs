/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:45
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
[assembly: InternalsVisibleTo("AKNet")]
[assembly: InternalsVisibleTo("AKNet.MSQuic")]
[assembly: InternalsVisibleTo("AKNet.LinuxTcp")]
[assembly: InternalsVisibleTo("AKNet.WebSocket")]
namespace AKNet.Common
{
    internal static class RandomTool
    {
        [ThreadStatic]
        private static Random m_Random;

        private static Random RandomGenerator
        {
            get
            {
                if (m_Random == null)
                {
                    m_Random = new Random(RandomNumberGenerator.GetInt32(int.MaxValue));
                }
                return m_Random;
            }
        }

        public static int RandomArrayIndex(int x, int y)
        {
            return RandomGenerator.Next(x, y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long InnerRandomInt64(long x, long y)
        {
            return (long)(x + RandomGenerator.NextDouble() * (y - x));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong InnerRandomUInt64(ulong x, ulong y)
        {
            return (ulong)(x + RandomGenerator.NextDouble() * (y - x));
        }

        public static int RandomInt32(int x, int y)
        {
            NetLog.Assert(x >= int.MinValue);
            NetLog.Assert(y <= int.MaxValue);
            NetLog.Assert(x <= y);
            return (int)InnerRandomInt64(x, y);
        }

        public static uint RandomUInt32(uint x, uint y)
        {
            NetLog.Assert(x >= uint.MinValue);
            NetLog.Assert(y <= uint.MaxValue);
            NetLog.Assert(x <= y);
            return (uint)InnerRandomInt64(x, y);
        }

        public static ulong RandomUInt64(ulong x, ulong y)
        {
            NetLog.Assert(x >= ulong.MinValue);
            NetLog.Assert(y <= ulong.MaxValue);
            NetLog.Assert(x <= y);
            return InnerRandomUInt64(x, y);
        }

        public static long RandomInt64(long x, long y)
        {
            NetLog.Assert(x >= long.MinValue);
            NetLog.Assert(y <= long.MaxValue);
            NetLog.Assert(x <= y);
            return InnerRandomInt64(x, y);
        }

        public static int GetIndexByRate(int[] mRateList)
        {
            int nSumRate = 0;
            foreach (var nRate in mRateList)
            {
                nSumRate = nSumRate + nRate;
            }

            int nTempTargetRate = nSumRate + 1;
            if (nSumRate >= 1)
            {
                nTempTargetRate = RandomInt32(1, nSumRate);
            }

            int nTempRate = 0;
            int nTargetIndex = -1;
            for (int i = 0; i < mRateList.Length; i++)
            {
                nTempRate = nTempRate + mRateList[i];
                if (nTempRate >= nTempTargetRate)
                {
                    nTargetIndex = i;
                    break;
                }
            }

            return nTargetIndex;
        }
    }
}
