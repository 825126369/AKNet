using AKNet.Common;
using System;

namespace AKNet.Udp4Tcp.Common
{
    internal class Connection : ConnectionPeer, IDisposable
    {
        readonly LogicWorker[] mLogicWorkerList = new LogicWorker[1];
        private bool bInit = false;
        public void Init()
        {
            if (bInit) return;
            bInit = true;

            ThreadWorkerMgr.Init();
            for (int i = 0; i < mLogicWorkerList.Length; i++)
            {
                int nThreadWorkerIndex = RandomTool.RandomArrayIndex(0, Environment.ProcessorCount);
                mLogicWorkerList[i] = new LogicWorker(nThreadWorkerIndex);
            }
        }

        public void Dispose()
        {

        }
        
        public bool ConnectAsync(ConnectionEventArgs arg)
        {
            Init();
            return true;
        }

        public bool DisconnectAsync(ConnectionEventArgs arg)
        {
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
