/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:16
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using AKNet.Common;
using AKNet.Udp3Tcp.Common;

namespace AKNet.Udp3Tcp.Client
{
    internal partial class ClientPeer
    {
        private readonly NetStreamCircularBuffer mReceiveStreamList = null;
        protected readonly NetStreamPackage mNetPackage = new NetStreamPackage();
        private readonly Queue<NetUdpReceiveFixedSizePackage> mWaitCheckPackageQueue = new Queue<NetUdpReceiveFixedSizePackage>();
        internal ClientPeer mClientPeer = null;
        private int nCurrentCheckPackageCount = 0;
        public MsgReceiveMgr(ClientPeer mClientPeer)
        {
            this.mClientPeer = mClientPeer;
            mReceiveStreamList = new NetStreamCircularBuffer();
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
        }

        private bool NetCheckPackageExecute()
        {
            NetUdpReceiveFixedSizePackage mPackage = null;
            lock (mWaitCheckPackageQueue)
            {
                if (mWaitCheckPackageQueue.TryDequeue(out mPackage))
                {
                    if (!mPackage.orInnerCommandPackage())
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
            ReadOnlySpan<byte> mBuff = e.MemoryBuffer.Span.Slice(e.Offset, e.BytesTransferred);
            while (true)
            {
                var mPackage = mClientPeer.GetObjectPoolManager().UdpReceivePackage_Pop();
                bool bSucccess = UdpPackageEncryption.Decode(mBuff, mPackage);
                if (bSucccess)
                {
                    int nReadBytesCount = mPackage.nBodyLength + Config.nUdpPackageFixedHeadSize;
                    lock (mWaitCheckPackageQueue)
                    {
                        mWaitCheckPackageQueue.Enqueue(mPackage);
                        if (!mPackage.orInnerCommandPackage())
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
                    mClientPeer.GetObjectPoolManager().UdpReceivePackage_Recycle(mPackage);
                    NetLog.LogError($"解码失败: {e.MemoryBuffer.Length} {e.BytesTransferred} | {mBuff.Length}");
                    break;
                }
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

        public void ReceiveTcpStream(NetUdpReceiveFixedSizePackage mPackage)
        {
            mReceiveStreamList.WriteFrom(mPackage.GetTcpBufferSpan());
            while (NetTcpPackageExecute())
            {

            }
        }

        public void Reset()
        {
            lock (mWaitCheckPackageQueue)
            {
                while (mWaitCheckPackageQueue.TryDequeue(out var mPackage))
                {
                    mClientPeer.GetObjectPoolManager().UdpReceivePackage_Recycle(mPackage);
                }
            }
        }

        public void Release()
        {
            Reset();
        }

    }
}