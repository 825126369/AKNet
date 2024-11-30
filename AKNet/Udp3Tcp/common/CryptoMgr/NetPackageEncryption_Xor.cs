/************************************Copyright*****************************************
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
		private byte[] mCheck = new byte[4] { (byte)'$', (byte)'$', (byte)'$', (byte)'$' };

		public NetPackageEncryption_Xor(XORCrypto mCryptoInterface)
		{
			this.mCryptoInterface = mCryptoInterface;
		}

		public bool Decode(ReadOnlySpan<byte> mBuff, NetUdpFixedSizePackage mPackage)
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
			
			mPackage.nRequestOrderId = BitConverter.ToUInt16(mBuff.Slice(8, 2));
            mPackage.nSureOrderId = BitConverter.ToUInt16(mBuff.Slice(10, 2));
            ushort nBodyLength = BitConverter.ToUInt16(mBuff.Slice(12, 2));

            if (Config.nUdpPackageFixedHeadSize + nBodyLength > Config.nUdpPackageFixedSize)
            {
                return false;
            }

            mPackage.CopyFrom(mBuff.Slice(Config.nUdpPackageFixedHeadSize, nBodyLength));
			return true;
		}

		public bool InnerCommandPeek(ReadOnlySpan<byte> mBuff, InnectCommandPeekPackage mPackage)
		{
			if (mBuff.Length < Config.nUdpPackageFixedHeadSize)
			{
				return false;
			}

			uint nOrderId = BitConverter.ToUInt32(mBuff.Slice(4, 4));
			byte nEncodeToken = (byte)nOrderId;
			for (int i = 0; i < 4; i++)
			{
				if (mBuff[i] != mCryptoInterface.Encode(i, mCheck[i], nEncodeToken))
				{
					return false;
				}
			}

			ushort nBodyLength = BitConverter.ToUInt16(mBuff.Slice(12, 2));
			if (nBodyLength != 0)
			{
				return false;
			}

			mPackage.mPackageId = (ushort)nOrderId;
			mPackage.Length = Config.nUdpPackageFixedHeadSize;
			return true;
		}

		public void Encode(NetUdpFixedSizePackage mPackage)
		{
			uint nOrderId = mPackage.nOrderId;
			ushort nRequestOrderId = mPackage.nRequestOrderId;
			ushort nSureOrderId = mPackage.nSureOrderId;
            ushort nBodyLength = (ushort)(mPackage.Length - Config.nUdpPackageFixedHeadSize);

            byte nEncodeToken = (byte)nOrderId;
			for (int i = 0; i < 4; i++)
			{
				mPackage.buffer[i] = mCryptoInterface.Encode(i, mCheck[i], nEncodeToken);
			}

			byte[] byCom = BitConverter.GetBytes(nOrderId);
			Array.Copy(byCom, 0, mPackage.buffer, 4, byCom.Length);

			byCom = BitConverter.GetBytes(nRequestOrderId);
			Array.Copy(byCom, 0, mPackage.buffer, 8, byCom.Length);

			byCom = BitConverter.GetBytes(nSureOrderId);
			Array.Copy(byCom, 0, mPackage.buffer, 10, byCom.Length);
			
			byCom = BitConverter.GetBytes(nBodyLength);
			Array.Copy(byCom, 0, mPackage.buffer, 12, byCom.Length);
		}

	}
}
