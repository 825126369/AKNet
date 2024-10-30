/************************************Copyright*****************************************
*        ProjectName:XKNet
*        Web:https://github.com/825126369/XKNet
*        Description:XKNet 网络库, 兼容 C#8.0 和 .Net Standard 2.1
*        Author:阿珂
*        CreateTime:2024/10/30 12:14:19
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
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
