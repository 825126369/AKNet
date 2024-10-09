namespace XKNet.Tcp.Client
{
    internal class Config
	{
		public const int nPackageFixedHeadSize = 8;
		public const int nBufferInitLength = 32;
		public const int nBufferMaxLength = 4096;

		public const int nIOContexBufferLength = 4096;

		public const double fSendHeartBeatMaxTimeOut = 1.0;
		public const double fReceiveHeartBeatMaxTimeOut = 5.0;
		public const double fReceiveReConnectMaxTimeOut = 2.0;
	}
}
