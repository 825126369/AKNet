namespace AKNet.Udp5Quic.Common
{
    internal enum QUIC_ENCRYPT_LEVEL
    {
        QUIC_ENCRYPT_LEVEL_INITIAL,
        QUIC_ENCRYPT_LEVEL_HANDSHAKE,
        QUIC_ENCRYPT_LEVEL_1_RTT,       // Also used for 0-RTT
        QUIC_ENCRYPT_LEVEL_COUNT
    }

    internal class QUIC_PACKET_SPACE
    {
        public QUIC_ENCRYPT_LEVEL EncryptLevel;
        public byte DeferredPacketsCount;
        public ulong NextRecvPacketNumber;
        public ulong EcnEctCounter;
        public ulong EcnCeCounter; // maps to ecn_ce_counters in RFC 9002.
        public QUIC_CONNECTION Connection;
        public QUIC_RX_PACKET DeferredPackets;
        public QUIC_ACK_TRACKER AckTracker;
        public ulong WriteKeyPhaseStartPacketNumber;
        public ulong ReadKeyPhaseStartPacketNumber;
        public ulong CurrentKeyPhaseBytesSent;
        public bool CurrentKeyPhase;
        public bool AwaitingKeyPhaseConfirmation;
    }

    internal static partial class MSQuicFunc
    {
        static ulong QuicPacketSpaceInitialize(QUIC_CONNECTION Connection, QUIC_ENCRYPT_LEVEL EncryptLevel, QUIC_PACKET_SPACE NewPackets)
        {
            QUIC_PACKET_SPACE Packets = CxPlatPoolAlloc(QuicLibraryGetPerProc().PacketSpacePool);
            if (Packets == null)
            {
                return QUIC_STATUS_OUT_OF_MEMORY;
            }

            Packets.Connection = Connection;
            Packets.EncryptLevel = EncryptLevel;
            QuicAckTrackerInitialize(Packets.AckTracker);

            NewPackets = Packets;
            return QUIC_STATUS_SUCCESS;
        }
    }
}
