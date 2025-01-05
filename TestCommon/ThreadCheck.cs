/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:22
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Collections;
using System.Diagnostics;

namespace TestCommon
{
    public static class MainThreadCheck
    {
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
                Console.WriteLine($"MainThreadCheck Error: {nMainThreadId}, {nThreadId}\n {new StackTrace(true)}");
            }
        }
    }
}
