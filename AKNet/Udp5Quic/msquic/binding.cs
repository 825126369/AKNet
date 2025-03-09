using AKNet.Common;

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

    internal static partial class MSQuicFunc
    {
        public static void QuicBindingReceive(CXPLAT_SOCKET Socket, QUIC_BINDING RecvCallbackContext, CXPLAT_RECV_DATA* DatagramChain)
        {
            NetLog.Assert(RecvCallbackContext != null);
            NetLog.Assert(DatagramChain != null);

            QUIC_BINDING* Binding = (QUIC_BINDING*)RecvCallbackContext;
            CXPLAT_RECV_DATA* ReleaseChain = NULL;
            CXPLAT_RECV_DATA** ReleaseChainTail = &ReleaseChain;
            CXPLAT_RECV_DATA* SubChain = NULL;
            CXPLAT_RECV_DATA** SubChainTail = &SubChain;
            CXPLAT_RECV_DATA** SubChainDataTail = &SubChain;
            uint32_t SubChainLength = 0;
            uint32_t SubChainBytes = 0;
            uint32_t TotalChainLength = 0;
            uint32_t TotalDatagramBytes = 0;

            CXPLAT_DBG_ASSERT(Socket == Binding->Socket);

            const uint16_t Partition = DatagramChain->PartitionIndex;
            const uint64_t PartitionShifted = ((uint64_t)Partition + 1) << 40;

            CXPLAT_RECV_DATA* Datagram;
            while ((Datagram = DatagramChain) != NULL)
            {
                TotalChainLength++;
                TotalDatagramBytes += Datagram->BufferLength;

                //
                // Remove the head.
                //
                DatagramChain = Datagram->Next;
                Datagram->Next = NULL;

                QUIC_RX_PACKET* Packet = (QUIC_RX_PACKET*)Datagram;
                Packet->PacketId =
                    PartitionShifted | InterlockedIncrement64((int64_t*)&QuicLibraryGetPerProc()->ReceivePacketId);
                Packet->PacketNumber = 0;
                Packet->SendTimestamp = UINT64_MAX;
                Packet->AvailBuffer = Datagram->Buffer;
                Packet->DestCid = NULL;
                Packet->SourceCid = NULL;
                Packet->AvailBufferLength = Datagram->BufferLength;
                Packet->HeaderLength = 0;
                Packet->PayloadLength = 0;
                Packet->DestCidLen = 0;
                Packet->SourceCidLen = 0;
                Packet->KeyType = QUIC_PACKET_KEY_INITIAL;
                Packet->Flags = 0;

                CXPLAT_DBG_ASSERT(Packet->PacketId != 0);
                QuicTraceEvent(
                    PacketReceive,
                    "[pack][%llu] Received",
                    Packet->PacketId);

#if QUIC_TEST_DATAPATH_HOOKS_ENABLED
        //
        // The test datapath receive callback allows for test code to modify
        // the datagrams on the receive path, and optionally indicate one or
        // more to be dropped.
        //
        QUIC_TEST_DATAPATH_HOOKS* Hooks = MsQuicLib.TestDatapathHooks;
        if (Hooks != NULL) {
            if (Hooks->Receive(Datagram)) {
                *ReleaseChainTail = Datagram;
                ReleaseChainTail = &Datagram->Next;
                QuicPacketLogDrop(Binding, Packet, "Test Dropped");
                continue;
            }
        }
#endif

                //
                // Perform initial validation.
                //
                BOOLEAN ReleaseDatagram;
                if (!QuicBindingPreprocessPacket(Binding, (QUIC_RX_PACKET*)Datagram, &ReleaseDatagram))
                {
                    if (ReleaseDatagram)
                    {
                        *ReleaseChainTail = Datagram;
                        ReleaseChainTail = &Datagram->Next;
                    }
                    continue;
                }

                CXPLAT_DBG_ASSERT(Packet->DestCid != NULL);
                CXPLAT_DBG_ASSERT(Packet->DestCidLen != 0 || Binding->Exclusive);
                CXPLAT_DBG_ASSERT(Packet->ValidatedHeaderInv);

                //
                // If the next datagram doesn't match the current subchain, deliver the
                // current subchain and start a new one.
                // (If the binding is exclusively owned, all datagrams are delivered to
                // the same connection and this chain-splitting step is skipped.)
                //
                if (!Binding->Exclusive && SubChain != NULL)
                {
                    QUIC_RX_PACKET* SubChainPacket = (QUIC_RX_PACKET*)SubChain;
                    if ((Packet->DestCidLen != SubChainPacket->DestCidLen ||
                         memcmp(Packet->DestCid, SubChainPacket->DestCid, Packet->DestCidLen) != 0))
                    {
                        if (!QuicBindingDeliverPackets(Binding, (QUIC_RX_PACKET*)SubChain, SubChainLength, SubChainBytes))
                        {
                            *ReleaseChainTail = SubChain;
                            ReleaseChainTail = SubChainDataTail;
                        }
                        SubChain = NULL;
                        SubChainTail = &SubChain;
                        SubChainDataTail = &SubChain;
                        SubChainLength = 0;
                        SubChainBytes = 0;
                    }
                }

                //
                // Insert the datagram into the current chain, with handshake packets
                // first (we assume handshake packets don't come after non-handshake
                // packets in a datagram).
                // We do this so that we can more easily determine if the chain of
                // packets can create a new connection.
                //

                SubChainLength++;
                SubChainBytes += Datagram->BufferLength;
                if (!QuicPacketIsHandshake(Packet->Invariant))
                {
                    *SubChainDataTail = Datagram;
                    SubChainDataTail = &Datagram->Next;
                }
                else
                {
                    if (*SubChainTail == NULL)
                    {
                        *SubChainTail = Datagram;
                        SubChainTail = &Datagram->Next;
                        SubChainDataTail = &Datagram->Next;
                    }
                    else
                    {
                        Datagram->Next = *SubChainTail;
                        *SubChainTail = Datagram;
                        SubChainTail = &Datagram->Next;
                    }
                }
            }

            if (SubChain != NULL)
            {
                //
                // Deliver the last subchain.
                //
                if (!QuicBindingDeliverPackets(Binding, (QUIC_RX_PACKET*)SubChain, SubChainLength, SubChainBytes))
                {
                    *ReleaseChainTail = SubChain;
                    ReleaseChainTail = SubChainTail; // cppcheck-suppress unreadVariable; NOLINT
                }
            }

            if (ReleaseChain != NULL)
            {
                CxPlatRecvDataReturn(ReleaseChain);
            }

            QuicPerfCounterAdd(QUIC_PERF_COUNTER_UDP_RECV, TotalChainLength);
            QuicPerfCounterAdd(QUIC_PERF_COUNTER_UDP_RECV_BYTES, TotalDatagramBytes);
            QuicPerfCounterIncrement(QUIC_PERF_COUNTER_UDP_RECV_EVENTS);
        }
    }
}
