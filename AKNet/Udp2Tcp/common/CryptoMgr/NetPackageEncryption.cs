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

namespace AKNet.Udp2Tcp.Common
{
    /// <summary>
    /// 把数据拿出来
    /// </summary>
    internal class NetPackageEncryption : NetPackageEncryptionInterface
    {
        private readonly byte[] mCheck = new byte[4] { (byte)'$', (byte)'$', (byte)'$', (byte)'$' };

        public bool Decode(ReadOnlySpan<byte> mBuff, NetUdpFixedSizePackage mPackage)
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

            mPackage.nOrderId = BitConverter.ToUInt16(mBuff.Slice(4, 2));
            if (mPackage.nOrderId == 0)
            {
                NetLog.LogError($"解码失败 3");
                return false;
            }

            mPackage.nRequestOrderId = BitConverter.ToUInt16(mBuff.Slice(6, 2));
            ushort nBodyLength = BitConverter.ToUInt16(mBuff.Slice(8, 2));

            if (Config.nUdpPackageFixedHeadSize + nBodyLength > Config.nUdpPackageFixedSize)
            {
                NetLog.LogError($"解码失败 4: {nBodyLength} | {Config.nUdpPackageFixedSize}");
                return false;
            }

            mPackage.CopyFrom(mBuff.Slice(Config.nUdpPackageFixedHeadSize, nBodyLength));
            return true;
        }

        public void Encode(NetUdpFixedSizePackage mPackage)
        {
            ushort nOrderId = mPackage.nOrderId;
            ushort nRequestOrderId = mPackage.nRequestOrderId;
            ushort nBodyLength = (ushort)(mPackage.Length - Config.nUdpPackageFixedHeadSize);

            Array.Copy(mCheck, 0, mPackage.buffer, 0, 4);

            byte[] byCom = BitConverter.GetBytes(nOrderId);
            Array.Copy(byCom, 0, mPackage.buffer, 4, byCom.Length);

            byCom = BitConverter.GetBytes(nRequestOrderId);
            Array.Copy(byCom, 0, mPackage.buffer, 6, byCom.Length);

            byCom = BitConverter.GetBytes(nBodyLength);
            Array.Copy(byCom, 0, mPackage.buffer, 8, byCom.Length);
        }

	}
}