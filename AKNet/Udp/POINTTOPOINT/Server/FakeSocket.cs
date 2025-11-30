/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:17
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp.POINTTOPOINT.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace AKNet.Udp.POINTTOPOINT.Server
{
    internal class FakeSocket : IPoolItemInterface
    {
        private readonly UdpServer mNetServer;
        private readonly AkCircularSpanBuffer mWaitCheckStreamList = new AkCircularSpanBuffer();
        private readonly Queue<NetUdpFixedSizePackage> mWaitCheckPackageQueue = new Queue<NetUdpFixedSizePackage>();
        private SOCKET_PEER_STATE mConnectionState;

        public FakeSocket(UdpServer mNetServer)
        {
            this.mNetServer = mNetServer;
            this.mConnectionState = SOCKET_PEER_STATE.DISCONNECTED;
        }

        public IPEndPoint RemoteEndPoint { get; set; }

        public void MultiThreadingReceiveNetPackage(NetUdpFixedSizePackage mPackage)
        {
            if (Config.bFakeSocketManageConnectState)
            {
                if (this.mConnectionState == SOCKET_PEER_STATE.DISCONNECTED)
                {
                    if (mPackage.nPackageId == UdpNetCommand.COMMAND_CONNECT)
                    {
                        mNetServer.GetClientPeerMgr2().MultiThreadingHandleConnectedSocket(this);
                        this.mConnectionState = SOCKET_PEER_STATE.CONNECTED;
                    }
                }
                else if (this.mConnectionState == SOCKET_PEER_STATE.CONNECTED)
                {
                    if (mPackage.nPackageId == UdpNetCommand.COMMAND_DISCONNECT)
                    {
                        this.mConnectionState = SOCKET_PEER_STATE.DISCONNECTED;
                    }
                }

                if (this.mConnectionState == SOCKET_PEER_STATE.CONNECTED)
                {
                    lock (mWaitCheckPackageQueue)
                    {
                        mWaitCheckPackageQueue.Enqueue(mPackage);
                    }
                }
            }
            else
            {
                lock (mWaitCheckPackageQueue)
                {
                    mWaitCheckPackageQueue.Enqueue(mPackage);
                }
            }
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
            lock (mWaitCheckPackageQueue)
            {
                return mWaitCheckPackageQueue.TryDequeue(out mPackage);
            }
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
                    bool bSucccess = UdpPackageEncryption.Decode(mBuff, mPackage);
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

        public bool SendToAsync(SocketAsyncEventArgs mArg)
        {
            return this.mNetServer.GetSocketMgr().SendToAsync(mArg);
        }

        public void Reset()
        {
            lock (mWaitCheckPackageQueue)
            {
                while (mWaitCheckPackageQueue.TryDequeue(out var mPackage))
                {
                    mNetServer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
                }
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
