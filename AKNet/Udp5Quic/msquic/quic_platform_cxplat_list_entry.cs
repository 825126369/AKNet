using AKNet.Common;

namespace AKNet.Udp5Quic.Common
{
    internal class CXPLAT_LIST_ENTRY
    {
        public CXPLAT_LIST_ENTRY Flink;
        public CXPLAT_LIST_ENTRY Blink;
    }

    internal class CXPLAT_LIST_ENTRY_QUIC_CONNECTION : CXPLAT_LIST_ENTRY
    {
        public QUIC_CONNECTION mQUIC_CONNECTION;

        public CXPLAT_LIST_ENTRY_QUIC_CONNECTION(QUIC_CONNECTION quicConnection)
        {
            mQUIC_CONNECTION = quicConnection;
        }

        public CXPLAT_LIST_ENTRY_QUIC_CONNECTION()
        {

        }
    }

    internal class CXPLAT_LIST_ENTRY_QUIC_RECV_BUFFER : CXPLAT_LIST_ENTRY
    {
        public QUIC_RECV_BUFFER mQUIC_RECV_BUFFER;
        public CXPLAT_LIST_ENTRY_QUIC_RECV_BUFFER(QUIC_RECV_BUFFER quicRecvBuffer)
        {
            mQUIC_RECV_BUFFER = quicRecvBuffer;
        }
        public CXPLAT_LIST_ENTRY_QUIC_RECV_BUFFER()
        {
        }
    }

    internal static partial class MSQuicFunc
    {
        static QUIC_CONNECTION CXPLAT_CONTAINING_RECORD_QUIC_CONNECTION(CXPLAT_LIST_ENTRY Entry)
        {
            return (Entry as CXPLAT_LIST_ENTRY_QUIC_CONNECTION).mQUIC_CONNECTION;
        }

        static void QuicListEntryValidate(CXPLAT_LIST_ENTRY Entry)
        {
            NetLog.Assert(Entry.Flink.Blink == Entry && Entry.Blink.Flink == Entry);
        }

        static bool CxPlatListIsEmpty(CXPLAT_LIST_ENTRY ListHead)
        {
            return ListHead.Flink == ListHead;
        }

        static void CxPlatListInitializeHead(CXPLAT_LIST_ENTRY ListHead)
        {
            ListHead.Flink = ListHead.Blink = ListHead;
        }

        static void CxPlatListInsertHead(CXPLAT_LIST_ENTRY ListHead, CXPLAT_LIST_ENTRY Entry)
        {
            QuicListEntryValidate(ListHead);
            CXPLAT_LIST_ENTRY Flink = ListHead.Flink;
            Entry.Flink = Flink;
            Entry.Blink = ListHead;
            Flink.Blink = Entry;
            ListHead.Flink = Entry;
        }
        
        static bool CxPlatListEntryRemove(CXPLAT_LIST_ENTRY Entry)
        {
            QuicListEntryValidate(Entry);
            CXPLAT_LIST_ENTRY Flink = Entry.Flink;
            CXPLAT_LIST_ENTRY Blink = Entry.Blink;
            Blink.Flink = Flink;
            Flink.Blink = Blink;
            return Flink == Blink;
        }

        static CXPLAT_LIST_ENTRY CxPlatListRemoveHead(CXPLAT_LIST_ENTRY ListHead)
        {
            QuicListEntryValidate(ListHead);
            CXPLAT_LIST_ENTRY Entry = ListHead.Flink;
            CXPLAT_LIST_ENTRY Flink = Entry.Flink;
            ListHead.Flink = Flink;
            Flink.Blink = ListHead;
            return Entry;
        }

    }
}
