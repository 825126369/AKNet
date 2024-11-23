/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/17 12:39:35
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;

namespace AKNet.Tcp.Common
{
    internal class NetStreamEncryption1 : NetStreamEncryptionInterface
    {
		private const int nPackageFixedHeadSize = 8;
        readonly XORCrypto mCryptoInterface = null;
        private byte[] mCheck = new byte[4] { (byte)'$', (byte)'$', (byte)'$', (byte)'$' };
		private byte[] mCacheSendBuffer = new byte[ReadonlyConfig.nIOContexBufferLength];
		private byte[] mCacheReceiveBuffer = new byte[ReadonlyConfig.nIOContexBufferLength];

		public NetStreamEncryption1(XORCrypto mCryptoInterface)
		{
			this.mCryptoInterface = mCryptoInterface;
        }

        private void EnSureSendBufferOk(int nSumLength)
		{
            BufferTool.EnSureBufferOk(ref mCacheSendBuffer, nSumLength);
        }

        private void EnSureReceiveBufferOk(int nSumLength)
        {
            BufferTool.EnSureBufferOk(ref mCacheReceiveBuffer, nSumLength);
        }

		public bool Decode(AkCircularBuffer<byte> mReceiveStreamList, TcpNetPackage mPackage)
		{
			if (mReceiveStreamList.Length < nPackageFixedHeadSize)
			{
				return false;
			}

			for (int i = 0; i < 4; i++)
			{
				if (mReceiveStreamList[i] != mCryptoInterface.Encode(i, mCheck[i]))
				{
					return false;
				}
			}

			ushort nPackageId = (ushort)(mReceiveStreamList[4] | mReceiveStreamList[5] << 8);
			int nBodyLength = mReceiveStreamList[6] | mReceiveStreamList[7] << 8;
			NetLog.Assert(nBodyLength >= 0);

			int nSumLength = nBodyLength + nPackageFixedHeadSize;
			if (!mReceiveStreamList.isCanWriteTo(nSumLength))
			{
				return false;
			}

			EnSureReceiveBufferOk(nBodyLength);

			Span<byte> mCacheReceiveBufferSpan = mCacheReceiveBuffer.AsSpan();
			mReceiveStreamList.WriteTo(0, mCacheReceiveBufferSpan.Slice(0, nBodyLength));

			mPackage.nPackageId = nPackageId;
			mPackage.mBuffer = mCacheReceiveBuffer;
			mPackage.nLength = nSumLength;
			return true;
		}

		public ReadOnlySpan<byte> Encode(int nPackageId, ReadOnlySpan<byte> mBufferSegment)
		{
			int nSumLength = mBufferSegment.Length + nPackageFixedHeadSize;
			EnSureSendBufferOk(nSumLength);

			for (int i = 0; i < mCheck.Length; i++)
			{
				mCacheSendBuffer[i] = mCryptoInterface.Encode(i, mCheck[i]);
			}

			mCacheSendBuffer[4] = (byte)nPackageId;
			mCacheSendBuffer[5] = (byte)(nPackageId >> 8);
			mCacheSendBuffer[6] = (byte)mBufferSegment.Length;
			mCacheSendBuffer[7] = (byte)(mBufferSegment.Length >> 8);

			Span<byte> mCacheSendBufferSpan = mCacheSendBuffer.AsSpan();
			mBufferSegment.CopyTo(mCacheSendBufferSpan.Slice(nPackageFixedHeadSize));
			return mCacheSendBufferSpan;
		}

	}
}
