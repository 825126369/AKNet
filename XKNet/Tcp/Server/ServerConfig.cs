namespace XKNet.Tcp.Server
{
	internal class ServerConfig
	{
		public const int numConnections = 10000;
		public const int nBufferMinLength = 64;
		public const int nBufferMaxLength = 4096;
		public const int nIOContexBufferLength = 4096;
		public const double fSendHeartBeatMaxTimeOut = 1.0; // 心跳时间
		public const double fReceiveHeartBeatMaxTimeOut = 5.0; //超时时间
	}
}
