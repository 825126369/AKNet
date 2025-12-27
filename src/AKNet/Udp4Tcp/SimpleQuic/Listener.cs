using AKNet.Udp4Tcp.Server;
using System.Collections.Generic;
using System.Net;

namespace AKNet.Udp4Tcp.Common
{
    internal partial class Listener
    {
        SocketMgr.Config mConfig;
        private readonly SocketMgr mSocketMgr = new SocketMgr();
        private readonly Dictionary<IPEndPoint, ConnectionPeer> mConnectionPeerDic = new Dictionary<IPEndPoint, ConnectionPeer>();
        private readonly ConnectionPeerPool mConnectionPeerPool = null;

        public void Open(SocketMgr.Config mConfig)
        {
            this.mConfig = mConfig;
            ThreadWorkerMgr.mListenerList.AddLast(this);
        }

        public void Start()
        {
            mSocketMgr.InitNet(mConfig);
        }

    }
}
