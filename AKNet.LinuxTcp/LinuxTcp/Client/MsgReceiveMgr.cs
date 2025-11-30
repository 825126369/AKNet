/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:21
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.LinuxTcp.Common;
using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace AKNet.LinuxTcp.Client
{
    internal class MsgReceiveMgr
    {
        private readonly NetStreamCircularBuffer mReceiveStreamList = null;
        protected readonly NetStreamPackage mNetPackage = new NetStreamPackage();
        private readonly Queue<sk_buff> mWaitCheckPackageQueue = new Queue<sk_buff>();
        internal ClientPeer mClientPeer = null;
        private readonly msghdr mTcpMsg = null;

        public MsgReceiveMgr(ClientPeer mClientPeer)
        {
            this.mClientPeer = mClientPeer;
            mReceiveStreamList = new NetStreamCircularBuffer();
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
            bool bSuccess = mClientPeer.mCryptoMgr.Decode(mReceiveStreamList, mNetPackage);
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