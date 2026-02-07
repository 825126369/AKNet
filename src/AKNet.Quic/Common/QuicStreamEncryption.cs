/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:27:01
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Runtime.CompilerServices;

namespace AKNet.Common
{
    internal class QuicStreamEncryption
    {
		private const int nPackageFixedHeadSize = 11;
        private static readonly byte[] mCheck = new byte[5] { (byte)'A', (byte)'K', (byte)'N', (byte)'E', (byte)'T' };
		private byte[] mCacheSendBuffer = new byte[1024];
		private byte[] mCacheReceiveBuffer = new byte[1024];
        private byte[] mCacheHead = new byte[nPackageFixedHeadSize];

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

		public bool Decode(NetStreamCircularBuffer mReceiveStreamList, QuicStreamReceivePackage mPackage)
		{
			if (mReceiveStreamList.Length < nPackageFixedHeadSize)
			{
				return false;
			}

            int nHeadLength = mReceiveStreamList.CopyTo(mCacheHead);
            byte nEncodeToken = mCacheHead[0];
			for (int i = 0; i < 5 ; i++)
			{
				if (mCacheHead[i + 1] != XORCrypto.Encode(i, mCheck[i], nEncodeToken))
				{
					return false;
				}
			}

            ushort nPackageId = EndianBitConverter.ToUInt16(mCacheHead, 6);
            ushort nBodyLength = EndianBitConverter.ToUInt16(mCacheHead, 8);
            byte nStreamIndex = mCacheHead[10];
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
			mPackage.nStreamIndex = nStreamIndex;
            mPackage.SetData(new Memory<byte>(mCacheReceiveBuffer, 0, nBodyLength));
			return true;
		}

		public ReadOnlySpan<byte> Encode(byte nSendStreamIndex, ushort nPackageId, ReadOnlySpan<byte> mBufferSegment)
		{
			int nSumLength = mBufferSegment.Length + nPackageFixedHeadSize;
			EnSureSendBufferOk(nSumLength);

			byte nEncodeToken = (byte)RandomTool.RandomInt32(0, byte.MaxValue);
			mCacheSendBuffer[0] = nEncodeToken;
			for (int i = 0; i < 5; i++)
			{
				mCacheSendBuffer[i + 1] = XORCrypto.Encode(i, mCheck[i], nEncodeToken);
			}

			EndianBitConverter.SetBytes(mCacheSendBuffer, 6, (ushort)nPackageId);
			EndianBitConverter.SetBytes(mCacheSendBuffer, 8, (ushort)mBufferSegment.Length);
            mCacheSendBuffer[10] = nSendStreamIndex;

            Span<byte> mCacheSendBufferSpan = mCacheSendBuffer.AsSpan();
			if (mBufferSegment.Length > 0)
			{
				mBufferSegment.CopyTo(mCacheSendBufferSpan.Slice(nPackageFixedHeadSize));
			}
			return mCacheSendBufferSpan.Slice(0, nSumLength);
		}

	}
}
