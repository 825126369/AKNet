/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using AKNet.Common;
using AKNet.Udp2Tcp.Common;

namespace AKNet.Udp2Tcp.Client
{
    internal class MsgReceiveMgr
    {
        private readonly AkCircularManyBuffer mReceiveStreamList = null;
        protected readonly TcpNetPackage mNetPackage = new TcpNetPackage();
        private readonly Queue<NetUdpFixedSizePackage> mWaitCheckPackageQueue = new Queue<NetUdpFixedSizePackage>();
        private int nCurrentCheckPackageCount = 0;
        internal ClientPeer mClientPeer = null;

        public MsgReceiveMgr(ClientPeer mClientPeer)
        {
            this.mClientPeer = mClientPeer;
            mReceiveStreamList = new AkCircularManyBuffer();
        }

        public int GetCurrentFrameRemainPackageCount()
        {
            return nCurrentCheckPackageCount;
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
                if (mWaitCheckPackageQueue.TryDequeue(out mPackage))
                {
                    if (UdpNetCommand.orNeedCheck(mPackage.GetPackageId()))
                    {
                        nCurrentCheckPackageCount--;
                    }
                }
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
                bool bSucccess = UdpPackageEncryption.Decode(mBuff, mPackage);
                if (bSucccess)
                {
                    int nReadBytesCount = mPackage.Length;

                    lock (mWaitCheckPackageQueue)
                    {
                        mWaitCheckPackageQueue.Enqueue(mPackage);
                        if (UdpNetCommand.orNeedCheck(mPackage.GetPackageId()))
                        {
                            nCurrentCheckPackageCount++;
                        }
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
            bool bSuccess = mClientPeer.mCryptoMgr.Decode(mReceiveStreamList, mNetPackage);
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
            mReceiveStreamList.Dispose();
        }
    }

}