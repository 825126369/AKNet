/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/28 7:14:05
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Runtime.CompilerServices;
using AKNet.Common;

namespace AKNet.Tcp.Common
{
    internal class NetStreamEncryption:NetStreamEncryptionInterface
    {
        private const int nPackageFixedHeadSize = 8;
        private byte[] mCheck = new byte[4] { (byte)'$', (byte)'$', (byte)'$', (byte)'$' };
		private byte[] mCacheSendBuffer = new byte[Config.nIOContexBufferLength];
		private byte[] mCacheReceiveBuffer = new byte[Config.nIOContexBufferLength];

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

		public bool Decode(AkCircularBuffer mReceiveStreamList, TcpNetPackage mPackage)
		{
			if (mReceiveStreamList.Length < nPackageFixedHeadSize)
			{
				return false;
			}

			for (int i = 0; i < 4; i++)
			{
				if (mReceiveStreamList[i] != mCheck[i])
				{
					return false;
				}
			}

			ushort nPackageId = EndianBitConverter.ToUInt16(mReceiveStreamList, 4);
			int nBodyLength = EndianBitConverter.ToUInt16(mReceiveStreamList, 6);
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
				mReceiveStreamList.WriteTo(0, mCacheReceiveBufferSpan.Slice(0, nBodyLength));
			}

			mPackage.nPackageId = nPackageId;
			mPackage.InitData(mCacheReceiveBuffer, 0, nBodyLength);
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
