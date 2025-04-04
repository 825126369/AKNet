﻿using AKNet.Common;
using System.Collections.Generic;
using System.Net;
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
        public Dictionary<IPEndPoint, QUIC_STATELESS_CONTEXT> StatelessOperTable;
        public CXPLAT_LIST_ENTRY StatelessOperList;
        public CXPLAT_POOL StatelessOperCtxPool;
        public uint StatelessOperCount;
        public Stats_DATA Stats;

        public class Stats_DATA
        {
            public Recv_DATA Recv;

            public class Recv_DATA
            {
                public long DroppedPackets;
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
        public int AvailBufferLength;
        public int HeaderLength;
        public int PayloadLength;
        public int DestCidLen;
        public int SourceCidLen;

        public QUIC_PACKET_KEY_TYPE KeyType;
        public uint Flags;
        public bool AssignedToConnection;
        public bool ValidatedHeaderInv;
        public bool IsShortHeader;
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
                Packet.PacketId = PartitionShifted | Interlocked.Increment(ref QuicLibraryGetPerProc().ReceivePacketId);
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
                NetLog.Assert(Packet.ValidatedHeaderInv);

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

            if (ReleaseChain != null)
            {
                CxPlatRecvDataReturn(ReleaseChain);
            }

            QuicPerfCounterAdd(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_UDP_RECV, TotalChainLength);
            QuicPerfCounterAdd(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_UDP_RECV_BYTES, TotalDatagramBytes);
            QuicPerfCounterAdd(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_UDP_RECV_EVENTS);
        }

        static void QuicBindingUnreachable(CXPLAT_SOCKET Socket, QUIC_BINDING Context, IPAddress RemoteAddress)
        {
            NetLog.Assert(Context != null);
            NetLog.Assert(RemoteAddress != null);

            QUIC_BINDING Binding = (QUIC_BINDING)Context;
            QUIC_CONNECTION Connection = QuicLookupFindConnectionByRemoteAddr(Binding.Lookup, RemoteAddress);

            if (Connection != null)
            {
                QuicConnQueueUnreachable(Connection, RemoteAddress);
                QuicConnRelease(Connection,  QUIC_CONNECTION_REF.QUIC_CONN_REF_LOOKUP_RESULT);
            }
        }

        static bool QuicBindingDropBlockedSourcePorts(QUIC_BINDING Binding, QUIC_RX_PACKET Packet)
        {
            int SourcePort = QuicAddrGetPort(Packet.Route.RemoteAddress);
            ushort[] BlockedPorts = new ushort[]
            {
                    11211,  // memcache
                    5353,   // mDNS
                    1900,   // SSDP
                    500,    // IKE
                    389,    // CLDAP
                    161,    // SNMP
                    138,    // NETBIOS Datagram Service
                    137,    // NETBIOS Name Service
                    123,    // NTP
                    111,    // Portmap
                    53,     // DNS
                    19,     // Chargen
                    17,     // Quote of the Day
                    0,      // Unusable
            };

            for (int i = 0; i < BlockedPorts.Length && SourcePort <= BlockedPorts[i]; ++i)
            {
                if (BlockedPorts[i] == SourcePort) 
                {
                    QuicPacketLogDrop(Binding, Packet, "Blocked source port");
                    return true;
                }
            }

            return false;
        }

        static bool QuicBindingQueueStatelessReset(QUIC_BINDING Binding, QUIC_RX_PACKET Packet)
        {
            NetLog.Assert(!Binding.Exclusive);
            NetLog.Assert(!((QUIC_SHORT_HEADER_V1)Packet.Buffer).IsLongHeader);

            if (Packet.BufferLength <= QUIC_MIN_STATELESS_RESET_PACKET_LENGTH)
            {
                QuicPacketLogDrop(Binding, Packet, "Packet too short for stateless reset");
                return false;
            }

            if (Binding.Exclusive)
            {
                QuicPacketLogDrop(Binding, Packet, "No stateless reset on exclusive binding");
                return false;
            }

            return QuicBindingQueueStatelessOperation(Binding, QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_STATELESS_RESET, Packet);
        }

        static bool QuicBindingDeliverPackets(QUIC_BINDING Binding, QUIC_RX_PACKET Packets, int PacketChainLength, int PacketChainByteLength)
        {
            NetLog.Assert(Packets.ValidatedHeaderInv);

            QUIC_CONNECTION Connection;
            if (!Binding.ServerOwned || Packets.IsShortHeader)
            {
                Connection = QuicLookupFindConnectionByLocalCid(Binding.Lookup, Packets.DestCid, Packets.DestCidLen);
            }
            else
            {
                Connection = QuicLookupFindConnectionByRemoteHash(Binding.Lookup, Packets.Route.RemoteAddress,Packets.SourceCidLen, Packets.SourceCid);
            }

            if (Connection == null)
            {
                if (!Binding.ServerOwned)
                {
                    QuicPacketLogDrop(Binding, Packets, "No matching client connection");
                    return false;
                }

                if (Binding.Exclusive)
                {
                    QuicPacketLogDrop(Binding, Packets, "No connection on exclusive binding");
                    return false;
                }

                if (QuicBindingDropBlockedSourcePorts(Binding, Packets))
                {
                    return false;
                }

                if (Packets.IsShortHeader)
                {
                    return QuicBindingQueueStatelessReset(Binding, Packets);
                }

                if (Packets->Invariant->LONG_HDR.Version == QUIC_VERSION_VER_NEG)
                {
                    QuicPacketLogDrop(Binding, Packets, "Version negotiation packet not matched with a connection");
                    return FALSE;
                }

                NetLog.Assert(QuicIsVersionSupported(Packets.Invariant.LONG_HDR.Version));
                switch (Packets.Invariant.LONG_HDR.Version)
                {
                    case QUIC_VERSION_1:
                    case QUIC_VERSION_DRAFT_29:
                    case QUIC_VERSION_MS_1:
                        if (Packets.LH.Type != QUIC_LONG_HEADER_TYPE_V1.QUIC_INITIAL_V1)
                        {
                            QuicPacketLogDrop(Binding, Packets, "Non-initial packet not matched with a connection");
                            return FALSE;
                        }
                        break;
                    case QUIC_VERSION_2:
                        if (Packets->LH->Type != QUIC_INITIAL_V2)
                        {
                            QuicPacketLogDrop(Binding, Packets, "Non-initial packet not matched with a connection");
                            return FALSE;
                        }
                }

                byte[] Token = null;
                int TokenLength = 0;
                if (!QuicPacketValidateLongHeaderV1(
                        Binding,
                        TRUE,
                        Packets,
                        &Token,
                        &TokenLength,
                        false))
                {
                    return false;
                }

                NetLog.Assert(Token != null);

                if (!QuicBindingHasListenerRegistered(Binding))
                {
                    QuicPacketLogDrop(Binding, Packets, "No listeners registered to accept new connection.");
                    return false;
                }

                NetLog.Assert(Binding.ServerOwned);

                bool DropPacket = false;
                if (QuicBindingShouldRetryConnection( Binding, Packets, TokenLength, Token, &DropPacket))
                {
                    return QuicBindingQueueStatelessOperation(Binding, QUIC_OPER_TYPE_RETRY, Packets);
                }

                if (!DropPacket)
                {
                    Connection = QuicBindingCreateConnection(Binding, Packets);
                }
            }

            if (Connection == null)
            {
                return false;
            }

            QuicConnQueueRecvPackets(Connection, Packets, PacketChainLength, PacketChainByteLength);
            QuicConnRelease(Connection,  QUIC_CONNECTION_REF.QUIC_CONN_REF_LOOKUP_RESULT);
            return true;
        }

        static bool QuicBindingPreprocessPacket(QUIC_BINDING Binding,QUIC_RX_PACKET Packet,ref bool ReleaseDatagram)
        {
            Packet.AvailBuffer = Packet.Buffer;
            Packet.AvailBufferLength = Packet.BufferLength;

            ReleaseDatagram = true;
            if (!QuicPacketValidateInvariant(Binding, Packet, Binding.Exclusive))
            {
                return false;
            }

            if (Packet->Invariant->IsLongHeader)
            {
                //
                // Validate we support this long header packet version.
                //
                if (Packet->Invariant->LONG_HDR.Version != QUIC_VERSION_VER_NEG &&
                    !QuicVersionNegotiationExtIsVersionServerSupported(Packet->Invariant->LONG_HDR.Version))
                {
                    //
                    // The QUIC packet has an unsupported and non-VN packet number. If
                    // we have a listener on this binding and the packet is long enough
                    // we should respond with a version negotiation packet.
                    //
                    if (!QuicBindingHasListenerRegistered(Binding))
                    {
                        QuicPacketLogDrop(Binding, Packet, "No listener to send VN");

                    }
                    else if (Packet->BufferLength < QUIC_MIN_UDP_PAYLOAD_LENGTH_FOR_VN)
                    {
                        QuicPacketLogDrop(Binding, Packet, "Too small to send VN");

                    }
                    else
                    {
                        *ReleaseDatagram =
                            !QuicBindingQueueStatelessOperation(
                                Binding, QUIC_OPER_TYPE_VERSION_NEGOTIATION, Packet);
                    }
                    return FALSE;
                }

                if (Binding->Exclusive)
                {
                    if (Packet->DestCidLen != 0)
                    {
                        QuicPacketLogDrop(Binding, Packet, "Non-zero length CID on exclusive binding");
                        return FALSE;
                    }
                }
                else
                {
                    if (Packet->DestCidLen == 0)
                    {
                        QuicPacketLogDrop(Binding, Packet, "Zero length DestCid");
                        return FALSE;
                    }
                    if (Packet->DestCidLen < QUIC_MIN_INITIAL_CONNECTION_ID_LENGTH)
                    {
                        QuicPacketLogDrop(Binding, Packet, "Less than min length CID on non-exclusive binding");
                        return FALSE;
                    }
                }
            }

            *ReleaseDatagram = FALSE;

            return TRUE;
        }

        static void QuicBindingRemoveConnection(QUIC_BINDING Binding, QUIC_CONNECTION Connection)
        {
            if (Connection.RemoteHashEntry != null)
            {
                QuicLookupRemoveRemoteHash(Binding.Lookup, Connection.RemoteHashEntry);
            }
            QuicLookupRemoveLocalCids(Binding.Lookup, Connection);
        }

        static QUIC_STATELESS_CONTEXT QuicBindingCreateStatelessOperation(QUIC_BINDING Binding, QUIC_WORKER Worker, QUIC_RX_PACKET Packet)
        {
            long TimeMs = CxPlatTime();
            IPEndPoint RemoteAddress = Packet.Route.RemoteAddress;
            uint Hash = QuicAddrHash(RemoteAddress);
            QUIC_STATELESS_CONTEXT StatelessCtx = null;

            CxPlatDispatchLockAcquire(Binding.StatelessOperLock);

            if (Binding.RefCount == 0)
            {
                goto Exit;
            }

            while (!CxPlatListIsEmpty(Binding.StatelessOperList))
            {
                QUIC_STATELESS_CONTEXT OldStatelessCtx = CXPLAT_CONTAINING_RECORD<QUIC_STATELESS_CONTEXT>(Binding.StatelessOperList.Flink);

                if (CxPlatTimeDiff64(OldStatelessCtx.CreationTimeMs, TimeMs) < MsQuicLib.Settings.StatelessOperationExpirationMs)
                {
                    break;
                }

                OldStatelessCtx.IsExpired = true;
                Binding.StatelessOperTable.Remove(RemoteAddress);

                CxPlatListEntryRemove(OldStatelessCtx.ListEntry);
                Binding.StatelessOperCount--;
                if (OldStatelessCtx.IsProcessed)
                {
                    OldStatelessCtx.Worker.StatelessContextPool.CxPlatPoolFree(OldStatelessCtx);
                }
            }

            if (Binding.StatelessOperCount >= MsQuicLib.Settings.MaxBindingStatelessOperations)
            {
                QuicPacketLogDrop(Binding, Packet, "Max binding operations reached");
                goto Exit;
            }

            if (Binding.StatelessOperTable.ContainsKey(RemoteAddress))
            {
                QuicPacketLogDrop(Binding, Packet, "Already in stateless oper table");
                goto Exit;
            }

            StatelessCtx = Worker.StatelessContextPool.CxPlatPoolAlloc();
            if (StatelessCtx == null)
            {
                QuicPacketLogDrop(Binding, Packet, "Alloc failure for stateless oper ctx");
                goto Exit;
            }

            StatelessCtx.Binding = Binding;
            StatelessCtx.Worker = Worker;
            StatelessCtx.Packet = Packet;
            StatelessCtx.CreationTimeMs = TimeMs;
            StatelessCtx.HasBindingRef = false;
            StatelessCtx.IsProcessed = false;
            StatelessCtx.IsExpired = false;
            StatelessCtx.RemoteAddress = RemoteAddress;

            Binding.StatelessOperTable.Add(RemoteAddress, StatelessCtx);
            CxPlatListInsertTail(Binding.StatelessOperList, StatelessCtx.ListEntry);
            Binding.StatelessOperCount++;
        Exit:
            CxPlatDispatchLockRelease(Binding.StatelessOperLock);
            return StatelessCtx;
        }

        static void QuicBindingReleaseStatelessOperation(QUIC_STATELESS_CONTEXT StatelessCtx, bool ReturnDatagram)
        {
            QUIC_BINDING Binding = StatelessCtx.Binding;
            if (ReturnDatagram)
            {
                CxPlatRecvDataReturn((CXPLAT_RECV_DATA)StatelessCtx.Packet);
            }
            StatelessCtx.Packet = null;

            CxPlatDispatchLockAcquire(Binding.StatelessOperLock);
            StatelessCtx.IsProcessed = true;

            bool FreeCtx = StatelessCtx.IsExpired;
            CxPlatDispatchLockRelease(Binding.StatelessOperLock);

            if (StatelessCtx.HasBindingRef)
            {
                QuicLibraryReleaseBinding(Binding);
            }

            if (FreeCtx)
            {
                StatelessCtx.Worker.StatelessContextPool.CxPlatPoolFree(StatelessCtx);
            }
        }

        static bool QuicBindingQueueStatelessOperation(QUIC_BINDING Binding, QUIC_OPERATION_TYPE OperType, QUIC_RX_PACKET Packet)
        {
            if (MsQuicLib.StatelessRegistration == null)
            {
                QuicPacketLogDrop(Binding, Packet, "NULL stateless registration");
                return false;
            }

            QUIC_WORKER Worker = QuicLibraryGetWorker(Packet);
            if (QuicWorkerIsOverloaded(Worker))
            {
                QuicPacketLogDrop(Binding, Packet, "Stateless worker overloaded (stateless oper)");
                return false;
            }

            QUIC_STATELESS_CONTEXT Context = QuicBindingCreateStatelessOperation(Binding, Worker, Packet);
            if (Context == null)
            {
                return false;
            }

            QUIC_OPERATION Oper = QuicOperationAlloc(Worker, OperType);
            if (Oper == null)
            {
                QuicPacketLogDrop(Binding, Packet, "Alloc failure for stateless operation");
                QuicBindingReleaseStatelessOperation(Context, false);
                return false;
            }

            Oper.STATELESS.Context = Context;
            QuicWorkerQueueOperation(Worker, Oper);
            return true;
        }

        static void QuicBindingProcessStatelessOperation(QUIC_OPERATION_TYPE OperationType, QUIC_STATELESS_CONTEXT StatelessCtx)
        {
            QUIC_BINDING Binding = StatelessCtx.Binding;
            QUIC_RX_PACKET RecvPacket = StatelessCtx.Packet;
            QUIC_BUFFER SendDatagram = null;

            NetLog.Assert(RecvPacket.ValidatedHeaderInv);

            CXPLAT_SEND_CONFIG SendConfig = new CXPLAT_SEND_CONFIG()
            {
                Route = RecvPacket.Route,
                MaxPacketSize =0,
                ECN = (byte)CXPLAT_ECN_TYPE.CXPLAT_ECN_NON_ECT,
                Flags = 0
            };

            CXPLAT_SEND_DATA SendData = CxPlatSendDataAlloc(Binding.Socket, SendConfig);
            if (SendData == null)
            {
                goto Exit;
            }

            if (OperationType == QUIC_OPER_TYPE_VERSION_NEGOTIATION)
            {

                CXPLAT_DBG_ASSERT(RecvPacket->DestCid != NULL);
                CXPLAT_DBG_ASSERT(RecvPacket->SourceCid != NULL);

                const uint32_t* SupportedVersions;
                uint32_t SupportedVersionsLength;
                if (MsQuicLib.Settings.IsSet.VersionSettings)
                {
                    SupportedVersions = MsQuicLib.Settings.VersionSettings->OfferedVersions;
                    SupportedVersionsLength = MsQuicLib.Settings.VersionSettings->OfferedVersionsLength;
                }
                else
                {
                    SupportedVersions = DefaultSupportedVersionsList;
                    SupportedVersionsLength = ARRAYSIZE(DefaultSupportedVersionsList);
                }

                const uint16_t PacketLength =
                    sizeof(QUIC_VERSION_NEGOTIATION_PACKET) +               // Header
                    RecvPacket->SourceCidLen +
                    sizeof(uint8_t) +
                    RecvPacket->DestCidLen +
                    sizeof(uint32_t) +                                      // One random version
                    (uint16_t)(SupportedVersionsLength * sizeof(uint32_t)); // Our actual supported versions

                SendDatagram =
                    CxPlatSendDataAllocBuffer(SendData, PacketLength);
                if (SendDatagram == NULL)
                {
                    QuicTraceEvent(
                        AllocFailure,
                        "Allocation of '%s' failed. (%llu bytes)",
                        "vn datagram",
                        PacketLength);
                    goto Exit;
                }

                QUIC_VERSION_NEGOTIATION_PACKET* VerNeg =
                    (QUIC_VERSION_NEGOTIATION_PACKET*)SendDatagram->Buffer;
                CXPLAT_DBG_ASSERT(SendDatagram->Length == PacketLength);

                VerNeg->IsLongHeader = TRUE;
                VerNeg->Version = QUIC_VERSION_VER_NEG;

                uint8_t* Buffer = VerNeg->DestCid;
                VerNeg->DestCidLength = RecvPacket->SourceCidLen;
                CxPlatCopyMemory(
                    Buffer,
                    RecvPacket->SourceCid,
                    RecvPacket->SourceCidLen);
                Buffer += RecvPacket->SourceCidLen;

                *Buffer = RecvPacket->DestCidLen;
                Buffer++;
                CxPlatCopyMemory(
                    Buffer,
                    RecvPacket->DestCid,
                    RecvPacket->DestCidLen);
                Buffer += RecvPacket->DestCidLen;

                uint8_t RandomValue = 0;
                CxPlatRandom(sizeof(uint8_t), &RandomValue);
                VerNeg->Unused = 0x7F & RandomValue;

                CxPlatCopyMemory(Buffer, &Binding->RandomReservedVersion, sizeof(uint32_t));
                Buffer += sizeof(uint32_t);

                CxPlatCopyMemory(
                    Buffer,
                    SupportedVersions,
                    SupportedVersionsLength * sizeof(uint32_t));

                RecvPacket->ReleaseDeferred = FALSE;

                QuicTraceLogVerbose(
                    PacketTxVersionNegotiation,
                    "[S][TX][-] VN");

            }
            else if (OperationType == QUIC_OPER_TYPE_STATELESS_RESET)
            {

                CXPLAT_DBG_ASSERT(RecvPacket->DestCid != NULL);
                CXPLAT_DBG_ASSERT(RecvPacket->SourceCid == NULL);

                //
                // There are a few requirements for sending stateless reset packets:
                //
                //   - It must be smaller than the received packet.
                //   - It must be larger than a spec defined minimum (39 bytes).
                //   - It must be sufficiently random so that a middle box cannot easily
                //     detect that it is a stateless reset packet.
                //

                //
                // Add a bit of randomness (3 bits worth) to the packet length.
                //
                uint8_t PacketLength;
                CxPlatRandom(sizeof(PacketLength), &PacketLength);
                PacketLength >>= 5; // Only drop 5 of the 8 bits of randomness.
                PacketLength += QUIC_RECOMMENDED_STATELESS_RESET_PACKET_LENGTH;

                if (PacketLength >= RecvPacket->AvailBufferLength)
                {
                    //
                    // Can't go over the recieve packet's length.
                    //
                    PacketLength = (uint8_t)RecvPacket->AvailBufferLength - 1;
                }

                if (PacketLength < QUIC_MIN_STATELESS_RESET_PACKET_LENGTH)
                {
                    CXPLAT_DBG_ASSERT(FALSE);
                    goto Exit;
                }

                SendDatagram =
                    CxPlatSendDataAllocBuffer(SendData, PacketLength);
                if (SendDatagram == NULL)
                {
                    QuicTraceEvent(
                        AllocFailure,
                        "Allocation of '%s' failed. (%llu bytes)",
                        "reset datagram",
                        PacketLength);
                    goto Exit;
                }

                QUIC_SHORT_HEADER_V1* ResetPacket =
                    (QUIC_SHORT_HEADER_V1*)SendDatagram->Buffer;
                CXPLAT_DBG_ASSERT(SendDatagram->Length == PacketLength);

                CxPlatRandom(
                    PacketLength - QUIC_STATELESS_RESET_TOKEN_LENGTH,
                    SendDatagram->Buffer);
                ResetPacket->IsLongHeader = FALSE;
                ResetPacket->FixedBit = 1;
                ResetPacket->KeyPhase = RecvPacket->SH->KeyPhase;
                QuicLibraryGenerateStatelessResetToken(
                    RecvPacket->DestCid,
                    SendDatagram->Buffer + PacketLength - QUIC_STATELESS_RESET_TOKEN_LENGTH);

                QuicTraceLogVerbose(
                    PacketTxStatelessReset,
                    "[S][TX][-] SR %s",
                    QuicCidBufToStr(
                        SendDatagram->Buffer + PacketLength - QUIC_STATELESS_RESET_TOKEN_LENGTH,
                        QUIC_STATELESS_RESET_TOKEN_LENGTH
                    ).Buffer);

                QuicPerfCounterIncrement(QUIC_PERF_COUNTER_SEND_STATELESS_RESET);

            }
            else if (OperationType == QUIC_OPER_TYPE_RETRY)
            {

                CXPLAT_DBG_ASSERT(RecvPacket->DestCid != NULL);
                CXPLAT_DBG_ASSERT(RecvPacket->SourceCid != NULL);

                uint16_t PacketLength = QuicPacketMaxBufferSizeForRetryV1();
                SendDatagram =
                    CxPlatSendDataAllocBuffer(SendData, PacketLength);
                if (SendDatagram == NULL)
                {
                    QuicTraceEvent(
                        AllocFailure,
                        "Allocation of '%s' failed. (%llu bytes)",
                        "retry datagram",
                        PacketLength);
                    goto Exit;
                }

                uint8_t NewDestCid[QUIC_CID_MAX_LENGTH];
                CXPLAT_DBG_ASSERT(sizeof(NewDestCid) >= MsQuicLib.CidTotalLength);
                CxPlatRandom(sizeof(NewDestCid), NewDestCid);

                QUIC_TOKEN_CONTENTS Token = { 0 };
                Token.Authenticated.Timestamp = (uint64_t)CxPlatTimeEpochMs64();
                Token.Authenticated.IsNewToken = FALSE;

                Token.Encrypted.RemoteAddress = RecvPacket->Route->RemoteAddress;
                CxPlatCopyMemory(Token.Encrypted.OrigConnId, RecvPacket->DestCid, RecvPacket->DestCidLen);
                Token.Encrypted.OrigConnIdLength = RecvPacket->DestCidLen;

                uint8_t Iv[CXPLAT_MAX_IV_LENGTH];
                if (MsQuicLib.CidTotalLength >= CXPLAT_IV_LENGTH)
                {
                    CxPlatCopyMemory(Iv, NewDestCid, CXPLAT_IV_LENGTH);
                    for (uint8_t i = CXPLAT_IV_LENGTH; i < MsQuicLib.CidTotalLength; ++i)
                    {
                        Iv[i % CXPLAT_IV_LENGTH] ^= NewDestCid[i];
                    }
                }
                else
                {
                    CxPlatZeroMemory(Iv, CXPLAT_IV_LENGTH);
                    CxPlatCopyMemory(Iv, NewDestCid, MsQuicLib.CidTotalLength);
                }

                CxPlatDispatchLockAcquire(&MsQuicLib.StatelessRetryKeysLock);

                CXPLAT_KEY* StatelessRetryKey = QuicLibraryGetCurrentStatelessRetryKey();
                if (StatelessRetryKey == NULL)
                {
                    CxPlatDispatchLockRelease(&MsQuicLib.StatelessRetryKeysLock);
                    goto Exit;
                }

                QUIC_STATUS Status =
                    CxPlatEncrypt(
                        StatelessRetryKey,
                        Iv,
                        sizeof(Token.Authenticated), (uint8_t*)&Token.Authenticated,
                        sizeof(Token.Encrypted) + sizeof(Token.EncryptionTag), (uint8_t*)&(Token.Encrypted));

                CxPlatDispatchLockRelease(&MsQuicLib.StatelessRetryKeysLock);
                if (QUIC_FAILED(Status))
                {
                    goto Exit;
                }

                SendDatagram->Length =
                    QuicPacketEncodeRetryV1(
                        RecvPacket->LH->Version,
                        RecvPacket->SourceCid, RecvPacket->SourceCidLen,
                        NewDestCid, MsQuicLib.CidTotalLength,
                        RecvPacket->DestCid, RecvPacket->DestCidLen,
                        sizeof(Token),
                        (uint8_t*)&Token,
                        (uint16_t)SendDatagram->Length,
                        SendDatagram->Buffer);
                if (SendDatagram->Length == 0)
                {
                    CXPLAT_DBG_ASSERT(CxPlatIsRandomMemoryFailureEnabled());
                    goto Exit;
                }

                QuicTraceLogVerbose(
                    PacketTxRetry,
                    "[S][TX][-] LH Ver:0x%x DestCid:%s SrcCid:%s Type:R OrigDestCid:%s (Token %hu bytes)",
                    RecvPacket->LH->Version,
                    QuicCidBufToStr(RecvPacket->SourceCid, RecvPacket->SourceCidLen).Buffer,
                    QuicCidBufToStr(NewDestCid, MsQuicLib.CidTotalLength).Buffer,
                    QuicCidBufToStr(RecvPacket->DestCid, RecvPacket->DestCidLen).Buffer,
                    (uint16_t)sizeof(Token));

                QuicPerfCounterIncrement(QUIC_PERF_COUNTER_SEND_STATELESS_RETRY);

            }
            else
            {
                CXPLAT_TEL_ASSERT(FALSE); // Should be unreachable code.
                goto Exit;
            }

            QuicBindingSend(
                Binding,
                RecvPacket->Route,
                SendData,
                SendDatagram->Length,
                1);
            SendData = NULL;

        Exit:

            if (SendData != NULL)
            {
                CxPlatSendDataFree(SendData);
            }
        }

        static void QuicBindingUninitialize(QUIC_BINDING Binding)
        {
            NetLog.Assert(Binding.RefCount == 0);
            NetLog.Assert(CxPlatListIsEmpty(Binding.Listeners));
            CxPlatSocketDelete(Binding.Socket);
            while (!CxPlatListIsEmpty(Binding.StatelessOperList))
            {
                QUIC_STATELESS_CONTEXT StatelessCtx = CXPLAT_CONTAINING_RECORD<QUIC_STATELESS_CONTEXT>(CxPlatListRemoveHead(Binding.StatelessOperList));
                Binding.StatelessOperCount--;
                CxPlatHashtableRemove(Binding.StatelessOperTable,StatelessCtx.TableEntry, null);
                NetLog.Assert(StatelessCtx.IsProcessed);
                StatelessCtx.Worker.StatelessContextPool.CxPlatPoolFree(StatelessCtx);
            }
            NetLog.Assert(Binding.StatelessOperCount == 0);
            NetLog.Assert(Binding.StatelessOperTable.NumEntries == 0);

            QuicLookupUninitialize(Binding.Lookup);
            CxPlatDispatchLockUninitialize(&Binding->StatelessOperLock);
            CxPlatHashtableUninitialize(&Binding->StatelessOperTable);
            CxPlatDispatchRwLockUninitialize(&Binding->RwLock);

            QuicTraceEvent(
                BindingDestroyed,
                "[bind][%p] Destroyed",
                Binding);
            CXPLAT_FREE(Binding, QUIC_POOL_BINDING);
        }
    }
}
