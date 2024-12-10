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

namespace AKNet.Udp2Tcp.Common
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

			mPackage.nOrderId = EndianBitConverter.ToUInt16(mBuff.Slice(4));
			byte nEncodeToken = (byte)mPackage.nOrderId;
			for (int i = 0; i < 4; i++)
			{
				if (mBuff[i] != mCryptoInterface.Encode(i, mCheck[i], nEncodeToken))
				{
					return false;
				}
			}
			
			mPackage.nRequestOrderId = EndianBitConverter.ToUInt16(mBuff.Slice(6));
            ushort nBodyLength = EndianBitConverter.ToUInt16(mBuff.Slice(8));

            if (Config.nUdpPackageFixedHeadSize + nBodyLength > Config.nUdpPackageFixedSize)
            {
                return false;
            }

            mPackage.CopyFrom(mBuff.Slice(Config.nUdpPackageFixedHeadSize, nBodyLength));
			return true;
		}

		public void Encode(NetUdpFixedSizePackage mPackage)
		{
			ushort nOrderId = mPackage.nOrderId;
			ushort nRequestOrderId = mPackage.nRequestOrderId;
            ushort nBodyLength = (ushort)(mPackage.Length - Config.nUdpPackageFixedHeadSize);

            byte nEncodeToken = (byte)nOrderId;
			for (int i = 0; i < 4; i++)
			{
				mPackage.buffer[i] = mCryptoInterface.Encode(i, mCheck[i], nEncodeToken);
			}

            EndianBitConverter.SetBytes(mPackage.buffer, 4, nOrderId);
            EndianBitConverter.SetBytes(mPackage.buffer, 6, nRequestOrderId);
            EndianBitConverter.SetBytes(mPackage.buffer, 8, nBodyLength);
        }

	}
}
