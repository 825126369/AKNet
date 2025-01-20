/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/28 7:14:06
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.LinuxTcp;
using AKNet.Udp4LinuxTcp.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace AKNet.Udp4LinuxTcp.Server
{
    internal class FakeSocket : IPoolItemInterface
    {
        private readonly UdpServer mNetServer;
        private readonly Queue<sk_buff> mWaitCheckPackageQueue = new Queue<sk_buff>();
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
                var mPackage = new sk_buff();
                bool bSucccess = mNetServer.GetCryptoMgr().Decode(mBuff, mPackage);
                if (bSucccess)
                {
                    int nReadBytesCount = LinuxTcpFunc.ip_hdr(mPackage).tot_len;
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
                        NetLog.Assert(mBuff.Length == nReadBytesCount);
                        break;
                    }
                }
                else
                {
                    NetLog.LogError("解码失败 !!!");
                    break;
                }
            }
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
