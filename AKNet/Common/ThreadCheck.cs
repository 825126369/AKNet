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
        static readonly int nMainThreadId = Thread.CurrentThread.ManagedThreadId;

        public static bool orInMainThread()
        {
#if DEBUG
            return Thread.CurrentThread.ManagedThreadId == nMainThreadId;
#else
            return true;
#endif
        }
        
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
