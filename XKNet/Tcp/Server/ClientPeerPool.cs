/************************************Copyright*****************************************
*        ProjectName:XKNet
*        Web:https://github.com/825126369/XKNet
*        Description:XKNet 网络库, 兼容 C#8.0 和 .Net Standard 2.1
*        Author:阿珂
*        CreateTime:2024/10/30 12:14:19
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
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
