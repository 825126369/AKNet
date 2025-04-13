using System.IO;
using System.Reflection;
using System;
using AKNet.Common;
using static System.Net.WebRequestMethods;
using AKNet.Udp5Quic.Common;

namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_PACKET_BUILDER
    {
        public QUIC_CONNECTION Connection;
        public QUIC_PATH Path;
        public QUIC_CID_HASH_ENTRY SourceCid;
        public CXPLAT_SEND_DATA SendData;
        public QUIC_BUFFER Datagram;
        public QUIC_PACKET_KEY Key;
        public byte[] CipherBatch = new byte[MSQuicFunc.CXPLAT_HP_SAMPLE_LENGTH * MSQuicFunc.QUIC_MAX_CRYPTO_BATCH_COUNT];
        public byte[] HpMask = new byte[MSQuicFunc.CXPLAT_HP_SAMPLE_LENGTH * MSQuicFunc.QUIC_MAX_CRYPTO_BATCH_COUNT];
        public byte[] HeaderBatch = new byte[MSQuicFunc.QUIC_MAX_CRYPTO_BATCH_COUNT];
        public bool PacketBatchSent;
        public bool PacketBatchRetransmittable;
        public byte BatchCount;
        public bool EcnEctSet;
        public bool WrittenConnectionCloseFrame;
        public byte TotalCountDatagrams;
        public byte EncryptionOverhead;
        public QUIC_ENCRYPT_LEVEL EncryptLevel;
        public byte PacketType;
        public byte PacketNumberLength;
        public ushort DatagramLength;
        public int TotalDatagramsLength;
        public ushort MinimumDatagramLength;
        public ushort PacketStart;
        public ushort HeaderLength;
        public ushort PayloadLengthOffset;
        public uint SendAllowance;
        public ulong BatchId;
        public QUIC_SENT_PACKET_METADATA Metadata;
        public QUIC_MAX_SENT_PACKET_METADATA MetadataStorage;
    }

    internal static partial class MSQuicFunc
    {
        static bool QuicPacketBuilderInitialize(QUIC_PACKET_BUILDER Builder, QUIC_CONNECTION Connection, QUIC_PATH Path)
        {
            NetLog.Assert(Path.DestCid != null);
            Builder.Connection = Connection;
            Builder.Path = Path;
            Builder.PacketBatchSent = false;
            Builder.PacketBatchRetransmittable = false;
            Builder.WrittenConnectionCloseFrame = false;
            Builder.Metadata = Builder.MetadataStorage.Metadata;
            Builder.EncryptionOverhead = CXPLAT_ENCRYPTION_OVERHEAD;
            Builder.TotalDatagramsLength = 0;

            if (Connection.SourceCids.Next == null)
            {
                return false;
            }

            Builder.SourceCid = CXPLAT_CONTAINING_RECORD<QUIC_CID_HASH_ENTRY>(Connection.SourceCids.Next);

            long TimeNow = CxPlatTime();
            long TimeSinceLastSend;
            if (Connection.Send.LastFlushTimeValid)
            {
                TimeSinceLastSend = CxPlatTimeDiff64(Connection.Send.LastFlushTime, TimeNow);
            }
            else
            {
                TimeSinceLastSend = 0;
            }
            Builder.SendAllowance = QuicCongestionControlGetSendAllowance(Connection.CongestionControl, TimeSinceLastSend, Connection.Send.LastFlushTimeValid);
            if (Builder.SendAllowance > Path.Allowance)
            {
                Builder.SendAllowance = Path.Allowance;
            }
            Connection.Send.LastFlushTime = TimeNow;
            Connection.Send.LastFlushTimeValid = true;
            return true;
        }

        static bool QuicPacketBuilderPrepareForControlFrames(QUIC_PACKET_BUILDER Builder, bool IsTailLossProbe, uint SendFlags)
        {
            NetLog.Assert(!BoolOk(SendFlags & QUIC_CONN_SEND_FLAG_DPLPMTUD));
            QUIC_PACKET_KEY_TYPE PacketKeyType;

            return
                QuicPacketBuilderGetPacketTypeAndKeyForControlFrames(
                    Builder,
                    SendFlags,
                    &PacketKeyType) &&
                QuicPacketBuilderPrepare(
                    Builder,
                    PacketKeyType,
                    IsTailLossProbe,
                    FALSE);
        }

        static bool QuicPacketBuilderGetPacketTypeAndKeyForControlFrames(QUIC_PACKET_BUILDER Builder, uint SendFlags, QUIC_PACKET_KEY_TYPE PacketKeyType)
        {
            QUIC_CONNECTION Connection = Builder.Connection;

            NetLog.Assert(SendFlags != 0);
            QuicSendValidate(Builder.Connection.Send);

            QUIC_PACKET_KEY_TYPE MaxKeyType = Connection.Crypto.TlsState.WriteKey;
            if (BoolOk(SendFlags & (QUIC_CONN_SEND_FLAG_CONNECTION_CLOSE | QUIC_CONN_SEND_FLAG_APPLICATION_CLOSE)))
            {
                if (!Connection.State.HandshakeConfirmed && MaxKeyType >= QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_HANDSHAKE)
                {
                    QUIC_PACKET_KEY_TYPE PreviousKeyType = MaxKeyType == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT
                            ? QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_HANDSHAKE
                            : QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL;

                    if (!Builder.WrittenConnectionCloseFrame && Connection.Crypto.TlsState.WriteKeys[(int)PreviousKeyType] != null)
                    {
                        MaxKeyType = PreviousKeyType;
                    }
                }


                if (MaxKeyType == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_0_RTT)
                {
                    PacketKeyType = QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL;
                }
                else
                {
                    PacketKeyType = MaxKeyType;
                }

                return true;
            }

            for (QUIC_PACKET_KEY_TYPE KeyType = 0; KeyType <= MaxKeyType; ++KeyType)
            {

                if (KeyType == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_0_RTT)
                {
                    continue;
                }

                QUIC_PACKET_KEY PacketsKey = Connection.Crypto.TlsState.WriteKeys[(int)KeyType];
                if (PacketsKey == null)
                {
                    continue;
                }

                QUIC_ENCRYPT_LEVEL EncryptLevel = QuicKeyTypeToEncryptLevel(KeyType);
                if (EncryptLevel == QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_1_RTT)
                {
                    PacketKeyType = QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT;
                    return true;
                }

                QUIC_PACKET_SPACE Packets = Connection.Packets[(int)EncryptLevel];
                NetLog.Assert(Packets != null);

                if (BoolOk(SendFlags & QUIC_CONN_SEND_FLAG_ACK) && Packets.AckTracker.AckElicitingPacketsToAcknowledge)
                {
                    PacketKeyType = KeyType;
                    return true;
                }

                if (BoolOk(SendFlags & QUIC_CONN_SEND_FLAG_CRYPTO) && QuicCryptoHasPendingCryptoFrame(Connection.Crypto) &&
                    EncryptLevel == QuicCryptoGetNextEncryptLevel(Connection.Crypto))
                {
                    PacketKeyType = KeyType;
                    return true;
                }
            }

            if (BoolOk(SendFlags & QUIC_CONN_SEND_FLAG_PING))
            {
                if (MaxKeyType == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_0_RTT)
                {
                    PacketKeyType =  QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL;
                }
                else
                {
                    PacketKeyType = MaxKeyType;
                }
                return true;
            }

            if (Connection.Crypto.TlsState.WriteKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT] != null)
            {
                PacketKeyType = QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT;
                return true;
            }
            return false;
        }

        static bool QuicPacketBuilderPrepare(QUIC_PACKET_BUILDER Builder, QUIC_PACKET_KEY_TYPE NewPacketKeyType, bool IsTailLossProbe,bool IsPathMtuDiscovery)
        {
            QUIC_CONNECTION Connection = Builder.Connection;
            if (Connection.Crypto.TlsState.WriteKeys[(int)NewPacketKeyType] == null)
            {
                QuicConnSilentlyAbort(Connection);
                return false;
            }

            bool Result = false;
            byte NewPacketType = Connection.Stats.QuicVersion == QUIC_VERSION_2 ? QuicKeyTypeToPacketTypeV2(NewPacketKeyType) : QuicKeyTypeToPacketTypeV1(NewPacketKeyType);
            
            bool FixedBit = (QuicConnIsClient(Connection) &&
                (NewPacketType == (byte)QUIC_LONG_HEADER_TYPE_V1.QUIC_INITIAL_V1 || 
                NewPacketKeyType == (byte)QUIC_LONG_HEADER_TYPE_V2.QUIC_INITIAL_V2) ? true : Connection.State.FixedBit;

            uint16_t DatagramSize = Builder->Path->Mtu;
            if ((uint32_t)DatagramSize > Builder->Path->Allowance)
            {
                CXPLAT_DBG_ASSERT(!IsPathMtuDiscovery); // PMTUD always happens after source addr validation.
                DatagramSize = (uint16_t)Builder->Path->Allowance;
            }
            CXPLAT_DBG_ASSERT(!IsPathMtuDiscovery || !IsTailLossProbe); // Never both.
            QuicPacketBuilderValidate(Builder, FALSE);

            //
            // Next, make sure the current QUIC packet matches the new packet type. If
            // the current one doesn't match, finalize it and then start a new one.
            //

            const uint16_t Partition = Connection->Worker->PartitionIndex;
            const uint64_t PartitionShifted = ((uint64_t)Partition + 1) << 40;

            BOOLEAN NewQuicPacket = FALSE;
            if (Builder->PacketType != NewPacketType || IsPathMtuDiscovery ||
                (Builder->Datagram != NULL && (Builder->Datagram->Length - Builder->DatagramLength) < QUIC_MIN_PACKET_SPARE_SPACE))
            {
                //
                // The current data cannot go in the current QUIC packet. Finalize the
                // current QUIC packet up so we can create another.
                //
                if (Builder->SendData != NULL)
                {
                    BOOLEAN FlushDatagrams = IsPathMtuDiscovery;
                    if (Builder->PacketType != NewPacketType &&
                        Builder->PacketType == SEND_PACKET_SHORT_HEADER_TYPE)
                    {
                        FlushDatagrams = TRUE;
                    }
                    QuicPacketBuilderFinalize(Builder, FlushDatagrams);
                }
                if (Builder->SendData == NULL &&
                    Builder->TotalCountDatagrams >= QUIC_MAX_DATAGRAMS_PER_SEND)
                {
                    goto Error;
                }
                NewQuicPacket = TRUE;

            }
            else if (Builder->Datagram == NULL)
            {
                NewQuicPacket = TRUE;
            }

            if (Builder->Datagram == NULL)
            {

                //
                // Allocate and initialize a new send buffer (UDP packet/payload).
                //
                BOOLEAN SendDataAllocated = FALSE;
                if (Builder->SendData == NULL)
                {
                    Builder->BatchId =
                        PartitionShifted | InterlockedIncrement64((int64_t*)&QuicLibraryGetPerProc()->SendBatchId);
                    CXPLAT_SEND_CONFIG SendConfig = {
                &Builder->Path->Route,
                IsPathMtuDiscovery ?
                    0 :
                    MaxUdpPayloadSizeForFamily(
                        QuicAddrGetFamily(&Builder->Path->Route.RemoteAddress),
                        DatagramSize),
                Builder->EcnEctSet ? CXPLAT_ECN_ECT_0 : CXPLAT_ECN_NON_ECT,
                Builder->Connection->Registration->ExecProfile == QUIC_EXECUTION_PROFILE_TYPE_MAX_THROUGHPUT ?
                    CXPLAT_SEND_FLAGS_MAX_THROUGHPUT : CXPLAT_SEND_FLAGS_NONE
            };
                    Builder->SendData =
                        CxPlatSendDataAlloc(Builder->Path->Binding->Socket, &SendConfig);
                    if (Builder->SendData == NULL)
                    {
                        QuicTraceEvent(
                            AllocFailure,
                            "Allocation of '%s' failed. (%llu bytes)",
                            "packet send context",
                            0);
                        goto Error;
                    }
                    SendDataAllocated = TRUE;
                }

                uint16_t NewDatagramLength =
                    MaxUdpPayloadSizeForFamily(
                        QuicAddrGetFamily(&Builder->Path->Route.RemoteAddress),
                        IsPathMtuDiscovery ? Builder->Path->MtuDiscovery.ProbeSize : DatagramSize);
                if ((Connection->PeerTransportParams.Flags & QUIC_TP_FLAG_MAX_UDP_PAYLOAD_SIZE) &&
                    NewDatagramLength > Connection->PeerTransportParams.MaxUdpPayloadSize)
                {
                    NewDatagramLength = (uint16_t)Connection->PeerTransportParams.MaxUdpPayloadSize;
                }

                Builder->Datagram =
                    CxPlatSendDataAllocBuffer(
                        Builder->SendData,
                        NewDatagramLength);
                if (Builder->Datagram == NULL)
                {
                    QuicTraceEvent(
                        AllocFailure,
                        "Allocation of '%s' failed. (%llu bytes)",
                        "packet datagram",
                        NewDatagramLength);
                    if (SendDataAllocated)
                    {
                        CxPlatSendDataFree(Builder->SendData);
                        Builder->SendData = NULL;
                    }
                    goto Error;
                }

                Builder->DatagramLength = 0;
                Builder->MinimumDatagramLength = 0;

                if (IsTailLossProbe && QuicConnIsClient(Connection))
                {
                    if (NewPacketType == SEND_PACKET_SHORT_HEADER_TYPE)
                    {
                        //
                        // Short header (1-RTT) packets need to be padded enough to
                        // elicit stateless resets from the server.
                        //
                        Builder->MinimumDatagramLength =
                            QUIC_RECOMMENDED_STATELESS_RESET_PACKET_LENGTH +
                            8 /* a little fudge factor */;
                    }
                    else
                    {
                        //
                        // Initial/Handshake packets need to be padded to unblock a
                        // server (possibly) blocked on source address validation.
                        //
                        Builder->MinimumDatagramLength = NewDatagramLength;
                    }

                }
                else if ((Connection->Stats.QuicVersion == QUIC_VERSION_2 && NewPacketType == QUIC_INITIAL_V2) ||
                    (Connection->Stats.QuicVersion != QUIC_VERSION_2 && NewPacketType == QUIC_INITIAL_V1))
                {

                    //
                    // Make sure to pad Initial packets.
                    //
                    Builder->MinimumDatagramLength =
                        MaxUdpPayloadSizeForFamily(
                            QuicAddrGetFamily(&Builder->Path->Route.RemoteAddress),
                            Builder->Path->Mtu);

                    if ((uint32_t)Builder->MinimumDatagramLength > Builder->Datagram->Length)
                    {
                        //
                        // On server, if we're limited by amplification protection, just
                        // pad up to that limit instead.
                        //
                        Builder->MinimumDatagramLength = (uint16_t)Builder->Datagram->Length;
                    }

                }
                else if (IsPathMtuDiscovery)
                {
                    Builder->MinimumDatagramLength = NewDatagramLength;
                }
            }

            if (NewQuicPacket)
            {

                //
                // Initialize the new QUIC packet state.
                //

                Builder->PacketType = NewPacketType;
                Builder->EncryptLevel =
                    Connection->Stats.QuicVersion == QUIC_VERSION_2 ?
                        QuicPacketTypeToEncryptLevelV2(NewPacketType) :
                        QuicPacketTypeToEncryptLevelV1(NewPacketType);
                Builder->Key = Connection->Crypto.TlsState.WriteKeys[NewPacketKeyType];
                CXPLAT_DBG_ASSERT(Builder->Key != NULL);
                CXPLAT_DBG_ASSERT(Builder->Key->PacketKey != NULL);
                CXPLAT_DBG_ASSERT(Builder->Key->HeaderKey != NULL);
                if (NewPacketKeyType == QUIC_PACKET_KEY_1_RTT &&
                    Connection->State.Disable1RttEncrytion)
                {
                    Builder->EncryptionOverhead = 0;
                }

                Builder->Metadata->PacketId =
                    PartitionShifted | InterlockedIncrement64((int64_t*)&QuicLibraryGetPerProc()->SendPacketId);
                QuicTraceEvent(
                    PacketCreated,
                    "[pack][%llu] Created in batch %llu",
                    Builder->Metadata->PacketId,
                    Builder->BatchId);

                Builder->Metadata->FrameCount = 0;
                Builder->Metadata->PacketNumber = Connection->Send.NextPacketNumber++;
                Builder->Metadata->Flags.KeyType = NewPacketKeyType;
                Builder->Metadata->Flags.IsAckEliciting = FALSE;
                Builder->Metadata->Flags.IsMtuProbe = IsPathMtuDiscovery;
                Builder->Metadata->Flags.SuspectedLost = FALSE;
#if DEBUG
                Builder->Metadata->Flags.Freed = FALSE;
#endif

                Builder->PacketStart = Builder->DatagramLength;
                Builder->HeaderLength = 0;

                uint8_t* Header =
                    Builder->Datagram->Buffer + Builder->DatagramLength;
                uint16_t BufferSpaceAvailable =
                    (uint16_t)Builder->Datagram->Length - Builder->DatagramLength;

                if (NewPacketType == SEND_PACKET_SHORT_HEADER_TYPE)
                {
                    QUIC_PACKET_SPACE* PacketSpace = Connection->Packets[Builder->EncryptLevel];

                    Builder->PacketNumberLength = 4; // TODO - Determine correct length based on BDP.

                    switch (Connection->Stats.QuicVersion)
                    {
                        case QUIC_VERSION_1:
                        case QUIC_VERSION_DRAFT_29:
                        case QUIC_VERSION_MS_1:
                        case QUIC_VERSION_2:
                            Builder->HeaderLength =
                                QuicPacketEncodeShortHeaderV1(
                                    &Builder->Path->DestCid->CID,
                                    Builder->Metadata->PacketNumber,
                                    Builder->PacketNumberLength,
                                    Builder->Path->SpinBit,
                                    PacketSpace->CurrentKeyPhase,
                                    FixedBit,
                                    BufferSpaceAvailable,
                                    Header);
                            Builder->Metadata->Flags.KeyPhase = PacketSpace->CurrentKeyPhase;
                            break;
                        default:
                            CXPLAT_FRE_ASSERT(FALSE);
                            Builder->HeaderLength = 0; // For build warning.
                            break;
                    }

                }
                else
                { // Long Header

                    switch (Connection->Stats.QuicVersion)
                    {
                        case QUIC_VERSION_1:
                        case QUIC_VERSION_DRAFT_29:
                        case QUIC_VERSION_MS_1:
                        case QUIC_VERSION_2:
                        default:
                            Builder->HeaderLength =
                                QuicPacketEncodeLongHeaderV1(
                                    Connection->Stats.QuicVersion,
                                    NewPacketType,
                                    FixedBit,
                                    &Builder->Path->DestCid->CID,
                                    &Builder->SourceCid->CID,
                                    Connection->Send.InitialTokenLength,
                                    Connection->Send.InitialToken,
                                    (uint32_t)Builder->Metadata->PacketNumber,
                                    BufferSpaceAvailable,
                                    Header,
                                    &Builder->PayloadLengthOffset,
                                    &Builder->PacketNumberLength);
                            break;
                    }
                }

                Builder->DatagramLength += Builder->HeaderLength;
            }

            CXPLAT_DBG_ASSERT(Builder->PacketType == NewPacketType);
            CXPLAT_DBG_ASSERT(Builder->Key == Connection->Crypto.TlsState.WriteKeys[NewPacketKeyType]);
            CXPLAT_DBG_ASSERT(Builder->BatchCount == 0 || Builder->PacketType == SEND_PACKET_SHORT_HEADER_TYPE);

            Result = TRUE;

        Error:

            QuicPacketBuilderValidate(Builder, FALSE);

            return Result;
        }

    }
}
