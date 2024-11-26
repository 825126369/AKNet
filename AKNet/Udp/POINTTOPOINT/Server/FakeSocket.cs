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
        private readonly AkCircularSpanBuffer<byte> mWaitCheckStreamList = new AkCircularSpanBuffer<byte>();
        private readonly Queue<NetUdpFixedSizePackage> mWaitCheckPackageQueue = new Queue<NetUdpFixedSizePackage>();

        public FakeSocket(UdpServer mNetServer)
        {
            this.mNetServer = mNetServer;
        }

        public IPEndPoint RemoteEndPoint { get; set; }

        public void MultiThreadingReceiveNetPackage(NetUdpFixedSizePackage mPackage)
        {
            lock (mWaitCheckPackageQueue)
            {
                mWaitCheckPackageQueue.Enqueue(mPackage);
            }
        }

        public void MultiThreadingReceiveNetPackage(SocketAsyncEventArgs e)
        {
            lock (mWaitCheckStreamList)
            {
                mWaitCheckStreamList.WriteFrom(e.Buffer.AsSpan().Slice(e.Offset, e.BytesTransferred));
            }
        }

        public bool GetReceivePackage(out NetUdpFixedSizePackage mPackage)
        {
            MainThreadCheck.Check();
            if (Config.bUseFakeSocketManager2)
            {
                GetReceivePackage();
                return mWaitCheckPackageQueue.TryDequeue(out mPackage);
            }
            else
            {
                lock (mWaitCheckPackageQueue)
                {
                    return mWaitCheckPackageQueue.TryDequeue(out mPackage);
                }
            }
        }
        
        private readonly byte[] mCacheBuffer = new byte[Config.nUdpPackageFixedSize];
        private void GetReceivePackage()
        {
            MainThreadCheck.Check();

            Span<byte> mBuff = mCacheBuffer;

            int nLength = mWaitCheckStreamList.CurrentSegmentLength;
            if (nLength > 0)
            {
                mBuff = mBuff.Slice(0, nLength);
                lock (mWaitCheckStreamList)
                {
                    mWaitCheckStreamList.WriteTo(mBuff);
                }
                
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
                mWaitCheckPackageQueue.Clear();
            }
        }

        public void Close()
        {
            this.mNetServer.GetFakeSocketManager().RemoveFakeSocket(this);
        }
    }
}
