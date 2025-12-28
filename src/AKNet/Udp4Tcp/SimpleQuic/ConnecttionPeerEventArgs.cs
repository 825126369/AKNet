using System;

namespace AKNet.Udp4Tcp.Common
{
    internal class ConnectionPeerEventArgs
    {
        public enum E_OP_TYPE
        {
            None = 0,
            Accept,
            Connect,
            Disconnect,
            Receive,
            Send,
        }

        public event EventHandler<ConnectionPeerEventArgs> Completed;
        public E_OP_TYPE nLastOpType;
        public Memory<byte> mBuffer;
        public ConnectionPeer mConnectionPeer;
        public bool Result;

        public void TriggerEvent()
        {
            Completed?.Invoke(null, this);
        }
    }
}
