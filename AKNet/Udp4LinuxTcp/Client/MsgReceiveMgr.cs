/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/20 10:55:54
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp4LinuxTcp.Common;
using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace AKNet.Udp4LinuxTcp.Client
{
    internal class MsgReceiveMgr
    {
        private readonly AkCircularBuffer mReceiveStreamList = null;
        protected readonly LikeTcpNetPackage mNetPackage = new LikeTcpNetPackage();
        private readonly Queue<sk_buff> mWaitCheckPackageQueue = new Queue<sk_buff>();
        internal ClientPeer mClientPeer = null;

        public MsgReceiveMgr(ClientPeer mClientPeer)
        {
            this.mClientPeer = mClientPeer;
            mReceiveStreamList = new AkCircularBuffer();
        }

        public void Update(double elapsed)
        {
            while (NetCheckPackageExecute())
            {

            }
        }

        private bool NetCheckPackageExecute()
        {
            sk_buff mPackage = null;
            lock (mWaitCheckPackageQueue)
            {
                mWaitCheckPackageQueue.TryDequeue(out mPackage);
            }

            if (mPackage != null)
            {
                UdpStatistical.AddReceivePackageCount();
                mClientPeer.mUdpCheckPool.ReceiveNetPackage(mPackage);
                return true;
            }

            return false;
        }

        public void MultiThreading_ReceiveWaitCheckNetPackage(SocketAsyncEventArgs e)
        {
            ReadOnlySpan<byte> mBuff = e.MemoryBuffer.Span.Slice(e.Offset, e.BytesTransferred);
            var skb = LinuxTcpFunc.build_skb(mBuff);
            
            mWaitCheckPackageQueue.Enqueue(skb);
        }

        private bool NetTcpPackageExecute()
        {
            bool bSuccess = LikeTcpNetPackageEncryption.Decode(mReceiveStreamList, mNetPackage);
            if (bSuccess)
            {
                mClientPeer.NetPackageExecute(mNetPackage);
            }
            return bSuccess;
        }

        public void ReceiveTcpStream(sk_buff mPackage)
        {
            mReceiveStreamList.WriteFrom(mPackage.GetTcpReceiveBufferSpan());
            while (NetTcpPackageExecute())
            {

            }
        }

        public void Reset()
        {
            //lock (mWaitCheckPackageQueue)
            //{
            //    while (mWaitCheckPackageQueue.TryDequeue(out var mPackage))
            //    {
            //        mClientPeer.GetObjectPoolManager().Skb_Recycle(mPackage);
            //    }
            //}
        }

        public void Release()
        {
            Reset();
        }

    }
}