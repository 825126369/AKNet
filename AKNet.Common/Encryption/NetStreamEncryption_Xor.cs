/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/29 4:33:36
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Runtime.CompilerServices;

namespace AKNet.Common
{
    internal class NetStreamEncryption_Xor : NetStreamEncryptionInterface
    {
		private const int nPackageFixedHeadSize = 9;
        readonly XORCrypto mCryptoInterface = null;
        private byte[] mCheck = new byte[4] { (byte)'$', (byte)'$', (byte)'$', (byte)'$' };
		private byte[] mCacheSendBuffer = new byte[1024];
		private byte[] mCacheReceiveBuffer = new byte[1024];
        private byte[] mCacheHead = new byte[nPackageFixedHeadSize];

        public NetStreamEncryption_Xor(XORCrypto mCryptoInterface)
		{
			this.mCryptoInterface = mCryptoInterface;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnSureSendBufferOk(int nSumLength)
		{
            BufferTool.EnSureBufferOk_Power2(ref mCacheSendBuffer, nSumLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnSureReceiveBufferOk(int nSumLength)
        {
            BufferTool.EnSureBufferOk_Power2(ref mCacheReceiveBuffer, nSumLength);
        }

		public bool Decode(NetStreamCircularBuffer mReceiveStreamList, NetStreamPackage mPackage)
		{
			if (mReceiveStreamList.Length < nPackageFixedHeadSize)
			{
				return false;
			}

            int nHeadLength = mReceiveStreamList.CopyTo(mCacheHead);
            byte nEncodeToken = mCacheHead[0];
			for (int i = 0; i < 4 ; i++)
			{
				if (mCacheHead[i + 1] != mCryptoInterface.Encode(i, mCheck[i], nEncodeToken))
				{
					return false;
				}
			}

            ushort nPackageId = EndianBitConverter.ToUInt16(mCacheHead, 5);
            int nBodyLength = EndianBitConverter.ToUInt16(mCacheHead, 7);
            NetLog.Assert(nBodyLength >= 0);

			int nSumLength = nBodyLength + nPackageFixedHeadSize;
			if (!mReceiveStreamList.isCanWriteTo(nSumLength))
			{
				return false;
			}

			mReceiveStreamList.ClearBuffer(nPackageFixedHeadSize);
			if (nBodyLength > 0)
			{
				EnSureReceiveBufferOk(nBodyLength);
				Span<byte> mCacheReceiveBufferSpan = mCacheReceiveBuffer.AsSpan();
				mReceiveStreamList.WriteTo(mCacheReceiveBufferSpan.Slice(0, nBodyLength));
			}

			mPackage.nPackageId = nPackageId;
			mPackage.SetData(new Memory<byte>(mCacheReceiveBuffer, 0, nBodyLength));
			return true;
		}

		public ReadOnlySpan<byte> Encode(ushort nPackageId, ReadOnlySpan<byte> mBufferSegment)
		{
			int nSumLength = mBufferSegment.Length + nPackageFixedHeadSize;
			EnSureSendBufferOk(nSumLength);

			byte nEncodeToken = (byte)RandomTool.Random(0, 255);
			mCacheSendBuffer[0] = nEncodeToken;
			for (int i = 0; i < 4; i++)
			{
				mCacheSendBuffer[i + 1] = mCryptoInterface.Encode(i, mCheck[i], nEncodeToken);
			}

			EndianBitConverter.SetBytes(mCacheSendBuffer, 5, (ushort)nPackageId);
			EndianBitConverter.SetBytes(mCacheSendBuffer, 7, (ushort)mBufferSegment.Length);

			Span<byte> mCacheSendBufferSpan = mCacheSendBuffer.AsSpan();
			if (mBufferSegment.Length > 0)
			{
				mBufferSegment.CopyTo(mCacheSendBufferSpan.Slice(nPackageFixedHeadSize));
			}
			return mCacheSendBufferSpan.Slice(0, nSumLength);
		}

	}
}
