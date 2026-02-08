/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:48
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Udp1Tcp.Common
{
    public static class Config
	{
        public const bool bUdpCheck = true;
        public const bool bUseSendAsync = true;
        public const bool bUseSendStream = true;
        public const bool bSocketSendMultiPackage = true;

        public const int nUseFakeSocketMgrType = 2;
        public const bool bFakeSocketManageConnectState = false;

        public const ushort nUdpMinOrderId = 1;
		public const ushort nUdpMaxOrderId = ushort.MaxValue;
		public const int nUdpPackageFixedSize = 1400;
		public const int nUdpPackageFixedHeadSize = 14;
        public const int nUdpPackageFixedBodySize = nUdpPackageFixedSize - nUdpPackageFixedHeadSize;
        public const int nMaxDataLength = ushort.MaxValue;

		public const double fReceiveHeartBeatTimeOut = 5.0;
		public const double fMySendHeartBeatMaxTime = 2.0;
        public const double fReConnectMaxCdTime = 3.0;
        public const int MaxPlayerCount = 10000;
	}
}
