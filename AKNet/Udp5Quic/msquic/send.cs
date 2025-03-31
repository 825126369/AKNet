using System;

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
        public long CumulativeTimeUs;
        public long LastStartTimeUs;
    }

    internal class QUIC_SEND
    {
        public bool FlushOperationPending;
        public bool DelayedAckTimerActive;
        public bool LastFlushTimeValid;
        public bool TailLossProbeNeeded;
        public bool Uninitialized;
        public long NextPacketNumber;
        public long LastFlushTime;
        public long NumPacketsSentWithEct;
        public long MaxData;
        public long PeerMaxData;
        public long OrderedStreamBytesReceived;
        public long OrderedStreamBytesSent;
        public long OrderedStreamBytesDeliveredAccumulator;
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

        static void QuicSendClearStreamSendFlag(QUIC_SEND Send,QUIC_STREAM Stream, uint SendFlags)
        {
            if (BoolOk(Stream.SendFlags & SendFlags))
            {
                Stream.SendFlags &= ~SendFlags;

                if (Stream.SendFlags == 0 && Stream.SendLink.Flink != null)
                {
                    CxPlatListEntryRemove(Stream.SendLink);
                    Stream.SendLink.Flink = null;
                    QuicStreamRelease(Stream,  QUIC_STREAM_REF.QUIC_STREAM_REF_SEND);
                }
            }
        }

        static void QuicSendClearSendFlag(QUIC_SEND Send, uint SendFlags)
        {
            if (BoolOk(Send.SendFlags & SendFlags))
            {
                Send.SendFlags &= ~SendFlags;
            }

            QuicSendValidate(Send);
        }

        static void QuicSendValidate(QUIC_SEND Send)
        {
            if (Send.Uninitialized)
            {
                return;
            }

            QUIC_CONNECTION Connection = QuicSendGetConnection(Send);
            bool HasAckElicitingPacketsToAcknowledge = false;
            for (int i = 0; i < (int)QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_COUNT; ++i)
            {
                if (Connection.Packets[i] != null)
                {
                    if (Connection.Packets[i].AckTracker.AckElicitingPacketsToAcknowledge)
                    {
                        HasAckElicitingPacketsToAcknowledge = true;
                        break;
                    }
                }
            }

            if (Send->SendFlags & QUIC_CONN_SEND_FLAG_ACK)
            {
                CXPLAT_DBG_ASSERT(!Send->DelayedAckTimerActive);
                CXPLAT_DBG_ASSERT(HasAckElicitingPacketsToAcknowledge);
            }
            else if (Send->DelayedAckTimerActive)
            {
                CXPLAT_DBG_ASSERT(HasAckElicitingPacketsToAcknowledge);
            }
            else if (!Connection->State.ClosedLocally && !Connection->State.ClosedRemotely)
            {
                CXPLAT_DBG_ASSERT(!HasAckElicitingPacketsToAcknowledge);
            }
        }

    }
}
