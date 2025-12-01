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
[assembly: InternalsVisibleTo("AKNet2")]
[assembly: InternalsVisibleTo("AKNet.Other")]
namespace AKNet.Common
{
    internal static partial class RandomTool
    {
        private static readonly Random mRandom = new Random(RandomNumberGenerator.GetInt32(int.MaxValue));

        public static double Random()
        {
            return mRandom.NextDouble();
        }

        public static int RandomArrayIndex(int x, int y)
        {
            return mRandom.Next(x, y);
        }

        public static int Random(int x, int y)
        {
            NetLog.Assert(y < int.MaxValue);
            return mRandom.Next(x, y + 1);
        }

        public static uint Random(uint x, uint y)
        {
            NetLog.Assert(y < int.MaxValue);
            return (uint)mRandom.Next((int)x, (int)y + 1);
        }

        public static ulong Random(ulong x, ulong y)
        {
            return (ulong)(x + mRandom.NextDouble() * (y - x));
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
                nTempTargetRate = Random(1, nSumRate);
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
