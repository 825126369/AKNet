﻿/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/28 7:14:07
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;
using System.Runtime.CompilerServices;

namespace AKNet.Udp2Tcp.Common
{
    /// <summary>
    /// 把数据拿出来
    /// </summary>
    internal static class LikeTcpNetPackageEncryption
    {
        private const int nPackageFixedHeadSize = 8;
        private static readonly byte[] mCheck = new byte[4] { (byte)'$', (byte)'$', (byte)'$', (byte)'$' };
        private static byte[] mCacheSendBuffer = new byte[Config.nUdpPackageFixedSize];
        private static byte[] mCacheReceiveBuffer = new byte[Config.nUdpPackageFixedSize];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EnSureSendBufferOk(int nSumLength)
        {
            BufferTool.EnSureBufferOk(ref mCacheSendBuffer, nSumLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EnSureReceiveBufferOk(int nSumLength)
        {
            BufferTool.EnSureBufferOk(ref mCacheReceiveBuffer, nSumLength);
        }

        public static bool Decode(AkCircularBuffer<byte> mReceiveStreamList, LikeTcpNetPackage mPackage)
        {
            if (mReceiveStreamList.Length < nPackageFixedHeadSize)
            {
                return false;
            }

            for (int i = 0; i < 4; i++)
            {
                if (mReceiveStreamList[i] != mCheck[i])
                {
                    NetLog.Log("11111");
                    return false;
                }
            }

            ushort nPackageId = (ushort)(mReceiveStreamList[4] | mReceiveStreamList[5] << 8);
            int nBodyLength = mReceiveStreamList[6] | mReceiveStreamList[7] << 8;
            NetLog.Assert(nBodyLength >= 0);

            int nSumLength = nBodyLength + nPackageFixedHeadSize;
            if (!mReceiveStreamList.isCanWriteTo(nSumLength))
            {
                return false;
            }

            mReceiveStreamList.ClearBuffer(nPackageFixedHeadSize);
            if (nBodyLength > 0)
            {
                EnSureReceiveBufferOk(nBodyLength);
                Span<byte> mCacheReceiveBufferSpan = mCacheReceiveBuffer.AsSpan();
                mReceiveStreamList.WriteTo(0, mCacheReceiveBufferSpan.Slice(0, nBodyLength));
            }

            mPackage.nPackageId = nPackageId;
            mPackage.InitData(mCacheReceiveBuffer, 0, nBodyLength);
            return true;
        }

        public static ReadOnlySpan<byte> Encode(int nPackageId, ReadOnlySpan<byte> mBufferSegment)
        {
            int nSumLength = mBufferSegment.Length + nPackageFixedHeadSize;
            EnSureSendBufferOk(nSumLength);

            Array.Copy(mCheck, mCacheSendBuffer, 4);
            mCacheSendBuffer[4] = (byte)nPackageId;
            mCacheSendBuffer[5] = (byte)(nPackageId >> 8);
            mCacheSendBuffer[6] = (byte)mBufferSegment.Length;
            mCacheSendBuffer[7] = (byte)(mBufferSegment.Length >> 8);

            Span<byte> mCacheSendBufferSpan = mCacheSendBuffer.AsSpan();
            mBufferSegment.CopyTo(mCacheSendBufferSpan.Slice(nPackageFixedHeadSize));
            return mCacheSendBufferSpan.Slice(0, nSumLength);
        }

    }
}