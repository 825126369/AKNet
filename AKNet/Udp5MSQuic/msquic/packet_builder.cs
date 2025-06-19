using AKNet.Common;
using System;
using System.Reflection;

namespace AKNet.Udp5MSQuic.Common
{
    internal class QUIC_PACKET_BUILDER
    {
        public QUIC_CONNECTION Connection;
        public QUIC_PATH Path;
        public QUIC_CID SourceCid;
        public CXPLAT_SEND_DATA SendData;//表示一组 UDP 数据报
        public QUIC_BUFFER Datagram;//当前构建的 UDP 数据报缓冲区
        public QUIC_PACKET_KEY Key;//当前使用的加密密钥

        //加密批处理数据（用于头保护)
        public byte[] CipherBatch = new byte[MSQuicFunc.CXPLAT_HP_SAMPLE_LENGTH * MSQuicFunc.QUIC_MAX_CRYPTO_BATCH_COUNT];
        //头保护掩码
        public byte[] HpMask = new byte[MSQuicFunc.CXPLAT_HP_SAMPLE_LENGTH * MSQuicFunc.QUIC_MAX_CRYPTO_BATCH_COUNT];
        //批量数据包头的指针数组
        public QUIC_BUFFER[] HeaderBatch = new QUIC_BUFFER[MSQuicFunc.QUIC_MAX_CRYPTO_BATCH_COUNT];

        public bool PacketBatchSent;//是否已经发送了一个批次的数据包
        public bool PacketBatchRetransmittable;//当前批次是否包含可重传的数据包
        public byte BatchCount;//当前批次中数据包的数量
        public bool EcnEctSet;//是否设置了 ECN ECT 位
        public bool WrittenConnectionCloseFrame;//是否写入了 CONNECTION_CLOSE 帧
        public byte TotalCountDatagrams;//已创建的 UDP [数据报] 总数
        public byte EncryptionOverhead; //加密开销（如 AEAD tag 长度）
        public QUIC_ENCRYPT_LEVEL EncryptLevel;
        public byte PacketType; //数据包类型（如 Initial/Handshake/1RTT）
        public int PacketNumberLength;//包号编码后的长度                       
        public int TotalDatagramsLength;//所有数据报总长度
        public int MinimumDatagramLength;//最小数据报长度（用于填充）
        public int PacketStart;//当前数据包起始偏移
        public int HeaderLength;//数据包头部长度
        public int PayloadLengthOffset;//负载长度字段偏移
        public int SendAllowance;//当前允许发送的字节数
        public ulong BatchId;//批次 ID，用于调试或跟踪
        public QUIC_SENT_PACKET_METADATA Metadata = null;//已发送数据包的元数据指针
        public readonly QUIC_MAX_SENT_PACKET_METADATA MetadataStorage = new QUIC_MAX_SENT_PACKET_METADATA();

        //Datagram的偏移值Offset，用DatagramLength来表示
        public int DatagramLength; //这个代码现在数据报的长度，相当于 Datagram 的 Offset偏移值

        public QUIC_SSBuffer GetDatagramCanWriteSSBufer()
        {
            return Datagram.Slice(DatagramLength, Datagram.Length - EncryptionOverhead - DatagramLength);
        }

        public void SetDatagramOffset(QUIC_SSBuffer mBuffer)
        {
            DatagramLength = mBuffer.Offset;
        }
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

            if (CxPlatListIsEmpty(Connection.SourceCids.Next))
            {
                return false;
            }

            Builder.SourceCid = CXPLAT_CONTAINING_RECORD<QUIC_CID>(Connection.SourceCids.Next);

            long TimeNow = CxPlatTime();
            long TimeSinceLastSend;
            if (Connection.Send.LastFlushTimeValid)
            {
                TimeSinceLastSend = CxPlatTimeDiff(Connection.Send.LastFlushTime, TimeNow);
            }
            else
            {
                TimeSinceLastSend = 0;
            }
            Builder.SendAllowance = (int)QuicCongestionControlGetSendAllowance(Connection.CongestionControl, TimeSinceLastSend, Connection.Send.LastFlushTimeValid);
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
            if (!QuicPacketBuilderGetPacketTypeAndKeyForControlFrames(Builder, SendFlags, out PacketKeyType))
            {
                return false;
            }

            if(!QuicPacketBuilderPrepare(Builder, PacketKeyType, IsTailLossProbe, false))
            {
                return false;
            }

            return true;
        }

        static bool QuicPacketBuilderGetPacketTypeAndKeyForControlFrames(QUIC_PACKET_BUILDER Builder, uint SendFlags, out QUIC_PACKET_KEY_TYPE PacketKeyType)
        {
            QUIC_CONNECTION Connection = Builder.Connection;

            NetLog.Assert(SendFlags != 0);
            QuicSendValidate(Builder.Connection.Send);
            PacketKeyType = QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL;

            QUIC_PACKET_KEY_TYPE MaxKeyType = Connection.Crypto.TlsState.WriteKey;
            if (HasFlag(SendFlags, QUIC_CONN_SEND_FLAG_CONNECTION_CLOSE | QUIC_CONN_SEND_FLAG_APPLICATION_CLOSE))
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

                if (HasFlag(SendFlags, QUIC_CONN_SEND_FLAG_ACK) && Packets.AckTracker.AckElicitingPacketsToAcknowledge > 0)
                {
                    PacketKeyType = KeyType;
                    return true;
                }

                if (HasFlag(SendFlags, QUIC_CONN_SEND_FLAG_CRYPTO) && QuicCryptoHasPendingCryptoFrame(Connection.Crypto) &&
                    EncryptLevel == QuicCryptoGetNextEncryptLevel(Connection.Crypto))
                {
                    PacketKeyType = KeyType;
                    return true;
                }
            }

            if (HasFlag(SendFlags, QUIC_CONN_SEND_FLAG_PING))
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

        //IsPathMtuDiscovery: 是否启用Mtu发现
        static bool QuicPacketBuilderPrepare(QUIC_PACKET_BUILDER Builder, QUIC_PACKET_KEY_TYPE NewPacketKeyType, bool IsTailLossProbe, bool IsPathMtuDiscovery)
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
                (byte)NewPacketKeyType == (byte)QUIC_LONG_HEADER_TYPE_V2.QUIC_INITIAL_V2)) 
                ? true : Connection.State.FixedBit;

            ushort DatagramSize = Builder.Path.Mtu;
            if (DatagramSize > Builder.Path.Allowance)
            {
                NetLog.Assert(!IsPathMtuDiscovery); // PMTUD always happens after source addr validation.
                DatagramSize = (ushort)Builder.Path.Allowance;
            }

            NetLog.Assert(!IsPathMtuDiscovery || !IsTailLossProbe); // Never both.
            QuicPacketBuilderValidate(Builder, false);


            //接下来，确保当前QUIC数据包与新的数据包类型匹配。如果
            //当前的不匹配，请完成它，然后开始一个新的。
            int Partition = Connection.Worker.PartitionIndex;
            ulong PartitionShifted = ((ulong)Partition + 1) << 40;

            bool NewQuicPacket = false;
            if (Builder.PacketType != NewPacketType || IsPathMtuDiscovery || 
                (Builder.Datagram != null && (Builder.Datagram.Length - Builder.DatagramLength) < QUIC_MIN_PACKET_SPARE_SPACE))
            {
                if (Builder.SendData != null)
                {
                    bool FlushDatagrams = IsPathMtuDiscovery;
                    if (Builder.PacketType != NewPacketType &&
                        Builder.PacketType == SEND_PACKET_SHORT_HEADER_TYPE)
                    {
                        FlushDatagrams = true;
                    }
                    QuicPacketBuilderFinalize(Builder, FlushDatagrams);
                }
                if (Builder.SendData == null && Builder.TotalCountDatagrams >= QUIC_MAX_DATAGRAMS_PER_SEND)
                {
                    goto Error;
                }
                NewQuicPacket = true;
            }
            else if (Builder.Datagram == null)
            {
                NewQuicPacket = true;
            }

            if (Builder.Datagram == null)
            {
                bool SendDataAllocated = false;
                if (Builder.SendData == null)
                {
                    QUIC_LIBRARY_PP mPP = QuicLibraryGetPerProc();
                    Builder.BatchId = PartitionShifted | (ulong)InterlockedEx.Increment(ref mPP.SendBatchId);
                    CXPLAT_SEND_CONFIG SendConfig = new CXPLAT_SEND_CONFIG()
                    {
                        Route = Builder.Path.Route,
                        MaxPacketSize = IsPathMtuDiscovery ? 0 : MaxUdpPayloadSizeForFamily(QuicAddrGetFamily(Builder.Path.Route.RemoteAddress), DatagramSize),
                        ECN = Builder.EcnEctSet ? (byte)CXPLAT_ECN_TYPE.CXPLAT_ECN_ECT_0 : (byte)CXPLAT_ECN_TYPE.CXPLAT_ECN_NON_ECT,
                        Flags = Builder.Connection.Registration.ExecProfile == QUIC_EXECUTION_PROFILE.QUIC_EXECUTION_PROFILE_TYPE_MAX_THROUGHPUT ? 
                            (byte)CXPLAT_SEND_FLAGS.CXPLAT_SEND_FLAGS_MAX_THROUGHPUT : (byte)CXPLAT_SEND_FLAGS.CXPLAT_SEND_FLAGS_NONE
                    };

                    Builder.SendData = CxPlatSendDataAlloc(Builder.Path.Binding.Socket, SendConfig);
                    if (Builder.SendData == null)
                    {
                        goto Error;
                    }
                    SendDataAllocated = true;
                }

                int NewDatagramLength = MaxUdpPayloadSizeForFamily(QuicAddrGetFamily(Builder.Path.Route.RemoteAddress), IsPathMtuDiscovery ? Builder.Path.MtuDiscovery.ProbeSize : DatagramSize);
                if (BoolOk(Connection.PeerTransportParams.Flags & QUIC_TP_FLAG_MAX_UDP_PAYLOAD_SIZE) && NewDatagramLength > Connection.PeerTransportParams.MaxUdpPayloadSize)
                {
                    NewDatagramLength = (int)Connection.PeerTransportParams.MaxUdpPayloadSize;
                }

                Builder.Datagram = CxPlatSendDataAllocBuffer(Builder.SendData, NewDatagramLength);
                if (Builder.Datagram == null)
                {
                    if (SendDataAllocated)
                    {
                        CxPlatSendDataFree(Builder.SendData);
                        Builder.SendData = null;
                    }
                    goto Error;
                }

                Builder.DatagramLength = 0;
                Builder.MinimumDatagramLength = 0;

                if (IsTailLossProbe && QuicConnIsClient(Connection))
                {
                    if (NewPacketType == SEND_PACKET_SHORT_HEADER_TYPE)
                    {
                        Builder.MinimumDatagramLength = QUIC_RECOMMENDED_STATELESS_RESET_PACKET_LENGTH + 8;
                    }
                    else
                    {
                        Builder.MinimumDatagramLength = NewDatagramLength;
                    }
                }
                else if ((Connection.Stats.QuicVersion == QUIC_VERSION_2 && NewPacketType == (byte)QUIC_LONG_HEADER_TYPE_V2.QUIC_INITIAL_V2) ||
                    (Connection.Stats.QuicVersion != QUIC_VERSION_2 && NewPacketType == (byte)QUIC_LONG_HEADER_TYPE_V1.QUIC_INITIAL_V1))
                {
                    Builder.MinimumDatagramLength = MaxUdpPayloadSizeForFamily(QuicAddrGetFamily(Builder.Path.Route.RemoteAddress), Builder.Path.Mtu);
                    if (Builder.MinimumDatagramLength > Builder.Datagram.Length)
                    {
                        Builder.MinimumDatagramLength = Builder.Datagram.Length;
                    }
                }
                else if (IsPathMtuDiscovery)
                {
                    Builder.MinimumDatagramLength = NewDatagramLength;
                }
            }

            if (NewQuicPacket)
            {
                Builder.PacketType = NewPacketType;
                Builder.EncryptLevel = Connection.Stats.QuicVersion == QUIC_VERSION_2 ? QuicPacketTypeToEncryptLevelV2((QUIC_LONG_HEADER_TYPE_V2)NewPacketType) :QuicPacketTypeToEncryptLevelV1((QUIC_LONG_HEADER_TYPE_V1)NewPacketType);
                Builder.Key = Connection.Crypto.TlsState.WriteKeys[(int)NewPacketKeyType];
                NetLog.Assert(Builder.Key != null);
                NetLog.Assert(Builder.Key.PacketKey != null);
                NetLog.Assert(Builder.Key.HeaderKey != null);
                if (NewPacketKeyType ==  QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT && Connection.State.Disable1RttEncrytion)
                {
                    Builder.EncryptionOverhead = 0;
                }

                Builder.Metadata.PacketId = PartitionShifted | InterlockedEx.Increment(ref QuicLibraryGetPerProc().SendPacketId);
                Builder.Metadata.FrameCount = 0;
                Builder.Metadata.PacketNumber = Connection.Send.NextPacketNumber++;
                Builder.Metadata.Flags.KeyType = NewPacketKeyType;
                Builder.Metadata.Flags.IsAckEliciting = false;
                Builder.Metadata.Flags.IsMtuProbe = IsPathMtuDiscovery;
                Builder.Metadata.Flags.SuspectedLost = false;

                Builder.PacketStart = Builder.DatagramLength;
                Builder.HeaderLength = 0;

                QUIC_SSBuffer Header = Builder.Datagram.Slice(Builder.DatagramLength, Builder.Datagram.Length - Builder.DatagramLength);
                if (NewPacketType == SEND_PACKET_SHORT_HEADER_TYPE)
                {
                    QUIC_PACKET_SPACE PacketSpace = Connection.Packets[(int)Builder.EncryptLevel];
                    Builder.PacketNumberLength = 4;

                    switch (Connection.Stats.QuicVersion)
                    {
                        case QUIC_VERSION_1:
                        case QUIC_VERSION_2:
                            Builder.HeaderLength =
                                QuicPacketEncodeShortHeaderV1(
                                    Builder.Path.DestCid,
                                    Builder.Metadata.PacketNumber,
                                    Builder.PacketNumberLength,
                                    Builder.Path.SpinBit,
                                    PacketSpace.CurrentKeyPhase,
                                    FixedBit,
                                    Header);
                            Builder.Metadata.Flags.KeyPhase = PacketSpace.CurrentKeyPhase;
                            break;
                        default:
                            NetLog.Assert(false);
                            Builder.HeaderLength = 0; // For build warning.
                            break;
                    }
                }
                else
                {
                    switch (Connection.Stats.QuicVersion)
                    {
                        case QUIC_VERSION_1:
                        case QUIC_VERSION_2:
                        default:
                            Builder.HeaderLength =
                                QuicPacketEncodeLongHeaderV1(
                                    Connection.Stats.QuicVersion,
                                    NewPacketType,
                                    FixedBit,
                                    Builder.Path.DestCid,
                                    Builder.SourceCid,
                                    Connection.Send.InitialToken,
                                    (uint)Builder.Metadata.PacketNumber,
                                    Header,
                                    ref Builder.PayloadLengthOffset,
                                    ref Builder.PacketNumberLength);
                            break;
                    }
                }

                Builder.DatagramLength += Builder.HeaderLength;
                NetLogHelper.PrintByteArray("QuicPacketBuilderPrepare: ", Header.Slice(0, Builder.HeaderLength).GetSpan());
            }
            
            NetLog.Assert(Builder.PacketType == NewPacketType);
            NetLog.Assert(Builder.Key == Connection.Crypto.TlsState.WriteKeys[(int)NewPacketKeyType]);
            NetLog.Assert(Builder.BatchCount == 0 || Builder.PacketType == SEND_PACKET_SHORT_HEADER_TYPE);
            Result = true;
        Error:
            QuicPacketBuilderValidate(Builder, false);
            return Result;
        }

        static bool QuicPacketBuilderFinalize(QUIC_PACKET_BUILDER Builder, bool FlushBatchedDatagrams)
        {
            QUIC_CONNECTION Connection = Builder.Connection;
            bool FinalQuicPacket = false;
            bool CanKeepSending = true;

            QuicPacketBuilderValidate(Builder, false);

            if (Builder.Datagram == null || Builder.Metadata.FrameCount == 0)
            {
                if (Builder.Datagram != null)
                {
                    --Connection.Send.NextPacketNumber;
                    Builder.DatagramLength -= Builder.HeaderLength;
                    Builder.HeaderLength = 0;
                    CanKeepSending = false;

                    if (Builder.DatagramLength == 0)
                    {
                        CxPlatSendDataFreeBuffer(Builder.SendData, Builder.Datagram);
                        Builder.Datagram = null;
                    }
                }
                if (Builder.Path.Allowance != uint.MaxValue)
                {
                    QuicConnAddOutFlowBlockedReason(Connection, QUIC_FLOW_BLOCKED_AMPLIFICATION_PROT);
                }
                FinalQuicPacket = FlushBatchedDatagrams && (Builder.TotalCountDatagrams != 0);
                goto Exit;
            }

            QuicPacketBuilderValidate(Builder, true);

            QUIC_SSBuffer Header = Builder.Datagram.Slice(Builder.PacketStart);
            int PayloadLength = Builder.DatagramLength - (Builder.PacketStart + Builder.HeaderLength);
            int ExpectedFinalDatagramLength = Builder.DatagramLength + Builder.EncryptionOverhead;//期待的最终的长度

            if (FlushBatchedDatagrams || Builder.PacketType == SEND_PACKET_SHORT_HEADER_TYPE || Builder.Datagram.Length - ExpectedFinalDatagramLength < QUIC_MIN_PACKET_SPARE_SPACE)
            {
                FinalQuicPacket = true;
                if (!FlushBatchedDatagrams && CxPlatDataPathIsPaddingPreferred(MsQuicLib.Datapath, Builder.SendData))
                {
                    Builder.MinimumDatagramLength = Builder.Datagram.Length;
                }
            }

            int PaddingLength;
            if (FinalQuicPacket && ExpectedFinalDatagramLength < Builder.MinimumDatagramLength)
            {
                PaddingLength = Builder.MinimumDatagramLength - ExpectedFinalDatagramLength;
            }
            else if (Builder.PacketNumberLength + PayloadLength < sizeof(uint))
            {
                PaddingLength = sizeof(uint) - Builder.PacketNumberLength - PayloadLength;
            }
            else
            {
                PaddingLength = 0;
            }

            if (PaddingLength != 0)
            {
                //Builder.Datagram.GetSpan().Clear(0, 1); 是否需要清理
                PayloadLength += PaddingLength;
                Builder.DatagramLength += PaddingLength;
            }

            if (Builder.PacketType != SEND_PACKET_SHORT_HEADER_TYPE)
            {
                switch (Connection.Stats.QuicVersion)
                {
                    case QUIC_VERSION_1:
                    case QUIC_VERSION_2:
                    default:
                        QuicVarIntEncode2Bytes(
                            (ulong)(Builder.PacketNumberLength + PayloadLength + Builder.EncryptionOverhead),
                            Header + Builder.PayloadLengthOffset
                            );
                        break;
                }
            }

            if (Builder.EncryptionOverhead != 0 && 
                !(Builder.Key.Type ==  QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT && Connection.Paths[0].EncryptionOffloading))
            {
                PayloadLength += Builder.EncryptionOverhead;
                Builder.DatagramLength += Builder.EncryptionOverhead;

                QUIC_SSBuffer Header2 = Header.Slice(0, Builder.HeaderLength);
                QUIC_SSBuffer Payload = Header.Slice(Builder.HeaderLength, PayloadLength);
                byte[] Iv = new byte[CXPLAT_MAX_IV_LENGTH];

                QuicCryptoCombineIvAndPacketNumber(Builder.Key.Iv, Builder.Metadata.PacketNumber, Iv);
                ulong Status;
                if (QUIC_FAILED(Status = CxPlatEncrypt(Builder.Key.PacketKey, Iv, Header2, Payload)))
                {
                    QuicConnFatalError(Connection, Status, "Encryption failure");
                    goto Exit;
                }

                if (Connection.State.HeaderProtectionEnabled)
                {
                    QUIC_SSBuffer PnStart = Payload.Slice(-Builder.PacketNumberLength);
                    if (Builder.PacketType == SEND_PACKET_SHORT_HEADER_TYPE)
                    {
                        NetLog.Assert(Builder.BatchCount < QUIC_MAX_CRYPTO_BATCH_COUNT);

                        PnStart.GetSpan().Slice(4, CXPLAT_HP_SAMPLE_LENGTH).CopyTo(Builder.CipherBatch.AsSpan().Slice(Builder.BatchCount * CXPLAT_HP_SAMPLE_LENGTH));

                        PnStart.Slice(4, CXPLAT_HP_SAMPLE_LENGTH).GetSpan().CopyTo(Builder.CipherBatch.AsSpan().Slice(Builder.BatchCount * CXPLAT_HP_SAMPLE_LENGTH));
                        Builder.HeaderBatch[Builder.BatchCount] = Header;

                        if (++Builder.BatchCount == QUIC_MAX_CRYPTO_BATCH_COUNT)
                        {
                            QuicPacketBuilderFinalizeHeaderProtection(Builder);
                        }
                    }
                    else
                    {
                        NetLog.Assert(Builder.BatchCount == 0);
                        if (QUIC_FAILED(Status = CxPlatHpComputeMask(Builder.Key.HeaderKey, 1, PnStart.Slice(4), Builder.HpMask)))
                        {
                            NetLog.Assert(false);
                            QuicConnFatalError(Connection, Status, "HP failure");
                            goto Exit;
                        }

                        Header[0] = (byte)(Header[0] ^ (Builder.HpMask[0] & 0x0f)); // Bottom 4 bits for LH
                        for (int i = 0; i < Builder.PacketNumberLength; ++i)
                        {
                            PnStart[i] ^= Builder.HpMask[1 + i];
                        }
                    }
                }
            
                QUIC_PACKET_SPACE PacketSpace = Connection.Packets[(int)Builder.EncryptLevel];
                PacketSpace.CurrentKeyPhaseBytesSent += (PayloadLength - Builder.EncryptionOverhead);
                
                if (Builder.PacketType == SEND_PACKET_SHORT_HEADER_TYPE && PacketSpace.CurrentKeyPhaseBytesSent + CXPLAT_MAX_MTU >= Connection.Settings.MaxBytesPerKey &&
                    !PacketSpace.AwaitingKeyPhaseConfirmation && Connection.State.HandshakeConfirmed)
                {
                    Status = QuicCryptoGenerateNewKeys(Connection);
                    if (QUIC_FAILED(Status))
                    {
                        QuicConnFatalError(Connection, Status, "Send-triggered key update");
                        goto Exit;
                    }

                    QuicCryptoUpdateKeyPhase(Connection, true);
                    Builder.Key = Connection.Crypto.TlsState.WriteKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT];
                    NetLog.Assert(Builder.Key != null);
                    NetLog.Assert(Builder.Key.PacketKey != null);
                    NetLog.Assert(Builder.Key.HeaderKey != null);
                }
            }
            
            NetLog.Assert(Builder.Metadata.FrameCount != 0);

            Builder.Metadata.SentTime = CxPlatTime();
            Builder.Metadata.PacketLength = Builder.HeaderLength + PayloadLength;
            Builder.Metadata.Flags.EcnEctSet = Builder.EcnEctSet;
            QuicLossDetectionOnPacketSent(Connection.LossDetection, Builder.Path, Builder.Metadata);
            Builder.Metadata.FrameCount = 0;

            if (Builder.Metadata.Flags.IsAckEliciting)
            {
                Builder.PacketBatchRetransmittable = true;
                if (Builder.Metadata.PacketLength > Builder.SendAllowance)
                {
                    Builder.SendAllowance = 0;
                }
                else
                {
                    Builder.SendAllowance -= Builder.Metadata.PacketLength;
                }
            }

        Exit:
            if (FinalQuicPacket)
            {
                if (Builder.Datagram != null)
                {
                    if (Builder.Metadata.Flags.EcnEctSet)
                    {
                        ++Connection.Send.NumPacketsSentWithEct;
                    }

                    Builder.Datagram = null;
                    ++Builder.TotalCountDatagrams;
                    Builder.TotalDatagramsLength += Builder.DatagramLength;
                    Builder.DatagramLength = 0;
                }

                if (FlushBatchedDatagrams || CxPlatSendDataIsFull(Builder.SendData))
                {
                    if (Builder.BatchCount != 0)
                    {
                        QuicPacketBuilderFinalizeHeaderProtection(Builder);
                    }
                    NetLog.Assert(Builder.TotalCountDatagrams > 0);
                    QuicPacketBuilderSendBatch(Builder);
                    NetLog.Assert(Builder.Metadata.FrameCount == 0);
                }

                if ((Connection.Stats.QuicVersion != QUIC_VERSION_2 && Builder.PacketType == (byte)QUIC_LONG_HEADER_TYPE_V1.QUIC_RETRY_V1) ||
                    (Connection.Stats.QuicVersion == QUIC_VERSION_2 && Builder.PacketType == (byte)QUIC_LONG_HEADER_TYPE_V2.QUIC_RETRY_V2))
                {
                    NetLog.Assert(Builder.Metadata.PacketNumber == 0);
                    QuicConnCloseLocally(Connection, QUIC_CLOSE_SILENT, QUIC_ERROR_NO_ERROR, null);
                }

            }
            else if (FlushBatchedDatagrams)
            {
                if (Builder.Datagram != null)
                {
                    CxPlatSendDataFreeBuffer(Builder.SendData, Builder.Datagram);
                    Builder.Datagram = null;
                }

                if (Builder.SendData != null)
                {
                    CxPlatSendDataFree(Builder.SendData);
                    Builder.SendData = null;
                }
            }
            
            QuicPacketBuilderValidate(Builder, false);
            NetLog.Assert(!FlushBatchedDatagrams || Builder.SendData == null);
            return CanKeepSending;
        }

        static void QuicPacketBuilderFinalizeHeaderProtection(QUIC_PACKET_BUILDER Builder)
        {
            NetLog.Assert(Builder.Key != null);

            ulong Status;
            if (QUIC_FAILED(Status = CxPlatHpComputeMask(Builder.Key.HeaderKey, Builder.BatchCount, Builder.CipherBatch, Builder.HpMask)))
            {
                NetLog.Assert(false);
                QuicConnFatalError(Builder.Connection, Status, "HP failure");
                return;
            }

            for (int i = 0; i < Builder.BatchCount; ++i)
            {
                int Offset = i * CXPLAT_HP_SAMPLE_LENGTH;
                QUIC_SSBuffer Header = Builder.HeaderBatch[i];
                Header[0] = (byte)(Header[0] ^ (Builder.HpMask[Offset] & 0x1f));
                Header = Header.Slice(1 + Builder.Path.DestCid.Data.Length);
                for (int j = 0; j < Builder.PacketNumberLength; ++j)
                {
                    Header[j] ^= Builder.HpMask[Offset + 1 + j];
                }
            }
            Builder.BatchCount = 0;
        }

        static bool QuicPacketBuilderAddFrame(QUIC_PACKET_BUILDER Builder, QUIC_FRAME_TYPE FrameType, bool IsAckEliciting)
        {
            NetLog.Assert(Builder.Metadata.FrameCount < QUIC_MAX_FRAMES_PER_PACKET);
            Builder.Metadata.Frames[Builder.Metadata.FrameCount].Type = FrameType;
            Builder.Metadata.Flags.IsAckEliciting |= IsAckEliciting;
            return ++Builder.Metadata.FrameCount == QUIC_MAX_FRAMES_PER_PACKET;
        }

        static bool QuicPacketBuilderHasAllowance(QUIC_PACKET_BUILDER Builder)
        {
            return Builder.SendAllowance > 0 || QuicCongestionControlGetExemptions(Builder.Connection.CongestionControl) > 0;
        }

        static void QuicPacketBuilderCleanup(QUIC_PACKET_BUILDER Builder)
        {
            NetLog.Assert(Builder.SendData == null);

            if (Builder.PacketBatchSent && Builder.PacketBatchRetransmittable)
            {
                QuicLossDetectionUpdateTimer(Builder.Connection.LossDetection, false);
            }

            QuicSentPacketMetadataReleaseFrames(Builder.Metadata, Builder.Connection);
            Array.Clear(Builder.HpMask, 0, Builder.HpMask.Length);
        }

        static void QuicPacketBuilderValidate(QUIC_PACKET_BUILDER Builder, bool ShouldHaveData)
        {
            if (ShouldHaveData)
            {
                NetLog.Assert(Builder.Key != null);
                NetLog.Assert(Builder.SendData != null);
                NetLog.Assert(Builder.Datagram != null);
                NetLog.Assert(Builder.DatagramLength != 0);
                NetLog.Assert(Builder.HeaderLength != 0);
                NetLog.Assert(Builder.Metadata.FrameCount != 0);
            }

            NetLog.Assert(Builder.Path != null);
            NetLog.Assert(Builder.Path.DestCid != null);
            NetLog.Assert(Builder.BatchCount <= QUIC_MAX_CRYPTO_BATCH_COUNT);

            if (Builder.Key != null)
            {
                NetLog.Assert(Builder.Key.PacketKey != null);
                NetLog.Assert(Builder.Key.HeaderKey != null);
            }

            NetLog.Assert(Builder.EncryptionOverhead <= 16);
            if (Builder.SendData == null)
            {
                NetLog.Assert(Builder.Datagram == null);
            }

            if (Builder.Datagram != null)
            {
                NetLog.Assert(Builder.Datagram.Length != 0);
                NetLog.Assert(Builder.Datagram.Length <= ushort.MaxValue);
                NetLog.Assert(Builder.Datagram.Length >= Builder.MinimumDatagramLength);
                NetLog.Assert(Builder.Datagram.Length >= (Builder.DatagramLength + Builder.EncryptionOverhead));
                NetLog.Assert(Builder.DatagramLength >= Builder.PacketStart);
                NetLog.Assert(Builder.DatagramLength >= Builder.HeaderLength);
                NetLog.Assert(Builder.DatagramLength >= Builder.PacketStart + Builder.HeaderLength);
                if (Builder.PacketType != SEND_PACKET_SHORT_HEADER_TYPE)
                {
                    NetLog.Assert(Builder.PayloadLengthOffset != 0);
                    if (ShouldHaveData)
                    {
                        NetLog.Assert(Builder.DatagramLength >= Builder.PacketStart + Builder.PayloadLengthOffset);
                    }
                }
            }
            else
            {
                NetLog.Assert(Builder.DatagramLength == 0);
                NetLog.Assert(Builder.Metadata.FrameCount == 0);
            }
        }

        static bool QuicPacketBuilderPrepareForPathMtuDiscovery(QUIC_PACKET_BUILDER Builder)
        {
            return QuicPacketBuilderPrepare(Builder, QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT, false, true);
        }

        static bool QuicPacketBuilderPrepareForStreamFrames(QUIC_PACKET_BUILDER Builder, bool IsTailLossProbe)
        {
            QUIC_PACKET_KEY_TYPE PacketKeyType;

            if (Builder.Connection.Crypto.TlsState.WriteKeys[(byte)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_0_RTT] != null &&

                Builder.Connection.Crypto.TlsState.WriteKeys[(byte)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT] == null)
            {
                PacketKeyType = QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_0_RTT;
            }
            else
            {
                NetLog.Assert(Builder.Connection.Crypto.TlsState.WriteKeys[(byte)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT] != null);
                PacketKeyType = QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT;
            }

            return QuicPacketBuilderPrepare(Builder, PacketKeyType, IsTailLossProbe, false);
        }


        static bool QuicPacketBuilderAddStreamFrame(QUIC_PACKET_BUILDER Builder, QUIC_STREAM Stream, QUIC_FRAME_TYPE FrameType)
        {
            Builder.Metadata.Frames[Builder.Metadata.FrameCount].MAX_STREAM_DATA.Stream = Stream;
            QuicStreamSentMetadataIncrement(Stream);
            return QuicPacketBuilderAddFrame(Builder, FrameType, true);
        }

        static void  QuicPacketBuilderSendBatch(QUIC_PACKET_BUILDER Builder)
        {
            QuicBindingSend(
                Builder.Path.Binding,
                Builder.Path.Route,
                Builder.SendData,
                Builder.TotalDatagramsLength,
                Builder.TotalCountDatagrams);

            Builder.PacketBatchSent = true;
            Builder.SendData = null;
            Builder.TotalDatagramsLength = 0;
            Builder.Metadata.FrameCount = 0;
        }

    }
}
