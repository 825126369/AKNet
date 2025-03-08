/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp5Quic.Common;
using System;
using System.Net;
using System.Net.Sockets;

namespace AKNet.Udp5Quic.Server
{
    internal class FakeSocket : IPoolItemInterface
    {
        private readonly UdpServer mNetServer;
        private ClientPeer mClientPeer;
        private readonly AkCircularSpanBuffer mAkCircularSpanBuffer = new AkCircularSpanBuffer();
        public IPEndPoint RemoteEndPoint;
        public FakeSocket(UdpServer mNetServer)
        {
            this.mNetServer = mNetServer;
        }

        public void SetClientPeer(ClientPeer mClientPeer)
        {
            this.mClientPeer = mClientPeer;
        }

        public void MultiThreadingReceiveNetPackage(SocketAsyncEventArgs e)
        {
            ReadOnlySpan<byte> mBuff = e.MemoryBuffer.Span.Slice(e.Offset, e.BytesTransferred);
            lock (mAkCircularSpanBuffer)
            {
                mAkCircularSpanBuffer.WriteFrom(mBuff);
            }
        }

        public sk_buff GetReceivePackage()
        {
            MainThreadCheck.Check();

            sk_buff mPackage = null;
            lock (mAkCircularSpanBuffer)
            {
                if (mAkCircularSpanBuffer.GetSpanCount() > 0)
                {
                    mPackage = this.mClientPeer.GetObjectPoolManager().Skb_Pop();
                    LinuxTcpFunc.build_skb(mPackage);
                    int nSize = mAkCircularSpanBuffer.WriteTo(mPackage.GetTailRoomSpan());
                    mPackage.nBufferLength += nSize;
                }
            }

            return mPackage; ;
        }
        
        public bool SendToAsync(SocketAsyncEventArgs mArg)
        {
            return this.mNetServer.GetSocketMgr().SendToAsync(mArg);
        }

        public void Reset()
        {
            MainThreadCheck.Check();
            lock (mAkCircularSpanBuffer)
            {
                mAkCircularSpanBuffer.reset();
            }
        }

        public void Close()
        {
            this.mNetServer.GetFakeSocketMgr().RemoveFakeSocket(this);
        }
    }
}
