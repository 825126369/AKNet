/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/28 7:14:06
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp3Tcp.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace AKNet.Udp3Tcp.Server
{
    internal class FakeSocket : IPoolItemInterface
    {
        private readonly UdpServer mNetServer;
        private readonly AkCircularSpanBuffer<byte> mWaitCheckStreamList = new AkCircularSpanBuffer<byte>();
        private readonly Queue<NetUdpFixedSizePackage> mWaitCheckPackageQueue = new Queue<NetUdpFixedSizePackage>();
        private SOCKET_PEER_STATE mConnectionState;

        public IPEndPoint RemoteEndPoint { get; set; }

        public FakeSocket(UdpServer mNetServer)
        {
            this.mNetServer = mNetServer;
            this.mConnectionState = SOCKET_PEER_STATE.DISCONNECTED;
        }
        
        public void MultiThreadingReceiveNetPackage(SocketAsyncEventArgs e)
        {
            lock (mWaitCheckStreamList)
            {
                mWaitCheckStreamList.WriteFrom(e.MemoryBuffer.Span.Slice(e.Offset, e.BytesTransferred));
            }
        }

        public int GetCurrentFrameRemainPackageCount()
        {
            return mWaitCheckPackageQueue.Count + mWaitCheckStreamList.GetSpanCount();
        }

        public bool GetReceivePackage(out NetUdpFixedSizePackage mPackage)
        {
            GetReceivePackage();
            return mWaitCheckPackageQueue.TryDequeue(out mPackage);
        }
        
        private readonly Memory<byte> mCacheBuffer = new byte[Config.nUdpPackageFixedSize];
        private void GetReceivePackage()
        {
            MainThreadCheck.Check();

            Span<byte> mBuff = mCacheBuffer.Span;

            int nLength = 0;

            lock (mWaitCheckStreamList)
            {
                nLength = mWaitCheckStreamList.WriteTo(mBuff);
            }

            if (nLength > 0)
            {
                mBuff = mBuff.Slice(0, nLength);
                while (true)
                {
                    var mPackage = mNetServer.GetObjectPoolManager().NetUdpFixedSizePackage_Pop();
                    bool bSucccess = mNetServer.GetCryptoMgr().Decode(mBuff, mPackage);
                    if (bSucccess)
                    {
                        int nReadBytesCount = mPackage.Length;
                        mWaitCheckPackageQueue.Enqueue(mPackage);
                        if (mBuff.Length > nReadBytesCount)
                        {
                            mBuff = mBuff.Slice(nReadBytesCount);
                        }
                        else
                        {
                            NetLog.Assert(mBuff.Length == nReadBytesCount);
                            break;
                        }
                    }
                    else
                    {
                        mNetServer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
                        NetLog.LogError("解码失败 !!!");
                        break;
                    }
                }
            }
        }

        public bool ConnectAsync()
        {

        }

        public bool SendToAsync(SocketAsyncEventArgs mArg)
        {
            return this.mNetServer.GetSocketMgr().SendToAsync(mArg);
        }

        public void Reset()
        {
            MainThreadCheck.Check();
            while (mWaitCheckPackageQueue.TryDequeue(out var mPackage))
            {
                mNetServer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
            }

            lock (mWaitCheckStreamList)
            {
                mWaitCheckStreamList.reset();
            }
        }

        public void Close()
        {
            this.mNetServer.GetFakeSocketMgr().RemoveFakeSocket(this);
        }
    }
}
