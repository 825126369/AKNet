/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/29 4:33:42
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;

namespace AKNet.Udp.POINTTOPOINT.Common
{
    public class Config
	{
        public const bool bUdpCheck = true;
        public const bool bUseSocketLock = false;
        public const bool bUseSendAsync = true;
        public const bool bUseSendStream = true;
        public const bool bSocketSendMultiPackage = true;

        public const int nUseFakeSocketMgrType = 2;
        public const bool bFakeSocketManageConnectState = false;

        public const ushort nUdpMinOrderId = 1;
		public const ushort nUdpMaxOrderId = ushort.MaxValue;
		public const int nUdpPackageFixedSize = 1024;
		public const int nUdpPackageFixedHeadSize = 14;
        public const int nUdpPackageFixedBodySize = nUdpPackageFixedSize - nUdpPackageFixedHeadSize;
        public const int nMaxDataLength = ushort.MaxValue;

		public double fReceiveHeartBeatTimeOut = 5.0;
		public double fMySendHeartBeatMaxTime = 2.0;
        public double fReConnectMaxCdTime = 3.0;
        public int MaxPlayerCount = 10000;
	}
}
