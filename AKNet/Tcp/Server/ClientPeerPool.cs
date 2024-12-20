/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/20 10:55:53
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System.Collections.Generic;

namespace AKNet.Tcp.Server
{
    internal class ClientPeerPool
    {
        readonly Stack<ClientPeer> mObjectPool = new Stack<ClientPeer>();
        TcpServer mTcpServer = null;
        private int nMaxCapacity = 0;
        private ClientPeer GenerateObject()
        {
            ClientPeer clientPeer = new ClientPeer(this.mTcpServer);
            return clientPeer;
        }

        public ClientPeerPool(TcpServer mTcpServer, int initCapacity = 0, int nMaxCapacity = 0)
        {
            this.mTcpServer = mTcpServer;
            SetMaxCapacity(nMaxCapacity);
            for (int i = 0; i < initCapacity; i++)
            {
                mObjectPool.Push(GenerateObject());
            }
        }

        public void SetMaxCapacity(int nCapacity)
        {
            this.nMaxCapacity = nCapacity;
        }

        public int Count()
        {
            return mObjectPool.Count;
        }

        public ClientPeer Pop()
        {
            MainThreadCheck.Check();

            ClientPeer t = null;
            if (!mObjectPool.TryPop(out t))
            {
                t = GenerateObject();
            }
            return t;
        }

        public void recycle(ClientPeer t)
        {
            MainThreadCheck.Check();
#if DEBUG
            NetLog.Assert(!mObjectPool.Contains(t));
#endif
            t.Reset();
            //防止 内存一直增加，合理的GC
            bool bRecycle = nMaxCapacity <= 0 || mObjectPool.Count < nMaxCapacity;
            if (bRecycle)
            {
                mObjectPool.Push(t);
            }
        }
    }
}
