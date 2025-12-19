/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:16
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;

namespace AKNet.Udp4Tcp.Common
{
    /// <summary>
    /// 把数据拿出来
    /// </summary>
    internal static class UdpPackageEncryption
    {
        private static readonly byte[] mCacheSendHeadBuffer = new byte[Config.nUdpPackageFixedHeadSize];
        public static bool Decode(ReadOnlySpan<byte> mBuff, NetUdpReceiveFixedSizePackage mPackage)
        {
            if (mBuff.Length < Config.nUdpPackageFixedHeadSize)
            {
                NetLog.LogError($"解码失败 1: {mBuff.Length} | {Config.nUdpPackageFixedHeadSize}");
                return false;
            }

            mPackage.nOrderId = EndianBitConverter.ToUInt32(mBuff.Slice(0));
            mPackage.nRequestOrderId = EndianBitConverter.ToUInt32(mBuff.Slice(4));
            mPackage.nBodyLength = EndianBitConverter.ToUInt16(mBuff.Slice(8));
            
            ushort nBodyLength = mPackage.nBodyLength;
            if (Config.nUdpPackageFixedHeadSize + nBodyLength > Config.nUdpPackageFixedSize)
            {
                NetLog.LogError($"解码失败 3: {nBodyLength} | {Config.nUdpPackageFixedSize}");
                return false;
            }

            mPackage.CopyFrom(mBuff.Slice(Config.nUdpPackageFixedHeadSize, (int)nBodyLength));
            return true;
        }
        
        public static ReadOnlySpan<byte> EncodeHead(NetUdpSendFixedSizePackage mPackage)
        {
            uint nOrderId = mPackage.nOrderId;
            uint nRequestOrderId = mPackage.nRequestOrderId;
            ushort nBodyLength = (ushort)mPackage.nBodyLength;
            EndianBitConverter.SetBytes(mCacheSendHeadBuffer, 0, nOrderId);
            EndianBitConverter.SetBytes(mCacheSendHeadBuffer, 4, nRequestOrderId);
            EndianBitConverter.SetBytes(mCacheSendHeadBuffer, 8, nBodyLength);
            return mCacheSendHeadBuffer;
        }

	}
}
