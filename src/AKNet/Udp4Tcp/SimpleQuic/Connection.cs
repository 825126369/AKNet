using AKNet.Common;
using System;

namespace AKNet.Udp4Tcp.Common
{
    internal class Connection : ConnectionPeer, IDisposable
    {
        SocketMgr.Config mConfig;
        readonly LogicWorker[] mLogicWorkerList = new LogicWorker[1];
        private bool bInit = false;
        private SocketMgr mSocketMgr = new SocketMgr();

        public Connection()
        {
            ThreadWorkerMgr.Init();
            for (int i = 0; i < mLogicWorkerList.Length; i++)
            {
                int nThreadWorkerIndex = RandomTool.RandomArrayIndex(0, Environment.ProcessorCount);
                mLogicWorkerList[i] = new LogicWorker(nThreadWorkerIndex);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
        }
        
        public bool ConnectAsync(ConnectionEventArgs arg)
        {
            bool bIOPending = true;
            SocketMgr.Config mConfig = new SocketMgr.Config();
            mConfig.bServer = false;
            mConfig.mEndPoint = arg.RemoteEndPoint;
            mConfig.mReceiveFunc = WorkerThreadReceiveNetPackage;
            this.mConfig = mConfig;

            int nState = mSocketMgr.InitNet(mConfig);
            if(nState == 0)
            {
                mWRConnectEventArgs.SetTarget(arg);
                SendConnect();
            }
            else
            {

            }

            return bIOPending;
        }

        public bool DisconnectAsync(ConnectionEventArgs arg)
        {
            mWRDisConnectEventArgs.SetTarget(arg);
            SendDisConnect();
            return true;
        }
        
        public bool SendAsync(ConnectionEventArgs arg)
        {
            arg.LastOperation = ConnectionAsyncOperation.Send;
            arg.ConnectionError = ConnectionError.Success;
            SendTcpStream(arg.GetSpan());
            return true;
        }

        public bool ReceiveAsync(ConnectionEventArgs arg)
        {
            return true;
        }

        public bool Connected
        {
            get
            {
                return m_Connected;
            }
        }
    }
}
