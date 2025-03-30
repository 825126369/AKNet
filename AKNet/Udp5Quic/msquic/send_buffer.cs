using AKNet.Common;

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
    }
}
