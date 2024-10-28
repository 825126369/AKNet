using System;

namespace XKNet.Udp.POINTTOPOINT.Common
{
	internal static class UdpNetCommand
    {
		public const ushort COMMAND_PACKAGECHECK = 1;
		public const ushort COMMAND_HEARTBEAT = 2;
		public const ushort COMMAND_CONNECT = 3;
		public const ushort COMMAND_DISCONNECT = 4;

		public const ushort COMMAND_TESTCHAT = 51;

		public static bool orNeedCheck(ushort id)
		{
			return !orInnerCommand(id);
		}

		public static bool orInnerCommand(ushort id)
		{
			return id >= 1 && id <= 10;
		}

        public static NetUdpFixedSizePackage GetUdpInnerCommandPackage(UInt16 id, ushort nOrderId = 0)
        {
            NetUdpFixedSizePackage mPackage = ObjectPoolManager.Instance.mUdpFixedSizePackagePool.Pop();
            mPackage.nOrderId = nOrderId;
            mPackage.nGroupCount = 0;
            mPackage.nPackageId = id;
            mPackage.Length = Config.nUdpPackageFixedHeadSize;
            NetPackageEncryption.Encryption(mPackage);
            return mPackage;
        }
    }
}
