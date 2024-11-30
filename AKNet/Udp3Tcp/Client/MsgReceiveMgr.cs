/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/28 7:14:06
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using AKNet.Common;
using AKNet.Udp3Tcp.Common;

namespace AKNet.Udp3Tcp.Client
{
    internal class MsgReceiveMgr
    {
        private readonly AkCircularBuffer<byte> mReceiveStreamList = null;
        protected readonly LikeTcpNetPackage mNetPackage = new LikeTcpNetPackage();
        private readonly Queue<NetUdpFixedSizePackage> mWaitCheckPackageQueue = new Queue<NetUdpFixedSizePackage>();
        internal ClientPeer mClientPeer = null;

        public MsgReceiveMgr(ClientPeer mClientPeer)
        {
            this.mClientPeer = mClientPeer;
            mReceiveStreamList = new AkCircularBuffer<byte>();
        }

        public int GetCurrentFrameRemainPackageCount()
        {
            return mWaitCheckPackageQueue.Count;
        }

        public void Update(double elapsed)
        {
            while (NetCheckPackageExecute())
            {

            }

            while (NetTcpPackageExecute())
            {

            }
        }

        private bool NetCheckPackageExecute()
        {
            NetUdpFixedSizePackage mPackage = null;
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
            var mBuff = new ReadOnlySpan<byte>(e.Buffer, e.Offset, e.BytesTransferred);
            while (true)
            {
                var mPackage = mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Pop();
                bool bSucccess = mClientPeer.GetCryptoMgr().Decode(mBuff, mPackage);
                if (bSucccess)
                {
                    int nReadBytesCount = mPackage.Length;

                    lock (mWaitCheckPackageQueue)
                    {
                        mWaitCheckPackageQueue.Enqueue(mPackage);
                    }

                    if (mBuff.Length > nReadBytesCount)
                    {
                        mBuff = mBuff.Slice(nReadBytesCount);
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
                    NetLog.LogError($"解码失败: {e.Buffer.Length} {e.BytesTransferred} | {mBuff.Length}");
                    break;
                }
            }
        }

        private bool NetTcpPackageExecute()
        {
            bool bSuccess = LikeTcpNetPackageEncryption.Decode(mReceiveStreamList, mNetPackage);
            if (bSuccess)
            {
                mClientPeer.mPackageManager.NetPackageExecute(this.mClientPeer, mNetPackage);
            }
            return bSuccess;
        }

        public void ReceiveTcpStream(NetUdpFixedSizePackage mPackage)
        {
            mReceiveStreamList.WriteFrom(mPackage.GetTcpBufferSpan());
        }

        public void Reset()
        {
            lock (mWaitCheckPackageQueue)
            {
                while (mWaitCheckPackageQueue.TryDequeue(out var mPackage))
                {
                    mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
                }
            }
        }

        public void Release()
        {
            Reset();
        }

    }
}