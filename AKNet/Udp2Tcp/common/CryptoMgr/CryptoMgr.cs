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

namespace AKNet.Udp2Tcp.Common
{
    internal interface NetPackageEncryptionInterface
    {
        void Encode(NetUdpFixedSizePackage mPackage);
        bool Decode(ReadOnlySpan<byte> mBuff, NetUdpFixedSizePackage mPackage);
    }

    internal class CryptoMgr : NetPackageEncryptionInterface
    {
        readonly NetPackageEncryptionInterface mNetPackageEncryption = null;
        readonly Config mConfig;
        public CryptoMgr(Config mConfig)
        {
            this.mConfig = mConfig;
            ECryptoType nECryptoType = mConfig.nECryptoType;
            string password1 = mConfig.CryptoPasswrod1;
            string password2 = mConfig.CryptoPasswrod2;

            ////Test
            //nECryptoType = ECryptoType.Xor;
            //password1 = "2024/11/23-0208";
            //password2 = "2026/11/23-0208";

            if (nECryptoType == ECryptoType.Xor)
            {
                var mCryptoInterface = new XORCrypto(password1);
                mNetPackageEncryption = new NetPackageEncryption_Xor(mCryptoInterface);
            }
            else
            {
                mNetPackageEncryption = new NetPackageEncryption();
            }
        }

        public void Encode(NetUdpFixedSizePackage mPackage)
        {
            mNetPackageEncryption.Encode(mPackage);
        }

        public bool Decode(ReadOnlySpan<byte> mBuff, NetUdpFixedSizePackage mPackage)
        {
            return mNetPackageEncryption.Decode(mBuff, mPackage);
        }
    }
}
