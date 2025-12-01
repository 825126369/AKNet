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
using AKNet.Udp2Tcp.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace AKNet.Udp2Tcp.Server
{
    internal class FakeSocket : IPoolItemInterface
    {
        private readonly UdpServer mNetServer;
        private readonly Queue<NetUdpFixedSizePackage> mWaitCheckPackageQueue = new Queue<NetUdpFixedSizePackage>();
        private int nCurrentCheckPackageCount = 0;
        public IPEndPoint RemoteEndPoint { get; set; }

        public FakeSocket(UdpServer mNetServer)
        {
            this.mNetServer = mNetServer;
        }

        public void MultiThreadingReceiveNetPackage(SocketAsyncEventArgs e)
        {
            Span<byte> mBuff = e.MemoryBuffer.Span.Slice(e.Offset, e.BytesTransferred);
            while (true)
            {
                var mPackage = mNetServer.GetObjectPoolManager().NetUdpFixedSizePackage_Pop();
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

        public int GetCurrentFrameRemainPackageCount()
        {
            return nCurrentCheckPackageCount;
        }

        public bool GetReceivePackage(out NetUdpFixedSizePackage mPackage)
        {
            lock (mWaitCheckPackageQueue)
            {
                if (mWaitCheckPackageQueue.TryDequeue(out mPackage))
                {
                    if (UdpNetCommand.orNeedCheck(mPackage.GetPackageId()))
                    {
                        nCurrentCheckPackageCount--;
                    }
                    return true;
                }
                return false;
            }
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
                    mNetServer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
                }
            }
        }

        public void Close()
        {
            this.mNetServer.GetFakeSocketMgr().RemoveFakeSocket(this);
        }
    }
}
