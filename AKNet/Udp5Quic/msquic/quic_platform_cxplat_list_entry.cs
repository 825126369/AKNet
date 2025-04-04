using AKNet.Common;

namespace AKNet.Udp5Quic.Common
{
    internal abstract class CXPLAT_LIST_ENTRY
    {
        public CXPLAT_LIST_ENTRY Flink; //指向链表中当前节点的 [下一个] 节点。
        public CXPLAT_LIST_ENTRY Blink; //指向链表中当前节点的 [上一个] 节点。
    }

    internal class CXPLAT_LIST_ENTRY<T>: CXPLAT_LIST_ENTRY
    {
        public readonly T value; //当前节点的值
        public CXPLAT_LIST_ENTRY(T value)
        {
            this.value = value;
        }
    }

    internal static partial class MSQuicFunc
    {
        static T CXPLAT_CONTAINING_RECORD<T>(CXPLAT_LIST_ENTRY Entry)
        {
            return (Entry as CXPLAT_LIST_ENTRY<T>).value;
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

        static void CxPlatListInsertTail(CXPLAT_LIST_ENTRY ListHead, CXPLAT_LIST_ENTRY Entry)
        {
            QuicListEntryValidate(ListHead);
            CXPLAT_LIST_ENTRY Blink = ListHead.Blink;
            Entry.Flink = ListHead;
            Entry.Blink = Blink;
            Blink.Flink = Entry;
            ListHead.Blink = Entry;
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

        static void CxPlatListMoveItems(CXPLAT_LIST_ENTRY Source, CXPLAT_LIST_ENTRY Destination)
        {
            if (!CxPlatListIsEmpty(Source))
            {
                if (CxPlatListIsEmpty(Destination))
                {
                    Destination.Flink = Source.Flink;
                    Destination.Blink = Source.Blink;
                    Destination.Flink.Blink = Destination;
                    Destination.Blink.Flink = Destination;
                }
                else
                {
                    Source.Flink.Blink = Destination.Blink;
                    Destination.Blink.Flink = Source.Flink;
                    Source.Blink.Flink = Destination;
                    Destination.Blink = Source.Blink;
                }
                CxPlatListInitializeHead(Source);
            }
        }

    }
}
