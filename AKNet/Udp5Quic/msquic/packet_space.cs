using AKNet.Common;
using System.Diagnostics;

namespace AKNet.Udp5Quic.Common
{
    internal enum QUIC_ENCRYPT_LEVEL
    {
        QUIC_ENCRYPT_LEVEL_INITIAL,
        QUIC_ENCRYPT_LEVEL_HANDSHAKE,
        QUIC_ENCRYPT_LEVEL_1_RTT,       // Also used for 0-RTT
        QUIC_ENCRYPT_LEVEL_COUNT
    }

    internal class QUIC_PACKET_SPACE: CXPLAT_POOL_Interface<QUIC_PACKET_SPACE>
    {
        public readonly CXPLAT_POOL_ENTRY<QUIC_PACKET_SPACE> POOL_ENTRY = null;

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
        
        public QUIC_PACKET_SPACE()
        {
            POOL_ENTRY = new CXPLAT_POOL_ENTRY<QUIC_PACKET_SPACE>(this);
        }
        public CXPLAT_POOL_ENTRY<QUIC_PACKET_SPACE> GetEntry()
        {
            return POOL_ENTRY;
        }
        public void Reset()
        {
            throw new System.NotImplementedException();
        }
    }

    internal static partial class MSQuicFunc
    {
        static ulong QuicPacketSpaceInitialize(QUIC_CONNECTION Connection, QUIC_ENCRYPT_LEVEL EncryptLevel, QUIC_PACKET_SPACE NewPackets)
        {
            QUIC_PACKET_SPACE Packets = QuicLibraryGetPerProc().PacketSpacePool.Pop();
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

        static void QuicPacketSpaceUninitialize(QUIC_PACKET_SPACE Packets)
        {
            if (Packets.DeferredPackets != null)
            {
                QUIC_RX_PACKET Packet = Packets.DeferredPackets;
                do
                {
                    Packet.QueuedOnConnection = false;
                } while ((Packet = (QUIC_RX_PACKET)Packet.Next) != null);
                CxPlatRecvDataReturn(Packets.DeferredPackets);
            }

            QuicAckTrackerUninitialize(Packets.AckTracker);
            QuicLibraryGetPerProc().PacketSpacePool.recycle(Packets);
        }

        static QUIC_ENCRYPT_LEVEL QuicKeyTypeToEncryptLevel(QUIC_PACKET_KEY_TYPE KeyType)
        {
            switch (KeyType)
            {
                case QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL: 
                    return QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_INITIAL;
                case QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_0_RTT: 
                    return QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_1_RTT;
                case QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_HANDSHAKE: 
                    return QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_HANDSHAKE;
                case QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT:
                default: 
                    return QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_1_RTT;
            }
        }

        static QUIC_PACKET_SPACE QuicAckTrackerGetPacketSpace(QUIC_ACK_TRACKER Tracker)
        {
            return CXPLAT_CONTAINING_RECORD<QUIC_PACKET_SPACE>(Tracker, QUIC_PACKET_SPACE, AckTracker);
        }

    }
}
