/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/28 7:14:07
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.LinuxTcp;
using System;

namespace AKNet.Udp4LinuxTcp.Common
{
    /// <summary>
    /// 把数据拿出来
    /// </summary>
    internal class NetPackageEncryption : NetPackageEncryptionInterface
    {
        private readonly byte[] mCheck = new byte[4] { (byte)'$', (byte)'$', (byte)'$', (byte)'$' };
        private readonly byte[] mCacheSendHeadBuffer = new byte[Config.nUdpPackageFixedHeadSize];

        public bool Decode(ReadOnlySpan<byte> mBuff, sk_buff mPackage)
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

            int nSunLength = 
            mPackage.nInnerCommandId = mBuff[0];
            mBuff.CopyTo(mPackage.mBuffer);
            return true;
        }
        
        public byte[] EncodeHead(sk_buff mPackage)
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
