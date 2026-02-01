/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:49
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System.Collections.Generic;

namespace AKNet.Udp2Tcp.Server
{
    internal class ClientPeerPool
    {
        readonly Stack<ClientPeer> mObjectPool = new Stack<ClientPeer>();
        ServerMgr mServer = null;
        private int nMaxCapacity = 0;
        private ClientPeer GenerateObject()
        {
            ClientPeer clientPeer = new ClientPeer(this.mServer);
            return clientPeer;
        }

        public ClientPeerPool(ServerMgr mTcpServer, int initCapacity = 0, int nMaxCapacity = 0)
        {
            this.mServer = mTcpServer;
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
            else
            {
                t.Release();
            }
        }
    }
}
