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
        private void SendUDPPackage2(NetUdpSendFixedSizePackage mPackage)
        {
            lock (mSendUDPPackageList)
            {
                var mBufferItem = mSendUDPPackageList.BeginSpan();

                Span<byte> mMemoryBuffer = mPackage.mSendArgs.MemoryBuffer.Span;
                ReadOnlySpan<byte> mEncodeHead = UdpPackageEncryption.EncodeHead(mPackage);
                mEncodeHead.CopyTo(mMemoryBuffer);
                mMemoryBuffer = mMemoryBuffer.Slice(mEncodeHead.Length);
                
                if (mPackage.WindowBuff != null)
                {
                    mPackage.WindowBuff.CopyTo(mMemoryBuffer, mPackage.WindowOffset, mPackage.WindowLength);
                }
                mPackage.mSendArgs.SetBuffer(0, mPackage.WindowLength + mEncodeHead.Length);

                mSocketItem.SendToAsync(mPackage.mSendArgs);
            }
        }

        public void WorkerThreadReceiveNetPackage(SocketAsyncEventArgs e)
        {
            ReadOnlySpan<byte> mBuff = e.MemoryBuffer.Span.Slice(e.Offset, e.BytesTransferred);
            while (true)
            {
                var mPackage = mThreadWorker.mReceivePackagePool.Pop();
                bool bSucccess = UdpPackageEncryption.Decode(mBuff, mPackage);
                if (bSucccess)
                {
                    int nReadBytesCount = mPackage.nBodyLength + Config.nUdpPackageFixedHeadSize;
                    ReceiveNetPackage(mPackage);
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
                    mThreadWorker.mReceivePackagePool.recycle(mPackage);
                    NetLog.LogError("解码失败 !!!");
                    break;
                }
            }
        }
    }
}
