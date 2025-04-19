using AKNet.Common;
using System;
using System.Collections.Generic;

namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_STREAM_TYPE_INFO
    {
        public int MaxTotalStreamCount;
        public int TotalStreamCount;
        public int MaxCurrentStreamCount;
        public int CurrentStreamCount;
    }

    internal class QUIC_STREAM_SET
    {
        public readonly QUIC_STREAM_TYPE_INFO[] Types = new QUIC_STREAM_TYPE_INFO[MSQuicFunc.NUMBER_OF_STREAM_TYPES];
        public readonly Dictionary<uint, QUIC_STREAM> StreamTable = new Dictionary<uint, QUIC_STREAM>();
        public CXPLAT_LIST_ENTRY WaitingStreams;
        public CXPLAT_LIST_ENTRY ClosedStreams;
        public QUIC_CONNECTION mCONNECTION;
    }


    internal static partial class MSQuicFunc
    {
        static void QuicStreamSetInitialize(QUIC_STREAM_SET StreamSet)
        {
            CxPlatListInitializeHead(StreamSet.ClosedStreams);
            CxPlatListInitializeHead(StreamSet.WaitingStreams);
        }

        static ulong QuicStreamSetNewLocalStream(QUIC_STREAM_SET StreamSet, uint Type, bool FailOnBlocked, QUIC_STREAM Stream)
        {
            ulong Status = QUIC_STATUS_SUCCESS;
            QUIC_STREAM_TYPE_INFO Info = StreamSet.Types[Type];
            uint NewStreamId = (uint)(Type + (Info.TotalStreamCount << 2));
            bool NewStreamBlocked = Info.TotalStreamCount >= Info.MaxTotalStreamCount;

            if (FailOnBlocked && NewStreamBlocked)
            {
                if (Stream.Connection.State.PeerTransportParameterValid)
                {
                    QuicSendSetSendFlag(Stream.Connection.Send, STREAM_ID_IS_UNI_DIR(Type) ? QUIC_CONN_SEND_FLAG_UNI_STREAMS_BLOCKED : QUIC_CONN_SEND_FLAG_BIDI_STREAMS_BLOCKED);
                }
                Status = QUIC_STATUS_STREAM_LIMIT_REACHED;
                goto Exit;
            }

            Stream.ID = NewStreamId;
            if (!QuicStreamSetInsertStream(StreamSet, Stream))
            {
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                Stream.ID = uint.MaxValue;
                goto Exit;
            }

            if (NewStreamBlocked)
            {
                Stream.OutFlowBlockedReasons |= QUIC_FLOW_BLOCKED_STREAM_ID_FLOW_CONTROL;
                Stream.BlockedTimings.StreamIdFlowControl.LastStartTimeUs = CxPlatTime();
                if (Stream.Connection.State.PeerTransportParameterValid)
                {
                    QuicSendSetSendFlag(Stream.Connection.Send, STREAM_ID_IS_UNI_DIR(Stream.ID) ? QUIC_CONN_SEND_FLAG_UNI_STREAMS_BLOCKED : QUIC_CONN_SEND_FLAG_BIDI_STREAMS_BLOCKED);
                }
            }

            Info.CurrentStreamCount++;
            Info.TotalStreamCount++;
            QuicStreamAddRef(Stream, QUIC_STREAM_REF.QUIC_STREAM_REF_STREAM_SET);
        Exit:
            return Status;
        }

        static void QuicStreamSetReleaseStream(QUIC_STREAM_SET StreamSet, QUIC_STREAM Stream)
        {
            if (Stream.Flags.InStreamTable)
            {
                StreamSet.StreamTable.Remove(Stream.ID);
                Stream.Flags.InStreamTable = false;
            }
            else if (Stream.Flags.InWaitingList)
            {
                CxPlatListEntryRemove(Stream.WaitingLink);
                Stream.Flags.InWaitingList = false;
            }
            else
            {
                return;
            }

            CxPlatListInsertTail(StreamSet.ClosedStreams, Stream.ClosedLink);
            uint Flags = (uint)(Stream.ID & STREAM_ID_MASK);
            QUIC_STREAM_TYPE_INFO Info = StreamSet.Types[Flags];

            NetLog.Assert(Info.CurrentStreamCount != 0);
            Info.CurrentStreamCount--;

            if (BoolOk(Flags & STREAM_ID_FLAG_IS_SERVER) == QuicConnIsServer(Stream.Connection))
            {
                return;
            }

            if (Info.CurrentStreamCount < Info.MaxCurrentStreamCount)
            {
                Info.MaxTotalStreamCount++;
                QuicSendSetSendFlag(QuicStreamSetGetConnection(StreamSet).Send,
                        BoolOk(Flags & STREAM_ID_FLAG_IS_UNI_DIR) ? QUIC_CONN_SEND_FLAG_MAX_STREAMS_UNI : QUIC_CONN_SEND_FLAG_MAX_STREAMS_BIDI);
            }
        }

        static void QuicStreamSetShutdown(QUIC_STREAM_SET StreamSet)
        {
            if (StreamSet.StreamTable != null)
            {
                var Enumerator = StreamSet.StreamTable.GetEnumerator();
                while (Enumerator.MoveNext())
                {
                    var Entry = Enumerator.Current;
                    QUIC_STREAM Stream = Entry.Value;
                    QuicStreamShutdown(
                        Stream,
                        QUIC_STREAM_SHUTDOWN_FLAG_ABORT_SEND |
                        QUIC_STREAM_SHUTDOWN_FLAG_ABORT_RECEIVE |
                        QUIC_STREAM_SHUTDOWN_SILENT,
                        0);
                }
            }

            CXPLAT_LIST_ENTRY Link = StreamSet.WaitingStreams.Flink;
            while (Link != StreamSet.WaitingStreams)
            {
                QUIC_STREAM Stream = CXPLAT_CONTAINING_RECORD<QUIC_STREAM>(Link);
                Link = Link.Flink;
                QuicStreamShutdown(
                    Stream,
                    QUIC_STREAM_SHUTDOWN_FLAG_ABORT_SEND |
                    QUIC_STREAM_SHUTDOWN_FLAG_ABORT_RECEIVE |
                    QUIC_STREAM_SHUTDOWN_SILENT,
                    0);
            }
        }

        static void QuicStreamSetGetFlowControlSummary(QUIC_STREAM_SET StreamSet, ref long FcAvailable, ref long SendWindow)
        {
            FcAvailable = 0;
            SendWindow = 0;

            if (StreamSet.StreamTable != null)
            {
                var Enumerator = StreamSet.StreamTable.GetEnumerator();
                while (Enumerator.MoveNext())
                {
                    var Entry = Enumerator.Current;
                    QUIC_STREAM Stream = Entry.Value;

                    if ((long.MaxValue - FcAvailable) >= (Stream.MaxAllowedSendOffset - Stream.NextSendOffset))
                    {
                        FcAvailable += Stream.MaxAllowedSendOffset - Stream.NextSendOffset;
                    }
                    else
                    {
                        FcAvailable = long.MaxValue;
                    }

                    if ((long.MaxValue - SendWindow) >= Stream.SendWindow)
                    {
                        SendWindow += Stream.SendWindow;
                    }
                    else
                    {
                        SendWindow = long.MaxValue;
                    }
                }
            }
        }

        static bool QuicStreamSetInsertStream(QUIC_STREAM_SET StreamSet, QUIC_STREAM Stream)
        {
            Stream.Flags.InStreamTable = true;
            StreamSet.StreamTable.Add(Stream.ID, Stream);
            return true;
        }

        static void QuicStreamIndicatePeerAccepted(QUIC_STREAM Stream)
        {
            if (Stream.Flags.IndicatePeerAccepted)
            {
                QUIC_STREAM_EVENT Event = new QUIC_STREAM_EVENT();
                Event.Type = QUIC_STREAM_EVENT_TYPE.QUIC_STREAM_EVENT_PEER_ACCEPTED;
                QuicStreamIndicateEvent(Stream, Event);
            }
        }

        static void QuicStreamSetInitializeTransportParameters(QUIC_STREAM_SET StreamSet, int BidiStreamCount, int UnidiStreamCount, bool FlushIfUnblocked)
        {
            QUIC_CONNECTION Connection = QuicStreamSetGetConnection(StreamSet);
            uint Type = QuicConnIsServer(Connection) ? STREAM_ID_FLAG_IS_SERVER : STREAM_ID_FLAG_IS_CLIENT;

            bool UpdateAvailableStreams = false;
            bool MightBeUnblocked = false;

            if (BidiStreamCount != 0)
            {
                StreamSet.Types[Type | STREAM_ID_FLAG_IS_BI_DIR].MaxTotalStreamCount = BidiStreamCount;
                UpdateAvailableStreams = true;
            }

            if (UnidiStreamCount != 0)
            {
                StreamSet.Types[Type | STREAM_ID_FLAG_IS_UNI_DIR].MaxTotalStreamCount = UnidiStreamCount;
                UpdateAvailableStreams = true;
            }

            if (StreamSet.StreamTable != null)
            {
                var Enumerator = StreamSet.StreamTable.GetEnumerator();
                while (Enumerator.MoveNext())
                {
                    var Entry = Enumerator.Current;
                    QUIC_STREAM Stream = Entry.Value;

                    byte FlowBlockedFlagsToRemove = 0;

                    ulong StreamType = Stream.ID & STREAM_ID_MASK;
                    int StreamCount = (int)((Stream.ID >> 2) + 1);
                    QUIC_STREAM_TYPE_INFO Info = Stream.Connection.Streams.Types[StreamType];
                    if (Info.MaxTotalStreamCount >= StreamCount && BoolOk(Stream.OutFlowBlockedReasons & QUIC_FLOW_BLOCKED_STREAM_ID_FLOW_CONTROL))
                    {
                        FlowBlockedFlagsToRemove |= QUIC_FLOW_BLOCKED_STREAM_ID_FLOW_CONTROL;
                        QuicStreamIndicatePeerAccepted(Stream);
                    }
                    else
                    {
                        QuicSendSetSendFlag(Stream.Connection.Send, STREAM_ID_IS_UNI_DIR(Stream.ID) ? QUIC_CONN_SEND_FLAG_UNI_STREAMS_BLOCKED : QUIC_CONN_SEND_FLAG_BIDI_STREAMS_BLOCKED);
                    }

                    int NewMaxAllowedSendOffset = QuicStreamGetInitialMaxDataFromTP(Stream.ID, QuicConnIsServer(Connection), Connection.PeerTransportParams);

                    if (Stream.MaxAllowedSendOffset < NewMaxAllowedSendOffset)
                    {
                        Stream.MaxAllowedSendOffset = NewMaxAllowedSendOffset;
                        FlowBlockedFlagsToRemove |= QUIC_FLOW_BLOCKED_STREAM_FLOW_CONTROL;
                        Stream.SendWindow = Math.Min(Stream.MaxAllowedSendOffset, int.MaxValue);
                    }

                    if (BoolOk(FlowBlockedFlagsToRemove))
                    {
                        QuicStreamRemoveOutFlowBlockedReason(Stream, FlowBlockedFlagsToRemove);
                        QuicStreamSendDumpState(Stream);
                        MightBeUnblocked = true;
                    }
                }
            }

            if (UpdateAvailableStreams)
            {
                QuicStreamSetIndicateStreamsAvailable(StreamSet);
            }

            if (MightBeUnblocked && FlushIfUnblocked)
            {
                QuicSendQueueFlush(Connection.Send, QUIC_SEND_FLUSH_REASON.REASON_TRANSPORT_PARAMETERS);
            }
        }

        static void QuicStreamSetIndicateStreamsAvailable(QUIC_STREAM_SET StreamSet)
        {
            QUIC_CONNECTION Connection = QuicStreamSetGetConnection(StreamSet);
            uint Type = QuicConnIsServer(Connection) ? STREAM_ID_FLAG_IS_SERVER : STREAM_ID_FLAG_IS_CLIENT;

            QUIC_CONNECTION_EVENT Event = new QUIC_CONNECTION_EVENT();
            Event.Type = QUIC_CONNECTION_EVENT_TYPE.QUIC_CONNECTION_EVENT_STREAMS_AVAILABLE;
            Event.STREAMS_AVAILABLE.BidirectionalCount = QuicStreamSetGetCountAvailable(StreamSet, Type | STREAM_ID_FLAG_IS_BI_DIR);
            Event.STREAMS_AVAILABLE.UnidirectionalCount = QuicStreamSetGetCountAvailable(StreamSet, Type | STREAM_ID_FLAG_IS_UNI_DIR);
            QuicConnIndicateEvent(Connection, Event);
        }

        static int QuicStreamSetGetCountAvailable(QUIC_STREAM_SET StreamSet, uint Type)
        {
            QUIC_STREAM_TYPE_INFO Info = StreamSet.Types[Type];
            if (Info.TotalStreamCount >= Info.MaxTotalStreamCount)
            {
                return 0;
            }

            int Count = Info.MaxTotalStreamCount - Info.TotalStreamCount;
            return (Count > ushort.MaxValue) ? ushort.MaxValue : Count;
        }

    }

}
