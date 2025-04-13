using AKNet.Common;
using System;

namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_CRYPTO
    {
        public bool Initialized;
        public bool InRecovery;
        public bool CertValidationPending;
        public CXPLAT_TLS TLS;

        public CXPLAT_TLS_PROCESS_STATE TlsState;
        public uint ResultFlags;
        public int MaxSentLength;
        public int UnAckedOffset;
        public int NextSendOffset;
        public int RecoveryNextOffset;
        public int RecoveryEndOffset;
        public QUIC_RANGE SparseAckRanges;
        public int RecvTotalConsumed;
        public bool TicketValidationPending;
        public bool TicketValidationRejecting;
        public int PendingValidationBufferLength;
        public int RecvEncryptLevelStartOffset;
        public QUIC_RECV_BUFFER RecvBuffer;
        public byte[] ResumptionTicket;
        public int ResumptionTicketLength;

        public QUIC_CONNECTION mConnection;
        public bool RECOV_WINDOW_OPEN()
        {
            return RecoveryNextOffset < RecoveryEndOffset;
        }
    }

    internal static partial class MSQuicFunc
    {
        static ulong QuicCryptoInitialize(QUIC_CRYPTO Crypto)
        {
            NetLog.Assert(Crypto.Initialized == false);
            ulong Status;
            QUIC_CONNECTION Connection = QuicCryptoGetConnection(Crypto);
            int SendBufferLength = QuicConnIsServer(Connection) ? QUIC_MAX_TLS_SERVER_SEND_BUFFER : QUIC_MAX_TLS_CLIENT_SEND_BUFFER;
            int InitialRecvBufferLength = QuicConnIsServer(Connection) ? QUIC_MAX_TLS_CLIENT_SEND_BUFFER : QUIC_DEFAULT_STREAM_RECV_BUFFER_SIZE;
            byte[] HandshakeCid;
            int HandshakeCidLength;
            bool RecvBufferInitialized = false;

            QUIC_VERSION_INFO VersionInfo = QuicSupportedVersionList[0]; // Default to latest
            for (int i = 0; i < QuicSupportedVersionList.Length; ++i)
            {
                if (QuicSupportedVersionList[i].Number == Connection.Stats.QuicVersion)
                {
                    VersionInfo = QuicSupportedVersionList[i];
                    break;
                }
            }

            QuicRangeInitialize(QUIC_MAX_RANGE_ALLOC_SIZE, Crypto.SparseAckRanges);

            Crypto.TlsState.BufferAllocLength = SendBufferLength;
            Crypto.TlsState.Buffer = new byte[SendBufferLength];
            if (Crypto.TlsState.Buffer == null)
            {
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                goto Exit;
            }

            QuicRangeInitialize(QUIC_MAX_RANGE_ALLOC_SIZE, Crypto.SparseAckRanges);

            Status = QuicRecvBufferInitialize(Crypto.RecvBuffer, InitialRecvBufferLength, QUIC_DEFAULT_STREAM_FC_WINDOW_SIZE / 2, QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_SINGLE, null);
            if (QUIC_FAILED(Status))
            {
                goto Exit;
            }
            RecvBufferInitialized = true;

            if (QuicConnIsServer(Connection))
            {
                NetLog.Assert(Connection.SourceCids.Next != null);
                QUIC_CID_HASH_ENTRY SourceCid = CXPLAT_CONTAINING_RECORD<QUIC_CID_HASH_ENTRY>(Connection.SourceCids.Next);

                HandshakeCid = SourceCid.CID.Data;
                HandshakeCidLength = SourceCid.CID.Length;
            }
            else
            {
                NetLog.Assert(!CxPlatListIsEmpty(Connection.DestCids));
                QUIC_CID_LIST_ENTRY DestCid = CXPLAT_CONTAINING_RECORD<QUIC_CID_LIST_ENTRY>(Connection.DestCids.Flink);

                HandshakeCid = DestCid.CID.Data;
                HandshakeCidLength = DestCid.CID.Length;
            }

            Status = QuicPacketKeyCreateInitial(
                    QuicConnIsServer(Connection),
                    VersionInfo.HkdfLabels,
                    VersionInfo.Salt,
                    HandshakeCidLength,
                    HandshakeCid,
                    Crypto.TlsState.ReadKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL],
                    Crypto.TlsState.WriteKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL]);

            if (QUIC_FAILED(Status))
            {
                goto Exit;
            }

            NetLog.Assert(Crypto.TlsState.ReadKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL] != null);
            NetLog.Assert(Crypto.TlsState.WriteKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL] != null);
            Crypto.Initialized = true;
        Exit:
            return Status;
        }

        static ulong QuicCryptoInitializeTls(QUIC_CRYPTO Crypto, CXPLAT_SEC_CONFIG SecConfig, QUIC_TRANSPORT_PARAMETERS Params)
        {
            ulong Status;
            CXPLAT_TLS_CONFIG TlsConfig = { 0 };
            QUIC_CONNECTION Connection = QuicCryptoGetConnection(Crypto);
            bool IsServer = QuicConnIsServer(Connection);

            NetLog.Assert(Params != null);
            NetLog.Assert(SecConfig != null);
            NetLog.Assert(Connection.Configuration != null);

            Crypto.MaxSentLength = 0;
            Crypto.UnAckedOffset = 0;
            Crypto.NextSendOffset = 0;
            Crypto.RecoveryNextOffset = 0;
            Crypto.RecoveryEndOffset = 0;
            Crypto.InRecovery = false;
            Crypto.TlsState.BufferLength = 0;
            Crypto.TlsState.BufferTotalLength = 0;

            TlsConfig.IsServer = IsServer;
            if (IsServer)
            {
                TlsConfig.AlpnBuffer = Crypto.TlsState.NegotiatedAlpn;
                TlsConfig.AlpnBufferLength = 1 + Crypto.TlsState.NegotiatedAlpn[0];
            }
            else
            {
                TlsConfig.AlpnBuffer = Connection.Configuration.AlpnList;
                TlsConfig.AlpnBufferLength = Connection.Configuration.AlpnListLength;
            }
            TlsConfig.SecConfig = SecConfig;
            TlsConfig.Connection = Connection;
            TlsConfig.ResumptionTicketBuffer = Crypto.ResumptionTicket;
            TlsConfig.ResumptionTicketLength = Crypto.ResumptionTicketLength;
            if (QuicConnIsClient(Connection))
            {
                TlsConfig.ServerName = Connection.RemoteServerName;
            }
            TlsConfig.TlsSecrets = Connection.TlsSecrets;

            TlsConfig.HkdfLabels = QuicSupportedVersionList[0].HkdfLabels; // Default to latest
            for (int i = 0; i < QuicSupportedVersionList.Length; ++i)
            {
                if (QuicSupportedVersionList[i].Number == Connection.Stats.QuicVersion)
                {
                    TlsConfig.HkdfLabels = QuicSupportedVersionList[i].HkdfLabels;
                    break;
                }
            }

            TlsConfig.TPType = Connection.Stats.QuicVersion != QUIC_VERSION_DRAFT_29 ? TLS_EXTENSION_TYPE_QUIC_TRANSPORT_PARAMETERS : TLS_EXTENSION_TYPE_QUIC_TRANSPORT_PARAMETERS_DRAFT;
            TlsConfig.LocalTPBuffer = QuicCryptoTlsEncodeTransportParameters(Connection, QuicConnIsServer(Connection), Params,
                (Connection.State.TestTransportParameterSet ? Connection.TestTransportParameter : null), TlsConfig.LocalTPLength);

            if (TlsConfig.LocalTPBuffer == null)
            {
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                goto Error;
            }

            if (Crypto.TLS != null)
            {
                CxPlatTlsUninitialize(Crypto.TLS);
                Crypto.TLS = null;
            }

            Status = CxPlatTlsInitialize(TlsConfig, Crypto.TlsState, Crypto.TLS);
            if (QUIC_FAILED(Status))
            {
                CXPLAT_FREE(TlsConfig.LocalTPBuffer, QUIC_POOL_TLS_TRANSPARAMS);
                goto Error;
            }

            Crypto.ResumptionTicket = null; // Owned by TLS now.
            Crypto.ResumptionTicketLength = 0;
            Status = QuicCryptoProcessData(Crypto, !IsServer);

        Error:
            return Status;
        }

        static void QuicCryptoCustomTicketValidationComplete(QUIC_CRYPTO Crypto, bool Result)
        {
            if (!Crypto.TicketValidationPending || Crypto.TicketValidationRejecting)
            {
                return;
            }

            if (Result)
            {
                Crypto.TicketValidationPending = false;
                QuicCryptoProcessDataComplete(Crypto, Crypto.PendingValidationBufferLength);

                if (QuicRecvBufferHasUnreadData(Crypto.RecvBuffer))
                {
                    QuicCryptoProcessData(Crypto, false);
                }
            }
            else
            {
                Crypto.TicketValidationRejecting = true;

                Crypto.TlsState.ReadKey = QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL;
                Crypto.TlsState.WriteKey = QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL;
                Crypto.TlsState.BufferOffsetHandshake = 0;
                Crypto.TlsState.BufferOffset1Rtt = 0;
                for (int i = (int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_0_RTT; i < (int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_COUNT; ++i)
                {
                    Crypto.TlsState.ReadKeys[i] = null;
                    Crypto.TlsState.WriteKeys[i] = null;
                }
                QuicRecvBufferResetRead(Crypto.RecvBuffer);
                QUIC_CONNECTION Connection = QuicCryptoGetConnection(Crypto);
                ulong Status = QuicCryptoInitializeTls(Crypto, Connection.Configuration.SecurityConfig, Connection.HandshakeTP);
                if (Status != QUIC_STATUS_SUCCESS)
                {
                    QuicConnFatalError(Connection, Status, "Failed finalizing resumption ticket rejection");
                }
            }
            Crypto.PendingValidationBufferLength = 0;
        }



        static ulong QuicCryptoProcessData(QUIC_CRYPTO Crypto, bool IsClientInitial)
        {
            ulong Status = QUIC_STATUS_SUCCESS;
            int BufferCount = 1;

            QUIC_BUFFER Buffer = new QUIC_BUFFER();
            if (Crypto.CertValidationPending || (Crypto.TicketValidationPending && !Crypto.TicketValidationRejecting))
            {
                return Status;
            }

            if (IsClientInitial)
            {
                Buffer.Length = 0;
                Buffer.Buffer = null;
            }
            else
            {
                long BufferOffset = 0;
                QuicRecvBufferRead(Crypto.RecvBuffer, ref BufferOffset, ref BufferCount, Buffer);
                NetLog.Assert(BufferCount == 1);

                QUIC_CONNECTION Connection = QuicCryptoGetConnection(Crypto);
                Buffer.Length = QuicCryptoTlsGetCompleteTlsMessagesLength(Buffer.Buffer.AsSpan().Slice(0, Buffer.Length));
                if (Buffer.Length == 0)
                {
                    goto Error;
                }

                if (QuicConnIsServer(Connection) && !Connection.State.ListenerAccepted)
                {
                    NetLog.Assert(BufferOffset == 0);
                    QUIC_NEW_CONNECTION_INFO Info = new QUIC_NEW_CONNECTION_INFO();
                    Status = QuicCryptoTlsReadInitial(Connection, Buffer.Buffer, Info);
                    if (QUIC_FAILED(Status))
                    {
                        QuicConnTransportError(Connection, QUIC_ERROR_CRYPTO_HANDSHAKE_FAILURE);
                        goto Error;
                    }
                    else if (Status == QUIC_STATUS_PENDING)
                    {
                        goto Error;
                    }

                    Status = QuicConnProcessPeerTransportParameters(Connection, false);
                    if (QUIC_FAILED(Status))
                    {
                        goto Error;
                    }

                    QuicRecvBufferDrain(Crypto.RecvBuffer, 0);
                    QuicCryptoValidate(Crypto);

                    Info.QuicVersion = Connection.Stats.QuicVersion;
                    Info.LocalAddress = Connection.Paths[0].Route.LocalAddress;
                    Info.RemoteAddress = Connection.Paths[0].Route.RemoteAddress;
                    Info.CryptoBufferLength = Buffer.Length;
                    Info.CryptoBuffer = Buffer.Buffer;

                    QuicBindingAcceptConnection(Connection.Paths[0].Binding, Connection, Info);

                    if (Connection.TlsSecrets != null && !Connection.State.HandleClosed && Connection.State.ExternalOwner)
                    {
                        QuicCryptoTlsReadClientRandom(Buffer.Buffer, Buffer.Length, Connection.TlsSecrets);
                    }
                    return Status;
                }
            }

            NetLog.Assert(Crypto.TLS != null);
            if (Crypto.TLS == null)
            {
                goto Error;
            }

            QuicCryptoValidate(Crypto);

            Crypto.ResultFlags = CxPlatTlsProcessData(Crypto.TLS, CXPLAT_TLS_CRYPTO_DATA, Buffer.Buffer, Buffer.Length, Crypto.TlsState);
            QuicCryptoProcessDataComplete(Crypto, Buffer.Length);
            return Status;

        Error:
            QuicRecvBufferDrain(Crypto.RecvBuffer, 0);
            QuicCryptoValidate(Crypto);
            return Status;
        }

        static void QuicCryptoProcessDataComplete(QUIC_CRYPTO Crypto, int RecvBufferConsumed)
        {
            if (Crypto.TicketValidationPending || Crypto.CertValidationPending)
            {
                Crypto.PendingValidationBufferLength = RecvBufferConsumed;
                return;
            }

            if (RecvBufferConsumed != 0)
            {
                Crypto.RecvTotalConsumed += RecvBufferConsumed;
                QuicRecvBufferDrain(Crypto.RecvBuffer, RecvBufferConsumed);
            }

            QuicCryptoValidate(Crypto);
            QuicCryptoProcessTlsCompletion(Crypto);
        }

        static ulong QuicCryptoOnVersionChange(QUIC_CRYPTO Crypto)
        {
            ulong Status;
            QUIC_CONNECTION Connection = QuicCryptoGetConnection(Crypto);
            byte[] HandshakeCid;
            int HandshakeCidLength;

            if (!Crypto.Initialized)
            {
                return QUIC_STATUS_SUCCESS;
            }

            QUIC_VERSION_INFO VersionInfo = QuicSupportedVersionList[0]; // Default to latest
            for (int i = 0; i < QuicSupportedVersionList.Length; ++i)
            {
                if (QuicSupportedVersionList[i].Number == Connection.Stats.QuicVersion)
                {
                    VersionInfo = QuicSupportedVersionList[i];
                    break;
                }
            }

            if (Crypto.TLS != null)
            {
                CxPlatTlsUpdateHkdfLabels(Crypto.TLS, VersionInfo.HkdfLabels);
            }

            if (QuicConnIsServer(Connection))
            {
                NetLog.Assert(Connection.SourceCids.Next != null);
                QUIC_CID_HASH_ENTRY SourceCid = CXPLAT_CONTAINING_RECORD<QUIC_CID_HASH_ENTRY>(Connection.SourceCids.Next);

                HandshakeCid = SourceCid.CID.Data;
                HandshakeCidLength = SourceCid.CID.Length;
            }
            else
            {
                NetLog.Assert(!CxPlatListIsEmpty(Connection.DestCids));
                QUIC_CID_LIST_ENTRY DestCid = CXPLAT_CONTAINING_RECORD<QUIC_CID_LIST_ENTRY>(Connection.DestCids.Flink);
                HandshakeCid = DestCid.CID.Data;
                HandshakeCidLength = DestCid.CID.Length;
            }

            if (Crypto.TlsState.ReadKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL] != null)
            {
                NetLog.Assert(Crypto.TlsState.WriteKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL] != null);
                Crypto.TlsState.ReadKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL] = null;
                Crypto.TlsState.WriteKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL] = null;
            }

            Status = QuicPacketKeyCreateInitial(
                    QuicConnIsServer(Connection),
                    &VersionInfo.HkdfLabels,
                    VersionInfo.Salt,
                    HandshakeCidLength,
                    HandshakeCid,
                    &Crypto.TlsState.ReadKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL],
                    &Crypto.TlsState.WriteKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL]);

            if (QUIC_FAILED(Status))
            {
                QuicConnFatalError(Connection, Status, "New version key OOM");
                goto Exit;
            }
            NetLog.Assert(Crypto.TlsState.ReadKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL] != null);
            NetLog.Assert(Crypto.TlsState.WriteKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL] != null);

        Exit:
            if (QUIC_FAILED(Status))
            {
                for (int i = 0; i < (int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_COUNT; ++i)
                {
                    Crypto.TlsState.ReadKeys[i] = null;
                    Crypto.TlsState.WriteKeys[i] = null;
                }
            }
            return Status;
        }

        static void QuicCryptoProcessTlsCompletion(QUIC_CRYPTO Crypto)
        {
            NetLog.Assert(!Crypto.TicketValidationPending && !Crypto.CertValidationPending);
            QUIC_CONNECTION Connection = QuicCryptoGetConnection(Crypto);

            if (BoolOk(Crypto.ResultFlags & CXPLAT_TLS_RESULT_ERROR))
            {
                QuicConnTransportError(Connection, QUIC_ERROR_CRYPTO_ERROR((uint)(0xFF & Crypto.TlsState.AlertCode)));
                return;
            }

            QuicCryptoValidate(Crypto);

            if (BoolOk(Crypto.ResultFlags & CXPLAT_TLS_RESULT_EARLY_DATA_ACCEPT))
            {
                NetLog.Assert(Crypto.TlsState.EarlyDataState == CXPLAT_TLS_EARLY_DATA_STATE.CXPLAT_TLS_EARLY_DATA_ACCEPTED);
            }

            if (BoolOk(Crypto.ResultFlags & CXPLAT_TLS_RESULT_EARLY_DATA_REJECT))
            {
                NetLog.Assert(Crypto.TlsState.EarlyDataState != CXPLAT_TLS_EARLY_DATA_STATE.CXPLAT_TLS_EARLY_DATA_ACCEPTED);
                if (QuicConnIsClient(Connection))
                {
                    QuicCryptoDiscardKeys(Crypto, QUIC_PACKET_KEY_0_RTT);
                    QuicLossDetectionOnZeroRttRejected(&Connection->LossDetection);
                }
                else
                {
                    QuicConnDiscardDeferred0Rtt(Connection);
                }
            }

            if (Crypto.ResultFlags & CXPLAT_TLS_RESULT_WRITE_KEY_UPDATED)
            {
                NetLog.Assert(Crypto.TlsState.WriteKey <= QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT);
                NetLog.Assert(Crypto.TlsState.WriteKey >= 0);
                NetLog.Assert(Crypto.TlsState.WriteKeys[(int)Crypto.TlsState.WriteKey] != null);
                if (Crypto.TlsState.WriteKey == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT)
                {
                    if (QuicConnIsClient(Connection))
                    {
                        QuicCryptoDiscardKeys(Crypto, QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_0_RTT);
                    }
                    QuicSendQueueFlush(Connection.Send, REASON_NEW_KEY);
                }

                if (QuicConnIsServer(Connection))
                {
                    if (Crypto.TlsState.WriteKey == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT)
                    {
                        Connection.Stats.Handshake.ServerFlight1Bytes = Crypto.TlsState.BufferOffset1Rtt;
                    }
                }
                else
                {
                    if (Crypto.TlsState.WriteKey == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_HANDSHAKE)
                    {
                        Connection.Stats.Handshake.ClientFlight1Bytes = Crypto.TlsState.BufferOffsetHandshake;
                    }

                    if (Crypto.TlsState.WriteKey == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT)
                    {
                        Connection.Stats.Handshake.ClientFlight2Bytes = Crypto.TlsState.BufferOffset1Rtt - Crypto.TlsState.BufferOffsetHandshake;
                    }
                }
            }

            if (BoolOk(Crypto.ResultFlags & CXPLAT_TLS_RESULT_READ_KEY_UPDATED))
            {
                if (QuicRecvBufferHasUnreadData(Crypto.RecvBuffer))
                {
                    QuicConnTransportError(Connection, QUIC_ERROR_PROTOCOL_VIOLATION);
                    return;
                }

                Crypto.RecvEncryptLevelStartOffset = Crypto.RecvTotalConsumed;
                NetLog.Assert(Crypto.TlsState.ReadKey <= QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT);
                NetLog.Assert(Crypto.TlsState.ReadKey >= 0);
                NetLog.Assert(Crypto.TlsState.WriteKey >= Crypto.TlsState.ReadKey);
                NetLog.Assert(Crypto.TlsState.ReadKeys[(int)Crypto.TlsState.ReadKey] != null);

                if (QuicConnIsServer(Connection))
                {
                    if (Crypto.TlsState.ReadKey == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_HANDSHAKE)
                    {
                        Connection.Stats.Handshake.ClientFlight1Bytes = Crypto.RecvTotalConsumed;
                    }

                    if (Crypto.TlsState.ReadKey == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT)
                    {
                        Connection.Stats.Handshake.ClientFlight2Bytes = Crypto.RecvTotalConsumed - Connection.Stats.Handshake.ClientFlight1Bytes;
                    }
                }
                else
                {
                    if (Crypto.TlsState.ReadKey == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT)
                    {
                        Connection.Stats.Handshake.ServerFlight1Bytes = Crypto.RecvTotalConsumed;
                    }
                }

                if (Connection.Stats.Timing.InitialFlightEnd == 0)
                {
                    Connection.Stats.Timing.InitialFlightEnd = CxPlatTime();
                }

                if (Crypto.TlsState.ReadKey == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT)
                {
                    Connection.Stats.Timing.HandshakeFlightEnd = CxPlatTime();
                }
            }

            if (BoolOk(Crypto.ResultFlags & CXPLAT_TLS_RESULT_DATA))
            {
                if (Connection.TlsSecrets != null &&
                    QuicConnIsClient(Connection) &&
                    (Crypto.TlsState.WriteKey == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL ||
                        Crypto.TlsState.WriteKey == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_0_RTT) &&
                    Crypto.TlsState.BufferLength > 0)
                {

                    QuicCryptoTlsReadClientRandom(
                        Crypto.TlsState.Buffer,
                        Crypto.TlsState.BufferLength,
                        Connection.TlsSecrets);

                    Connection.TlsSecrets = null;
                }

                QuicSendSetSendFlag(QuicCryptoGetConnection(Crypto).Send, QUIC_CONN_SEND_FLAG_CRYPTO);
                QuicCryptoDumpSendState(Crypto);
                QuicCryptoValidate(Crypto);
            }

            if (BoolOk(Crypto.ResultFlags & CXPLAT_TLS_RESULT_HANDSHAKE_COMPLETE))
            {
                NetLog.Assert(!BoolOk(Crypto.ResultFlags & CXPLAT_TLS_RESULT_ERROR));
                NetLog.Assert(!Connection.State.Connected);
                NetLog.Assert(!Crypto.TicketValidationPending && !Crypto.CertValidationPending);

                NetLog.Assert(Crypto.TlsState.ReadKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT] != null);
                NetLog.Assert(Crypto.TlsState.WriteKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT] != null);

                if (QuicConnIsServer(Connection))
                {
                    QuicSendSetSendFlag(Connection.Send, QUIC_CONN_SEND_FLAG_HANDSHAKE_DONE);
                    QuicCryptoHandshakeConfirmed(Connection.Crypto, false);
                    NetLog.Assert(Connection.SourceCids.Next != null);
                    NetLog.Assert(Connection.SourceCids.Next.Next != null);
                    NetLog.Assert(Connection.SourceCids.Next.Next != null);
                    NetLog.Assert(Connection.SourceCids.Next.Next.Next == null);
                    QUIC_CID_HASH_ENTRY InitialSourceCid = CXPLAT_CONTAINING_RECORD<QUIC_CID_HASH_ENTRY>(Connection.SourceCids.Next.Next);

                    NetLog.Assert(InitialSourceCid.CID.IsInitial);
                    Connection.SourceCids.Next.Next = Connection.SourceCids.Next.Next.Next;
                    NetLog.Assert(!InitialSourceCid.CID.IsInLookupTable);
                    CXPLAT_FREE(InitialSourceCid, QUIC_POOL_CIDHASH);
                }

                Connection.State.Connected = true;
                QuicPerfCounterIncrement(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_CONNECTED);
                QuicConnGenerateNewSourceCids(Connection, false);
                NetLog.Assert(Crypto.RecvBuffer.TlsState.NegotiatedAlpn != null);

                if (QuicConnIsClient(Connection))
                {
                    Crypto.TlsState.NegotiatedAlpn =
                        CxPlatTlsAlpnFindInList(
                            Connection.Configuration.AlpnListLength,
                            Connection.Configuration.AlpnList,
                            Crypto.TlsState.NegotiatedAlpn[0],
                            Crypto.TlsState.NegotiatedAlpn + 1);
                    NetLog.Assert(Crypto.TlsState.NegotiatedAlpn != null);
                }

                QUIC_CONNECTION_EVENT Event;
                Event.Type = QUIC_CONNECTION_EVENT_TYPE.QUIC_CONNECTION_EVENT_CONNECTED;
                Event.CONNECTED.SessionResumed = Crypto.TlsState.SessionResumed;
                Event.CONNECTED.NegotiatedAlpnLength = Crypto.TlsState.NegotiatedAlpn[0];
                Event.CONNECTED.NegotiatedAlpn = Crypto.TlsState.NegotiatedAlpn + 1;
                QuicConnIndicateEvent(Connection, Event);
                if (Crypto.TlsState.SessionResumed)
                {
                    QuicPerfCounterIncrement(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_RESUMED);
                }
                Connection.Stats.ResumptionSucceeded = Crypto.TlsState.SessionResumed;

                NetLog.Assert(Connection.PathsCount == 1);
                QUIC_PATH Path = Connection.Paths[0];
                NetLog.Assert(Path.IsActive);

                if (Connection.Settings.EncryptionOffloadAllowed)
                {
                    QuicPathUpdateQeo(Connection, Path, CXPLAT_QEO_OPERATION_ADD);
                }

                QuicMtuDiscoveryPeerValidated(Path.MtuDiscovery, Connection);

                if (QuicConnIsServer(Connection) &&
                    Crypto.TlsState.BufferOffset1Rtt != 0 &&
                    Crypto.UnAckedOffset == Crypto.TlsState.BufferTotalLength)
                {
                    QuicConnCleanupServerResumptionState(Connection);
                }
            }

            QuicCryptoValidate(Crypto);
            if (Crypto.ResultFlags & CXPLAT_TLS_RESULT_READ_KEY_UPDATED)
            {
                QuicConnFlushDeferred(Connection);
            }
        }

        static void QuicCryptoValidate(QUIC_CRYPTO Crypto)
        {
            NetLog.Assert(Crypto.TlsState.BufferTotalLength >= Crypto.MaxSentLength);
            NetLog.Assert(Crypto.MaxSentLength >= Crypto.UnAckedOffset);
            NetLog.Assert(Crypto.MaxSentLength >= Crypto.NextSendOffset);
            NetLog.Assert(Crypto.MaxSentLength >= Crypto.RecoveryNextOffset);
            NetLog.Assert(Crypto.MaxSentLength >= Crypto.RecoveryEndOffset);
            NetLog.Assert(Crypto.NextSendOffset >= Crypto.UnAckedOffset);
            NetLog.Assert(Crypto.TlsState.BufferLength + Crypto.UnAckedOffset == Crypto.TlsState.BufferTotalLength);
        }

        static void QuicCryptoDumpSendState(QUIC_CRYPTO Crypto)
        {
           
        }

        static void QuicCryptoHandshakeConfirmed(QUIC_CRYPTO Crypto,bool SignalBinding)
        {
            QUIC_CONNECTION Connection = QuicCryptoGetConnection(Crypto);
            Connection.State.HandshakeConfirmed = true;

            if (SignalBinding)
            {
                QUIC_PATH Path = Connection.Paths[0];
                NetLog.Assert(Path.Binding != null);
                QuicBindingOnConnectionHandshakeConfirmed(Path.Binding, Connection);
            }

            QuicCryptoDiscardKeys(Crypto, QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_HANDSHAKE);
        }

        static bool QuicCryptoDiscardKeys(QUIC_CRYPTO Crypto, QUIC_PACKET_KEY_TYPE KeyType)
        {
            if (Crypto.TlsState.WriteKeys[(int)KeyType] == null && Crypto.TlsState.ReadKeys[(int)KeyType] == null)
            {
                return false;
            }

            QUIC_CONNECTION Connection = QuicCryptoGetConnection(Crypto);
            Crypto.TlsState.WriteKeys[(int)KeyType] = null;
            Crypto.TlsState.ReadKeys[(int)KeyType] = null;

            QUIC_ENCRYPT_LEVEL EncryptLevel = QuicKeyTypeToEncryptLevel(KeyType);
            NetLog.Assert(EncryptLevel >= 0);
            if (EncryptLevel >=  QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_1_RTT)
            {
                return true;
            }

            NetLog.Assert(Connection.Packets[(int)EncryptLevel] != null);
            bool HasAckElicitingPacketsToAcknowledge = Connection.Packets[(int)EncryptLevel].AckTracker.AckElicitingPacketsToAcknowledge;
            QuicLossDetectionDiscardPackets(Connection.LossDetection, KeyType);
            QuicPacketSpaceUninitialize(Connection.Packets[(int)EncryptLevel]);
            Connection.Packets[(int)EncryptLevel] = null;

            //
            // Clean up any possible left over recovery state.
            //
            int BufferOffset = KeyType ==  QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL ? Crypto.TlsState.BufferOffsetHandshake : Crypto.TlsState.BufferOffset1Rtt;
            NetLog.Assert(BufferOffset != 0);
            NetLog.Assert(Crypto.MaxSentLength >= BufferOffset);
            if (Crypto.NextSendOffset < BufferOffset)
            {
                Crypto.NextSendOffset = BufferOffset;
            }
            if (Crypto.RecoveryNextOffset < BufferOffset)
            {
                Crypto.RecoveryNextOffset = BufferOffset;
            }
            if (Crypto.UnAckedOffset < BufferOffset)
            {
                int DrainLength = BufferOffset - Crypto.UnAckedOffset;
                NetLog.Assert(DrainLength <= Crypto.TlsState.BufferLength);
                if (Crypto.TlsState.BufferLength > DrainLength)
                {
                    Crypto.TlsState.BufferLength -= DrainLength;
                    for(int i = 0; i < Crypto.TlsState.BufferLength; i++)
                    {
                        Crypto.TlsState.Buffer[i] = Crypto.TlsState.Buffer[DrainLength + i];
                    }
                }
                else
                {
                    Crypto.TlsState.BufferLength = 0;
                }
                Crypto.UnAckedOffset = BufferOffset;
                QuicRangeSetMin(Crypto.SparseAckRanges, (ulong)Crypto.UnAckedOffset);
            }

            if (HasAckElicitingPacketsToAcknowledge)
            {
                QuicSendUpdateAckState(Connection.Send);
            }

            QuicCryptoValidate(Crypto);
            return true;
        }

        static bool QuicCryptoHasPendingCryptoFrame(QUIC_CRYPTO Crypto)
        {
            return Crypto.RECOV_WINDOW_OPEN() || (Crypto.NextSendOffset < Crypto.TlsState.BufferTotalLength);
        }

        static QUIC_ENCRYPT_LEVEL QuicCryptoGetNextEncryptLevel(QUIC_CRYPTO Crypto)
        {
            int SendOffset = Crypto.RECOV_WINDOW_OPEN() ? Crypto.RecoveryNextOffset : Crypto.NextSendOffset;

            if (Crypto.TlsState.BufferOffset1Rtt != 0 &&
                SendOffset >= Crypto.TlsState.BufferOffset1Rtt)
            {
                return  QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_1_RTT;
            }

            if (Crypto.TlsState.BufferOffsetHandshake != 0 &&
                SendOffset >= Crypto.TlsState.BufferOffsetHandshake)
            {
                return  QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_HANDSHAKE;
            }

            return  QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_INITIAL;
        }

    }
}
