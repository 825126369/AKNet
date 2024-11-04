/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/4 20:04:54
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Collections.Generic;
using AKNet.Common;

namespace AKNet.Tcp.Server
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
