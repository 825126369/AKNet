/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:53
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;

namespace AKNet.Udp4Tcp.Common
{
    internal static class UdpPackageEncryption
    {
        private static readonly byte[] mCheck = new byte[2] { (byte)'$', (byte)'$'};

        public static bool Decode(ReadOnlySpan<byte> mBuff, NetUdpReceiveFixedSizePackage mPackage)
        {
            if (mBuff.Length < Config.nUdpPackageFixedHeadSize)
            {
                NetLog.LogError($"解码失败 1: {mBuff.Length} | {Config.nUdpPackageFixedHeadSize}");
                return false;
            }

            for (int i = 0; i < 2; i++)
            {
                if (mBuff[i] != mCheck[i])
                {
                    NetLog.LogError($"解码失败 2");
                    return false;
                }
            }

            ushort nBodyLength = EndianBitConverter.ToUInt16(mBuff.Slice(10));
            if (Config.nUdpPackageFixedHeadSize + nBodyLength > Config.nUdpPackageFixedSize)
            {
                NetLog.LogError($"解码失败 3: {nBodyLength} | {Config.nUdpPackageFixedSize}");
                return false;
            }

            if (Config.nUdpPackageFixedHeadSize + nBodyLength > mBuff.Length)
            {
                NetLog.LogError($"解码失败 4: {nBodyLength + Config.nUdpPackageFixedHeadSize} | {mBuff.Length}");
                return false;
            }

            mPackage.nBodyLength = nBodyLength;
            mPackage.nOrderId = EndianBitConverter.ToUInt32(mBuff.Slice(2));
            mPackage.nRequestOrderId = EndianBitConverter.ToUInt32(mBuff.Slice(6));
            mPackage.CopyFrom(mBuff.Slice(Config.nUdpPackageFixedHeadSize, nBodyLength));

            return true;
        }
        
        public static void EncodeHead(Span<byte> mDestBuffer, NetUdpSendFixedSizePackage mPackage)
        {
            uint nOrderId = mPackage.nOrderId;
            uint nRequestOrderId = mPackage.nRequestOrderId;
            ushort nBodyLength = (ushort)mPackage.nBodyLength;

            mCheck.CopyTo(mDestBuffer);
            EndianBitConverter.SetBytes(mDestBuffer, 2, nOrderId);
            EndianBitConverter.SetBytes(mDestBuffer, 6, nRequestOrderId);
            EndianBitConverter.SetBytes(mDestBuffer, 10, nBodyLength);
        }

	}
}
