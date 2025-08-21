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

namespace AKNet.Udp4LinuxTcp.Common
{
    internal class CryptoMgr : NetStreamEncryptionInterface
    {
        readonly NetStreamEncryptionInterface mNetPackageEncryption = null;
        public CryptoMgr(Config mConfig)
        {
            ECryptoType nECryptoType = mConfig.nECryptoType;
            if (nECryptoType == ECryptoType.Xor)
            {
                var mCryptoInterface = new XORCrypto();
                mNetPackageEncryption = new NetStreamEncryption_Xor(mCryptoInterface);
            }
            else
            {
                mNetPackageEncryption = new NetStreamEncryption();
            }
        }

        public ReadOnlySpan<byte> Encode(ushort nPackageId, ReadOnlySpan<byte> mBufferSegment)
        {
            return mNetPackageEncryption.Encode(nPackageId, mBufferSegment);
        }

        public bool Decode(AkCircularManyBuffer mReceiveStreamList, TcpNetPackage mPackage)
        {
            return mNetPackageEncryption.Decode(mReceiveStreamList, mPackage);
        }
    }
}
