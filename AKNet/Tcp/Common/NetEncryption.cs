/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:AKNet 网络库, 兼容 C#8.0 和 .Net Standard 2.1
*        Author:阿珂
*        CreateTime:2024/10/30 21:55:40
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using AKNet.Common;

namespace AKNet.Tcp.Common
{
    /// <summary>
    /// 把数据拿出来
    /// </summary>
    internal static class NetPackageEncryption
	{
        private static byte[] mCheck = new byte[4] { (byte)'$', (byte)'$', (byte)'$', (byte)'$' };
		private static byte[] mCacheSendBuffer = new byte[Config.nMsgPackageBufferMaxLength];
		private static byte[] mCacheReceiveBuffer = new byte[Config.nMsgPackageBufferMaxLength];

		public static bool DeEncryption(CircularBuffer<byte> mReceiveStreamList, TcpNetPackage mPackage)
		{
			if (mReceiveStreamList.Length < Config.nPackageFixedHeadSize)
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

			ushort nPackageId = (ushort)(mReceiveStreamList[4] | mReceiveStreamList[5] << 8);
			int nBodyLength = mReceiveStreamList[6] | mReceiveStreamList[7] << 8;
			NetLog.Assert(nBodyLength >= 0);

			int nSumLength = nBodyLength + Config.nPackageFixedHeadSize;
			if (mReceiveStreamList.Length < nSumLength)
			{
				return false;
			}

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

			mReceiveStreamList.WriteTo(0, mCacheReceiveBuffer, 0, nSumLength);

			mPackage.nPackageId = nPackageId;
			mPackage.mBuffer = mCacheReceiveBuffer;
            mPackage.mLength = nSumLength;
			return true;
		}

		public static ReadOnlySpan<byte> Encryption(int nPackageId, ReadOnlySpan<byte> mBufferSegment)
		{
			int nSumLength = mBufferSegment.Length + Config.nPackageFixedHeadSize;
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

			Array.Copy(mCheck, mCacheSendBuffer, 4);
			mCacheSendBuffer[4] = (byte)nPackageId;
			mCacheSendBuffer[5] = (byte)(nPackageId >> 8);
			mCacheSendBuffer[6] = (byte)mBufferSegment.Length;
			mCacheSendBuffer[7] = (byte)(mBufferSegment.Length >> 8);

			for (int i = 0; i < mBufferSegment.Length; i++)
			{
				mCacheSendBuffer[Config.nPackageFixedHeadSize + i] = mBufferSegment[i];
			}

			return new ArraySegment<byte>(mCacheSendBuffer, 0, nSumLength);
		}

	}
}
