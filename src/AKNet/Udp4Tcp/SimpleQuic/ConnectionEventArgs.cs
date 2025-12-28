using System;
using System.Net;

namespace AKNet.Udp4Tcp.Common
{
    public enum ConnectionAsyncOperation
    {
        None = 0,
        Accept,
        Connect,
        Disconnect,
        Receive,
        Send,
    }

    public enum ConnectionError
    {
        Success = 1,
        Error = 2,
    }

    internal class ConnectionEventArgs
    {
        public event EventHandler<ConnectionEventArgs> Completed;
        public ConnectionAsyncOperation LastOperation;
        public Memory<byte> MemoryBuffer;
        public int Offset;
        public int Length;
        public int BytesTransferred;

        public ConnectionPeer AcceptConnection;
        public ConnectionPeer mConnectionPeer;
        public IPEndPoint RemoteEndPoint;
        public ConnectionError ConnectionError;

        public void SetBuffer(int Offset, int Length)
        {
            this.Offset = Offset;
            this.Length = Length;
        }

        public ReadOnlySpan<byte> GetSpan()
        {
            return MemoryBuffer.Span.Slice(Offset, Length);
        }

        public void TriggerEvent()
        {
            Completed?.Invoke(null, this);
        }
    }
}
