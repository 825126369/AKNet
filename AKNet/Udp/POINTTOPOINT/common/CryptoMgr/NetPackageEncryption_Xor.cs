/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;

namespace AKNet.Udp.POINTTOPOINT.Common
{
	internal class NetPackageEncryption_Xor : NetPackageEncryptionInterface
	{
		readonly XORCrypto mCryptoInterface = null;
		private byte[] mCheck = new byte[4] { (byte)'$', (byte)'$', (byte)'$', (byte)'$' };

		public NetPackageEncryption_Xor(XORCrypto mCryptoInterface)
		{
			this.mCryptoInterface = mCryptoInterface;
		}

		public bool Decode(NetUdpFixedSizePackage mPackage)
		{
			if (mPackage.Length < Config.nUdpPackageFixedHeadSize)
			{
				return false;
			}

			mPackage.nOrderId = EndianBitConverter.ToUInt16(mPackage.buffer, 4);
			byte nEncodeToken = (byte)mPackage.nOrderId;
			for (int i = 0; i < 4; i++)
			{
				if (mPackage.buffer[i] != mCryptoInterface.Encode(i, mCheck[i], nEncodeToken))
				{
					return false;
				}
			}

			mPackage.nGroupCount = EndianBitConverter.ToUInt16(mPackage.buffer, 6);
			mPackage.nPackageId = EndianBitConverter.ToUInt16(mPackage.buffer, 8);
			mPackage.nRequestOrderId = EndianBitConverter.ToUInt16(mPackage.buffer, 10);
			return true;
		}

		public bool Decode(ReadOnlySpan<byte> mBuff, NetUdpFixedSizePackage mPackage)
		{
			if (mBuff.Length < Config.nUdpPackageFixedHeadSize)
			{
				return false;
			}

			mPackage.nOrderId = EndianBitConverter.ToUInt16(mBuff.Slice(4, 2));
			byte nEncodeToken = (byte)mPackage.nOrderId;
			for (int i = 0; i < 4; i++)
			{
				if (mBuff[i] != mCryptoInterface.Encode(i, mCheck[i], nEncodeToken))
				{
					return false;
				}
			}

			mPackage.nGroupCount = EndianBitConverter.ToUInt16(mBuff.Slice(6));
			mPackage.nPackageId = EndianBitConverter.ToUInt16(mBuff.Slice(8));
			mPackage.nRequestOrderId = EndianBitConverter.ToUInt16(mBuff.Slice(10));
			ushort nBodyLength = EndianBitConverter.ToUInt16(mBuff.Slice(12));

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

			mPackage.nOrderId = EndianBitConverter.ToUInt16(mBuff.Slice(4));
			byte nEncodeToken = (byte)mPackage.nOrderId;
			for (int i = 0; i < 4; i++)
			{
				if (mBuff[i] != mCryptoInterface.Encode(i, mCheck[i], nEncodeToken))
				{
					return false;
				}
			}

			ushort nBodyLength = EndianBitConverter.ToUInt16(mBuff.Slice(12));
			if (nBodyLength != 0)
			{
				return false;
			}

			mPackage.nPackageId = EndianBitConverter.ToUInt16(mBuff.Slice(8));
			mPackage.Length = Config.nUdpPackageFixedHeadSize;
			return true;
		}

        public void Encode(NetUdpFixedSizePackage mPackage)
		{
			ushort nOrderId = mPackage.nOrderId;
			ushort nGroupCount = mPackage.nGroupCount;
			ushort nPackageId = mPackage.nPackageId;
			ushort nSureOrderId = mPackage.nRequestOrderId;
            ushort nBodyLength = (ushort)(mPackage.Length - Config.nUdpPackageFixedHeadSize);

            byte nEncodeToken = (byte)nOrderId;
			for (int i = 0; i < 4; i++)
			{
				mPackage.buffer[i] = mCryptoInterface.Encode(i, mCheck[i], nEncodeToken);
			}

            EndianBitConverter.SetBytes(mPackage.buffer, 4, nOrderId);
            EndianBitConverter.SetBytes(mPackage.buffer, 6, nGroupCount);
            EndianBitConverter.SetBytes(mPackage.buffer, 8, nPackageId);
            EndianBitConverter.SetBytes(mPackage.buffer, 10, nSureOrderId);
            EndianBitConverter.SetBytes(mPackage.buffer, 12, nBodyLength);
        }
	}
}
