/************************************Copyright*****************************************
*        ProjectName:XKNet
*        Web:https://github.com/825126369/XKNet
*        Description:XKNet 网络库, 兼容 C#8.0 和 .Net Standard 2.1
*        Author:阿珂
*        CreateTime:2024/10/30 12:14:19
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using XKNet.Common;

namespace XKNet.Udp.BROADCAST.COMMON
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

