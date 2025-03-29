namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_SEND_BUFFER
    {
        public long PostedBytes;
        public long BufferedBytes;
        public long IdealBytes;
    }

    internal static partial class MSQuicFunc
    {
        static void QuicSendBufferInitialize(QUIC_SEND_BUFFER SendBuffer)
        {
            SendBuffer.IdealBytes = QUIC_DEFAULT_IDEAL_SEND_BUFFER_SIZE;
        }
    }
}
