/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/28 7:14:06
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp4LinuxTcp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace AKNet.Udp4LinuxTcp.Server
{
    internal class FakeSocket : IPoolItemInterface
    {
        private readonly UdpServer mNetServer;
        private readonly Queue<sk_buff> mWaitCheckPackageQueue = new Queue<sk_buff>();
        private int nCurrentCheckPackageCount = 0;
        public IPEndPoint RemoteEndPoint;

        public FakeSocket(UdpServer mNetServer)
        {
            this.mNetServer = mNetServer;
        }

        public void MultiThreadingReceiveNetPackage(SocketAsyncEventArgs e)
        {
            ReadOnlySpan<byte> mBuff = e.MemoryBuffer.Span.Slice(e.Offset, e.BytesTransferred);
            var skb = LinuxTcpFunc.build_skb(mBuff);

            lock (mWaitCheckPackageQueue)
            {
                mWaitCheckPackageQueue.Enqueue(skb);
            }
        }

        public bool GetReceivePackage(out sk_buff mPackage)
        {
            lock (mWaitCheckPackageQueue)
            {
                if (mWaitCheckPackageQueue.TryDequeue(out mPackage))
                {
                    return true;
                }
            }

            return false;
        }
        
        public bool SendToAsync(SocketAsyncEventArgs mArg)
        {
            return this.mNetServer.GetSocketMgr().SendToAsync(mArg);
        }

        public void Reset()
        {
            MainThreadCheck.Check();

            //lock (mWaitCheckPackageQueue)
            //{
            //    while (mWaitCheckPackageQueue.TryDequeue(out var mPackage))
            //    {
            //        mNetServer.GetObjectPoolManager().UdpReceivePackage_Recycle(mPackage);
            //    }
            //}
        }

        public void Close()
        {
            this.mNetServer.GetFakeSocketMgr().RemoveFakeSocket(this);
        }
    }
}
