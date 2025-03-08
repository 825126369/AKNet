using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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
}
