/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:16
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace AKNet.Udp4Tcp.Common
{
    internal partial class ThreadWorker:IDisposable
    {
        private readonly static LinkedList<Connection> mConnectionList = new LinkedList<Connection>();
        private readonly static LinkedList<Listener> mListenerList = new LinkedList<Listener>();

        private ConcurrentQueue<SSocketAsyncEventArgs> mSocketAsyncEventArgsQueue = new ConcurrentQueue<SSocketAsyncEventArgs>();
        private AutoResetEvent mEventQReady = new AutoResetEvent(false);
        private readonly ObjectPool<ConnectionPeer> mConnectionPeerPool = null;
        private readonly ObjectPool<NetUdpSendFixedSizePackage> mSendPackagePool = null;
        private readonly ObjectPool<NetUdpReceiveFixedSizePackage> mReceivePackagePool = null;

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
                foreach (var v in mListenerList)
                {
                    v.Update();
                }

                foreach (var v in mConnectionList)
                {
                    v.Update();
                }

                while (mSocketAsyncEventArgsQueue.TryDequeue(out SSocketAsyncEventArgs arg))
                {
                    arg.Do();
                }
            }
        }

        public void Add_SocketAsyncEventArgs(SocketAsyncEventArgs arg)
        {
            mSocketAsyncEventArgsQueue.Enqueue(arg);
        }

    }
}









