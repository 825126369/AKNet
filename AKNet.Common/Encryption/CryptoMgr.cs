/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/14 8:56:43
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;

namespace AKNet.Common
{
    internal interface NetStreamEncryptionInterface
    {
        ReadOnlySpan<byte> Encode(ushort nPackageId, ReadOnlySpan<byte> mBufferSegment);
        bool Decode(NetStreamCircularBuffer mReceiveStreamList, NetStreamPackage mPackage);
    }

    internal class CryptoMgr : NetStreamEncryptionInterface
    {
        readonly NetStreamEncryptionInterface mNetPackageEncryption = null;
        public CryptoMgr()
        {
#if DEBUG
            mNetPackageEncryption = new NetStreamEncryption();

#else
            var mCryptoInterface = new XORCrypto();
            mNetPackageEncryption = new NetStreamEncryption_Xor(mCryptoInterface);
#endif
        }

        public ReadOnlySpan<byte> Encode(ushort nPackageId, ReadOnlySpan<byte> mBufferSegment)
        {
            return mNetPackageEncryption.Encode(nPackageId, mBufferSegment);
        }

        public bool Decode(NetStreamCircularBuffer mReceiveStreamList, NetStreamPackage mPackage)
        {
            return mNetPackageEncryption.Decode(mReceiveStreamList, mPackage);
        }
    }
}
