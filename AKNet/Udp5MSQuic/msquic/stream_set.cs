using AKNet.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using static System.Net.WebRequestMethods;

namespace AKNet.Udp5MSQuic.Common
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
        public readonly Dictionary<ulong, QUIC_STREAM> StreamTable = new Dictionary<ulong, QUIC_STREAM>();
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

            CXPLAT_LIST_ENTRY Link = StreamSet.WaitingStreams.Next;
            while (Link != StreamSet.WaitingStreams)
            {
                QUIC_STREAM Stream = CXPLAT_CONTAINING_RECORD<QUIC_STREAM>(Link);
                Link = Link.Next;
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
                        Stream.SendWindow = (uint)Math.Min(Stream.MaxAllowedSendOffset, int.MaxValue);
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

        static QUIC_STREAM QuicStreamSetLookupStream(QUIC_STREAM_SET StreamSet, ulong ID)
        {
            if (StreamSet.StreamTable == null)
            {
                return null; // No streams have been created yet.
            }

            foreach (var Entry in StreamSet.StreamTable)
            {
                if (Entry.Key == ID)
                {
                    return Entry.Value;
                }
            }
            return null;
        }

        static QUIC_STREAM QuicStreamSetGetStreamForPeer(QUIC_STREAM_SET StreamSet, ulong StreamId, bool FrameIn0Rtt, bool CreateIfMissing, ref bool FatalError)
        {
            QUIC_CONNECTION Connection = QuicStreamSetGetConnection(StreamSet);
            FatalError = false;
            if (QuicConnIsClosed(Connection))
            {
                return null;
            }

            ulong StreamType = StreamId & STREAM_ID_MASK;
            int StreamCount = (int)(StreamId >> 2) + 1;
            QUIC_STREAM_TYPE_INFO Info = StreamSet.Types[StreamType];

            QUIC_STREAM_OPEN_FLAGS StreamFlags = 0;
            if (STREAM_ID_IS_UNI_DIR(StreamId))
            {
                StreamFlags |=  QUIC_STREAM_OPEN_FLAGS.QUIC_STREAM_OPEN_FLAG_UNIDIRECTIONAL;
            }
            if (FrameIn0Rtt)
            {
                StreamFlags |= QUIC_STREAM_OPEN_FLAGS.QUIC_STREAM_OPEN_FLAG_0_RTT;
            }

            if (StreamCount > Info.MaxTotalStreamCount)
            {
                QuicConnTransportError(Connection, QUIC_ERROR_STREAM_LIMIT_ERROR);
                FatalError = true;
                return null;
            }

            QUIC_STREAM Stream = null;
            if (StreamCount <= Info.TotalStreamCount)
            {
                Stream = QuicStreamSetLookupStream(StreamSet, StreamId);

            }
            else if (CreateIfMissing)
            {
                do
                {
                    ulong NewStreamId = StreamType + (ulong)(Info.TotalStreamCount << 2);
                    QUIC_STREAM_OPEN_FLAGS OpenFlags = QUIC_STREAM_OPEN_FLAGS.QUIC_STREAM_OPEN_FLAG_NONE;
                    if (STREAM_ID_IS_UNI_DIR(StreamId))
                    {
                        OpenFlags |= QUIC_STREAM_OPEN_FLAGS.QUIC_STREAM_OPEN_FLAG_UNIDIRECTIONAL;
                    }
                    if (FrameIn0Rtt)
                    {
                        OpenFlags |= QUIC_STREAM_OPEN_FLAGS.QUIC_STREAM_OPEN_FLAG_0_RTT;
                    }

                    ulong Status = QuicStreamInitialize(Connection, true, OpenFlags, Stream);
                    if (QUIC_FAILED(Status))
                    {
                        FatalError = true;
                        QuicConnTransportError(Connection, QUIC_ERROR_INTERNAL_ERROR);
                        goto Exit;
                    }

                    Stream.ID = NewStreamId;
                    Status = QuicStreamStart(Stream, QUIC_STREAM_START_FLAGS.QUIC_STREAM_START_FLAG_NONE, true);
                    if (QUIC_FAILED(Status))
                    {
                        FatalError = true;
                        QuicConnTransportError(Connection, QUIC_ERROR_INTERNAL_ERROR);
                        QuicStreamRelease(Stream,  QUIC_STREAM_REF.QUIC_STREAM_REF_APP);
                        Stream = null;
                        break;
                    }

                    if (!QuicStreamSetInsertStream(StreamSet, Stream))
                    {
                        FatalError = true;
                        QuicConnTransportError(Connection, QUIC_ERROR_INTERNAL_ERROR);
                        QuicStreamRelease(Stream,  QUIC_STREAM_REF.QUIC_STREAM_REF_APP);
                        Stream = null;
                        break;
                    }
                    Info.CurrentStreamCount++;
                    Info.TotalStreamCount++;

                    QuicStreamAddRef(Stream,  QUIC_STREAM_REF.QUIC_STREAM_REF_STREAM_SET);

                    QUIC_CONNECTION_EVENT Event = new QUIC_CONNECTION_EVENT();
                    Event.Type =  QUIC_CONNECTION_EVENT_TYPE.QUIC_CONNECTION_EVENT_PEER_STREAM_STARTED;
                    Event.PEER_STREAM_STARTED.Stream = Stream;
                    Event.PEER_STREAM_STARTED.Flags = StreamFlags;

                    Status = QuicConnIndicateEvent(Connection, Event);

                    if (QUIC_FAILED(Status))
                    {
                        QuicStreamClose(Stream);
                        Stream = null;
                    }
                    else if(Stream.Flags.HandleClosed)
                    {
                        Stream = null; // App accepted but immediately closed the stream.
                    }
                    else
                    {
                        NetLog.Assert(Stream.ClientCallbackHandler != null, "App MUST set callback handler!");
                        if (Event.PEER_STREAM_STARTED.Flags.HasFlag(QUIC_STREAM_OPEN_FLAGS.QUIC_STREAM_OPEN_FLAG_DELAY_ID_FC_UPDATES))
                        {
                            Stream.Flags.DelayIdFcUpdate = true;
                        }
                    }

                } while (Info.TotalStreamCount != StreamCount);
            }
            else
            {
                QuicConnTransportError(Connection, QUIC_ERROR_PROTOCOL_VIOLATION);
                FatalError = true;
            }

        Exit:
            if (Stream != null)
            {
                QuicStreamAddRef(Stream, QUIC_STREAM_REF.QUIC_STREAM_REF_LOOKUP);
            }
            return Stream;
        }

        static void QuicStreamSetUpdateMaxStreams(QUIC_STREAM_SET StreamSet, bool BidirectionalStreams, int MaxStreams)
        {
            QUIC_CONNECTION Connection = QuicStreamSetGetConnection(StreamSet);
            ulong Mask;
            QUIC_STREAM_TYPE_INFO Info;

            if (QuicConnIsServer(Connection))
            {
                if (BidirectionalStreams)
                {
                    Mask = STREAM_ID_FLAG_IS_SERVER | STREAM_ID_FLAG_IS_BI_DIR;
                }
                else
                {
                    Mask = STREAM_ID_FLAG_IS_SERVER | STREAM_ID_FLAG_IS_UNI_DIR;
                }
            }
            else
            {
                if (BidirectionalStreams)
                {
                    Mask = STREAM_ID_FLAG_IS_CLIENT | STREAM_ID_FLAG_IS_BI_DIR;
                }
                else
                {
                    Mask = STREAM_ID_FLAG_IS_CLIENT | STREAM_ID_FLAG_IS_UNI_DIR;
                }
            }

            Info = StreamSet.Types[Mask];

            if (MaxStreams > Info.MaxTotalStreamCount)
            {
                bool FlushSend = false;
                if (StreamSet.StreamTable != null)
                {
                    foreach(var v in  StreamSet.StreamTable)
                    {
                        QUIC_STREAM Stream = v.Value;

                        ulong Count = (Stream.ID >> 2) + 1;
                        if ((Stream.ID & STREAM_ID_MASK) == Mask && Count > (ulong)Info.MaxTotalStreamCount && Count <= (ulong)MaxStreams &&
                            QuicStreamRemoveOutFlowBlockedReason(Stream, QUIC_FLOW_BLOCKED_STREAM_ID_FLOW_CONTROL))
                        {
                            QuicStreamIndicatePeerAccepted(Stream);
                            FlushSend = true;
                        }
                    }
                }

                Info.MaxTotalStreamCount = MaxStreams;
                QuicStreamSetIndicateStreamsAvailable(StreamSet);

                if (FlushSend)
                {
                    QuicSendQueueFlush(Connection.Send,  QUIC_SEND_FLUSH_REASON.REASON_STREAM_ID_FLOW_CONTROL);
                }
            }
        }

        static void QuicStreamSetDrainClosedStreams(QUIC_STREAM_SET StreamSet)
        {
            while (!CxPlatListIsEmpty(StreamSet.ClosedStreams))
            {
                QUIC_STREAM Stream = CXPLAT_CONTAINING_RECORD<QUIC_STREAM>(CxPlatListRemoveHead(StreamSet.ClosedStreams));
                Stream.ClosedLink.Next = null;
                QuicStreamRelease(Stream,  QUIC_STREAM_REF.QUIC_STREAM_REF_STREAM_SET);
            }
        }

        static void QuicStreamSetUpdateMaxCount(QUIC_STREAM_SET StreamSet, uint Type, int Count)
        {
            QUIC_CONNECTION Connection = QuicStreamSetGetConnection(StreamSet);
            QUIC_STREAM_TYPE_INFO Info = StreamSet.Types[Type];

            if (!Connection.State.Started)
            {
                Info.MaxTotalStreamCount = Count;

            }
            else
            {
                if (Count >= Info.MaxCurrentStreamCount)
                {
                    Info.MaxTotalStreamCount += (Count - Info.MaxCurrentStreamCount);
                    QuicSendSetSendFlag(Connection.Send,
                        BoolOk(Type & STREAM_ID_FLAG_IS_UNI_DIR) ?
                            QUIC_CONN_SEND_FLAG_MAX_STREAMS_UNI :
                            QUIC_CONN_SEND_FLAG_MAX_STREAMS_BIDI);
                }
            }

            Info.MaxCurrentStreamCount = Count;
        }

    }

}
