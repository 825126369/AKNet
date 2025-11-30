/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/29 4:33:37
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Diagnostics;
using System.Reflection;
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
            Init();

            Action<string> LogFunc = (string message)=>
            {
                Console.ForegroundColor = ConsoleColor.Green;
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
            Init();
        }

        private static bool bInit = false;
        public static void Init()
        {
            if (bInit) return; bInit = true;

            System.AppDomain.CurrentDomain.UnhandledException += _OnUncaughtExceptionHandler;
            LogErrorFunc += LogErrorToFile;
#if DEBUG
            try
            {
                // 在使用ProcessStartInfo 的重定向输出输入流 时，这里报错
                Console.Clear();
            }
            catch { }
#endif
        }

        static void LogErrorToFile(string Message)
        {
            LogFileMgr.AddMsg(Message);
        }

        private static void _OnUncaughtExceptionHandler(object sender, System.UnhandledExceptionEventArgs args)
        {
            bool IsFromMyAssembly(Exception ex)
            {
                // 取最顶层栈帧
                var frame = new StackTrace(ex, fNeedFileInfo: false).GetFrame(0);
                if (frame == null) return false;
                var method = frame.GetMethod();
                var asm = method?.DeclaringType?.Assembly;
                return asm == Assembly.GetExecutingAssembly();
            }

            Exception exception = args.ExceptionObject as Exception;
           // if (IsFromMyAssembly(exception))
            {
                LogUncaughtException(exception);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetMsgStr(string logTag, object msgObj, string StackTraceObj)
        {
            string message = msgObj != null ? msgObj.ToString() : string.Empty;
            string StackTraceInfo = StackTraceObj != null ? "\n" + StackTraceObj : string.Empty;
            return $"{DateTime.Now.ToString()}  {logTag}: {message} {StackTraceInfo}";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetAssertMsg(object msgObj, string StackTraceInfo)
        {
            return GetMsgStr("Assert Error", msgObj, StackTraceInfo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string Get_OnUncaughtExceptionMsg(object msgObj, string StackTraceInfo)
        {
            return GetMsgStr("___OnUncaught Exception", msgObj, StackTraceInfo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetExceptionMsg(object msgObj, string StackTraceInfo)
        {
            return GetMsgStr("Exception", msgObj, StackTraceInfo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetErrorMsg(object msgObj, string StackTraceInfo)
        {
            return GetMsgStr("Error", msgObj, StackTraceInfo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetLogMsg(object msgObj, string StackTraceInfo = null)
        {
            return GetMsgStr("Log", msgObj, StackTraceInfo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetWarningMsg(object msgObj, string StackTraceInfo = null)
        {
            return GetMsgStr("Warning", msgObj, StackTraceInfo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetStackTraceInfo()
        {
            StackTrace st = new StackTrace(1, true);
            return st.ToString();
        }

        public static void Log(object message)
        {
            if (!bPrintLog) return;
            string msg = GetLogMsg(message);
            if (LogFunc != null)
            {
                LogFunc(msg);
            }
        }

        public static void LogWarning(object message)
        {
            if (!bPrintLog) return;
            string msg = GetWarningMsg(message);
            if (LogWarningFunc != null)
            {
                LogWarningFunc(msg);
            }
        }

        private static void LogUncaughtException(Exception e)
        {
            if (!bPrintLog) return;
            string msg = Get_OnUncaughtExceptionMsg(e, GetStackTraceInfo());
            if (LogErrorFunc != null)
            {
                LogErrorFunc(msg);
            }
        }

        public static void LogException(Exception e)
        {
            if (!bPrintLog) return;
            string msg = GetExceptionMsg(e, GetStackTraceInfo());
            if (LogErrorFunc != null)
            {
                LogErrorFunc(msg);
            }
        }

        public static void LogError(object message)
        {
            if (!bPrintLog) return;
            string msg = GetErrorMsg(message, GetStackTraceInfo());
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
                if (LogErrorFunc != null)
                {
                    LogErrorFunc(msg);
                }
            }
        }
    }
}
