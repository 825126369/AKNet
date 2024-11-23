/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/17 12:39:35
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Text;
using AKNet.Common;

namespace AKNet.Tcp.Common
{
    internal class NetStreamEncryption2: NetStreamEncryptionInterface
    {
        private const int nPackageFixedHeadSize = 8;
        private readonly int nCryptoHeadLength = 16;

        readonly NetPackageCryptoInterface mCryptoInterface;
        private byte[] mCheck = new byte[4] { (byte)'$', (byte)'$', (byte)'$', (byte)'$' };
		private byte[] mCacheSendBuffer = new byte[Config.nIOContexBufferLength];
		private byte[] mCacheReceiveBuffer = new byte[Config.nIOContexBufferLength];

        public NetStreamEncryption2(int nCryptoHeadLength, NetPackageCryptoInterface mCryptoInterface)
        {
            this.nCryptoHeadLength = nCryptoHeadLength;
            this.mCryptoInterface = mCryptoInterface;
        }

        private void EnSureSendBufferOk(int nSumLength)
		{
            if (mCacheSendBuffer.Length < nSumLength)
            {
                byte[] mOldBuffer = mCacheSendBuffer;
                int newSize = mOldBuffer.Length * 2;
                while (newSize < nSumLength)
                {
                    newSize *= 2;
                }
                mCacheSendBuffer = new byte[newSize];
            }
        }

        private void EnSureReceiveBufferOk(int nSumLength)
        {
            if (mCacheReceiveBuffer.Length < nSumLength)
            {
                byte[] mOldBuffer = mCacheReceiveBuffer;
                int newSize = mOldBuffer.Length * 2;
                while (newSize < nSumLength)
                {
                    newSize *= 2;
                }

                mCacheReceiveBuffer = new byte[newSize];
            }
        }

        public bool Decode(AkCircularBuffer<byte> mReceiveStreamList, TcpNetPackage mPackage)
        {
            if (mReceiveStreamList.Length < nCryptoHeadLength)
            {
                return false;
            }

            Span<byte> mCacheReceiveBufferSpan = mCacheReceiveBuffer.AsSpan();
            Span<byte> mCacheReceiveBufferSpanHead = mCacheReceiveBufferSpan.Slice(0, nCryptoHeadLength);
            mReceiveStreamList.WriteTo(0, mCacheReceiveBufferSpanHead);
            var mHeadResult = mCryptoInterface.Decode(mCacheReceiveBufferSpanHead);

            if (mHeadResult.Length < nPackageFixedHeadSize)
            {
                return false;
            }

            for (int i = 0; i < 4; i++)
            {
                if (mHeadResult[i] != mCheck[i])
                {
                    return false;
                }
            }

            ushort nPackageId = (ushort)(mHeadResult[4] | mHeadResult[5] << 8);
            int nBodyLength = mHeadResult[6] | mHeadResult[7] << 8;
            NetLog.Assert(nBodyLength >= 0);

            int nSumLength = nBodyLength + nPackageFixedHeadSize;
            if (!mReceiveStreamList.isCanWriteTo(nSumLength))
            {
                return false;
            }

            EnSureReceiveBufferOk(nBodyLength);
            mReceiveStreamList.WriteTo(nCryptoHeadLength, mCacheReceiveBufferSpan.Slice(0, nBodyLength));
            mPackage.nPackageId = nPackageId;
            mPackage.mBuffer = mCacheReceiveBuffer;
            mPackage.nLength = nBodyLength;
            return true;
        }

		public ReadOnlySpan<byte> Encode(int nPackageId, ReadOnlySpan<byte> mBufferSegment)
		{
			Array.Copy(mCheck, mCacheSendBuffer, 4);
			mCacheSendBuffer[4] = (byte)nPackageId;
			mCacheSendBuffer[5] = (byte)(nPackageId >> 8);
			mCacheSendBuffer[6] = (byte)mBufferSegment.Length;
			mCacheSendBuffer[7] = (byte)(mBufferSegment.Length >> 8);
			
			Span<byte> mCacheSendBufferSpan = mCacheSendBuffer.AsSpan();
            var mHeadResult = mCryptoInterface.Encode(mCacheSendBufferSpan.Slice(0, nPackageFixedHeadSize));
            NetLog.Log("mHeadResult: " + mHeadResult.Length + " | " + Encoding.UTF8.GetString(mHeadResult));
            NetLog.Assert(mHeadResult.Length == nCryptoHeadLength);

            EnSureSendBufferOk(mBufferSegment.Length + mHeadResult.Length);
            mBufferSegment.CopyTo(mCacheSendBufferSpan.Slice(mHeadResult.Length));
			return mCacheSendBufferSpan;
		}
	}
}
