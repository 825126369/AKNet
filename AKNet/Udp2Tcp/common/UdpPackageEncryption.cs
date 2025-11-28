/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/29 4:33:39
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;

namespace AKNet.Udp2Tcp.Common
{
    /// <summary>
    /// 把数据拿出来
    /// </summary>
    internal static class UdpPackageEncryption
    {
        private static readonly byte[] mCheck = new byte[4] { (byte)'$', (byte)'$', (byte)'$', (byte)'$' };
        public static bool Decode(ReadOnlySpan<byte> mBuff, NetUdpFixedSizePackage mPackage)
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

            mPackage.nOrderId = EndianBitConverter.ToUInt16(mBuff.Slice(4));
            if (mPackage.nOrderId == 0)
            {
                NetLog.LogError($"解码失败 3");
                return false;
            }

            mPackage.nRequestOrderId = EndianBitConverter.ToUInt16(mBuff.Slice(6));
            ushort nBodyLength = EndianBitConverter.ToUInt16(mBuff.Slice(8));

            if (Config.nUdpPackageFixedHeadSize + nBodyLength > Config.nUdpPackageFixedSize)
            {
                NetLog.LogError($"解码失败 4: {nBodyLength} | {Config.nUdpPackageFixedSize}");
                return false;
            }

            mPackage.CopyFrom(mBuff.Slice(Config.nUdpPackageFixedHeadSize, nBodyLength));
            return true;
        }

        public static void Encode(NetUdpFixedSizePackage mPackage)
        {
            ushort nOrderId = mPackage.nOrderId;
            ushort nRequestOrderId = mPackage.nRequestOrderId;
            ushort nBodyLength = (ushort)(mPackage.Length - Config.nUdpPackageFixedHeadSize);

            Array.Copy(mCheck, 0, mPackage.buffer, 0, 4);
            EndianBitConverter.SetBytes(mPackage.buffer, 4, nOrderId);
            EndianBitConverter.SetBytes(mPackage.buffer, 6, nRequestOrderId);
            EndianBitConverter.SetBytes(mPackage.buffer, 8, nBodyLength);
        }

	}
}
