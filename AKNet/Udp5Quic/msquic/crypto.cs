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
        public CXPLAT_TLS_RESULT_FLAGS ResultFlags;
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

        static ulong QuicCryptoProcessData(QUIC_CRYPTO Crypto,bool IsClientInitial)
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

                    Status = QuicConnProcessPeerTransportParameters(Connection, FALSE);
                    if (QUIC_FAILED(Status))
                    {
                        //
                        // Communicate error up the stack to perform Incompatible
                        // Version Negotiation.
                        //
                        goto Error;
                    }

                    QuicRecvBufferDrain(&Crypto->RecvBuffer, 0);
                    QuicCryptoValidate(Crypto);

                    Info.QuicVersion = Connection->Stats.QuicVersion;
                    Info.LocalAddress = &Connection->Paths[0].Route.LocalAddress;
                    Info.RemoteAddress = &Connection->Paths[0].Route.RemoteAddress;
                    Info.CryptoBufferLength = Buffer.Length;
                    Info.CryptoBuffer = Buffer.Buffer;

                    QuicBindingAcceptConnection(
                        Connection->Paths[0].Binding,
                        Connection,
                        &Info);

                    if (Connection->TlsSecrets != NULL &&
                        !Connection->State.HandleClosed &&
                        Connection->State.ExternalOwner)
                    {
                        //
                        // At this point, the connection was accepted by the listener,
                        // so now the ClientRandom can be copied.
                        //
                        QuicCryptoTlsReadClientRandom(
                            Buffer.Buffer,
                            Buffer.Length,
                            Connection->TlsSecrets);
                    }
                    return Status;
                }
            }

            CXPLAT_DBG_ASSERT(Crypto->TLS != NULL);
            if (Crypto->TLS == NULL)
            {
                //
                // The listener still hasn't given us the security config to initialize
                // TLS with yet.
                //
                goto Error;
            }

            QuicCryptoValidate(Crypto);

            Crypto->ResultFlags =
                CxPlatTlsProcessData(
                    Crypto->TLS,
                    CXPLAT_TLS_CRYPTO_DATA,
                    Buffer.Buffer,
                    &Buffer.Length,
                    &Crypto->TlsState);

            QuicCryptoProcessDataComplete(Crypto, Buffer.Length);

            return Status;

        Error:

            QuicRecvBufferDrain(&Crypto->RecvBuffer, 0);
            QuicCryptoValidate(Crypto);

            return Status;
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
                QuicPacketKeyFree(Crypto.TlsState.ReadKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL]);
                QuicPacketKeyFree(Crypto.TlsState.WriteKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL]);
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

    }
}
