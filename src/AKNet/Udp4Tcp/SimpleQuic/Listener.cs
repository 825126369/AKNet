
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace AKNet.Udp4Tcp.Common
{
    internal partial class Listener:IDisposable
    {
        private SocketMgr.Config mConfig;
        private readonly SocketMgr mSocketMgr = new SocketMgr();
        private readonly Dictionary<IPEndPoint, ConnectionPeer> mConnectionPeerDic = new Dictionary<IPEndPoint, ConnectionPeer>();
        private readonly Queue<ConnectionPeer> mNewConnectionQueue = new Queue<ConnectionPeer>();
        private readonly ManualResetEventSlim mManualResetEventSlim = new ManualResetEventSlim(false);
        readonly WeakReference<ConnectionEventArgs> mWRAcceptEventArgs = new WeakReference<ConnectionEventArgs>(null);
        readonly LogicWorker[] mLogicWorkerList = new LogicWorker[Config.nSocketCount];
        private bool bInit = false;

        private void Init()
        {
            if (bInit) return;
            bInit = true;

            ThreadWorkerMgr.Init();
            for (int i = 0; i < mLogicWorkerList.Length; i++)
            {
                mLogicWorkerList[i] = new LogicWorker(i);
            }
        }

        public void Bind(EndPoint mEndPoint)
        {
            Init();

            SocketMgr.Config mConfig = new SocketMgr.Config();
            mConfig.bServer = true;
            mConfig.mEndPoint = mEndPoint;
            mConfig.mReceiveFunc = MultiThreadingReceiveNetPackage;
            this.mConfig = mConfig;
            mSocketMgr.InitNet(mConfig);
            for (int i = 0; i < mLogicWorkerList.Length; i++)
            {
                mLogicWorkerList[i].SetSocketItem(mSocketMgr.GetSocketItem(i));
            }
        }

        public void Dispose()
        {
            mSocketMgr.Dispose();
        }

        public void Update()
        {

        }

        public bool AcceptAsync(ConnectionEventArgs arg)
        {
            bool bIOPending = true;
            arg.LastOperation = ConnectionAsyncOperation.Accept;
            arg.ConnectionError = ConnectionError.Success;

            lock (mNewConnectionQueue)
            {
                if (mNewConnectionQueue.TryDequeue(out arg.mConnectionPeer))
                {
                    bIOPending = false;
                    arg.TriggerEvent();
                }
                else
                {
                    mWRAcceptEventArgs.SetTarget(arg);
                    bIOPending = true;
                }
            }

            return bIOPending;
        }

        private void HandleNewConntion(ConnectionPeer peer)
        {
            lock (mNewConnectionQueue)
            {
                mNewConnectionQueue.Enqueue(peer);

                if (mWRAcceptEventArgs.TryGetTarget(out ConnectionEventArgs arg))
                {
                    mWRAcceptEventArgs.SetTarget(null);
                    arg.LastOperation = ConnectionAsyncOperation.Accept;
                    arg.ConnectionError = ConnectionError.Success;
                    arg.mConnectionPeer = mNewConnectionQueue.Dequeue();
                    arg.TriggerEvent();
                }
            }
        }

    }
}
