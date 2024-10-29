using System.Threading;

namespace XKNet.Common
{
    internal class ThreadCheck
    {
        readonly int nMainThreadId = Thread.CurrentThread.ManagedThreadId;
        public void Check()
        {
#if DEBUG
            int nThreadId = Thread.CurrentThread.ManagedThreadId;
            if (nThreadId != nMainThreadId)
            {
                NetLog.LogError($"ThreadCheck Id: {nMainThreadId}, {nThreadId}");
            }
#endif
        }
    }

    internal static class MainThreadCheck
    {
        static readonly int nMainThreadId = Thread.CurrentThread.ManagedThreadId;
        public static void Check()
        {
#if DEBUG
            int nThreadId = Thread.CurrentThread.ManagedThreadId;
            if (nThreadId != nMainThreadId)
            {
                NetLog.LogError($"MainThreadCheck: {nMainThreadId}, {nThreadId}");
            }
#endif
        }
    }
}
