﻿/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/17 12:39:36
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;

namespace AKNet.Udp.POINTTOPOINT.Common
{
    /// <summary>
    /// 把数据拿出来
    /// </summary>
    internal class NetPackageEncryption: NetPackageEncryptionInterface
    {
        private const int nPackageFixedHeadSize = 14;
        private readonly byte[] mCheck = new byte[4] { (byte)'A', (byte)'B', (byte)'C', (byte)'D' };

		public bool Decode(NetUdpFixedSizePackage mPackage)
		{
            if (mPackage.Length < Config.nUdpPackageFixedHeadSize)
            {
                return false;
            }

            for (int i = 0; i < 4; i++)
			{
				if (mPackage.buffer[i] != mCheck[i])
				{
					return false;
				}
			}

			mPackage.nOrderId = BitConverter.ToUInt16(mPackage.buffer, 4);
			mPackage.nGroupCount = BitConverter.ToUInt16(mPackage.buffer, 6);
			mPackage.nPackageId = BitConverter.ToUInt16(mPackage.buffer, 8);
			mPackage.nRequestOrderId = BitConverter.ToUInt16(mPackage.buffer, 10);
            return true;
		}

        public bool Decode(ReadOnlySpan<byte> mBuff, NetUdpFixedSizePackage mPackage)
        {
            if (mBuff.Length < Config.nUdpPackageFixedHeadSize)
            {
                return false;
            }

            for (int i = 0; i < 4; i++)
            {
                if (mBuff[i] != mCheck[i])
                {
                    return false;
                }
            }
            
            mPackage.nOrderId = BitConverter.ToUInt16(mBuff.Slice(4, 2));
            mPackage.nGroupCount = BitConverter.ToUInt16(mBuff.Slice(6, 2));
            mPackage.nPackageId = BitConverter.ToUInt16(mBuff.Slice(8, 2));
            mPackage.nRequestOrderId = BitConverter.ToUInt16(mBuff.Slice(10, 2));
            ushort nBodyLength = BitConverter.ToUInt16(mBuff.Slice(12, 2));

            mPackage.CopyFrom(mBuff.Slice(Config.nUdpPackageFixedHeadSize, nBodyLength));
            return true;
        }

        public void Encode(NetUdpFixedSizePackage mPackage)
        {
            ushort nOrderId = mPackage.nOrderId;
            ushort nGroupCount = mPackage.nGroupCount;
            ushort nPackageId = mPackage.nPackageId;
            ushort nSureOrderId = mPackage.nRequestOrderId;

            Array.Copy(mCheck, 0, mPackage.buffer, 0, 4);

            byte[] byCom = BitConverter.GetBytes(nOrderId);
            Array.Copy(byCom, 0, mPackage.buffer, 4, byCom.Length);
            byCom = BitConverter.GetBytes(nGroupCount);
            Array.Copy(byCom, 0, mPackage.buffer, 6, byCom.Length);
            byCom = BitConverter.GetBytes(nPackageId);
            Array.Copy(byCom, 0, mPackage.buffer, 8, byCom.Length);
            byCom = BitConverter.GetBytes(nSureOrderId);
            Array.Copy(byCom, 0, mPackage.buffer, 10, byCom.Length);

            if (Config.bSocketSendMultiPackage)
            {
                ushort nBodyLength = (ushort)(mPackage.Length - Config.nUdpPackageFixedHeadSize);
                byCom = BitConverter.GetBytes(nBodyLength);
                Array.Copy(byCom, 0, mPackage.buffer, 12, byCom.Length);
            }
        }

	}
}