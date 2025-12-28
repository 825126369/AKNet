/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:16
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace AKNet.Udp4Tcp.Common
{
    internal partial class ThreadWorker:IDisposable
    {
        public readonly static LinkedList<Connection> mConnectionPeerList = new LinkedList<Connection>();
        public readonly static LinkedList<Listener> mListenerList = new LinkedList<Listener>();

        public ConcurrentQueue<SocketAsyncEventArgs> mSocketAsyncEventArgsQueue = new ConcurrentQueue<SocketAsyncEventArgs>();
        private AutoResetEvent mEventQReady = new AutoResetEvent(false);

        public void Init()
        {
            Thread mThread = new Thread(ThreadFunc);
            mThread.IsBackground = true;
            mThread.Start();
        }

        public void Dispose()
        {
            
        }

        public void ThreadFunc()
        {
            while (true)
            {
                mEventQReady.WaitOne();
                foreach (var v in mEventQReady)
                {
                    v.Value.Update();
                }

                while (mSocketAsyncEventArgsQueue.TryDequeue(out SocketAsyncEventArgs arg))
                {

                }
            }
        }

        public void Add_SocketAsyncEventArgs(SocketAsyncEventArgs arg)
        {
            mSocketAsyncEventArgsQueue.Enqueue(arg);
        }

    }
}









