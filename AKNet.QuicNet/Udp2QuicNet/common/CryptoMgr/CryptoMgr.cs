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

namespace AKNet.QuicNet.Common
{
    internal interface NetStreamEncryptionInterface
    {
        Memory<byte> Encode(ushort nPackageId, ReadOnlySpan<byte> mBufferSegment);
        bool Decode(AkCircularBuffer mReceiveStreamList, TcpNetPackage mPackage);
    }

    internal class CryptoMgr : NetStreamEncryptionInterface
    {
        readonly NetStreamEncryptionInterface mNetStreamEncryption = null;
        readonly Config mConfig;
        public CryptoMgr(Config mConfig)
        {
            this.mConfig = mConfig;
            mNetStreamEncryption = new NetStreamEncryption();
        }

        public Memory<byte> Encode(ushort nPackageId, ReadOnlySpan<byte> mBufferSegment)
        {
#if DEBUG
            if (mBufferSegment.Length > Config.nDataMaxLength)
            {
                NetLog.LogError("发送尺寸超出最大限制" + mBufferSegment.Length + " | " + Config.nDataMaxLength);
            }
#endif
            return mNetStreamEncryption.Encode(nPackageId, mBufferSegment);
        }

        public bool Decode(AkCircularBuffer mReceiveStreamList, TcpNetPackage mPackage)
        {
            return mNetStreamEncryption.Decode(mReceiveStreamList, mPackage);
        }
    }
}
