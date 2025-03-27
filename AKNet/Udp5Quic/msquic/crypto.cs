namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_CRYPTO
    {
        public bool Initialized;
        public bool InRecovery;
        public bool CertValidationPending;
        public CXPLAT_TLS TLS;

        CXPLAT_TLS_PROCESS_STATE TlsState;

        //
        // Result flags from the last Tls process call.
        //
        CXPLAT_TLS_RESULT_FLAGS ResultFlags;

        //
        // The length of bytes that have been sent at least once.
        //
        uint32_t MaxSentLength;

        //
        // The smallest offset for unacknowledged send data. This variable is
        // similar to RFC793 SND.UNA.
        //
        uint32_t UnAckedOffset;

        //
        // The next offset we will start sending at.
        //
        uint32_t NextSendOffset;

        //
        // Recovery window
        //
        uint32_t RecoveryNextOffset;
        uint32_t RecoveryEndOffset;
#define RECOV_WINDOW_OPEN(S) ((S)->RecoveryNextOffset < (S)->RecoveryEndOffset)

        //
        // The ACK ranges greater than 'UnAckedOffset', with holes between them.
        //
        QUIC_RANGE SparseAckRanges;

        //
        // Recv State
        //

        //
        // The total amount of data consumed by TLS.
        //
        uint32_t RecvTotalConsumed;

        //
        // Indicates Resumption ticket validation is under validation asynchronously
        //
        BOOLEAN TicketValidationPending : 1;
        BOOLEAN TicketValidationRejecting : 1;
        uint32_t PendingValidationBufferLength;

        //
        // The offset the current receive encryption level starts.
        //
        uint32_t RecvEncryptLevelStartOffset;

        //
        // The structure for tracking received buffers.
        //
        QUIC_RECV_BUFFER RecvBuffer;

        //
        // Resumption ticket to send to server.
        //
        uint8_t* ResumptionTicket;
        uint32_t ResumptionTicketLength;

    }
}
