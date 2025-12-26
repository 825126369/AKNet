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
using AKNet.Udp4Tcp.Common;
using System;
using System.Net.Sockets;
using System.Threading;

namespace AKNet.Udp4Tcp.Client
{
    internal partial class ClientPeer
    {
        private AutoResetEvent mEventQReady = new AutoResetEvent(false);

        private void InitThreadWorker()
        {
            Thread mThread = new Thread(ThreadFunc);
            mThread.IsBackground = true;
            mThread.Start();
        }

        private void ThreadFunc()
        {
            while (true)
            {
                mEventQReady.WaitOne();
                while (NetCheckPackageExecute())
                {
                    
                }

                mUdpCheckPool.Update();
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
                mUdpCheckPool.ReceiveNetPackage(mPackage);
                return true;
            }

            return false;
        }

        private void MultiThreading_ReceiveWaitCheckNetPackage(SocketAsyncEventArgs e)
        {
            ReadOnlySpan<byte> mBuff = e.MemoryBuffer.Span.Slice(e.Offset, e.BytesTransferred);
            while (true)
            {
                var mPackage = GetObjectPoolManager().UdpReceivePackage_Pop();
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
                    GetObjectPoolManager().UdpReceivePackage_Recycle(mPackage);
                    NetLog.LogError($"解码失败: {e.MemoryBuffer.Length} {e.BytesTransferred} | {mBuff.Length}");
                    break;
                }
            }

            mEventQReady.Set();
        }

    }
}









