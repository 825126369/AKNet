/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp5Quic.Common;
using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace AKNet.Udp5Quic.Client
{
    internal class MsgReceiveMgr
    {
        private readonly AkCircularBuffer mReceiveStreamList = null;
        protected readonly LikeTcpNetPackage mNetPackage = new LikeTcpNetPackage();
        private readonly Queue<sk_buff> mWaitCheckPackageQueue = new Queue<sk_buff>();
        internal ClientPeer mClientPeer = null;
        private readonly msghdr mTcpMsg = null;

        public MsgReceiveMgr(ClientPeer mClientPeer)
        {
            this.mClientPeer = mClientPeer;
            mReceiveStreamList = new AkCircularBuffer();
            mTcpMsg = new msghdr(mReceiveStreamList, 1500);
        }

        public void Update(double elapsed)
        {
            while (NetCheckPackageExecute())
            {

            }

            ReceiveTcpStream();
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
                mClientPeer.mUdpCheckPool.ReceiveNetPackage(mPackage);
                return true;
            }

            return false;
        }

        public void MultiThreading_ReceiveWaitCheckNetPackage(SocketAsyncEventArgs e)
        {
            ReadOnlySpan<byte> mBuff = e.MemoryBuffer.Span.Slice(e.Offset, e.BytesTransferred);
            var skb = mClientPeer.GetObjectPoolManager().Skb_Pop();
            skb = LinuxTcpFunc.build_skb(skb, mBuff);

            lock (mWaitCheckPackageQueue)
            {
                mWaitCheckPackageQueue.Enqueue(skb);
            }
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

        public void ReceiveTcpStream()
        {
            while (mClientPeer.mUdpCheckPool.ReceiveTcpStream(mTcpMsg))
            {
                while (NetTcpPackageExecute())
                {

                }
            }
        }

        public void Reset()
        {
           
        }

        public void Release()
        {
            Reset();
        }

    }
}