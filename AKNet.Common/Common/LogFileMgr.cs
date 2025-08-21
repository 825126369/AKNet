using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AKNet.Common
{
    internal static class LogFileMgr
    {
        private const string logFileDir = "aknet_Log/";
        private static readonly string logFilePath = "aknet_Log.txt";
        private static readonly ConcurrentQueue<string> mLogQueue = new ConcurrentQueue<string>();
        private static readonly List<string> tempList = new List<string>();

        static LogFileMgr()
        {
            if (!Directory.Exists(logFileDir))
            {
                Directory.CreateDirectory(logFileDir);
            }

            int nLogIndex = 1;
            while (true)
            {
                try
                {
                    logFilePath = $"{logFileDir + Path.GetFileName(logFilePath)}_{nLogIndex}.txt";
                    File.Delete(logFilePath); //每次启动删掉原来的日志
                    var mFileStream = File.Open(logFilePath, FileMode.OpenOrCreate); // 如果报错，说明文件被占用，那么就新建文件
                    break;
                }
                catch (Exception e)
                {
                    nLogIndex++;
                    if (nLogIndex > 10)
                    {
                        Console.WriteLine("File.Open Error: " + e.ToString());
                        break;
                    }
                }
            }

            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            WriteLogToFileThreadFunc();
        }

        private static async void OnProcessExit(object sender, EventArgs e)
        {
            await FlushLogs(true);
        }

        public static void AddMsg(string msg)
        {
            mLogQueue.Enqueue(msg);
        }

        public static async void WriteLogToFileThreadFunc()
        {
            while (true)
            {
                await Task.Delay(1000).ConfigureAwait(false); //这里必须加个 false,因为上层有可能自定义了同步上下文
                await FlushLogs().ConfigureAwait(false); //捕获上下文，性能会很差
            }
        }

        private static async Task FlushLogs(bool bFlushAll = false)
        {
            tempList.Clear();
            int nFlushMaxCount = bFlushAll ? int.MaxValue : 1000;
            while (tempList.Count < nFlushMaxCount && mLogQueue.TryDequeue(out string log))
            {
                tempList.Add(log);
            }

            if (tempList.Count > 0)
            {
                try
                {
                    await File.AppendAllLinesAsync(logFilePath, tempList);
                }
                catch (Exception e)
                {
                    Console.WriteLine("File.AppendAllLinesAsync Error: " + e.ToString());
                }
            }
        }

    }
}
