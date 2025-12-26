/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:16
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp4Tcp.Common;
using System.Collections.Generic;

namespace AKNet.Udp4Tcp.Server
{
    internal class ConnectionPeerPool
    {
        readonly Stack<ConnectionPeer> mObjectPool = new Stack<ConnectionPeer>();
        ServerMgr mUdpServer = null;
        private int nMaxCapacity = 0;
        private ConnectionPeer GenerateObject()
        {
            ConnectionPeer clientPeer = new ConnectionPeer(this.mUdpServer);
            return clientPeer;
        }

        public ConnectionPeerPool(ServerMgr mUdpServer, int initCapacity = 0, int nMaxCapacity = 0)
        {
            this.mUdpServer = mUdpServer;
            SetMaxCapacity(nMaxCapacity);
            for (int i = 0; i < initCapacity; i++)
            {
                ConnectionPeer clientPeer = GenerateObject();
                mObjectPool.Push(clientPeer);
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

        public ConnectionPeer Pop()
        {
            ConnectionPeer t = null;
            lock (mObjectPool)
            {
                mObjectPool.TryPop(out t);
            }

            if (t == null)
            {
                t = GenerateObject();
            }

            return t;
        }

        public void recycle(ConnectionPeer t)
        {
#if DEBUG
            NetLog.Assert(!mObjectPool.Contains(t));
#endif
            t.Reset();
            bool bRecycle = nMaxCapacity <= 0 || mObjectPool.Count < nMaxCapacity;
            if (bRecycle)
            {
                lock (mObjectPool)
                {
                    mObjectPool.Push(t);
                }
            }
        }
    }
}
