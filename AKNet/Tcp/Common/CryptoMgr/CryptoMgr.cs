using AKNet.Common;
using System;

namespace AKNet.Tcp.Common
{
    internal interface NetStreamEncryptionInterface
    {
        ReadOnlySpan<byte> Encode(int nPackageId, ReadOnlySpan<byte> mBufferSegment);
        bool Decode(AkCircularBuffer<byte> mReceiveStreamList, TcpNetPackage mPackage);
    }

    internal class CryptoMgr : NetStreamEncryptionInterface
    {
        readonly NetStreamEncryptionInterface mNetStreamEncryption = null;
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
                mNetStreamEncryption = new NetStreamEncryption_Xor(mCryptoInterface);
            }
            else
            {
                mNetStreamEncryption = new NetStreamEncryption();
            }
        }

        public ReadOnlySpan<byte> Encode(int nPackageId, ReadOnlySpan<byte> mBufferSegment)
        {
#if DEBUG
            if (mBufferSegment.Length > Config.nDataMaxLength)
            {
                NetLog.LogError("发送尺寸超出最大限制" + mBufferSegment.Length + " | " + Config.nDataMaxLength);
            }
#endif
            return mNetStreamEncryption.Encode(nPackageId, mBufferSegment);
        }

        public bool Decode(AkCircularBuffer<byte> mReceiveStreamList, TcpNetPackage mPackage)
        {
            return mNetStreamEncryption.Decode(mReceiveStreamList, mPackage);
        }
    }
}
