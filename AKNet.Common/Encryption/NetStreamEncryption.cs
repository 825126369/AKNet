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
using System.Runtime.CompilerServices;

namespace AKNet.Common
{
    internal class NetStreamEncryption:NetStreamEncryptionInterface
    {
        private const int nPackageFixedHeadSize = 8;
        private byte[] mCheck = new byte[4] { (byte)'$', (byte)'$', (byte)'$', (byte)'$' };
		private byte[] mCacheSendBuffer = new byte[1024];
		private byte[] mCacheReceiveBuffer = new byte[1024];
        private byte[] mCacheHead = new byte[nPackageFixedHeadSize];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnSureSendBufferOk(int nSumLength)
		{
			BufferTool.EnSureBufferOk(ref mCacheSendBuffer, nSumLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnSureReceiveBufferOk(int nSumLength)
        {
            BufferTool.EnSureBufferOk(ref mCacheReceiveBuffer, nSumLength);
        }

		public bool Decode(NetStreamCircularBuffer mReceiveStreamList, NetStreamPackage mPackage)
		{
			if (mReceiveStreamList.Length < nPackageFixedHeadSize)
			{
                //NetLog.Log("解码失败 1111111111111111111: " + mReceiveStreamList.Length);
                return false;
			}

            int nHeadLength = mReceiveStreamList.CopyTo(mCacheHead);

            for (int i = 0; i < 4; i++)
			{
				if (mCacheHead[i] != mCheck[i])
				{
                    //NetLog.Log("解码失败 2222222222222");
                    return false;
				}
			}

            ushort nPackageId = EndianBitConverter.ToUInt16(mCacheHead, 4);
			int nBodyLength = EndianBitConverter.ToUInt16(mCacheHead, 6);
			NetLog.Assert(nBodyLength >= 0);

			int nSumLength = nBodyLength + nPackageFixedHeadSize;
			if (!mReceiveStreamList.isCanWriteTo(nSumLength))
			{
                //NetLog.Log("解码失败 3333333333333: " + mReceiveStreamList.Length + " | " + nSumLength);
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

			Buffer.BlockCopy(mCheck, 0, mCacheSendBuffer, 0, 4);
            EndianBitConverter.SetBytes(mCacheSendBuffer, 4, nPackageId);
            EndianBitConverter.SetBytes(mCacheSendBuffer, 6, (ushort)mBufferSegment.Length);

            Span<byte> mCacheSendBufferSpan = mCacheSendBuffer.AsSpan();
            mBufferSegment.CopyTo(mCacheSendBufferSpan.Slice(nPackageFixedHeadSize));
			return mCacheSendBufferSpan.Slice(0, nSumLength);
		}

	}
}
