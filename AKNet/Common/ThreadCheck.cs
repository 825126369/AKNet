/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/23 22:12:35
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Runtime.CompilerServices;
using System.Threading;

namespace AKNet.Common
{
    internal static class MainThreadCheck
    {
#if DEBUG
        static readonly int nMainThreadId = Thread.CurrentThread.ManagedThreadId;
        public static bool orInMainThread()
        {
            return Thread.CurrentThread.ManagedThreadId == nMainThreadId;
        }
        public static void Check()
        {
            int nThreadId = Thread.CurrentThread.ManagedThreadId;
            if (nThreadId != nMainThreadId)
            {
                NetLog.LogError($"MainThreadCheck: {nMainThreadId}, {nThreadId}");
            }
        }
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool orInMainThread()
        {
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Check()
        {
           
        }
#endif
    }

    internal class ThreadOnlyOneCheck
    {
        int nThreadCount = 0;
        public void Enter(string tag = "")
        {
            NetLog.Assert(nThreadCount == 0, $"{nThreadCount}个线程同时访问同一个代码块: {tag}");
            nThreadCount++;
        }

        public void Exit()
        {
            nThreadCount--;
        }
    }
}
