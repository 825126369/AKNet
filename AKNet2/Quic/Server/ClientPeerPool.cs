/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System.Collections.Generic;

namespace AKNet.Quic.Server
{
    internal class ClientPeerPool
    {
        readonly Stack<ClientPeer_Private> mObjectPool = new Stack<ClientPeer_Private>();
        QuicServer mTcpServer = null;
        private int nMaxCapacity = 0;
        private ClientPeer_Private GenerateObject()
        {
            ClientPeer_Private clientPeer = new ClientPeer_Private(this.mTcpServer);
            return clientPeer;
        }

        public ClientPeerPool(QuicServer mTcpServer, int initCapacity = 0, int nMaxCapacity = 0)
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

        public ClientPeer_Private Pop()
        {
            MainThreadCheck.Check();

            ClientPeer_Private t = null;
            if (!mObjectPool.TryPop(out t))
            {
                t = GenerateObject();
            }
            return t;
        }

        public void recycle(ClientPeer_Private t)
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
            else
            {
                t.Release();
            }
        }
    }
}
