/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/20 10:55:53
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
        private readonly Queue<NetUdpReceiveFixedSizePackage> mWaitCheckPackageQueue = new Queue<NetUdpReceiveFixedSizePackage>();
        private int nCurrentCheckPackageCount = 0;
        public IPEndPoint RemoteEndPoint;

        public FakeSocket(UdpServer mNetServer)
        {
            this.mNetServer = mNetServer;
        }

        public void MultiThreadingReceiveNetPackage(SocketAsyncEventArgs e)
        {
            ReadOnlySpan<byte> mBuff = e.MemoryBuffer.Span.Slice(e.Offset, e.BytesTransferred);
            while (true)
            {
                var mPackage = mNetServer.GetObjectPoolManager().UdpReceivePackage_Pop();
                bool bSucccess = mNetServer.GetCryptoMgr().Decode(mBuff, mPackage);
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
                        NetLog.Assert(mBuff.Length == nReadBytesCount);
                        break;
                    }
                }
                else
                {
                    mNetServer.GetObjectPoolManager().UdpReceivePackage_Recycle(mPackage);
                    NetLog.LogError("解码失败 !!!");
                    break;
                }
            }
        }

        public int GetCurrentFrameRemainPackageCount()
        {
            return nCurrentCheckPackageCount;
        }

        public bool GetReceivePackage(out NetUdpReceiveFixedSizePackage mPackage)
        {
            lock (mWaitCheckPackageQueue)
            {
                if (mWaitCheckPackageQueue.TryDequeue(out mPackage))
                {
                    if (!mPackage.orInnerCommandPackage())
                    {
                        nCurrentCheckPackageCount--;
                    }

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

            lock (mWaitCheckPackageQueue)
            {
                while (mWaitCheckPackageQueue.TryDequeue(out var mPackage))
                {
                    mNetServer.GetObjectPoolManager().UdpReceivePackage_Recycle(mPackage);
                }
            }
        }

        public void Close()
        {
            this.mNetServer.GetFakeSocketMgr().RemoveFakeSocket(this);
        }
    }
}
