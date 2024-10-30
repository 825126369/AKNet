/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:AKNet 网络库, 兼容 C#8.0 和 .Net Standard 2.1
*        Author:阿珂
*        CreateTime:2024/10/30 21:55:40
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Common
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
