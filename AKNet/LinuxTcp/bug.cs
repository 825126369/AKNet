using AKNet.Common;

namespace AKNet.LinuxTcp
{
    internal static partial class LinuxTcpFunc
    {
        public static void WARN_ON(bool condition)
        {
            if (!condition)
            {
                NetLog.LogWarning(condition);
            }
        }
    }
}
