﻿/************************************Copyright*****************************************
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
    internal class NetStreamEncryption_Xor : NetStreamEncryptionInterface
    {
		private const int nPackageFixedHeadSize = 9;
        readonly XORCrypto mCryptoInterface = null;
        private byte[] mCheck = new byte[4] { (byte)'$', (byte)'$', (byte)'$', (byte)'$' };
		private byte[] mCacheSendBuffer = new byte[Config.nIOContexBufferLength];
		private byte[] mCacheReceiveBuffer = new byte[Config.nIOContexBufferLength];

		public NetStreamEncryption_Xor(XORCrypto mCryptoInterface)
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

			byte nEncodeToken = mReceiveStreamList[0];
			for (int i = 0; i < 4 ; i++)
			{
				if (mReceiveStreamList[i + 1] != mCryptoInterface.Encode(i, mCheck[i], nEncodeToken))
				{
					return false;
				}
			}

			ushort nPackageId = (ushort)(mReceiveStreamList[5] | mReceiveStreamList[6] << 8);
			int nBodyLength = mReceiveStreamList[7] | mReceiveStreamList[8] << 8;
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

		public ReadOnlySpan<byte> Encode(int nPackageId, ReadOnlySpan<byte> mBufferSegment)
		{
			int nSumLength = mBufferSegment.Length + nPackageFixedHeadSize;
			EnSureSendBufferOk(nSumLength);

            byte nEncodeToken = (byte)RandomTool.Random(0, 255);
            mCacheSendBuffer[0] = nEncodeToken;
            for (int i = 0; i < 4; i++)
			{
                mCacheSendBuffer[i + 1] = mCryptoInterface.Encode(i, mCheck[i], nEncodeToken);
            }

			mCacheSendBuffer[5] = (byte)nPackageId;
			mCacheSendBuffer[6] = (byte)(nPackageId >> 8);
			mCacheSendBuffer[7] = (byte)mBufferSegment.Length;
			mCacheSendBuffer[8] = (byte)(mBufferSegment.Length >> 8);

			Span<byte> mCacheSendBufferSpan = mCacheSendBuffer.AsSpan();
			if (mBufferSegment.Length > 0)
			{
				mBufferSegment.CopyTo(mCacheSendBufferSpan.Slice(nPackageFixedHeadSize));
			}
			return mCacheSendBufferSpan.Slice(0, nSumLength);
		}

	}
}