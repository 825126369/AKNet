/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:14
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Runtime.CompilerServices;
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
        private static readonly Random mRandom = new Random(RandomNumberGenerator.GetInt32(int.MaxValue));

        public static int RandomArrayIndex(int x, int y)
        {
            return mRandom.Next(x, y);
        }

        public static int RandomInt32(int x, int y)
        {
            NetLog.Assert(y < int.MaxValue);
            return mRandom.Next(x, y + 1);
        }

        public static uint RandomUInt32(uint x, uint y)
        {
            NetLog.Assert(y < int.MaxValue);
            return (uint)mRandom.Next((int)x, (int)y + 1);
        }

        public static ulong RandomUInt64(ulong x, ulong y)
        {
            return (ulong)(x + mRandom.NextDouble() * (y - x));
        }

        public static long RandomInt64(long x, long y)
        {
            return (long)(x + mRandom.NextDouble() * (y - x));
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
