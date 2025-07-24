using AKNet.Common;
using AKNet.Udp4LinuxTcp.Common;
using System;

namespace AKNet.Udp2MSQuic.Common
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
        static T CXPLAT_CONTAINING_RECORD<T>(CXPLAT_LIST_ENTRY Entry) where T:class
        {
            CXPLAT_LIST_ENTRY<T> t = Entry as CXPLAT_LIST_ENTRY<T>;
            if (t != null)
            {
                return t.value;
            }
            return null;
        }

        static void EntryInQueueStateOk(CXPLAT_LIST_ENTRY Entry)
        {
            NetLog.Assert(Entry.Next.Prev == Entry && Entry.Prev.Next == Entry);
        }

        static void EntryNotInQueueStateOk(CXPLAT_LIST_ENTRY Entry)
        {
            NetLog.Assert(Entry.Prev == null && Entry.Next == null);
        }

        //判断队列数量为0
        static bool CxPlatListIsEmpty(CXPLAT_LIST_ENTRY Queue)
        {
            return Queue.Next == Queue;
        }

        public static void CxPlatListInitializeHead(CXPLAT_LIST_ENTRY Queue)
        {
            Queue.Next = Queue.Prev = Queue;
        }

        static void CxPlatListInsertHead(CXPLAT_LIST_ENTRY Queue, CXPLAT_LIST_ENTRY Entry)
        {
            EntryNotInQueueStateOk(Entry);
            EntryInQueueStateOk(Queue);
            CXPLAT_LIST_ENTRY Next = Queue.Next;
            Entry.Next = Next;
            Entry.Prev = Queue;
            Next.Prev = Entry;
            Queue.Next = Entry;
        }

        public static void CxPlatListInsertTail(CXPLAT_LIST_ENTRY Queue, CXPLAT_LIST_ENTRY Entry)
        {
            EntryNotInQueueStateOk(Entry);
            EntryInQueueStateOk(Queue);

            CXPLAT_LIST_ENTRY Prev = Queue.Prev;
            Entry.Next = Queue;
            Entry.Prev = Prev;
            Prev.Next = Entry;
            Queue.Prev = Entry;
        }

        static void CxPlatListEntryRemove(CXPLAT_LIST_ENTRY Entry)
        {
            EntryInQueueStateOk(Entry);
            
            CXPLAT_LIST_ENTRY Next = Entry.Next;
            CXPLAT_LIST_ENTRY Prev = Entry.Prev;
            Prev.Next = Next;
            Next.Prev = Prev;

            Entry.Next = null;
            Entry.Prev = null;
        }

        public static CXPLAT_LIST_ENTRY CxPlatListRemoveHead(CXPLAT_LIST_ENTRY ListHead)
        {
            EntryInQueueStateOk(ListHead);
            if (!CxPlatListIsEmpty(ListHead))
            {
                CXPLAT_LIST_ENTRY Entry = ListHead.Next; // cppcheck-suppress shadowFunction
                CXPLAT_LIST_ENTRY Next = Entry.Next;
                ListHead.Next = Next;
                Next.Prev = ListHead;

                Entry.Next = null;
                Entry.Prev = null;
                return Entry;
            }
            else
            {
                return null;
            }
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
