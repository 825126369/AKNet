using System.Collections.Generic;

namespace AKNet.Udp5Quic.Common
{
    // public delegate void QUIC_LISTENER_CALLBACK_HANDLER(QUIC_LISTENER Listener, IntPtr Context, ref QUIC_NEW_CONNECTION_INFO Info);

    internal enum QUIC_SEND_FLAGS
    {
        QUIC_SEND_FLAG_NONE = 0x0000,
        QUIC_SEND_FLAG_ALLOW_0_RTT = 0x0001,   // Allows the use of encrypting with 0-RTT key.
        QUIC_SEND_FLAG_START = 0x0002,   // Asynchronously starts the stream with the sent data.
        QUIC_SEND_FLAG_FIN = 0x0004,   // Indicates the request is the one last sent on the stream.
        QUIC_SEND_FLAG_DGRAM_PRIORITY = 0x0008,   // Indicates the datagram is higher priority than others.
        QUIC_SEND_FLAG_DELAY_SEND = 0x0010,   // Indicates the send should be delayed because more will be queued soon.
        QUIC_SEND_FLAG_CANCEL_ON_LOSS = 0x0020,   // Indicates that a stream is to be cancelled when packet loss is detected.
        QUIC_SEND_FLAG_PRIORITY_WORK = 0x0040,   // Higher priority than other connection work.
        QUIC_SEND_FLAG_CANCEL_ON_BLOCKED = 0x0080,   // Indicates that a frame should be dropped when it can't be sent immediately.
    }

    internal class QUIC_BUFFER
    {
        public int Length;
        public byte[] Buffer;
    }

    internal enum QUIC_STREAM_EVENT_TYPE
    {
        QUIC_STREAM_EVENT_START_COMPLETE = 0,
        QUIC_STREAM_EVENT_RECEIVE = 1,
        QUIC_STREAM_EVENT_SEND_COMPLETE = 2,
        QUIC_STREAM_EVENT_PEER_SEND_SHUTDOWN = 3,
        QUIC_STREAM_EVENT_PEER_SEND_ABORTED = 4,
        QUIC_STREAM_EVENT_PEER_RECEIVE_ABORTED = 5,
        QUIC_STREAM_EVENT_SEND_SHUTDOWN_COMPLETE = 6,
        QUIC_STREAM_EVENT_SHUTDOWN_COMPLETE = 7,
        QUIC_STREAM_EVENT_IDEAL_SEND_BUFFER_SIZE = 8,
        QUIC_STREAM_EVENT_PEER_ACCEPTED = 9,
        QUIC_STREAM_EVENT_CANCEL_ON_LOSS = 10,
    }

    internal enum QUIC_RECEIVE_FLAGS
    {
        QUIC_RECEIVE_FLAG_NONE = 0x0000,
        QUIC_RECEIVE_FLAG_0_RTT = 0x0001,   // Data was encrypted with 0-RTT key.
        QUIC_RECEIVE_FLAG_FIN = 0x0002,   // FIN was included with this data.
    }

    internal class QUIC_STREAM_EVENT
    {
        public QUIC_STREAM_EVENT_TYPE Type;
        public START_COMPLETE_Class START_COMPLETE;








        public class START_COMPLETE_Class
        {
            public long Status;
            public ulong ID;
            public bool PeerAccepted;
            public bool RESERVED;
        }

        public class RECEIVE_Class
        {
            public ulong AbsoluteOffset;
            public ulong TotalBufferLength;
            public readonly List<QUIC_BUFFER> Buffers = new List<QUIC_BUFFER>();
            public QUIC_RECEIVE_FLAGS Flags;
        }

        public class SEND_COMPLETE_Class
        {
            public bool Canceled;
            void* ClientContext;
        }

        public class PEER_SEND_ABORTED_Class
        {
            public ulong ErrorCode;
        }

        public class PEER_RECEIVE_ABORTED_Class
        {
            public ulong ErrorCode;
        }

        public class SEND_SHUTDOWN_COMPLETE_Class
        {
            public bool Graceful;
        }

        public class SHUTDOWN_COMPLETE_Class
        {
            public bool ConnectionShutdown;
            public bool AppCloseInProgress;
            public bool ConnectionShutdownByApp;
            public bool ConnectionClosedRemotely;
            public bool RESERVED;
            public ulong ConnectionErrorCode;
            public long ConnectionCloseStatus;
        }
        
        public class IDEAL_SEND_BUFFER_SIZE_Class
        {
            public ulong ByteCount;
        }

        public class CANCEL_ON_LOSS_Class
        {
            public ulong ErrorCode;
        }
    }
}
