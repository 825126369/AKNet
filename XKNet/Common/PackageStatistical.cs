namespace XKNet.Common
{
    public static class PackageStatistical
    {
        static ulong nSendPackageCount = 0;
        static ulong nReceivePackageCount = 0;
        static ulong nSendBytesCount = 0;
        static ulong nReceiveBytesCount = 0;

        public static void Reset()
        {
            nSendPackageCount = 0;
            nReceivePackageCount = 0;
            nSendBytesCount = 0;
            nReceiveBytesCount = 0;
        }


        public static void AddSendPackageCount()
        {
            nSendPackageCount++;
        }

        public static void AddReceivePackageCount()
        {
            nReceivePackageCount++;
        }

        public static void AddSendBytesCount(int nBytesLength)
        {
            nSendBytesCount += (ulong)nBytesLength;
        }

        public static void AddReceiveBytesCount(int nBytesLength)
        {
            nReceiveBytesCount += (ulong)nBytesLength;
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
