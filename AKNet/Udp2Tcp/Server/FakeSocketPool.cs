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

namespace AKNet.Udp2Tcp.Server
{
    internal class FakeSocketPool
    {
        readonly Stack<FakeSocket> mObjectPool = new Stack<FakeSocket>();
        UdpServer mUdpServer = null;
        private int nMaxCapacity = 0;
        private FakeSocket GenerateObject()
        {
            FakeSocket clientPeer = new FakeSocket(this.mUdpServer);
            return clientPeer;
        }

        public FakeSocketPool(UdpServer mUdpServer, int initCapacity = 0, int nMaxCapacity = 0)
        {
            this.mUdpServer = mUdpServer;
            SetMaxCapacity(nMaxCapacity);
            for (int i = 0; i < initCapacity; i++)
            {
                FakeSocket clientPeer = GenerateObject();
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

        public FakeSocket Pop()
        {
            FakeSocket t = null;
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

        public void recycle(FakeSocket t)
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
