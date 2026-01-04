
using AKNet.Common;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Net;
using System.Threading.Tasks;

namespace AKNet.Udp4Tcp.Common
{
    internal partial class Listener:IDisposable
    {
        private SocketMgr.Config mConfig;
        private readonly SocketMgr mSocketMgr = new SocketMgr();
        private readonly Dictionary<IPEndPoint, Connection> mConnectionPeerDic = new Dictionary<IPEndPoint, Connection>();
        private readonly Queue<Connection> mNewConnectionQueue = new Queue<Connection>();
        private readonly List<LogicWorker> mLogicWorkerList = new List<LogicWorker>();
        private bool bInit = false;
        private readonly KKValueTaskSource _acceptTcs = new KKValueTaskSource();
        private void Init()
        {
            if (bInit) return;
            bInit = true;

            ThreadWorkerMgr.Init();

            List<ThreadWorker> mRandomThreadWorkerList = ThreadWorkerMgr.GetRandomThreadWorkerList(Config.nSocketCount);
            for (int i = 0; i < Config.nSocketCount; i++)
            {
                var mLogicWorker = new LogicWorker();
                mLogicWorkerList.Add(mLogicWorker);
                mLogicWorker.Init(mRandomThreadWorkerList[i]);
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
            for (int i = 0; i < mLogicWorkerList.Count; i++)
            {
                mLogicWorkerList[i].SetSocketItem(mSocketMgr.GetSocketItem(i));
            }
        }

        public void Dispose()
        {
            mSocketMgr.Dispose();
            foreach(var mLogicWorker in mLogicWorkerList)
            {
                mLogicWorker.mThreadWorker.RemoveLogicWorker(mLogicWorker);
                mLogicWorker.mThreadWorker = null;
            }
            mLogicWorkerList.Clear();
        }

        public async ValueTask<Connection> AcceptAsync()
        {
            Connection mConnection = null;
            do
            {
                if (_acceptTcs.TryInitialize(out ValueTask valueTask, this))
                {

                }
                
                lock (mNewConnectionQueue)
                {
                    mNewConnectionQueue.TryDequeue(out mConnection);
                }

                await valueTask.ConfigureAwait(false);
            } while (mConnection == null);

            return mConnection;
        }

        private void HandleNewConntion(Connection peer)
        {
            lock (mNewConnectionQueue)
            {
                mNewConnectionQueue.Enqueue(peer);
                _acceptTcs.TrySetResult();
            }
        }

    }
}
