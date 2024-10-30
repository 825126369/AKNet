/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:AKNet 网络库, 兼容 C#8.0 和 .Net Standard 2.1
*        Author:阿珂
*        CreateTime:2024/10/30 21:55:41
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Collections.Concurrent;
using System.Linq;
using AKNet.Common;

namespace AKNet.Udp.POINTTOPOINT.Server
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
