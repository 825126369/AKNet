using System;
using System.Net.Sockets;

namespace AKNet.Udp2MSQuic.Common
{
    internal sealed class QuicStreamOptions
    {
        public QuicStreamType nType;
        public Action<SocketAsyncEventArgs> ReceiveBufferFunc { get; set; } = null!;
    }
}
