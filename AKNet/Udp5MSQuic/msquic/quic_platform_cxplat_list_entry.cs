using AKNet.Common;

namespace AKNet.Udp5MSQuic.Common
{
    internal abstract class CXPLAT_LIST_ENTRY
    {
        public CXPLAT_LIST_ENTRY Next; //指向链表中当前节点的 [下一个] 节点。
        public CXPLAT_LIST_ENTRY Prev; //指向链表中当前节点的 [上一个] 节点。
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
            NetLog.Assert(Entry.Next.Prev == Entry && Entry.Prev.Next == Entry);
        }

        static bool CxPlatListIsEmpty(CXPLAT_LIST_ENTRY ListHead)
        {
            return ListHead.Next == ListHead;
        }

        public static void CxPlatListInitializeHead(CXPLAT_LIST_ENTRY ListHead)
        {
            ListHead.Next = ListHead.Prev = ListHead;
        }

        static void CxPlatListInsertHead(CXPLAT_LIST_ENTRY ListHead, CXPLAT_LIST_ENTRY Entry)
        {
            QuicListEntryValidate(ListHead);
            CXPLAT_LIST_ENTRY Next = ListHead.Next;
            Entry.Next = Next;
            Entry.Prev = ListHead;
            Next.Prev = Entry;
            ListHead.Next = Entry;
        }

        public static void CxPlatListInsertTail(CXPLAT_LIST_ENTRY ListHead, CXPLAT_LIST_ENTRY Entry)
        {
            QuicListEntryValidate(ListHead);
            CXPLAT_LIST_ENTRY Prev = ListHead.Prev;
            Entry.Next = ListHead;
            Entry.Prev = Prev;
            Prev.Next = Entry;
            ListHead.Prev = Entry;
        }

        static bool CxPlatListEntryRemove(CXPLAT_LIST_ENTRY Entry)
        {
            QuicListEntryValidate(Entry);
            CXPLAT_LIST_ENTRY Next = Entry.Next;
            CXPLAT_LIST_ENTRY Prev = Entry.Prev;
            Prev.Next = Next;
            Next.Prev = Prev;
            return Next == Prev;
        }

        public static CXPLAT_LIST_ENTRY CxPlatListRemoveHead(CXPLAT_LIST_ENTRY ListHead)
        {
            QuicListEntryValidate(ListHead);
            CXPLAT_LIST_ENTRY Entry = ListHead.Next;
            CXPLAT_LIST_ENTRY Next = Entry.Next;
            ListHead.Next = Next;
            Next.Prev = ListHead;
            return Entry;
        }

        static void CxPlatListMoveItems(CXPLAT_LIST_ENTRY Source, CXPLAT_LIST_ENTRY Destination)
        {
            if (!CxPlatListIsEmpty(Source))
            {
                if (CxPlatListIsEmpty(Destination))
                {
                    Destination.Next = Source.Next;
                    Destination.Prev = Source.Prev;
                    Destination.Next.Prev = Destination;
                    Destination.Prev.Next = Destination;
                }
                else
                {
                    Source.Next.Prev = Destination.Prev;
                    Destination.Prev.Next = Source.Next;
                    Source.Prev.Next = Destination;
                    Destination.Prev = Source.Prev;
                }
                CxPlatListInitializeHead(Source);
            }
        }

    }
}
