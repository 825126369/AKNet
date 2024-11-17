/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/17 12:39:35
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using AKNet.Common;

namespace AKNet.Tcp.Common
{
    internal static class NetPackageEncryption
	{
        private static byte[] mCheck = new byte[4] { (byte)'$', (byte)'$', (byte)'$', (byte)'$' };
		private static byte[] mCacheSendBuffer = new byte[Config.nIOContexBufferLength];
		private static byte[] mCacheReceiveBuffer = new byte[Config.nIOContexBufferLength];

		private static void EnSureSendBufferOk(int nSumLength)
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

        private static void EnSureReceiveBufferOk(int nSumLength)
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

        public static bool DeEncryption(AkCircularBuffer<byte> mReceiveStreamList, TcpNetPackage mPackage)
		{
			if(AKNetConfig.TcpConfig != null && AKNetConfig.TcpConfig.NetPackageEncryptionInterface != null)
			{
				return AKNetConfig.TcpConfig.NetPackageEncryptionInterface.DeEncryption(mReceiveStreamList, mPackage);
            }

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
            if (!mReceiveStreamList.isCanWriteTo(nSumLength))
			{
				return false;
			}

            EnSureReceiveBufferOk(nSumLength);
            mReceiveStreamList.WriteTo(0, mCacheReceiveBuffer, 0, nSumLength);

			mPackage.nPackageId = nPackageId;
			mPackage.mBuffer = mCacheReceiveBuffer;
            mPackage.mLength = nSumLength;
			return true;
		}

		public static ReadOnlySpan<byte> Encryption(int nPackageId, ReadOnlySpan<byte> mBufferSegment)
		{
            if (AKNetConfig.TcpConfig != null && AKNetConfig.TcpConfig.NetPackageEncryptionInterface != null)
            {
                return AKNetConfig.TcpConfig.NetPackageEncryptionInterface.Encryption(nPackageId, mBufferSegment);
            }

            int nSumLength = mBufferSegment.Length + Config.nPackageFixedHeadSize;
			EnSureSendBufferOk(nSumLength);

			Array.Copy(mCheck, mCacheSendBuffer, 4);
			mCacheSendBuffer[4] = (byte)nPackageId;
			mCacheSendBuffer[5] = (byte)(nPackageId >> 8);
			mCacheSendBuffer[6] = (byte)mBufferSegment.Length;
			mCacheSendBuffer[7] = (byte)(mBufferSegment.Length >> 8);

			for (int i = 0; i < mBufferSegment.Length; i++)
			{
				mCacheSendBuffer[Config.nPackageFixedHeadSize + i] = mBufferSegment[i];
			}
			return new ReadOnlySpan<byte>(mCacheSendBuffer, 0, nSumLength);
		}

	}
}
