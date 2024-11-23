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
        NetStreamEncryptionInterface mNetStreamEncryption = null;

        public CryptoMgr(ECryptoType nECryptoType, string password1, string password2)
        {
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
            return mNetStreamEncryption.Encode(nPackageId, mBufferSegment);
        }

        public bool Decode(AkCircularBuffer<byte> mReceiveStreamList, TcpNetPackage mPackage)
        {
            return mNetStreamEncryption.Decode(mReceiveStreamList, mPackage);
        }
    }
}
