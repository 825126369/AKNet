using System;
using System.Diagnostics;
using System.IO;

namespace XKNet.Common
{
#if !DEBUG
    public static class NetLogMgr
    {
        public static void SetOrPrintLog(bool bPrintLog)
        {
            NetLog.bPrintLog = bPrintLog;
        }

        public static void AddLogFunc(Action<string> LogFunc, Action<string> LogErrorFunc, Action<string> LogWarningFunc)
        {
            NetLog.LogFunc += LogFunc;
            NetLog.LogErrorFunc += LogErrorFunc;
            NetLog.LogWarningFunc += LogWarningFunc;
        }

        public static void AddConsoleLog()
        {
            Action<string> LogFunc = (string message)=>
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine(message);
            };

            Action<string> LogErrorFunc = (string message) =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(message);
            };

            Action<string> LogWarningFunc = (string message) =>
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(message);
            };

            AddLogFunc(LogFunc, LogErrorFunc, LogWarningFunc);
        }
    }
#endif

    internal static class NetLog
    {
        public static bool bPrintLog = true;
        public static Action<string> LogFunc;
        public static Action<string> LogWarningFunc;
        public static Action<string> LogErrorFunc;

#if DEBUG
        private static readonly object lock_writeFile_obj = new object();
        static NetLog()
        {
            System.AppDomain.CurrentDomain.UnhandledException += _OnUncaughtExceptionHandler;
            LogErrorFunc += LogErrorToFile;
            Console.Clear();
        }

        static void LogErrorToFile(string Message)
        {
            lock (lock_writeFile_obj)
            {
                string logFilePath = "NetLog.txt";
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine($"[{DateTime.Now}] Error occurred:");
                    writer.WriteLine(Message);
                    writer.WriteLine();
                }
            }
        }

        private static void _OnUncaughtExceptionHandler(object sender, System.UnhandledExceptionEventArgs args)
        {
            Exception exception = args.ExceptionObject as Exception;
            LogErrorToFile(GetMsgStr(exception.Message, exception.StackTrace));
        }
#endif

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
