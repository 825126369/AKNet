/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/4 20:04:54
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;

namespace AKNet.Udp.POINTTOPOINT.Common
{
    /// <summary>
    /// 把数据拿出来
    /// </summary>
    internal static class NetPackageEncryption
	{
		private static readonly byte[] mCheck = new byte[4] { (byte)'A', (byte)'B', (byte)'C', (byte)'D' };

		public static bool DeEncryption(NetUdpFixedSizePackage mPackage)
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
			mPackage.nSureOrderId = BitConverter.ToUInt16(mPackage.buffer, 10);
			return true;
		}

		public static void Encryption(NetUdpFixedSizePackage mPackage)
		{
			UInt16 nOrderId = mPackage.nOrderId;
			UInt16 nGroupCount = mPackage.nGroupCount;
			UInt16 nPackageId = mPackage.nPackageId;
			UInt16 nSureOrderId = mPackage.nSureOrderId;

			Array.Copy(mCheck, 0, mPackage.buffer, 0, 4);

			byte[] byCom = BitConverter.GetBytes(nOrderId);
			Array.Copy(byCom, 0, mPackage.buffer, 4, byCom.Length);
			byCom = BitConverter.GetBytes(nGroupCount);
			Array.Copy(byCom, 0, mPackage.buffer, 6, byCom.Length);
			byCom = BitConverter.GetBytes(nPackageId);
			Array.Copy(byCom, 0, mPackage.buffer, 8, byCom.Length);
			byCom = BitConverter.GetBytes(nSureOrderId);
			Array.Copy(byCom, 0, mPackage.buffer, 10, byCom.Length);
		}
	}
}
