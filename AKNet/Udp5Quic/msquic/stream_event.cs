using System.Collections.Generic;

namespace AKNet.Udp5Quic.Common
{
    internal ref struct QUIC_STREAM_EVENT
    {
        public QUIC_STREAM_EVENT_TYPE Type;

        public START_COMPLETE_DATA START_COMPLETE;
        public RECEIVE_DATA RECEIVE;
        public SEND_COMPLETE_DATA SEND_COMPLETE;
        public PEER_SEND_ABORTED_DATA PEER_SEND_ABORTED;
        public PEER_RECEIVE_ABORTED_DATA PEER_RECEIVE_ABORTED;
        public SEND_SHUTDOWN_COMPLETE_DATA SEND_SHUTDOWN_COMPLETE;
        public IDEAL_SEND_BUFFER_SIZE_DATA IDEAL_SEND_BUFFER_SIZE;
        public CANCEL_ON_LOSS_DATA CANCEL_ON_LOSS;
        public SHUTDOWN_COMPLETE_DATA SHUTDOWN_COMPLETE;

        public ref struct START_COMPLETE_DATA
        {
            public ulong Status;
            public ulong ID;
            public bool PeerAccepted;
            public bool RESERVED;
        }

        public ref struct RECEIVE_DATA
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
            public ulong ConnectionErrorCode;
            public ulong ConnectionCloseStatus;
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
