using System;
using static AKNet.Udp5Quic.Common.QUIC_CONN_STATS;

namespace AKNet.Udp5Quic.Common
{
    internal enum QUIC_SEND_FLUSH_REASON
    {
        REASON_CONNECTION_FLAGS,
        REASON_STREAM_FLAGS,
        REASON_PROBE,
        REASON_LOSS,
        REASON_ACK,
        REASON_TRANSPORT_PARAMETERS,
        REASON_CONGESTION_CONTROL,
        REASON_CONNECTION_FLOW_CONTROL,
        REASON_NEW_KEY,
        REASON_STREAM_FLOW_CONTROL,
        REASON_STREAM_ID_FLOW_CONTROL,
        REASON_AMP_PROTECTION,
        REASON_SCHEDULING,
        REASON_ROUTE_COMPLETION,
    }

    internal class QUIC_FLOW_BLOCKED_TIMING_TRACKER
    {
        public ulong CumulativeTimeUs;
        public ulong LastStartTimeUs;
    }

    internal class QUIC_SEND
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
        static void QuicSendInitialize(QUIC_SEND Send, QUIC_SETTINGS_INTERNAL Settings)
        {
            CxPlatListInitializeHead(Send.SendStreams);
            Send.MaxData = Settings.ConnFlowControlWindow;
        }

        static bool QuicSendCanSendFlagsNow(QUIC_SEND Send)
        {
            QUIC_CONNECTION Connection = QuicSendGetConnection(Send);
            if (Connection.Crypto.TlsState.WriteKey < QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT)
            {
                if (Connection.Crypto.TlsState.WriteKeys[QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_0_RTT] != null && CxPlatListIsEmpty(Send.SendStreams))
                {
                    return true;
                }

                if ((!Connection.State.Started && QuicConnIsClient(Connection)) || !(Send.SendFlags &  QUIC_CONN_SEND_FLAG_ALLOWED_HANDSHAKE))
                {
                    return false;
                }
            }
            return true;
        }

        static void QuicSendQueueFlush(QUIC_SEND Send, QUIC_SEND_FLUSH_REASON Reason)
        {
            QUIC_CONNECTION Connection = QuicSendGetConnection(Send);
            if (!Send.FlushOperationPending && QuicSendCanSendFlagsNow(Send))
            {
                QUIC_OPERATION Oper;
                if ((Oper = QuicOperationAlloc(Connection.Worker,  QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_FLUSH_SEND)) != null)
                {
                    Send.FlushOperationPending = true;
                    QuicConnQueueOper(Connection, Oper);
                }
            }
        }

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

        static void QuicSendQueueFlushForStream(QUIC_SEND Send,QUIC_STREAM Stream, bool DelaySend)
        {
            if (Stream.SendLink.Flink == null)
            {
                CXPLAT_LIST_ENTRY Entry = Send.SendStreams.Blink;
                while (Entry != Send.SendStreams)
                {
                    if (Stream.SendPriority <= ((CXPLAT_LIST_ENTRY_QUIC_STREAM)Entry).mContain.SendPriority)
                    {
                        break;
                    }
                    Entry = Entry.Blink;
                }
                CxPlatListInsertHead(Entry, Stream.SendLink);
                QuicStreamAddRef(Stream, QUIC_STREAM_REF.QUIC_STREAM_REF_SEND);
            }

            if (DelaySend)
            {
                Stream.Flags.SendDelayed = true;

            }
            else if (Stream.Connection.State.Started)
            {
                Stream.Flags.SendDelayed = false;
                QuicSendQueueFlush(Send, QUIC_SEND_FLUSH_REASON.REASON_STREAM_FLAGS);
            }
        }

        static bool QuicSendSetStreamSendFlag(QUIC_SEND Send,QUIC_STREAM Stream, uint SendFlags, bool DelaySend)
        {
            QUIC_CONNECTION Connection = QuicSendGetConnection(Send);
            if (QuicConnIsClosed(Connection))
            {
                return false;
            }

            if (Stream.Flags.LocalCloseAcked)
            {
                SendFlags &=
                    ~(QUIC_STREAM_SEND_FLAG_SEND_ABORT |
                      QUIC_STREAM_SEND_FLAG_RELIABLE_ABORT |
                      QUIC_STREAM_SEND_FLAG_DATA_BLOCKED |
                      QUIC_STREAM_SEND_FLAG_DATA |
                      QUIC_STREAM_SEND_FLAG_OPEN |
                      QUIC_STREAM_SEND_FLAG_FIN);
            }
            else if (Stream.Flags.LocalCloseReset)
            {
                SendFlags &=
                    ~(QUIC_STREAM_SEND_FLAG_DATA_BLOCKED |
                      QUIC_STREAM_SEND_FLAG_DATA |
                      QUIC_STREAM_SEND_FLAG_OPEN |
                      QUIC_STREAM_SEND_FLAG_FIN);
            }
            if (Stream.Flags.RemoteCloseAcked)
            {
                SendFlags &= ~(QUIC_STREAM_SEND_FLAG_RECV_ABORT | QUIC_STREAM_SEND_FLAG_MAX_DATA);
            }
            else if (Stream.Flags.RemoteCloseFin || Stream.Flags.RemoteCloseReset)
            {
                SendFlags &= ~QUIC_STREAM_SEND_FLAG_MAX_DATA;
            }

            if ((Stream.SendFlags | SendFlags) != Stream.SendFlags || (Stream.Flags.SendDelayed && BoolOk(SendFlags & QUIC_STREAM_SEND_FLAG_DATA)))
            {
                if (Stream.Flags.Started)
                {
                    QuicSendQueueFlushForStream(Send, Stream, DelaySend);
                }
                Stream.SendFlags |= SendFlags;
            }

            return SendFlags != 0;
        }
    }
}
