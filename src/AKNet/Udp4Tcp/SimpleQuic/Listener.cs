
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

        public void Bind(EndPoint mEndPoint)
        {
            SocketMgr.Config mConfig = new SocketMgr.Config();
            mConfig.bServer = true;
            mConfig.mEndPoint = mEndPoint;
            mConfig.mReceiveFunc = MultiThreadingReceiveNetPackage;
            this.mConfig = mConfig;

            ThreadWorkerMgr.mListenerList.AddLast(this);
            mSocketMgr.InitNet(mConfig);
        }

        public void Dispose()
        {
            mSocketMgr.Dispose();
        }

        public bool AcceptAsync(ConnectionPeerEventArgs arg)
        {
            arg.nLastOpType = ConnectionPeerEventArgs.E_OP_TYPE.Accept;
            arg.Result = true;
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
