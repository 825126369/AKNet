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

    }
}
