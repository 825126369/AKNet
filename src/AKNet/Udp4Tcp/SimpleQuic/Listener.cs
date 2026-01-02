
using System;
using System.Collections.Generic;
using System.Net;

namespace AKNet.Udp4Tcp.Common
{
    internal partial class Listener:IDisposable
    {
        private SocketMgr.Config mConfig;
        private readonly SocketMgr mSocketMgr = new SocketMgr();
        private readonly Dictionary<IPEndPoint, Connection> mConnectionPeerDic = new Dictionary<IPEndPoint, Connection>();
        private readonly Queue<Connection> mNewConnectionQueue = new Queue<Connection>();
        private readonly WeakReference<ConnectionEventArgs> mWRAcceptEventArgs = new WeakReference<ConnectionEventArgs>(null);
        private readonly LogicWorker[] mLogicWorkerList = new LogicWorker[Config.nSocketCount];
        private bool bInit = false;

        private void Init()
        {
            if (bInit) return;
            bInit = true;

            ThreadWorkerMgr.Init();
            for (int i = 0; i < mLogicWorkerList.Length; i++)
            {
                mLogicWorkerList[i] = new LogicWorker();
                mLogicWorkerList[i].Init(i);
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
                if (mNewConnectionQueue.TryDequeue(out arg.mConnection))
                {
                    bIOPending = false;
                }
                else
                {
                    mWRAcceptEventArgs.SetTarget(arg);
                }
            }

            return bIOPending;
        }

        private void HandleNewConntion(Connection peer)
        {
            lock (mNewConnectionQueue)
            {
                mNewConnectionQueue.Enqueue(peer);

                if (mWRAcceptEventArgs.TryGetTarget(out ConnectionEventArgs arg))
                {
                    mWRAcceptEventArgs.SetTarget(null);
                    arg.LastOperation = ConnectionAsyncOperation.Accept;
                    arg.ConnectionError = ConnectionError.Success;
                    arg.AcceptConnection = mNewConnectionQueue.Dequeue();
                    arg.TriggerEvent();
                }
            }
        }

    }
}
