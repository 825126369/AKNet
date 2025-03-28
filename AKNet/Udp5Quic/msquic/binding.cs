using AKNet.Common;
using System.Threading;

namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_BINDING
    {
        public CXPLAT_LIST_ENTRY Link;
        public bool Exclusive;
        public bool ServerOwned;
        public bool Connected;
        public uint RefCount;
        public uint RandomReservedVersion;
        public uint CompartmentId;
        public CXPLAT_SOCKET Socket;
        public readonly object RwLock = new object();
        public CXPLAT_LIST_ENTRY Listeners;
        public QUIC_LOOKUP Lookup;

        public readonly object StatelessOperLock = new object();
        CXPLAT_HASHTABLE StatelessOperTable;
        CXPLAT_LIST_ENTRY StatelessOperList;
        CXPLAT_POOL StatelessOperCtxPool;
        public uint StatelessOperCount;

        public class Stats
        {
            public class Recv
            {
                public ulong DroppedPackets;
            }
        }
    }

    internal class QUIC_RX_PACKET:CXPLAT_RECV_DATA
    {
        public ulong PacketId;
        public ulong PacketNumber;
        public ulong SendTimestamp;
            
        public byte[] AvailBuffer;
        public QUIC_HEADER_INVARIANT Invariant;
        public QUIC_VERSION_NEGOTIATION_PACKET VerNeg;
        public QUIC_LONG_HEADER_V1 LH;
        public QUIC_RETRY_PACKET_V1 Retry;
        public QUIC_SHORT_HEADER_V1 SH;
        
        public byte[] DestCid = null;
        public byte[] SourceCid = null;
        public ushort AvailBufferLength;
        public ushort HeaderLength;
        public ushort PayloadLength;
        public byte DestCidLen;
        public byte SourceCidLen;

        public QUIC_PACKET_KEY_TYPE KeyType;
        public uint Flags;
        public byte AssignedToConnection;
        public byte ValidatedHeaderInv;
        public byte IsShortHeader;
        public byte ValidatedHeaderVer;
        public byte ValidToken;
        public byte PacketNumberSet;
        public byte Encrypted;
        public byte EncryptedWith0Rtt;
        public byte ReleaseDeferred;
        public byte CompletelyValid;
        public byte NewLargestPacketNumber;
        public byte HasNonProbingFrame;
    }

    internal static partial class MSQuicFunc
    {
        public static void QuicBindingReceive(CXPLAT_SOCKET Socket, QUIC_BINDING RecvCallbackContext, CXPLAT_RECV_DATA DatagramChain)
        {
            NetLog.Assert(RecvCallbackContext != null);
            NetLog.Assert(DatagramChain != null);

            QUIC_BINDING Binding = RecvCallbackContext;
            CXPLAT_RECV_DATA ReleaseChain = null;
            CXPLAT_RECV_DATA ReleaseChainTail = ReleaseChain;
            CXPLAT_RECV_DATA SubChain = null;
            CXPLAT_RECV_DATA SubChainTail = SubChain;
            CXPLAT_RECV_DATA SubChainDataTail = SubChain;
            int SubChainLength = 0;
            int SubChainBytes = 0;
            int TotalChainLength = 0;
            int TotalDatagramBytes = 0;

            NetLog.Assert(Socket == Binding.Socket);

            ushort Partition = DatagramChain.PartitionIndex;
            ulong PartitionShifted = ((ulong)Partition + 1) << 40;

            CXPLAT_RECV_DATA Datagram;
            while ((Datagram = DatagramChain) != null)
            {
                TotalChainLength++;
                TotalDatagramBytes += Datagram.BufferLength;
                DatagramChain = Datagram.Next;
                Datagram.Next = null;

                QUIC_RX_PACKET Packet = Datagram as QUIC_RX_PACKET;
                Packet.PacketId = PartitionShifted | Interlocked.Add(QuicLibraryGetPerProc().ReceivePacketId);
                Packet.PacketNumber = 0;
                Packet.SendTimestamp = ulong.MaxValue;
                Packet.AvailBuffer = Datagram.Buffer;
                Packet.DestCid = null;
                Packet.SourceCid = null;
                Packet.AvailBufferLength = Datagram.BufferLength;
                Packet.HeaderLength = 0;
                Packet.PayloadLength = 0;
                Packet.DestCidLen = 0;
                Packet.SourceCidLen = 0;
                Packet.KeyType = QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL;
                Packet.Flags = 0;

                NetLog.Assert(Packet.PacketId != 0);
                QuicTraceEvent(QuicEventId.PacketReceive, "[pack][%llu] Received", Packet.PacketId);

                bool ReleaseDatagram;
                if (!QuicBindingPreprocessPacket(Binding, (QUIC_RX_PACKET)Datagram, ReleaseDatagram))
                {
                    if (ReleaseDatagram)
                    {
                        ReleaseChainTail = Datagram;
                        ReleaseChainTail = Datagram.Next;
                    }
                    continue;
                }

                NetLog.Assert(Packet.DestCid != null);
                NetLog.Assert(Packet.DestCidLen != 0 || Binding.Exclusive);
                NetLog.Assert(Packet.ValidatedHeaderInv != null);

                if (!Binding.Exclusive && SubChain != null)
                {
                    QUIC_RX_PACKET SubChainPacket = (QUIC_RX_PACKET)SubChain;
                    if (Packet.DestCidLen != SubChainPacket.DestCidLen || !orBufferEqual(Packet.DestCid, SubChainPacket.DestCid, Packet.DestCidLen))
                    {
                        if (!QuicBindingDeliverPackets(Binding, (QUIC_RX_PACKET)SubChain, SubChainLength, SubChainBytes))
                        {
                            ReleaseChainTail = SubChain;
                            ReleaseChainTail = SubChainDataTail;
                        }
                        SubChain = null;
                        SubChainTail = SubChain;
                        SubChainDataTail = SubChain;
                        SubChainLength = 0;
                        SubChainBytes = 0;
                    }
                }

                SubChainLength++;
                SubChainBytes += Datagram.BufferLength;
                if (!QuicPacketIsHandshake(Packet.Invariant))
                {
                    SubChainDataTail = Datagram;
                    SubChainDataTail = Datagram.Next;
                }
                else
                {
                    if (SubChainTail == null)
                    {
                        SubChainTail = Datagram;
                        SubChainTail = Datagram.Next;
                        SubChainDataTail = Datagram.Next;
                    }
                    else
                    {
                        Datagram.Next = SubChainTail;
                        SubChainTail = Datagram;
                        SubChainTail = Datagram.Next;
                    }
                }
            }

            if (SubChain != null)
            {
                if (!QuicBindingDeliverPackets(Binding, (QUIC_RX_PACKET)SubChain, SubChainLength, SubChainBytes))
                {
                    ReleaseChainTail = SubChain;
                    ReleaseChainTail = SubChainTail;
                }
            }

            if (ReleaseChain != NULL)
            {
                CxPlatRecvDataReturn(ReleaseChain);
            }

            QuicPerfCounterAdd(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_UDP_RECV, TotalChainLength);
            QuicPerfCounterAdd(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_UDP_RECV_BYTES, TotalDatagramBytes);
            QuicPerfCounterAdd(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_UDP_RECV_EVENTS);
        }
    }
}
