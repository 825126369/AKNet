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
            string password1 = mConfig.password1;
            string password2 = mConfig.password2;

            if (nECryptoType == ECryptoType.Aes)
            {
                var mCryptoInterface = new AESCrypto(password1, password2);
                mNetStreamEncryption = new NetStreamEncryption2(16, mCryptoInterface);
            }
            else if (nECryptoType == ECryptoType.Xor)
            {
                var mCryptoInterface = new XORCrypto(password1);
                mNetStreamEncryption = new NetStreamEncryption1(mCryptoInterface);
            }
            else
            {
                mNetStreamEncryption = new NetStreamEncryption();
            }
        }

        public ReadOnlySpan<byte> Encode(int nPackageId, ReadOnlySpan<byte> mBufferSegment)
        {
#if DEBUG
            if (mBufferSegment.Length > mConfig.nDataMaxLength)
            {
                NetLog.LogWarning("发送尺寸超出最大限制" + mBufferSegment.Length + " | " + mConfig.nDataMaxLength);
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
