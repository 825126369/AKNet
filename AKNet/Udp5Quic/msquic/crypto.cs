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
        public bool RECOV_WINDOW_OPEN()
        {
            return RecoveryNextOffset < RecoveryEndOffset;
        }

        public QUIC_RANGE SparseAckRanges;
        public int RecvTotalConsumed;
        public bool TicketValidationPending;
        public bool TicketValidationRejecting;
        public int PendingValidationBufferLength;
        public int RecvEncryptLevelStartOffset;
        public QUIC_RECV_BUFFER RecvBuffer;
        public byte[] ResumptionTicket;
        public int ResumptionTicketLength;
    }
}
