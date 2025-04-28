using AKNet.Common;
using System;
using System.Net.NetworkInformation;
using System.Text;

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
            QUIC_SSBuffer HandshakeCid;
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

            Status = QuicRecvBufferInitialize(Crypto.RecvBuffer, InitialRecvBufferLength, QUIC_DEFAULT_STREAM_FC_WINDOW_SIZE / 2, QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_SINGLE, null, null);
            if (QUIC_FAILED(Status))
            {
                goto Exit;
            }
            RecvBufferInitialized = true;

            if (QuicConnIsServer(Connection))
            {
                NetLog.Assert(Connection.SourceCids.Next != null);
                QUIC_CID_HASH_ENTRY SourceCid = CXPLAT_CONTAINING_RECORD<QUIC_CID_HASH_ENTRY>(Connection.SourceCids.Next);

                HandshakeCid = SourceCid.CID.Data.Buffer;
                HandshakeCid.Length = SourceCid.CID.Data.Length;
            }
            else
            {
                NetLog.Assert(!CxPlatListIsEmpty(Connection.DestCids));
                QUIC_CID_LIST_ENTRY DestCid = CXPLAT_CONTAINING_RECORD<QUIC_CID_LIST_ENTRY>(Connection.DestCids.Flink);

                HandshakeCid = DestCid.CID.Data.Buffer;
                HandshakeCid.Length = DestCid.CID.Data.Length;
            }

            Status = QuicPacketKeyCreateInitial(
                    QuicConnIsServer(Connection),
                    VersionInfo.HkdfLabels,
                    VersionInfo.Salt,
                    HandshakeCid,
                   ref Crypto.TlsState.ReadKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL],
                    ref Crypto.TlsState.WriteKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL]);

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
            CXPLAT_TLS_CONFIG TlsConfig = new CXPLAT_TLS_CONFIG();
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
                (Connection.State.TestTransportParameterSet ? Connection.TestTransportParameter : null));

            if (TlsConfig.LocalTPBuffer == null)
            {
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                goto Error;
            }

            if (Crypto.TLS != null)
            {
                Crypto.TLS = null;
            }

            Status = CxPlatTlsInitialize(TlsConfig, Crypto.TlsState, Crypto.TLS);
            if (QUIC_FAILED(Status))
            {
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

            QUIC_BUFFER[] Buffer = new QUIC_BUFFER[1];
            if (Crypto.CertValidationPending || (Crypto.TicketValidationPending && !Crypto.TicketValidationRejecting))
            {
                return Status;
            }

            if (IsClientInitial)
            {
                Buffer[0] = new QUIC_BUFFER();
            }
            else
            {
                int BufferOffset = 0;
                QuicRecvBufferRead(Crypto.RecvBuffer, ref BufferOffset, ref BufferCount, Buffer);
                NetLog.Assert(BufferCount == 1);

                QUIC_CONNECTION Connection = QuicCryptoGetConnection(Crypto);
                Buffer[0].Length = QuicCryptoTlsGetCompleteTlsMessagesLength(Buffer[0]);
                if (Buffer.Length == 0)
                {
                    goto Error;
                }

                if (QuicConnIsServer(Connection) && !Connection.State.ListenerAccepted)
                {
                    NetLog.Assert(BufferOffset == 0);
                    QUIC_NEW_CONNECTION_INFO Info = new QUIC_NEW_CONNECTION_INFO();
                    Status = QuicCryptoTlsReadInitial(Connection, Buffer[0], Info);
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
                    Info.CryptoBuffer = Buffer[0];

                    QuicBindingAcceptConnection(Connection.Paths[0].Binding, Connection, Info);

                    if (Connection.TlsSecrets != null && !Connection.State.HandleClosed && Connection.State.ExternalOwner)
                    {
                        QuicCryptoTlsReadClientRandom(Buffer[0], Buffer.Length, Connection.TlsSecrets);
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

            Crypto.ResultFlags = CxPlatTlsProcessData(Crypto.TLS,  CXPLAT_TLS_DATA_TYPE.CXPLAT_TLS_CRYPTO_DATA, Buffer[0], Crypto.TlsState);
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
            QUIC_SSBuffer HandshakeCid;

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
                HandshakeCid.Length = SourceCid.CID.Data.Length;
            }
            else
            {
                NetLog.Assert(!CxPlatListIsEmpty(Connection.DestCids));
                QUIC_CID_LIST_ENTRY DestCid = CXPLAT_CONTAINING_RECORD<QUIC_CID_LIST_ENTRY>(Connection.DestCids.Flink);
                HandshakeCid = DestCid.CID.Data;
                HandshakeCid.Length = DestCid.CID.Data.Length;
            }

            if (Crypto.TlsState.ReadKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL] != null)
            {
                NetLog.Assert(Crypto.TlsState.WriteKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL] != null);
                Crypto.TlsState.ReadKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL] = null;
                Crypto.TlsState.WriteKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL] = null;
            }

            Status = QuicPacketKeyCreateInitial(
                    QuicConnIsServer(Connection),
                    VersionInfo.HkdfLabels,
                    VersionInfo.Salt,
                    HandshakeCid,
                    ref Crypto.TlsState.ReadKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL],
                    ref Crypto.TlsState.WriteKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL]);

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
                    QuicCryptoDiscardKeys(Crypto,  QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_0_RTT);
                    QuicLossDetectionOnZeroRttRejected(Connection.LossDetection);
                }
                else
                {
                    QuicConnDiscardDeferred0Rtt(Connection);
                }
            }

            if (BoolOk(Crypto.ResultFlags & CXPLAT_TLS_RESULT_WRITE_KEY_UPDATED))
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
                    QuicSendQueueFlush(Connection.Send,  QUIC_SEND_FLUSH_REASON.REASON_NEW_KEY);
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
                            Crypto.TlsState.NegotiatedAlpn.AsSpan().Slice(1));
                    NetLog.Assert(Crypto.TlsState.NegotiatedAlpn != null);
                }

                QUIC_CONNECTION_EVENT Event = new QUIC_CONNECTION_EVENT();
                Event.Type = QUIC_CONNECTION_EVENT_TYPE.QUIC_CONNECTION_EVENT_CONNECTED;
                Event.CONNECTED.SessionResumed = Crypto.TlsState.SessionResumed;
                Event.CONNECTED.NegotiatedAlpnLength = Crypto.TlsState.NegotiatedAlpn[0];
                Event.CONNECTED.NegotiatedAlpn = Crypto.TlsState.NegotiatedAlpn.AsSpan().Slice(1).ToArray();
                QuicConnIndicateEvent(Connection, Event);
                if (Crypto.TlsState.SessionResumed)
                {
                    QuicPerfCounterIncrement(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_RESUMED);
                }
                Connection.Stats.ResumptionSucceeded = Crypto.TlsState.SessionResumed;

                NetLog.Assert(Connection.PathsCount == 1);
                QUIC_PATH Path = Connection.Paths[0];
                NetLog.Assert(Path.IsActive);

                if (Connection.Settings.IsSet.EncryptionOffloadAllowed)
                {
                    QuicPathUpdateQeo(Connection, Path, CXPLAT_QEO_OPERATION.CXPLAT_QEO_OPERATION_ADD);
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
            if (BoolOk(Crypto.ResultFlags & CXPLAT_TLS_RESULT_READ_KEY_UPDATED))
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

        static void QuicCryptoHandshakeConfirmed(QUIC_CRYPTO Crypto, bool SignalBinding)
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
            if (EncryptLevel >= QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_1_RTT)
            {
                return true;
            }

            NetLog.Assert(Connection.Packets[(int)EncryptLevel] != null);
            bool HasAckElicitingPacketsToAcknowledge = Connection.Packets[(int)EncryptLevel].AckTracker.AckElicitingPacketsToAcknowledge > 0;
            QuicLossDetectionDiscardPackets(Connection.LossDetection, KeyType);
            QuicPacketSpaceUninitialize(Connection.Packets[(int)EncryptLevel]);
            Connection.Packets[(int)EncryptLevel] = null;
            int BufferOffset = KeyType == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL ? Crypto.TlsState.BufferOffsetHandshake : Crypto.TlsState.BufferOffset1Rtt;
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
                    for (int i = 0; i < Crypto.TlsState.BufferLength; i++)
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
                return QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_1_RTT;
            }

            if (Crypto.TlsState.BufferOffsetHandshake != 0 &&
                SendOffset >= Crypto.TlsState.BufferOffsetHandshake)
            {
                return QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_HANDSHAKE;
            }

            return QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_INITIAL;
        }

        static void QuicCryptoCombineIvAndPacketNumber(byte[] IvIn, ulong PacketNumber, byte[] IvOut)
        {
            IvOut[0] = IvIn[0];
            IvOut[1] = IvIn[1];
            IvOut[2] = IvIn[2];
            IvOut[3] = IvIn[3];
            IvOut[4] = (byte)(IvIn[4] ^ (byte)PacketNumber);
            IvOut[5] = (byte)(IvIn[5] ^ (byte)(PacketNumber >> 8));
            IvOut[6] = (byte)(IvIn[6] ^ (byte)(PacketNumber >> 16));
            IvOut[7] = (byte)(IvIn[7] ^ (byte)(PacketNumber >> 24));
            IvOut[8] = (byte)(IvIn[8] ^ (byte)(PacketNumber >> 32));
            IvOut[9] = (byte)(IvIn[9] ^ (byte)(PacketNumber >> 40));
            IvOut[10] = (byte)(IvIn[10] ^ (byte)(PacketNumber >> 48));
            IvOut[11] = (byte)(IvIn[11] ^ (byte)(PacketNumber >> 56));
        }

        static bool QuicCryptoWriteOneFrame(QUIC_CRYPTO Crypto, int EncryptLevelStart, int CryptoOffset, int FramePayloadBytes, ref int Offset,
            int BufferLength, QUIC_SSBuffer Buffer, QUIC_SENT_PACKET_METADATA PacketMetadata)
        {
            QuicCryptoValidate(Crypto);
            NetLog.Assert(FramePayloadBytes > 0);
            NetLog.Assert(CryptoOffset >= EncryptLevelStart);
            NetLog.Assert(CryptoOffset <= Crypto.TlsState.BufferTotalLength);
            NetLog.Assert(CryptoOffset >= (Crypto.TlsState.BufferTotalLength - Crypto.TlsState.BufferLength));

            QUIC_CONNECTION Connection = QuicCryptoGetConnection(Crypto);
            QUIC_CRYPTO_EX Frame = new QUIC_CRYPTO_EX()
            {
                Offset = (int)(CryptoOffset - EncryptLevelStart),
                Length = 0,
                Data = null
            };

            Frame.Data.Buffer = Crypto.TlsState.Buffer.AsSpan().Slice((int)(CryptoOffset - (Crypto.TlsState.BufferTotalLength - Crypto.TlsState.BufferLength))).ToArray();
            int HeaderLength = sizeof(byte) + QuicVarIntSize((ulong)CryptoOffset);

            if (BufferLength < Offset + HeaderLength + 4)
            {
                return false;
            }

            Frame.Length = BufferLength - Offset - HeaderLength;
            int LengthFieldByteCount = QuicVarIntSize((ulong)Frame.Length);
            Frame.Length -= LengthFieldByteCount;

            if (Frame.Length > FramePayloadBytes)
            {
                Frame.Length = FramePayloadBytes;
            }

            NetLog.Assert(Frame.Length > 0);
            FramePayloadBytes = (ushort)Frame.Length;
            NetLog.Assert(QuicCryptoFrameEncode(Frame, Offset, BufferLength, Buffer));

            PacketMetadata.Flags.IsAckEliciting = true;
            PacketMetadata.Frames[PacketMetadata.FrameCount].Type = QUIC_FRAME_TYPE.QUIC_FRAME_CRYPTO;
            PacketMetadata.Frames[PacketMetadata.FrameCount].CRYPTO.Offset = (int)CryptoOffset;
            PacketMetadata.Frames[PacketMetadata.FrameCount].CRYPTO.Length = (ushort)Frame.Length;
            PacketMetadata.Frames[PacketMetadata.FrameCount].Flags = 0;
            PacketMetadata.FrameCount++;

            return true;
        }

        static void QuicCryptoWriteCryptoFrames(QUIC_CRYPTO Crypto,QUIC_PACKET_BUILDER Builder,ref int Offset,int BufferLength,QUIC_SSBuffer Buffer)
        {
            QuicCryptoValidate(Crypto);

            while (Offset < BufferLength && Builder.Metadata.FrameCount < QUIC_MAX_FRAMES_PER_PACKET)
            {
                int Left;
                int Right;
                bool Recovery;
                if (RECOV_WINDOW_OPEN(Crypto))
                {
                    Left = Crypto.RecoveryNextOffset;
                    Recovery = true;
                }
                else
                {
                    Left = Crypto.NextSendOffset;
                    Recovery = false;
                }

                if (Left == Crypto.TlsState.BufferTotalLength)
                {
                    break;
                }

                Right = Left + BufferLength - Offset;

                if (Recovery && Right > Crypto.RecoveryEndOffset && Crypto.RecoveryEndOffset != Crypto.NextSendOffset)
                {
                    Right = Crypto.RecoveryEndOffset;
                }

                QUIC_SUBRANGE Sack;
                if (Left == Crypto.MaxSentLength)
                {
                    Sack = null;
                }
                else
                {
                    int i = 0;
                    while ((Sack = QuicRangeGetSafe(Crypto.SparseAckRanges, i++)) != null && Sack.Low < (ulong)Left)
                    {
                        NetLog.Assert(Sack.Low + (ulong)Sack.Count <= (ulong)Left);
                    }
                }

                if (Sack != null)
                {
                    if ((ulong)Right > Sack.Low)
                    {
                        Right = (int)Sack.Low;
                    }
                }
                else
                {
                    if (Right > Crypto.TlsState.BufferTotalLength)
                    {
                        Right = Crypto.TlsState.BufferTotalLength;
                    }
                }

                NetLog.Assert(Right >= Left);
                int EncryptLevelStart;
                uint PacketTypeRight;
                if (QuicCryptoGetConnection(Crypto).Stats.QuicVersion == QUIC_VERSION_2)
                {
                    switch (Builder.PacketType)
                    {
                        case (byte)QUIC_LONG_HEADER_TYPE_V2.QUIC_INITIAL_V2:
                            EncryptLevelStart = 0;
                            if (Crypto.TlsState.BufferOffsetHandshake != 0)
                            {
                                PacketTypeRight = (uint)Crypto.TlsState.BufferOffsetHandshake;
                            }
                            else
                            {
                                PacketTypeRight = (uint)Crypto.TlsState.BufferTotalLength;
                            }
                            break;
                        case (byte)QUIC_LONG_HEADER_TYPE_V2.QUIC_0_RTT_PROTECTED_V2:
                            NetLog.Assert(false);
                            EncryptLevelStart = 0;
                            PacketTypeRight = 0;
                            break;
                        case (byte)QUIC_LONG_HEADER_TYPE_V2.QUIC_HANDSHAKE_V2:
                            NetLog.Assert(Crypto.TlsState.BufferOffsetHandshake != 0);
                            NetLog.Assert(Left >= Crypto.TlsState.BufferOffsetHandshake);
                            EncryptLevelStart = (int)Crypto.TlsState.BufferOffsetHandshake;
                            PacketTypeRight = Crypto.TlsState.BufferOffset1Rtt == 0 ? (uint)Crypto.TlsState.BufferTotalLength : (uint)Crypto.TlsState.BufferOffset1Rtt;
                            break;
                        default:
                            NetLog.Assert(Crypto.TlsState.BufferOffset1Rtt != 0);
                            NetLog.Assert(Left >= Crypto.TlsState.BufferOffset1Rtt);
                            EncryptLevelStart = (int)Crypto.TlsState.BufferOffset1Rtt;
                            PacketTypeRight = (uint)Crypto.TlsState.BufferTotalLength;
                            break;
                    }
                }
                else
                {
                    switch (Builder.PacketType)
                    {
                        case (byte)QUIC_LONG_HEADER_TYPE_V1.QUIC_INITIAL_V1:
                            EncryptLevelStart = 0;
                            if (Crypto.TlsState.BufferOffsetHandshake != 0)
                            {
                                PacketTypeRight = (uint)Crypto.TlsState.BufferOffsetHandshake;
                            }
                            else
                            {
                                PacketTypeRight = (uint)Crypto.TlsState.BufferTotalLength;
                            }
                            break;
                        case (byte)QUIC_LONG_HEADER_TYPE_V1.QUIC_0_RTT_PROTECTED_V1:
                            NetLog.Assert(false);
                            EncryptLevelStart = 0;
                            PacketTypeRight = 0; // To get build to stop complaining.
                            break;
                        case (byte)QUIC_LONG_HEADER_TYPE_V1.QUIC_HANDSHAKE_V1:
                            NetLog.Assert(Crypto.TlsState.BufferOffsetHandshake != 0);
                            NetLog.Assert(Left >= Crypto.TlsState.BufferOffsetHandshake);
                            EncryptLevelStart = (int)Crypto.TlsState.BufferOffsetHandshake;
                            PacketTypeRight = Crypto.TlsState.BufferOffset1Rtt == 0 ? (uint)Crypto.TlsState.BufferTotalLength : (uint)Crypto.TlsState.BufferOffset1Rtt;
                            break;
                        default:
                            NetLog.Assert(Crypto.TlsState.BufferOffset1Rtt != 0);
                            NetLog.Assert(Left >= Crypto.TlsState.BufferOffset1Rtt);
                            EncryptLevelStart = (int)Crypto.TlsState.BufferOffset1Rtt;
                            PacketTypeRight = (uint)Crypto.TlsState.BufferTotalLength;
                            break;
                    }
                }

                if (Right > PacketTypeRight)
                {
                    Right = (int)PacketTypeRight;
                }

                if (Left >= Right)
                {
                    break;
                }

                NetLog.Assert(Right > Left);

                int FramePayloadBytes = (int)(Right - Left);

                if (!QuicCryptoWriteOneFrame(
                        Crypto,
                        EncryptLevelStart,
                        Left,
                        FramePayloadBytes,
                        ref Offset,
                        BufferLength,
                        Buffer,
                        Builder.Metadata))
                {
                    break;
                }

                Right = Left + FramePayloadBytes;
                if (Recovery)
                {
                    NetLog.Assert(Crypto.RecoveryNextOffset <= Right);
                    Crypto.RecoveryNextOffset = (int)Right;
                    if (Sack != null && (ulong)Crypto.RecoveryNextOffset == Sack.Low)
                    {
                        Crypto.RecoveryNextOffset += (int)Sack.Count;
                    }
                }

                if (Crypto.NextSendOffset < Right)
                {
                    Crypto.NextSendOffset = Right;
                    if (Sack != null && (ulong)Crypto.NextSendOffset == Sack.Low)
                    {
                        Crypto.NextSendOffset += (int)Sack.Count;
                    }
                }

                if (Crypto.MaxSentLength < Right)
                {
                    Crypto.MaxSentLength = Right;
                }

                QuicCryptoValidate(Crypto);
            }

            QuicCryptoDumpSendState(Crypto);
            QuicCryptoValidate(Crypto);
        }

        static bool QuicCryptoWriteFrames(QUIC_CRYPTO Crypto, QUIC_PACKET_BUILDER Builder)
        {
            NetLog.Assert(Builder.Metadata.FrameCount < QUIC_MAX_FRAMES_PER_PACKET);
            QUIC_CONNECTION Connection = QuicCryptoGetConnection(Crypto);
            if (!QuicCryptoHasPendingCryptoFrame(Crypto))
            {
                Connection.Send.SendFlags &= ~QUIC_CONN_SEND_FLAG_CRYPTO;
                return true;
            }

            if ((Connection.Stats.QuicVersion != QUIC_VERSION_2 && Builder.PacketType != QuicEncryptLevelToPacketTypeV1(QuicCryptoGetNextEncryptLevel(Crypto))) ||
                (Connection.Stats.QuicVersion == QUIC_VERSION_2 && Builder.PacketType != QuicEncryptLevelToPacketTypeV2(QuicCryptoGetNextEncryptLevel(Crypto))))
            {
                return true;
            }

            if (QuicConnIsClient(Connection) && Builder.Key == Crypto.TlsState.WriteKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_HANDSHAKE])
            {
                NetLog.Assert(Builder.Key != null);
                QuicCryptoDiscardKeys(Crypto,  QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL);
            }

            int PrevFrameCount = Builder.Metadata.FrameCount;

            int AvailableBufferLength = Builder.Datagram.Length - Builder.EncryptionOverhead;
            QuicCryptoWriteCryptoFrames(Crypto, Builder, ref Builder.DatagramLength, AvailableBufferLength, Builder.Datagram.Buffer);
            if (!QuicCryptoHasPendingCryptoFrame(Crypto))
            {
                Connection.Send.SendFlags &= ~QUIC_CONN_SEND_FLAG_CRYPTO;
            }

            return Builder.Metadata.FrameCount > PrevFrameCount;
        }

        static ulong QuicCryptoGenerateNewKeys(QUIC_CONNECTION Connection)
        {
            ulong Status = QUIC_STATUS_SUCCESS;
            QUIC_PACKET_KEY NewReadKey = Connection.Crypto.TlsState.ReadKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT_NEW];
            QUIC_PACKET_KEY NewWriteKey = Connection.Crypto.TlsState.WriteKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT_NEW];

            QUIC_VERSION_INFO VersionInfo = QuicSupportedVersionList[0];
            for (int i = 0; i < QuicSupportedVersionList.Length; ++i)
            {
                if (QuicSupportedVersionList[i].Number == Connection.Stats.QuicVersion)
                {
                    VersionInfo = QuicSupportedVersionList[i];
                    break;
                }
            }

            NetLog.Assert(!((NewReadKey == null) ^ (NewWriteKey == null)));

            if (NewReadKey == null)
            {
                Status = QuicPacketKeyUpdate(VersionInfo.HkdfLabels, Connection.Crypto.TlsState.ReadKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT], NewReadKey);
                if (QUIC_FAILED(Status))
                {
                    goto Error;
                }

                Status = QuicPacketKeyUpdate(VersionInfo.HkdfLabels, Connection.Crypto.TlsState.WriteKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT], NewWriteKey);
                if (QUIC_FAILED(Status))
                {
                    goto Error;
                }
            }

        Error:
            if (QUIC_FAILED(Status))
            {
                QuicPacketKeyFree(NewReadKey);
                NewReadKey = null;
            }
            return Status;
        }

        static void QuicCryptoUpdateKeyPhase(QUIC_CONNECTION Connection,bool LocalUpdate)
        {
            QUIC_PACKET_KEY Old = Connection.Crypto.TlsState.ReadKeys[(byte)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT_OLD];
            QuicPacketKeyFree(Old);

            QUIC_PACKET_KEY Current = Connection.Crypto.TlsState.ReadKeys[(byte)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT];
            QUIC_PACKET_KEY New = Connection.Crypto.TlsState.ReadKeys[(byte)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT_NEW];
            New.HeaderKey = Current.HeaderKey;
            Current.HeaderKey = null;
            Old = Current;
            Current = New;
            New = null;

            Old = Connection.Crypto.TlsState.WriteKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT_OLD];
            QuicPacketKeyFree(Old);

            Current = Connection.Crypto.TlsState.WriteKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT];
            New = Connection.Crypto.TlsState.WriteKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT_NEW];
            New.HeaderKey = Current.HeaderKey;
            Current.HeaderKey = null;
            Old = Current;
            Current = New;
            New = null;

            if (Connection.Stats.Misc.KeyUpdateCount < uint.MaxValue)
            {
                Connection.Stats.Misc.KeyUpdateCount++;
            }

            QUIC_PACKET_SPACE PacketSpace = Connection.Packets[(int)QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_1_RTT];
            PacketSpace.WriteKeyPhaseStartPacketNumber = Connection.Send.NextPacketNumber;
            PacketSpace.CurrentKeyPhase = !PacketSpace.CurrentKeyPhase;
            PacketSpace.ReadKeyPhaseStartPacketNumber = ulong.MaxValue;
            PacketSpace.AwaitingKeyPhaseConfirmation = true;
            PacketSpace.CurrentKeyPhaseBytesSent = 0;
        }

        static void QuicCryptoOnAck(QUIC_CRYPTO Crypto, QUIC_SENT_FRAME_METADATA FrameMetadata)
        {
            int Offset = FrameMetadata.CRYPTO.Offset;
            int Length = FrameMetadata.CRYPTO.Length;
            int FollowingOffset = Offset + Length;

            NetLog.Assert(FollowingOffset <= Crypto.TlsState.BufferTotalLength);
            QUIC_CONNECTION Connection = QuicCryptoGetConnection(Crypto);
            if (Offset <= Crypto.UnAckedOffset)
            {
                if (Crypto.UnAckedOffset < FollowingOffset)
                {
                    int OldUnAckedOffset = Crypto.UnAckedOffset;
                    Crypto.UnAckedOffset = FollowingOffset;
                    QuicRangeSetMin(Crypto.SparseAckRanges, (ulong)Crypto.UnAckedOffset);

                    QUIC_SUBRANGE Sack = QuicRangeGetSafe(Crypto.SparseAckRanges, 0);
                    if (Sack != null && Sack.Low == (ulong)Crypto.UnAckedOffset)
                    {
                        Crypto.UnAckedOffset = (int)(Sack.Low + (ulong)Sack.Count);
                        QuicRangeRemoveSubranges(Crypto.SparseAckRanges, 0, 1);
                    }

                    int DrainLength = Crypto.UnAckedOffset - OldUnAckedOffset;
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

                    if (Crypto.NextSendOffset < Crypto.UnAckedOffset)
                    {
                        Crypto.NextSendOffset = Crypto.UnAckedOffset;
                    }
                    if (Crypto.RecoveryNextOffset < Crypto.UnAckedOffset)
                    {
                        Crypto.RecoveryNextOffset = Crypto.UnAckedOffset;
                    }
                    if (Crypto.RecoveryEndOffset < Crypto.UnAckedOffset)
                    {
                        Crypto.InRecovery = false;
                    }
                    if (Connection.State.Connected && QuicConnIsServer(Connection) &&
                        Crypto.TlsState.BufferOffset1Rtt != 0 &&
                        Crypto.UnAckedOffset == Crypto.TlsState.BufferTotalLength)
                    {
                        QuicConnCleanupServerResumptionState(Connection);
                    }
                }

            }
            else
            {

                bool SacksUpdated = false;
                QUIC_SUBRANGE Sack = QuicRangeAddRange(Crypto.SparseAckRanges, (ulong)Offset, Length, ref SacksUpdated);
                if (Sack == null)
                {
                    QuicConnFatalError(Connection, QUIC_STATUS_OUT_OF_MEMORY, "Out of memory");
                    return;
                }

                if (SacksUpdated)
                {
                    if (Crypto.NextSendOffset >= (int)Sack.Low && Crypto.NextSendOffset < (int)(Sack.Low + (ulong)Sack.Count))
                    {
                        Crypto.NextSendOffset = (int)(Sack.Low + (ulong)Sack.Count);
                    }
                    if ((ulong)Crypto.RecoveryNextOffset >= Sack.Low &&
                        (ulong)Crypto.RecoveryNextOffset < Sack.Low + (ulong)Sack.Count)
                    {
                        Crypto.RecoveryNextOffset = (int)(Sack.Low + (ulong)Sack.Count);
                    }
                }
            }

            if (!QuicCryptoHasPendingCryptoFrame(Crypto))
            {
                QuicSendClearSendFlag(Connection.Send, QUIC_CONN_SEND_FLAG_CRYPTO);
            }

            QuicCryptoDumpSendState(Crypto);
            QuicCryptoValidate(Crypto);
        }

        static void QuicCryptoReset(QUIC_CRYPTO Crypto)
        {
            NetLog.Assert(QuicConnIsClient(QuicCryptoGetConnection(Crypto)));
            NetLog.Assert(Crypto.RecvTotalConsumed == 0);

            Crypto.MaxSentLength = 0;
            Crypto.UnAckedOffset = 0;
            Crypto.NextSendOffset = 0;
            Crypto.RecoveryNextOffset = 0;
            Crypto.RecoveryEndOffset = 0;
            Crypto.InRecovery = false;

            QuicSendSetSendFlag(QuicCryptoGetConnection(Crypto).Send,QUIC_CONN_SEND_FLAG_CRYPTO);
            QuicCryptoValidate(Crypto);
        }

        static ulong QuicCryptoProcessFrame(QUIC_CRYPTO Crypto, QUIC_PACKET_KEY_TYPE KeyType, QUIC_CRYPTO_EX Frame)
        {
            ulong Status = QUIC_STATUS_SUCCESS;
            bool DataReady = false;
            Status = QuicCryptoProcessDataFrame(Crypto, KeyType, Frame, ref DataReady);
            if (QUIC_FAILED(Status) || !DataReady)
            {
                goto Error;
            }

            Status = QuicCryptoProcessData(Crypto, false);
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            QUIC_CONNECTION Connection = QuicCryptoGetConnection(Crypto);
            if (Connection.State.ClosedLocally)
            {
                Status = QUIC_STATUS_INVALID_STATE;
            }

        Error:
            return Status;
        }

        static ulong QuicCryptoProcessDataFrame(QUIC_CRYPTO Crypto, QUIC_PACKET_KEY_TYPE KeyType, QUIC_CRYPTO_EX Frame, ref bool DataReady)
        {
            ulong Status;
            QUIC_CONNECTION Connection = QuicCryptoGetConnection(Crypto);
            int FlowControlLimit = int.MaxValue;

            DataReady = false;
            if (Frame.Data.Length == 0)
            {
                Status = QUIC_STATUS_SUCCESS;
            }
            else if (!Crypto.Initialized)
            {
                Status = QUIC_STATUS_SUCCESS;
            }
            else
            {
                if (KeyType == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT_OLD || KeyType == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT_NEW)
                {
                    KeyType = QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT;
                }

                NetLog.Assert(KeyType <= Crypto.TlsState.ReadKey);
                if (KeyType < Crypto.TlsState.ReadKey)
                {
                    Status = QUIC_STATUS_SUCCESS;
                    goto Error;
                }

                Status = QuicRecvBufferWrite(Crypto.RecvBuffer,
                        Crypto.RecvEncryptLevelStartOffset + Frame.Data.Offset,
                        Frame.Data.Length,
                        Frame.Data.Buffer,
                        FlowControlLimit,
                        ref DataReady);

                if (QUIC_FAILED(Status))
                {
                    if (Status == QUIC_STATUS_BUFFER_TOO_SMALL)
                    {
                        QuicConnTransportError(Connection, QUIC_ERROR_CRYPTO_BUFFER_EXCEEDED);
                    }
                    goto Error;
                }
            }
            ;
        Error:
            return Status;
        }


        static ulong QuicCryptoEncodeServerTicket(QUIC_CONNECTION Connection, uint QuicVersion, int AppDataLength, byte[] AppResumptionData,
            QUIC_TRANSPORT_PARAMETERS HandshakeTP, int AlpnLength, byte[] NegotiatedAlpn, ref QUIC_SSBuffer Ticket)
        {
            ulong Status;
            byte[] TicketBuffer = null;

            Ticket = null;

            QUIC_TRANSPORT_PARAMETERS HSTPCopy = HandshakeTP;
            HSTPCopy.Flags &= QUIC_TP_FLAG_ACTIVE_CONNECTION_ID_LIMIT |
            QUIC_TP_FLAG_INITIAL_MAX_DATA |
            QUIC_TP_FLAG_INITIAL_MAX_STRM_DATA_BIDI_LOCAL |
            QUIC_TP_FLAG_INITIAL_MAX_STRM_DATA_BIDI_REMOTE |
            QUIC_TP_FLAG_INITIAL_MAX_STRM_DATA_UNI |
            QUIC_TP_FLAG_INITIAL_MAX_STRMS_BIDI |
            QUIC_TP_FLAG_INITIAL_MAX_STRMS_UNI;

            var EncodedHSTP = QuicCryptoTlsEncodeTransportParameters(
                    Connection,
                    true,
                    HSTPCopy,
                    null);
            if (EncodedHSTP == null)
            {
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                goto Error;
            }

            EncodedHSTP.Length -= CxPlatTlsTPHeaderSize;
            int TotalTicketLength =
                (QuicVarIntSize(CXPLAT_TLS_RESUMPTION_TICKET_VERSION) +
                sizeof_QuicVersion +
                QuicVarIntSize(AlpnLength) +
                QuicVarIntSize(EncodedHSTP.Length) +
                QuicVarIntSize(AppDataLength) +
                AlpnLength +
                EncodedHSTP.Length +
                AppDataLength);

            TicketBuffer = new byte[TotalTicketLength];
            if (TicketBuffer == null)
            {
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                goto Error;
            }

            NetLog.Assert(TicketBuffer.Length >= 8);
            QUIC_SSBuffer TicketCursor = QuicVarIntEncode(CXPLAT_TLS_RESUMPTION_TICKET_VERSION, TicketBuffer);
            EndianBitConverter.SetBytes(TicketCursor, 0, QuicVersion);

            TicketCursor = TicketCursor.Slice(sizeof_QuicVersion);
            TicketCursor = QuicVarIntEncode(AlpnLength, TicketCursor);
            TicketCursor = QuicVarIntEncode(EncodedHSTP.Length, TicketCursor);
            TicketCursor = QuicVarIntEncode(AppDataLength, TicketCursor);
            NegotiatedAlpn.AsSpan().Slice(0, AlpnLength).CopyTo(TicketCursor);
            TicketCursor = TicketCursor.Slice(AlpnLength);

            EncodedHSTP.GetSpan().Slice(CxPlatTlsTPHeaderSize, EncodedHSTP.Length).CopyTo(TicketCursor.GetSpan());
            TicketCursor = TicketCursor.Slice(EncodedHSTP.Length);

            if (AppDataLength > 0)
            {
                AppResumptionData.AsSpan().Slice(0, AppDataLength).CopyTo(TicketCursor.GetSpan());
                TicketCursor = TicketCursor.Slice(AppDataLength);
            }
            NetLog.Assert(TicketCursor.Length == 0);
            Ticket = TicketBuffer;
            Ticket = Ticket.Slice(0, TotalTicketLength);
            Status = QUIC_STATUS_SUCCESS;

        Error:
            if (EncodedHSTP != null)
            {

            }
            return Status;
        }

        static void QuicCryptoUninitialize(QUIC_CRYPTO Crypto)
        {
            for (int i = 0; i < (int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_COUNT; ++i)
            {
                QuicPacketKeyFree(Crypto.TlsState.ReadKeys[i]);
                Crypto.TlsState.ReadKeys[i] = null;
                QuicPacketKeyFree(Crypto.TlsState.WriteKeys[i]);
                Crypto.TlsState.WriteKeys[i] = null;
            }
            if (Crypto.TLS != null)
            {
                CxPlatTlsUninitialize(Crypto.TLS);
                Crypto.TLS = null;
            }
            if (Crypto.ResumptionTicket != null)
            {
                Crypto.ResumptionTicket = null;
            }
            if (Crypto.TlsState.NegotiatedAlpn != null && QuicConnIsServer(QuicCryptoGetConnection(Crypto)))
            {
                Crypto.TlsState.NegotiatedAlpn = null;
            }
            if (Crypto.Initialized)
            {
                QuicRecvBufferUninitialize(Crypto.RecvBuffer);
                QuicRangeUninitialize(Crypto.SparseAckRanges);
                Crypto.TlsState.Buffer = null;
                Crypto.Initialized = false;
            }
        }

        static ulong QuicCryptoProcessAppData(QUIC_CRYPTO Crypto, QUIC_SSBuffer AppData)
        {
            ulong Status;

            Crypto.ResultFlags = CxPlatTlsProcessData(
                Crypto.TLS,
                 CXPLAT_TLS_DATA_TYPE.CXPLAT_TLS_TICKET_DATA,
                AppData,
                Crypto.TlsState);

            if (BoolOk(Crypto.ResultFlags & CXPLAT_TLS_RESULT_ERROR)
            {
                if (Crypto.TlsState.AlertCode != 0) {
                    Status = Crypto.TlsState.AlertCode;
                } else {
                    Status = QUIC_STATUS_INTERNAL_ERROR;
                }
                goto Error;
            }

            QuicCryptoProcessDataComplete(Crypto, 0);
            Status = QUIC_STATUS_SUCCESS;
        Error:
            return Status;
        }

        static ulong QuicCryptoReNegotiateAlpn(QUIC_CONNECTION Connection, int AlpnListLength, QUIC_SSBuffer AlpnList)
        {
            NetLog.Assert(Connection != null);
            NetLog.Assert(AlpnList != null);
            NetLog.Assert(AlpnListLength > 0);

            int AlpnListOffset = 0;
            QUIC_SSBuffer PrevNegotiatedAlpn = Connection.Crypto.TlsState.NegotiatedAlpn;
            if (orBufferEqual(AlpnList, PrevNegotiatedAlpn))
            {
                return QUIC_STATUS_SUCCESS;
            }

            byte[] NewNegotiatedAlpn = null;
            while (AlpnListLength != 0)
            {
                byte[] Result = CxPlatTlsAlpnFindInList(Connection.Crypto.TlsState.ClientAlpnListLength, Connection.Crypto.TlsState.ClientAlpnList, AlpnList[0], AlpnList.Slice(1));
                if (Result != null)
                {
                    NewNegotiatedAlpn = AlpnList.ToArray();
                    break;
                }
                AlpnListLength -= AlpnList[0] + 1;
                AlpnList = AlpnList.Slice(1 + AlpnList[0]);
            }

            if (NewNegotiatedAlpn == null)
            {
                QuicConnTransportError(Connection, QUIC_ERROR_CRYPTO_NO_APPLICATION_PROTOCOL);
                return QUIC_STATUS_INVALID_PARAMETER;
            }

            if (Connection.Crypto.TlsState.NegotiatedAlpn != Connection.Crypto.TlsState.SmallAlpnBuffer)
            {
                Connection.Crypto.TlsState.NegotiatedAlpn = null;
            }

            byte[] NegotiatedAlpn = null;
            int NegotiatedAlpnLength = NewNegotiatedAlpn[0];
            if (NegotiatedAlpnLength < TLS_SMALL_ALPN_BUFFER_SIZE)
            {
                NegotiatedAlpn = Connection.Crypto.TlsState.SmallAlpnBuffer;
            }
            else
            {
                NegotiatedAlpn = new byte[NegotiatedAlpnLength + sizeof(byte)];
                if (NegotiatedAlpn == null)
                {
                    QuicConnTransportError(Connection, QUIC_ERROR_INTERNAL_ERROR);
                    return QUIC_STATUS_OUT_OF_MEMORY;
                }
            }
            NegotiatedAlpn[0] = (byte)NegotiatedAlpnLength;
            Array.Copy(NewNegotiatedAlpn, 1, NegotiatedAlpn, 1, NegotiatedAlpnLength);
            Connection.Crypto.TlsState.NegotiatedAlpn = NegotiatedAlpn;
            return QUIC_STATUS_SUCCESS;
        }

        static void QuicCryptoCustomCertValidationComplete(QUIC_CRYPTO Crypto, bool Result, QUIC_TLS_ALERT_CODES TlsAlert)
        {
            if (!Crypto.CertValidationPending)
            {
                return;
            }

            Crypto.CertValidationPending = false;
            if (Result)
            {
                QuicCryptoProcessDataComplete(Crypto, Crypto.PendingValidationBufferLength);

                if (QuicRecvBufferHasUnreadData(Crypto.RecvBuffer))
                {
                    QuicCryptoProcessData(Crypto, false);
                }
            }
            else
            {
                NetLog.Assert(TlsAlert <= QUIC_TLS_ALERT_CODES.QUIC_TLS_ALERT_CODE_MAX);
                QuicConnTransportError(QuicCryptoGetConnection(Crypto), QUIC_ERROR_CRYPTO_ERROR(0xFF & (uint)TlsAlert));
            }
            Crypto.PendingValidationBufferLength = 0;
        }

        static bool QuicCryptoOnLoss(QUIC_CRYPTO Crypto, QUIC_SENT_FRAME_METADATA FrameMetadata)
        {
            ulong Start = (ulong)FrameMetadata.CRYPTO.Offset;
            ulong End = (ulong)Start + (ulong)FrameMetadata.CRYPTO.Length;

            if (End <= (ulong)Crypto.UnAckedOffset)
            {
                return false;
            }

            if (Start < (ulong)Crypto.UnAckedOffset)
            {
                Start = (ulong)Crypto.UnAckedOffset;
            }

            QUIC_SUBRANGE Sack;
            int i = 0;
            while ((Sack = QuicRangeGetSafe(Crypto.SparseAckRanges, i++)) != null && Sack.Low < (ulong)End)
            {
                if (Start < Sack.Low + (ulong)Sack.Count)
                {
                    if (Start >= Sack.Low)
                    {
                        if (End <= Sack.Low + (ulong)Sack.Count)
                        {
                            return false;
                        }
                        Start = Sack.Low + (ulong)Sack.Count;

                    }
                    else if (End <= Sack.Low + (ulong)Sack.Count)
                    {
                        End = Sack.Low;
                    }
                }
            }

            bool UpdatedRecoveryWindow = false;
            if (Start < (ulong)Crypto.RecoveryNextOffset)
            {
                Crypto.RecoveryNextOffset = (int)Start;
                UpdatedRecoveryWindow = true;
            }

            if (Crypto.RecoveryEndOffset < (int)End)
            {
                Crypto.RecoveryEndOffset = (int)End;
                UpdatedRecoveryWindow = true;
            }

            if (UpdatedRecoveryWindow)
            {
                QUIC_CONNECTION Connection = QuicCryptoGetConnection(Crypto);
                if (!Crypto.InRecovery)
                {
                    Crypto.InRecovery = true;
                }

                bool DataQueued = QuicSendSetSendFlag(Connection.Send, QUIC_CONN_SEND_FLAG_CRYPTO);
                QuicCryptoDumpSendState(Crypto);
                QuicCryptoValidate(Crypto);

                return DataQueued;
            }

            return false;
        }

    }

}
