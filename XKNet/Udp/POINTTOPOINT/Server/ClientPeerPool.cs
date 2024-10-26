using System.Collections.Concurrent;
using System.Linq;
using XKNet.Common;

namespace XKNet.Udp.POINTTOPOINT.Server
{
    internal class ClientPeerPool
    {
        readonly ConcurrentStack<ClientPeer> mObjectPool = new ConcurrentStack<ClientPeer>();
        UdpServer mUdpServer = null;
        private ClientPeer GenerateObject()
        {
            ClientPeer clientPeer = new ClientPeer(this.mUdpServer);
            return clientPeer;
        }

        public ClientPeerPool(UdpServer mUdpServer, int nCount)
        {
            this.mUdpServer = mUdpServer;
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
            mObjectPool.TryPop(out t);
            return t;
        }

        public void recycle(ClientPeer t)
        {
#if DEBUG
            NetLog.Assert(!mObjectPool.Contains(t));
#endif
            mObjectPool.Push(t);
        }
    }
}
