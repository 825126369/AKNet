using AKNet.Common;
using AKNet.Udp.POINTTOPOINT.Common;
using System;
using System.Net;
using System.Net.Sockets;

namespace AKNet.Udp.POINTTOPOINT.Server
{
    internal class FakeSocket : IPoolItemInterface
    {
        private readonly UdpServer mNetServer;
        public event EventHandler<NetUdpFixedSizePackage> Completed;

        public FakeSocket(UdpServer mNetServer)
        {
            this.mNetServer = mNetServer;
        }

        public IPEndPoint RemoteEndPoint { get; set; }

        public void WriteFrom(NetUdpFixedSizePackage mPackage)
        {
            Completed?.Invoke(null, mPackage);
        }

        public bool SendToAsync(SocketAsyncEventArgs mArg)
        {
            return this.mNetServer.GetSocketMgr().SendToAsync(mArg);
        }

        public void Reset()
        {
            
        }

        public void Close()
        {
            mNetServer.GetFakeSocketManager().RemoveFakeSocket(this);
        }
    }
}
