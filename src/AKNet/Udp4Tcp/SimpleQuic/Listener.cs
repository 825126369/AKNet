
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
        private readonly ConnectionPeerPool mConnectionPeerPool = null;

        private readonly ConcurrentQueue<ConnectionPeer> mAcceptConnectionQueue = new ConcurrentQueue<ConnectionPeer>();
        private readonly ManualResetEventSlim mManualResetEventSlim = new ManualResetEventSlim(false);

        readonly LogicWorker[] mLogicWorkerList = new LogicWorker[Environment.ProcessorCount];
        private bool bInit = false;
        private void Init()
        {
            if (bInit) return;
            bInit = true;

            ThreadWorkerMgr.Init();
            for (int i = 0; i < mLogicWorkerList.Length; i++)
            {
                mLogicWorkerList[i] = new LogicWorker(i);
                mLogicWorkerList[i].Init();
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
            arg.LastOperation = ConnectionAsyncOperation.Accept;
            arg.ConnectionError =  ConnectionError.Success;
            if (mAcceptConnectionQueue.TryDequeue(out arg.mConnectionPeer))
            {
                return false;
            }
            else
            {
                mManualResetEventSlim.Reset();
                Task.Run(() =>
                {
                    while(true)
                    {
                        mManualResetEventSlim.Wait(1000);
                        if (mAcceptConnectionQueue.TryDequeue(out arg.mConnectionPeer))
                        {
                            arg.TriggerEvent();
                        }
                    }
                });

                return true;
            }
        }
    }
}
