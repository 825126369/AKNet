using System.Runtime.InteropServices;

namespace AKNet.Udp5MSQuic.Common
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct QUIC_STREAM_EVENT
    {
        [FieldOffset(0)] public QUIC_STREAM_EVENT_TYPE Type;

        [FieldOffset(8)] public START_COMPLETE_DATA START_COMPLETE;
        [FieldOffset(8)] public RECEIVE_DATA RECEIVE;
        [FieldOffset(8)] public SEND_COMPLETE_DATA SEND_COMPLETE;
        [FieldOffset(8)] public PEER_SEND_ABORTED_DATA PEER_SEND_ABORTED;
        [FieldOffset(8)] public PEER_RECEIVE_ABORTED_DATA PEER_RECEIVE_ABORTED;
        [FieldOffset(8)] public SEND_SHUTDOWN_COMPLETE_DATA SEND_SHUTDOWN_COMPLETE;
        [FieldOffset(8)] public IDEAL_SEND_BUFFER_SIZE_DATA IDEAL_SEND_BUFFER_SIZE;
        [FieldOffset(8)] public CANCEL_ON_LOSS_DATA CANCEL_ON_LOSS;
        [FieldOffset(8)] public SHUTDOWN_COMPLETE_DATA SHUTDOWN_COMPLETE;

        public struct START_COMPLETE_DATA
        {
            public int Status;
            public ulong ID;
            public bool PeerAccepted;
            public bool RESERVED;
        }

        public struct RECEIVE_DATA
        {
            public int AbsoluteOffset;
            public int TotalBufferLength;
            public readonly QUIC_BUFFER[] Buffers;
            public int BufferCount;
            public QUIC_RECEIVE_FLAGS Flags;

            public RECEIVE_DATA(int _ = 0)
            {
                BufferCount = 0;
                AbsoluteOffset = 0;
                TotalBufferLength = 0;
                Buffers = new QUIC_BUFFER[6];
                Flags = 0;
            }
        }

        public struct SEND_COMPLETE_DATA
        {
            public bool Canceled;
            public object ClientContext;
        }

        public struct PEER_SEND_ABORTED_DATA
        {
            public ulong ErrorCode;
        }

        public struct PEER_RECEIVE_ABORTED_DATA
        {
            public ulong ErrorCode;
        }

        public struct SEND_SHUTDOWN_COMPLETE_DATA
        {
            public bool Graceful;
        }

        public struct SHUTDOWN_COMPLETE_DATA
        {
            public bool ConnectionShutdown;
            public bool AppCloseInProgress;
            public bool ConnectionShutdownByApp;
            public bool ConnectionClosedRemotely;
            public bool RESERVED;
            public int ConnectionErrorCode;
            public int ConnectionCloseStatus;
        }

        public struct IDEAL_SEND_BUFFER_SIZE_DATA
        {
            public int ByteCount;
        }

        public struct CANCEL_ON_LOSS_DATA
        {
            public ulong ErrorCode;
        }
    }
}
