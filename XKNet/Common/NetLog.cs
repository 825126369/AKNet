using System;
using System.Diagnostics;

namespace XKNet.Common
{
    public static class NetLog
    {
        public static bool bPrintLog = true;
        public static Action<string> LogFunc;
        public static Action<string> LogWarningFunc;
        public static Action<string> LogErrorFunc;

        static NetLog()
        {
#if DEBUG
            Console.Clear();
#endif
        }

        private static string GetMsgStr(object message, string StackTraceInfo = null)
        {
            if (StackTraceInfo != null)
            {
                return $"{DateTime.Now.ToLongTimeString()} : {message} | {StackTraceInfo}";
            }
            else
            {
                return $"{DateTime.Now.ToLongTimeString()} : {message}";
            }
        }

        private static string GetAssertMsg(object msgObj, string StackTraceInfo)
        {
            string message = msgObj.ToString();
            if (message == null)
            {
                message = $"Assert Error: {StackTraceInfo}";
            }
            else
            {
                message = $"Assert Error: {message} | {StackTraceInfo}";
            }
            return message;
        }

        private static string GetStackTraceInfo()
        {
            StackTrace st = new StackTrace(true);
            return st.GetFrame(2).ToString();
        }

        internal static void Log(object message)
        {
            if (!bPrintLog) return;
#if DEBUG
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine(GetMsgStr(message));
#else
            if(LogFunc != null)
            {
                LogFunc(GetMsgStr(message));
            }
#endif
        }

        internal static void LogWarning(object message)
        {
            if (!bPrintLog) return;
#if DEBUG
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(GetMsgStr(message));
#else
            if(LogWarningFunc != null)
            {
                LogWarningFunc(GetMsgStr(message));
            }
#endif
        }

        internal static void LogError(object message)
        {
            if (!bPrintLog) return;
#if DEBUG
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(GetMsgStr(message));
#else
            if(LogErrorFunc != null)
            {
                LogErrorFunc(GetMsgStr(message));
            }
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
#else
                if(LogErrorFunc != null)
                {
                    LogErrorFunc(GetAssertMsg(message, GetStackTraceInfo()));
                }
#endif
            }
        }
    }
}
