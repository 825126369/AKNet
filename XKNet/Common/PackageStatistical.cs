namespace XKNet.Common
{
    public static class PackageStatistical
    {
        static ulong nSendPackageCount = 0;
        static ulong nReceivePackageCount = 0;
        static ulong nSendBytesCount = 0;
        static ulong nReceiveBytesCount = 0;
        
        public static void AddSendPackageCount()
        {
#if DEBUG
            nSendPackageCount++;
#endif
        }

        public static void AddReceivePackageCount()
        {
#if DEBUG
            nReceivePackageCount++;
#endif
        }

        public static void AddSendBytesCount(int nBytesLength)
        {
#if DEBUG
            nSendBytesCount += (ulong)nBytesLength;
#endif
        }

        public static void AddReceiveBytesCount(int nBytesLength)
        {
#if DEBUG
            nReceiveBytesCount += (ulong)nBytesLength;
#endif
        }

        private static double GetMBytes(ulong nBytesLength)
        {
            return nBytesLength / 1024.0 / 1024.0;
        }

        public static void PrintLog()
        {
            NetLog.Log($"PackageStatistical: {nSendPackageCount}, {nReceivePackageCount}, {GetMBytes(nSendBytesCount)}M, {GetMBytes(nReceiveBytesCount)}M");
        }
    }
}
