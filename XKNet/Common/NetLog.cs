using System;
using System.Diagnostics;
using System.IO;

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
            System.AppDomain.CurrentDomain.UnhandledException += _OnUncaughtExceptionHandler;
            LogErrorFunc += LogErrorToFile;
#if DEBUG
            Console.Clear();
#endif
        }

        static void LogErrorToFile(string Message)
        {
            string logFilePath = "NetLog.txt";
            using (StreamWriter writer = new StreamWriter(logFilePath, true))
            {
                writer.WriteLine($"[{DateTime.Now}] Error occurred:");
                writer.WriteLine(Message);
                writer.WriteLine();
            }
        }

        private static void _OnUncaughtExceptionHandler(object sender, System.UnhandledExceptionEventArgs args)
        {
            Exception exception = args.ExceptionObject as Exception;
            LogErrorToFile(GetMsgStr(exception.Message, exception.StackTrace));
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
            if (msgObj == null)
            {
                return $"Assert Error: {StackTraceInfo}";
            }
            else
            {
                return $"Assert Error: {msgObj} | {StackTraceInfo}";
            }
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
            Console.WriteLine(GetMsgStr(message));
#endif
            if (LogFunc != null)
            {
                LogFunc(GetMsgStr(message));
            }
        }

        internal static void LogWarning(object message)
        {
            if (!bPrintLog) return;
#if DEBUG
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(GetMsgStr(message));
#endif
            if (LogWarningFunc != null)
            {
                LogWarningFunc(GetMsgStr(message));
            }
        }

//        internal static void LogError(Exception e)
//        {
//            if (!bPrintLog) return;
//#if DEBUG
//            Console.ForegroundColor = ConsoleColor.DarkRed;
//            Console.WriteLine(GetMsgStr(e.Message, e.StackTrace));
//#endif
//            if (LogErrorFunc != null)
//            {
//                LogErrorFunc(GetMsgStr(e.Message, e.StackTrace));
//            }
//        }

        internal static void LogError(object message)
        {
            if (!bPrintLog) return;
#if DEBUG
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(GetMsgStr(message));
#endif
            if (LogErrorFunc != null)
            {
                LogErrorFunc(GetMsgStr(message));
            }
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
                if (LogErrorFunc != null)
                {
                    LogErrorFunc(GetAssertMsg(message, GetStackTraceInfo()));
                }
            }
        }
    }
}
