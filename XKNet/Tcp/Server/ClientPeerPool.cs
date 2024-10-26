using System.Collections.Generic;
using XKNet.Common;

namespace XKNet.Tcp.Server
{
    internal class ClientPeerPool
    {
        Stack<ClientPeer> mObjectPool;
        TcpServer mTcpServer = null;

        private ClientPeer GenerateObject()
        {
            ClientPeer clientPeer = new ClientPeer(this.mTcpServer);
            return clientPeer;
        }

        public ClientPeerPool(TcpServer mTcpServer, int nCount)
        {
            this.mTcpServer = mTcpServer;
            this.mObjectPool = new Stack<ClientPeer>(nCount);

            for (int i = 0; i < nCount; i++)
            {
                ClientPeer clientPeer = GenerateObject();
                mObjectPool.Push(clientPeer);
            }
        }

        public int Count()
        {
            return mObjectPool.Count;
        }

        public ClientPeer Pop()
        {
            ClientPeer t = null;

            lock (mObjectPool)
            {
                if (mObjectPool.Count > 0)
                {
                    t = mObjectPool.Pop();
                }
            }

            return t;
        }

        public void recycle(ClientPeer t)
        {
            lock (mObjectPool)
            {
#if DEBUG
                NetLog.Assert(!mObjectPool.Contains(t));
#endif
                mObjectPool.Push(t);
            }
        }
    }
}
