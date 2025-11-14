/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:26:53
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using AKNet.Common;

namespace AKNet.Udp.BROADCAST.COMMON
{
    /// <summary>
    /// 把数据拿出来
    /// </summary>
    internal static class NetPackageEncryption
	{
		private static byte[] mCheck = new byte[4] { (byte)'A', (byte)'B', (byte)'C', (byte)'D' };
	   
		public static bool DeEncryption (NetUdpFixedSizePackage mPackage)
		{
			if (mPackage.Length < Config.nUdpPackageFixedHeadSize) {
				NetLog.LogError ("mPackage Length： " + mPackage.Length);
				return false;
			}

			for (int i = 0; i < 4; i++) {
				if (mPackage.buffer [i] != mCheck [i]) {
					NetLog.LogError ("22222222222222222222222222");
					return false;
				}
			}
				
			mPackage.nPackageId = BitConverter.ToUInt16 (mPackage.buffer, 4);
			return true;
		}

		public static void Encryption (NetUdpFixedSizePackage mPackage)
		{
			UInt16 nPackageId = mPackage.nPackageId;
			Array.Copy (mCheck, 0, mPackage.buffer, 0, 4);
			byte[] byCom = BitConverter.GetBytes (nPackageId);
			Array.Copy (byCom, 0, mPackage.buffer, 4, byCom.Length);
		}
	}
}
