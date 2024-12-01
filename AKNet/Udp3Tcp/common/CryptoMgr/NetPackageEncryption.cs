/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/28 7:14:07
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;

namespace AKNet.Udp3Tcp.Common
{
    /// <summary>
    /// 把数据拿出来
    /// </summary>
    internal class NetPackageEncryption : NetPackageEncryptionInterface
    {
        private readonly byte[] mCheck = new byte[4] { (byte)'$', (byte)'$', (byte)'$', (byte)'$' };

        public bool Decode(ReadOnlySpan<byte> mBuff, NetUdpReceiveFixedSizePackage mPackage)
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

            mPackage.nOrderId = BitConverter.ToUInt32(mBuff.Slice(4, 4));
            mPackage.nRequestOrderId = BitConverter.ToUInt32(mBuff.Slice(8, 4));
            var nPackageId = mPackage.GetPackageId();
            if (!UdpNetCommand.orInnerCommand(nPackageId))
            {
                int nBodyLength = (int)(mPackage.nRequestOrderId - mPackage.nOrderId);
                if (Config.nUdpPackageFixedHeadSize + nBodyLength > Config.nUdpPackageFixedSize)
                {
                    NetLog.LogError($"解码失败 3: {nBodyLength} | {Config.nUdpPackageFixedSize}");
                    return false;
                }

                mPackage.CopyFrom(mBuff.Slice(Config.nUdpPackageFixedHeadSize, (int)nBodyLength));
            }
            return true;
        }

        private static readonly byte[] mCacheSendHeadBuffer = new byte[Config.nUdpPackageFixedHeadSize];
        public byte[] EncodeHead(NetUdpSendFixedSizePackage mPackage)
        {
            uint nOrderId = mPackage.nOrderId;
            uint nRequestOrderId = mPackage.nRequestOrderId;

            Array.Copy(mCheck, 0, mCacheSendHeadBuffer, 0, 4);

            byte[] byCom = BitConverter.GetBytes(nOrderId);
            Array.Copy(byCom, 0, mCacheSendHeadBuffer, 4, byCom.Length);

            byCom = BitConverter.GetBytes(nRequestOrderId);
            Array.Copy(byCom, 0, mCacheSendHeadBuffer, 8, byCom.Length);

            return mCacheSendHeadBuffer;
        }

	}
}
