namespace AKNet.Common
{
    public class ConfigInstance
    {
        public int MaxPlayerCount = 10000;
        public bool bAutoReConnect = true;

        public ConfigInstance() 
        {
            bAutoReConnect = true;
            MaxPlayerCount = 10000;
        }
    }

    internal static class CommonTcpLayerConfig
    {
        public const int nIOContexBufferLength = 1024;
        public const int nDataMaxLength = ushort.MaxValue;
        public const double fReceiveHeartBeatTimeOut = 5.0;
        public const double fSendHeartBeatMaxTime = 2.0;
        public const double fReConnectMaxCdTime = 3.0;
    }

    internal static class CommonUdpLayerConfig
    {
        public const int nUdpPackageFixedSize = 1400;
    }
}
