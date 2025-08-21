/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;

namespace AKNet.Udp3Tcp.Common
{
    /// <summary>
    /// 把数据拿出来
    /// </summary>
    internal static class UdpPackageEncryption
    {
        private static readonly byte[] mCheck = new byte[4] { (byte)'$', (byte)'$', (byte)'$', (byte)'$' };
        private static readonly byte[] mCacheSendHeadBuffer = new byte[Config.nUdpPackageFixedHeadSize];

        public static bool Decode(ReadOnlySpan<byte> mBuff, NetUdpReceiveFixedSizePackage mPackage)
        {
            if (mBuff.Length < Config.nUdpPackageFixedHeadSize)
            {
                NetLog.LogError($"解码失败 1: {mBuff.Length} | {Config.nUdpPackageFixedHeadSize}");
                return false;
            }

            for (int i = 0; i < 4; i++)
            {
                if (mBuff[i] != mCheck[i])
                {
                    NetLog.LogError($"解码失败 2");
                    return false;
                }
            }

            mPackage.nOrderId = EndianBitConverter.ToUInt32(mBuff.Slice(4));
            mPackage.nRequestOrderId = EndianBitConverter.ToUInt32(mBuff.Slice(8));
            mPackage.nBodyLength = EndianBitConverter.ToUInt16(mBuff.Slice(12));
            
            ushort nBodyLength = mPackage.nBodyLength;
            if (Config.nUdpPackageFixedHeadSize + nBodyLength > Config.nUdpPackageFixedSize)
            {
                NetLog.LogError($"解码失败 3: {nBodyLength} | {Config.nUdpPackageFixedSize}");
                return false;
            }

            mPackage.CopyFrom(mBuff.Slice(Config.nUdpPackageFixedHeadSize, (int)nBodyLength));
            return true;
        }
        
        public static byte[] EncodeHead(NetUdpSendFixedSizePackage mPackage)
        {
            uint nOrderId = mPackage.nOrderId;
            uint nRequestOrderId = mPackage.nRequestOrderId;
            ushort nBodyLength = (ushort)mPackage.nBodyLength;

            Buffer.BlockCopy(mCheck, 0, mCacheSendHeadBuffer, 0, 4);
            EndianBitConverter.SetBytes(mCacheSendHeadBuffer, 4, nOrderId);
            EndianBitConverter.SetBytes(mCacheSendHeadBuffer, 8, nRequestOrderId);
            EndianBitConverter.SetBytes(mCacheSendHeadBuffer, 12, nBodyLength);

            return mCacheSendHeadBuffer;
        }

	}
}
