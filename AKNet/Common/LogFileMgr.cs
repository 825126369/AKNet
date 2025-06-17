using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AKNet.Common
{
    internal class LogFileMgr
    {
        private const string logFileDir = "aknet_Log/";
        private const string logFilePath = "aknet_Log.txt";
        private readonly object mFileStreamLock = new object();
        private readonly FileStream mFileStream;
        private readonly StreamWriter mFileStreamWriter = null;
        private readonly ConcurrentQueue<string> mLogQueue = new ConcurrentQueue<string>();

        public LogFileMgr()
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
                    string logFileName = $"{logFileDir + Path.GetFileName(logFilePath)}_{nLogIndex}.txt";
                    File.Delete(logFileName);

                    //string logFileName = $"{logFileDir + Path.GetFileName(logFilePath)}_{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss")}_{nLogIndex}.txt";
                    mFileStream = File.Open(logFileName, FileMode.OpenOrCreate);
                    mFileStreamWriter = new StreamWriter(mFileStream);
                    break;
                }
                catch (Exception e)
                {
                    nLogIndex++;
                    if (nLogIndex > 100)
                    {
                        Console.WriteLine("创建日志文件失败: " + e.ToString());
                        break;
                    }
                }
            }
        }

        public void AddMsg(string msg)
        {
            Task.Run(() =>
            {
                WriteToFile(msg);
            });
        }

        private void WriteToFile(string Message)
        {
            Monitor.Enter(mFileStreamLock);
            mFileStreamWriter.WriteLine(Message);
            Monitor.Exit(mFileStreamLock);
        }
    }
}
