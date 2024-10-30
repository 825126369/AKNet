/************************************Copyright*****************************************
*        ProjectName:XKNet
*        Web:https://github.com/825126369/XKNet
*        Description:XKNet 网络库, 兼容 C#8.0 和 .Net Standard 2.1
*        Author:阿珂
*        CreateTime:2024/10/30 12:14:19
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using XKNet.Common;

namespace XKNet.Udp.POINTTOPOINT.Common
{
	internal static class Config
	{
		//Udp Package OrderId
		public const ushort nUdpMinOrderId = 1;
		public const ushort nUdpMaxOrderId = ushort.MaxValue;

		public const int nUdpPackageFixedSize = 1024;
		public const int nUdpPackageFixedHeadSize = 12;
		public const int nUdpPackageFixedBodySize = nUdpPackageFixedSize - nUdpPackageFixedHeadSize;
		public const int nUdpCombinePackageInitSize = 1024 * 8; //合并包是可变的
		public const int nMsgPackageBufferMaxLength = 1024 * 8 - nUdpPackageFixedHeadSize;

		public const double fReceiveHeartBeatTimeOut = 5.0;
		public const double fMySendHeartBeatMaxTime = 2.0;

		//Server
		public const int numConnections = 10000;

		static Config()
		{
			NetLog.Assert(nUdpMaxOrderId - nUdpMinOrderId >= 1024);
		}
	}
}
