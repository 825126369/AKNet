/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace AKNet.Common
{
    public static partial class NetLog
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
    
    public static partial class NetLog
    {
        public static bool bPrintLog = true;
        public static event Action<string> LogFunc;
        public static event Action<string> LogWarningFunc;
        public static event Action<string> LogErrorFunc;
        
        static NetLog()
        {
            System.AppDomain.CurrentDomain.UnhandledException += _OnUncaughtExceptionHandler;
            LogErrorFunc += LogErrorToFile;
            LogErrorToFile("");
#if DEBUG
            try
            {
                // 在使用ProcessStartInfo 的重定向输出输入流 时，这里报错
                Console.Clear();
            }
            catch { }
#endif
        }

        public static void Init()
        {
            
        }

        static void LogErrorToFile(string Message)
        {
            LogFileMgr.AddMsg(Message);
        }

        private static void _OnUncaughtExceptionHandler(object sender, System.UnhandledExceptionEventArgs args)
        {
            Exception exception = args.ExceptionObject as Exception;
            LogUncaughtException(exception);
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

        private static string Get_OnUncaughtExceptionMsg(object msgObj, string StackTraceInfo)
        {
            return GetMsgStr("___OnUncaught Exception", msgObj, StackTraceInfo);
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
            StackTrace st = new StackTrace(1, true);
            return st.ToString();
        }

        public static void Log(object message)
        {
            if (!bPrintLog) return;
            string msg = GetLogMsg(message);
#if DEBUG
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine(msg);
#endif
            if (LogFunc != null)
            {
                LogFunc(msg);
            }
        }

        public static void LogWarning(object message)
        {
            if (!bPrintLog) return;
            string msg = GetWarningMsg(message);
#if DEBUG
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(msg);
#endif
            if (LogWarningFunc != null)
            {
                LogWarningFunc(msg);
            }
        }

        private static void LogUncaughtException(Exception e)
        {
            if (!bPrintLog) return;
            string msg = Get_OnUncaughtExceptionMsg(e, GetStackTraceInfo());
#if DEBUG
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(msg);
#endif
            if (LogErrorFunc != null)
            {
                LogErrorFunc(msg);
            }
        }

        public static void LogException(Exception e)
        {
            if (!bPrintLog) return;
            string msg = GetExceptionMsg(e, GetStackTraceInfo());
#if DEBUG
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(msg);
#endif
            if (LogErrorFunc != null)
            {
                LogErrorFunc(msg);
            }
        }

        public static void LogError(object message)
        {
            if (!bPrintLog) return;
            string msg = GetErrorMsg(message, GetStackTraceInfo());
#if DEBUG
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(msg);
#endif
            if (LogErrorFunc != null)
            {
                LogErrorFunc(msg);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert(bool bTrue, object message = null)
        {
            if (!bTrue)
            {
                string msg = GetAssertMsg(message, GetStackTraceInfo());
#if DEBUG
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine(msg);
#endif
                if (LogErrorFunc != null)
                {
                    LogErrorFunc(msg);
                }
            }
        }
    }
}
