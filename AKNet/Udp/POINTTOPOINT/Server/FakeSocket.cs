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
        private readonly AkCircularSpanBuffer<byte> mReceiveBuffer = new AkCircularSpanBuffer<byte>();
        private readonly Queue<NetUdpFixedSizePackage> mWaitCheckPackageQueue = new Queue<NetUdpFixedSizePackage>();
        private readonly UdpServer mNetServer;

        public FakeSocket(UdpServer mNetServer)
        {
            this.mNetServer = mNetServer;
        }

        public IPEndPoint RemoteEndPoint { get; set; }

        public void WriteFrom(NetUdpFixedSizePackage mPackage)
        {
            if (Config.bUseSendStream)
            {
                lock (mReceiveBuffer)
                {
                    mReceiveBuffer.WriteFrom(mPackage.GetBufferSpan());
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

        public bool WriteTo(Span<byte> buffer)
        {
            lock (mReceiveBuffer)
            {
                if (mReceiveBuffer.CurrentSegmentLength > 0)
                {
                    mReceiveBuffer.WriteTo(buffer);
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
            lock (mReceiveBuffer)
            {
                mReceiveBuffer.reset();
            }
        }
    }
}
