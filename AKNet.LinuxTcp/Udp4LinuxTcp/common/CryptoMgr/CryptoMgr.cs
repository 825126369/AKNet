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

namespace AKNet.Udp4LinuxTcp.Common
{
    internal interface NetPackageEncryptionInterface
    {
        byte[] EncodeHead(NetUdpSendFixedSizePackage mPackage);
        bool Decode(ReadOnlySpan<byte> mBuff, NetUdpReceiveFixedSizePackage mPackage);
    }

    internal class CryptoMgr : NetPackageEncryptionInterface
    {
        readonly NetPackageEncryptionInterface mNetPackageEncryption = null;
        public CryptoMgr()
        {
            mNetPackageEncryption = new NetPackageEncryption();
        }

        public byte[] EncodeHead(NetUdpSendFixedSizePackage mPackage)
        {
            return mNetPackageEncryption.EncodeHead(mPackage);
        }

        public bool Decode(ReadOnlySpan<byte> mBuff, NetUdpReceiveFixedSizePackage mPackage)
        {
            return mNetPackageEncryption.Decode(mBuff, mPackage);
        }
    }
}
