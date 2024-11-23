using AKNet.Common;
using System;

namespace AKNet.Udp.POINTTOPOINT.Common
{
    internal interface NetPackageEncryptionInterface
    {
        void Encode(NetUdpFixedSizePackage mPackage);
        bool Decode(NetUdpFixedSizePackage mPackage);
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
            string password1 = mConfig.password1;
            string password2 = mConfig.password2;

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

        public bool Decode(NetUdpFixedSizePackage mPackage)
        {
            return mNetPackageEncryption.Decode(mPackage);
        }

        public bool Decode(ReadOnlySpan<byte> mBuff, NetUdpFixedSizePackage mPackage)
        {
            return mNetPackageEncryption.Decode(mBuff, mPackage);
        }
    }
}
