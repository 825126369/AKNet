namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_MTU_DISCOVERY
    {
        public ulong SearchCompleteEnterTimeUs;
        public ushort MaxMtu;
        public ushort ProbeSize;
        public byte ProbeCount;
        public bool IsSearchComplete;
        public bool HasProbed1500;
    }

    internal static partial class MSQuicFunc
    {
        static void QuicMtuDiscoverySendProbePacket(QUIC_CONNECTION Connection)
        {
            QuicSendSetSendFlag(Connection.Send, QUIC_CONN_SEND_FLAG_DPLPMTUD);
        }

        public static bool orBufferEqual(byte[] buffer1, byte[] buffer2, int nLength)
        {
            if (buffer1.Length < nLength) return false;
            if (buffer2.Length < nLength) return false;

            for (int i = 0; i < nLength; i++)
            {
                if (buffer1[i] != buffer2[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}
