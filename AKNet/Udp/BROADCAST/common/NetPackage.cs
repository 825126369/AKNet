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
    public class NetUdpFixedSizePackage: IPoolItemInterface
	{
		public UInt16 nPackageId;

		public byte[] buffer;
		public int Length;

		public NetUdpFixedSizePackage ()
		{
			nPackageId = 0;
			Length = 0;
			buffer = new byte[Config.nUdpPackageFixedSize];
		}

        public void Reset()
        {
			nPackageId = 0;
			Length = 0;
        }
    }
}

