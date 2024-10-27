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
                NetLog.LogError($"Main Thread Id: {nMainThreadId}, Now Thread Id: {nThreadId}");
            }
#endif
        }
    }
}
