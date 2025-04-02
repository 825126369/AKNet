using AKNet.Common;
using System;

namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_SEND_BUFFER
    {
        public long PostedBytes;
        public long BufferedBytes;
        public long IdealBytes;
    }

    internal static partial class MSQuicFunc
    {
        static void QuicSendBufferInitialize(QUIC_SEND_BUFFER SendBuffer)
        {
            SendBuffer.IdealBytes = QUIC_DEFAULT_IDEAL_SEND_BUFFER_SIZE;
        }

        static bool QuicSendBufferHasSpace(QUIC_SEND_BUFFER SendBuffer)
        {
            return SendBuffer.BufferedBytes < SendBuffer.IdealBytes;
        }

        static void QuicSendBufferFill(QUIC_CONNECTION Connection)
        {
            QUIC_SEND_REQUEST Req;
            CXPLAT_LIST_ENTRY Entry;

            NetLog.Assert(Connection.Settings.SendBufferingEnabled);

            Entry = Connection.Send.SendStreams.Flink;
            while (QuicSendBufferHasSpace(Connection.SendBuffer) && Entry != Connection.Send.SendStreams)
            {
                QUIC_STREAM Stream = CXPLAT_CONTAINING_RECORD(Entry);
                Entry = Entry.Flink;
                Req = Stream.SendBufferBookmark;
                while (Req != null && QuicSendBufferHasSpace(Connection.SendBuffer))
                {
                    if (QUIC_FAILED(QuicStreamSendBufferRequest(Stream, Req)))
                    {
                        return;
                    }
                    Req = Req.Next;
                }

            }
        }

        static void QuicSendBufferConnectionAdjust(QUIC_CONNECTION Connection)
        {
            if (Connection.SendBuffer.IdealBytes == QUIC_MAX_IDEAL_SEND_BUFFER_SIZE || Connection.Streams.StreamTable == null)
            {
                return;
            }

            long NewIdealBytes = QuicGetNextIdealBytes(QuicCongestionControlGetBytesInFlightMax(Connection.CongestionControl));
            if (NewIdealBytes > Connection.SendBuffer.IdealBytes)
            {
                Connection.SendBuffer.IdealBytes = NewIdealBytes;

                CXPLAT_HASHTABLE_ENUMERATOR Enumerator;
                CXPLAT_HASHTABLE_ENTRY Entry;
                CxPlatHashtableEnumerateBegin(Connection.Streams.StreamTable, Enumerator);
                while ((Entry = CxPlatHashtableEnumerateNext(Connection.Streams.StreamTable, Enumerator)) != null)
                {
                    QUIC_STREAM Stream = CXPLAT_CONTAINING_RECORD(Entry, QUIC_STREAM, TableEntry);
                    if (Stream.Flags.SendEnabled)
                    {
                        QuicSendBufferStreamAdjust(Stream);
                    }
                }
                CxPlatHashtableEnumerateEnd(Connection.Streams.StreamTable, Enumerator);

                if (Connection.Settings.SendBufferingEnabled)
                {
                    QuicSendBufferFill(Connection);
                }
            }
        }

    }
}
