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
using System;
using System.Net.Sockets;

namespace AKNet.Udp4Tcp.Common
{
    internal partial class ConnectionPeer
    {
        public void MultiThreadingReceiveNetPackage(SocketAsyncEventArgs e)
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

        private void ProcessSend(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                SendNetStream2(e.BytesTransferred);
            }
            else
            {
                NetLog.LogError(e.SocketError);
                bSendIOContexUsed = false;
            }
        }

        public void AddUDPPackage(NetUdpSendFixedSizePackage mPackage)
        {
            lock (mSendStreamList)
            {
                var mBufferItem = mSendStreamList.BeginSpan();
                mSendStreamList.WriteFrom(UdpPackageEncryption.EncodeHead(mPackage));
                if (mPackage.WindowBuff != null)
                {
                    mPackage.WindowBuff.CopyTo(mBufferItem.GetCanWriteSpan(), mPackage.WindowOffset, mPackage.WindowLength);
                    mBufferItem.nSpanLength += mPackage.WindowLength;
                }
                mSendStreamList.FinishSpan();
            }
        }

        public void SendUDPPackage2(NetUdpSendFixedSizePackage mPackage)
        {
            lock (mSendStreamList)
            {
                var mBufferItem = mSendStreamList.BeginSpan();
                mSendStreamList.WriteFrom(UdpPackageEncryption.EncodeHead(mPackage));
                if (mPackage.WindowBuff != null)
                {
                    mPackage.WindowBuff.CopyTo(mBufferItem.GetCanWriteSpan(), mPackage.WindowOffset, mPackage.WindowLength);
                    mBufferItem.nSpanLength += mPackage.WindowLength;
                }
                mSendStreamList.FinishSpan();
            }

            if (!bSendIOContexUsed)
            {
                bSendIOContexUsed = true;
                SendNetStream2();
            }
        }
        
        private void SendNetStream2(int BytesTransferred = -1)
        {
            if (BytesTransferred >= 0)
            {
                if (BytesTransferred != nLastSendBytesCount)
                {
                    NetLog.LogError("UDP 发生短写");
                }
            }

            var mSendArgSpan = SendArgs.Buffer.AsSpan();
            int nSendBytesCount = 0;
            lock (mSendStreamList)
            {
                nSendBytesCount += mSendStreamList.WriteToMax(mSendArgSpan);
            }

            if (nSendBytesCount > 0)
            {
                nLastSendBytesCount = nSendBytesCount;
                SendArgs.SetBuffer(0, nSendBytesCount);
                if (!SendToAsync(SendArgs))
                {
                    ProcessSend(this, SendArgs);
                }
            }
            else
            {
                bSendIOContexUsed = false;
            }
        }

        public void Close()
        {
            this.mNetServer.RemoveFakeSocket(this);
        }

    }
}
