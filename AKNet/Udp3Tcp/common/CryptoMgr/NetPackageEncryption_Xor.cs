﻿/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/28 7:14:07
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;

namespace AKNet.Udp3Tcp.Common
{
	internal class NetPackageEncryption_Xor : NetPackageEncryptionInterface
	{
		readonly XORCrypto mCryptoInterface = null;
		private readonly byte[] mCheck = new byte[4] { (byte)'$', (byte)'$', (byte)'$', (byte)'$' };

		public NetPackageEncryption_Xor(XORCrypto mCryptoInterface)
		{
			this.mCryptoInterface = mCryptoInterface;
		}

		public bool Decode(ReadOnlySpan<byte> mBuff, NetUdpReceiveFixedSizePackage mPackage)
		{
			if (mBuff.Length < Config.nUdpPackageFixedHeadSize)
			{
				return false;
			}

			mPackage.nOrderId = BitConverter.ToUInt32(mBuff.Slice(4, 4));
			byte nEncodeToken = (byte)mPackage.nOrderId;
			for (int i = 0; i < 4; i++)
			{
				if (mBuff[i] != mCryptoInterface.Encode(i, mCheck[i], nEncodeToken))
				{
					return false;
				}
			}

			mPackage.nRequestOrderId = BitConverter.ToUInt32(mBuff.Slice(8, 4));
			int nBodyLength = (int)mPackage.nRequestOrderId;

			if (Config.nUdpPackageFixedHeadSize + nBodyLength > Config.nUdpPackageFixedSize)
			{
				return false;
			}

			mPackage.CopyFrom(mBuff.Slice(Config.nUdpPackageFixedHeadSize, nBodyLength));
			return true;
		}


        private static readonly byte[] mCacheSendHeadBuffer = new byte[Config.nUdpPackageFixedHeadSize];
        public byte[] EncodeHead(NetUdpSendFixedSizePackage mPackage)
		{
            byte nPackageId = mPackage.nPackageId;
            uint nOrderId = mPackage.nOrderId;
			uint nRequestOrderId = mPackage.nRequestOrderId;
			
            byte nEncodeToken = (byte)(nOrderId % byte.MaxValue);
			for (int i = 0; i < 4; i++)
			{
                mCacheSendHeadBuffer[i] = mCryptoInterface.Encode(i, mCheck[i], nEncodeToken);
			}

            byte[] byCom = BitConverter.GetBytes(nOrderId);
            Array.Copy(byCom, 0, mCacheSendHeadBuffer, 4, byCom.Length);

            byCom = BitConverter.GetBytes(nRequestOrderId);
            Array.Copy(byCom, 0, mCacheSendHeadBuffer, 8, byCom.Length);

            mCacheSendHeadBuffer[12] = nPackageId;
            return mCacheSendHeadBuffer;
        }

	}
}