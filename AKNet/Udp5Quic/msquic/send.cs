namespace AKNet.Udp5Quic.Common
{



    internal class QUIC_FLOW_BLOCKED_TIMING_TRACKER
    {
        public ulong CumulativeTimeUs;
        public ulong LastStartTimeUs;
    }

    internal struct QUIC_SEND
    {
        public bool FlushOperationPending;
        public bool DelayedAckTimerActive;
        public bool LastFlushTimeValid;
        public bool TailLossProbeNeeded;
        public bool Uninitialized;
        public ulong NextPacketNumber;
        public ulong LastFlushTime;
        public ulong NumPacketsSentWithEct;
        public ulong MaxData;
        public ulong PeerMaxData;
        public ulong OrderedStreamBytesReceived;
        public ulong OrderedStreamBytesSent;
        public ulong OrderedStreamBytesDeliveredAccumulator;
        public uint SendFlags;
        public CXPLAT_LIST_ENTRY SendStreams;
        public byte[] InitialToken;
        public ushort InitialTokenLength;

        public QUIC_CONNECTION mConnection;
    }

    internal static partial class MSQuicFunc
    {
        static bool QuicSendSetSendFlag(QUIC_SEND Send, uint SendFlags)
        {
            QUIC_CONNECTION Connection = QuicSendGetConnection(Send);
            bool IsCloseFrame = BoolOk(SendFlags & (QUIC_CONN_SEND_FLAG_CONNECTION_CLOSE | QUIC_CONN_SEND_FLAG_APPLICATION_CLOSE));
            bool CanSetFlag = !QuicConnIsClosed(Connection) || IsCloseFrame;

            if (BoolOk(SendFlags & QUIC_CONN_SEND_FLAG_ACK) && Send.DelayedAckTimerActive)
            {
                QuicConnTimerCancel(Connection, QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_ACK_DELAY);
                Send.DelayedAckTimerActive = false;
            }

            if (CanSetFlag && (Send->SendFlags & SendFlags) != SendFlags)
            {
                QuicTraceLogConnVerbose(
                    ScheduleSendFlags,
                    Connection,
                    "Scheduling flags 0x%x to 0x%x",
                    SendFlags,
                    Send->SendFlags);
                Send->SendFlags |= SendFlags;
                QuicSendQueueFlush(Send, REASON_CONNECTION_FLAGS);
            }

            if (IsCloseFrame)
            {
                QuicSendClear(Send);
            }

            QuicSendValidate(Send);

            return CanSetFlag;
        }
    }
}
