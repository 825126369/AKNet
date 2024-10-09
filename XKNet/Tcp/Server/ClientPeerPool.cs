using System.Collections.Generic;

namespace XKNetTcpServer
{
    public class ClientPeerPool
    {
        Stack<ClientPeer> mObjectPool;
        ServerBase mNetServer;

        private ClientPeer GenerateObject()
        {
            ClientPeer clientPeer = new ClientPeer(mNetServer);
            return clientPeer;
        }

        public ClientPeerPool(int nCount, ServerBase mNetServer)
        {
            this.mNetServer = mNetServer;
            mObjectPool = new Stack<ClientPeer>(nCount);

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
                mObjectPool.Push(t);
            }
        }
    }
}
