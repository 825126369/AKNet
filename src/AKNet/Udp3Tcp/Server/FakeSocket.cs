/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:16
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
        private readonly ServerMgr mNetServer;
        private readonly Queue<NetUdpReceiveFixedSizePackage> mWaitCheckPackageQueue = new Queue<NetUdpReceiveFixedSizePackage>();
        private int nCurrentCheckPackageCount = 0;
        public IPEndPoint RemoteEndPoint;

        public FakeSocket(ServerMgr mNetServer)
        {
            this.mNetServer = mNetServer;
        }

        public void MultiThreadingReceiveNetPackage(SocketAsyncEventArgs e)
        {
            ReadOnlySpan<byte> mBuff = e.MemoryBuffer.Span.Slice(e.Offset, e.BytesTransferred);
            while (true)
            {
                var mPackage = mNetServer.GetObjectPoolManager().UdpReceivePackage_Pop();
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
            return this.mNetServer.SendToAsync(mArg);
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
