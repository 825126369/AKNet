using AKNet.Common;
using System.IO;

namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_STREAM_TYPE_INFO
    {
        public long MaxTotalStreamCount;
        public long TotalStreamCount;

        public int MaxCurrentStreamCount;
        public int CurrentStreamCount;
    }

    internal class QUIC_STREAM_SET
    {
        public readonly QUIC_STREAM_TYPE_INFO[] Types = new QUIC_STREAM_TYPE_INFO[MSQuicFunc.NUMBER_OF_STREAM_TYPES];
        public CXPLAT_HASHTABLE StreamTable;
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

        static void QuicStreamSetReleaseStream(QUIC_STREAM_SET StreamSet, QUIC_STREAM Stream)
        {
            if (Stream.Flags.InStreamTable)
            {
                CxPlatHashtableRemove(StreamSet.StreamTable, Stream.TableEntry, null);
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

            ulong Flags = (Stream.ID & STREAM_ID_MASK);
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
                CXPLAT_HASHTABLE_ENUMERATOR Enumerator = new CXPLAT_HASHTABLE_ENUMERATOR();
                CXPLAT_HASHTABLE_ENTRY Entry;
                CxPlatHashtableEnumerateBegin(StreamSet.StreamTable, Enumerator);
                while ((Entry = CxPlatHashtableEnumerateNext(StreamSet.StreamTable, Enumerator)) != null)
                {
                    QUIC_STREAM Stream = CXPLAT_CONTAINING_RECORD(Entry);
                    QuicStreamShutdown(
                        Stream,
                        QUIC_STREAM_SHUTDOWN_FLAG_ABORT_SEND |
                        QUIC_STREAM_SHUTDOWN_FLAG_ABORT_RECEIVE |
                        QUIC_STREAM_SHUTDOWN_SILENT,
                        0);
                }
                CxPlatHashtableEnumerateEnd(StreamSet.StreamTable, Enumerator);
            }

            CXPLAT_LIST_ENTRY Link = StreamSet.WaitingStreams.Flink;
            while (Link != StreamSet.WaitingStreams)
            {
                QUIC_STREAM Stream = CXPLAT_CONTAINING_RECORD(Link);
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
                CXPLAT_HASHTABLE_ENUMERATOR Enumerator = new CXPLAT_HASHTABLE_ENUMERATOR();
                CXPLAT_HASHTABLE_ENTRY Entry;
                CxPlatHashtableEnumerateBegin(StreamSet.StreamTable, Enumerator);
                while ((Entry = CxPlatHashtableEnumerateNext(StreamSet.StreamTable, Enumerator)) != null)
                {
                    QUIC_STREAM Stream = CXPLAT_CONTAINING_RECORD(Entry);
                    
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
                CxPlatHashtableEnumerateEnd(StreamSet->StreamTable, &Enumerator);
            }
        }
    }

}
