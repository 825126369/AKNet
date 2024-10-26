namespace XKNet.Tcp.Common
{
    internal static class Config
    {
        //Common
        public const int nPackageFixedHeadSize = 8;

        public const int nSendReceiveCacheBufferInitLength = 1024 * 24;
        public const int nMsgPackageBufferMaxLength = 1024 * 8;
        public const int nIOContexBufferLength = 1024;
        //Client

        public const double fSendHeartBeatMaxTimeOut = 2.0;
        public const double fReceiveHeartBeatMaxTimeOut = 5.0;
        public const double fReceiveReConnectMaxTimeOut = 2.0;
        //Server
        public const int numConnections = 10000;
    }
}
