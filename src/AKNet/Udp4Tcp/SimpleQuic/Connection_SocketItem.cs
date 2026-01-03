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
    internal partial class Connection
    {
        private void SendUDPPackage2(NetUdpSendFixedSizePackage mPackage)
        {
            SimpleQuicFunc.ThreadCheck(this);

            SSocketAsyncEventArgs mSendArgs = mSendEventArgsPool.Pop();
            Span<byte> mMemoryBuffer = mSendArgs.MemoryBuffer.Span;
            ReadOnlySpan<byte> mEncodeHead = UdpPackageEncryption.EncodeHead(mPackage);
            mEncodeHead.CopyTo(mMemoryBuffer);
            mMemoryBuffer = mMemoryBuffer.Slice(mEncodeHead.Length);

            if (mPackage.WindowBuff != null)
            {
                mPackage.WindowBuff.CopyTo(mMemoryBuffer, mPackage.WindowOffset, mPackage.WindowLength);
            }

            mSendArgs.SetBuffer(0, mPackage.WindowLength + mEncodeHead.Length);
            mSendArgs.UserToken = mSendEventArgsPool;
            mSendArgs.RemoteEndPoint = RemoteEndPoint;
            mLogicWorker.mSocketItem.SendToAsync(mSendArgs);
        }

        public void WorkerThreadReceiveNetPackage(SocketAsyncEventArgs e)
        {
            if (e.RemoteEndPoint != RemoteEndPoint) return;

            SocketItem mSocketItem = e.UserToken as SocketItem;
            SimpleQuicFunc.ThreadCheck(this);
            ReadOnlySpan<byte> mBuff = e.MemoryBuffer.Span.Slice(e.Offset, e.BytesTransferred);
            NetUdpReceiveFixedSizePackage mPackage = null;

            while (true)
            {
                mPackage = mSocketItem.mLogicWorker.mThreadWorker.mReceivePackagePool.Pop();
                bool bSucccess = UdpPackageEncryption.Decode(mBuff, mPackage);
                if (bSucccess)
                {
                    int nReadBytesCount = mPackage.nBodyLength + Config.nUdpPackageFixedHeadSize;
                    mUdpCheckMgr.ReceiveNetPackage(mPackage);

                    if (!mPackage.orInnerCommandPackage())
                    {
                        nCurrentCheckPackageCount++;
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
                    mLogicWorker.mThreadWorker.mReceivePackagePool.recycle(mPackage);
                    NetLog.LogError("解码失败 !!!");
                    break;
                }
            }
        }
    }
}
