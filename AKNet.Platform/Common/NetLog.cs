/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Diagnostics;

namespace AKNet.Platform
{
    internal static class NetLog
    {
        public static bool bPrintLog = true;
        static NetLog()
        {
#if DEBUG
            try
            {
                // 在使用ProcessStartInfo 的重定向输出输入流 时，这里报错
                Console.Clear();
            }
            catch { }
#endif
        }

        private static string GetMsgStr(string logTag, object msgObj, string StackTraceObj)
        {
            string message = msgObj != null ? msgObj.ToString() : string.Empty;
            string StackTraceInfo = StackTraceObj != null ? "\n" + StackTraceObj : string.Empty;
            return $"{DateTime.Now.ToString()}  {logTag}: {message} {StackTraceInfo}";
        }

        private static string GetAssertMsg(object msgObj, string StackTraceInfo)
        {
            return GetMsgStr("Assert Error", msgObj, StackTraceInfo);
        }

        private static string GetExceptionMsg(object msgObj, string StackTraceInfo)
        {
            return GetMsgStr("Exception", msgObj, StackTraceInfo);
        }

        private static string GetErrorMsg(object msgObj, string StackTraceInfo)
        {
            return GetMsgStr("Error", msgObj, StackTraceInfo);
        }

        private static string GetLogMsg(object msgObj, string StackTraceInfo = null)
        {
            return GetMsgStr("Log", msgObj, StackTraceInfo);
        }

        private static string GetWarningMsg(object msgObj, string StackTraceInfo = null)
        {
            return GetMsgStr("Warning", msgObj, StackTraceInfo);
        }

        private static string GetStackTraceInfo()
        {
            StackTrace st = new StackTrace(true);
            return st.ToString();
        }

        internal static void Log(object message)
        {
            if (!bPrintLog) return;
#if DEBUG
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine(GetLogMsg(message));
#endif
        }

        internal static void LogWarning(object message)
        {
            if (!bPrintLog) return;
#if DEBUG
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(GetWarningMsg(message));
#endif
        }

        internal static void LogException(Exception e)
        {
            if (!bPrintLog) return;
#if DEBUG
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(GetExceptionMsg(e.Message, e.StackTrace));
#endif
        }

        internal static void LogError(object message)
        {
            if (!bPrintLog) return;
#if DEBUG
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(GetErrorMsg(message, GetStackTraceInfo()));
#endif
        }

        internal static void Assert(bool bTrue, object message = null)
        {
            if (!bPrintLog) return;
            if (!bTrue)
            {
#if DEBUG
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine(GetAssertMsg(message, GetStackTraceInfo()));
#endif
            }
        }
    }
}
