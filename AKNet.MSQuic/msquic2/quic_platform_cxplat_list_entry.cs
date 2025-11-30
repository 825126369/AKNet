/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:19
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;

namespace MSQuic2
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
            CXPLAT_LIST_ENTRY<T> t = Entry as CXPLAT_LIST_ENTRY<T>;
            return t.value;
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
        public static bool CxPlatListIsEmpty(CXPLAT_LIST_ENTRY Queue)
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

        //这种适合中间元素插入
        public static void CxPlatListInsertMiddle(CXPLAT_LIST_ENTRY Queue, CXPLAT_LIST_ENTRY EntryInQueue, CXPLAT_LIST_ENTRY Entry)
        {
            EntryNotInQueueStateOk(Entry);
            EntryInQueueStateOk(Queue);
            EntryInQueueStateOk(EntryInQueue);

            Entry.Next = EntryInQueue.Next;
            Entry.Prev = EntryInQueue;
            EntryInQueue.Next.Prev = Entry;
            EntryInQueue.Next = Entry;
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
                ListHead.Next = Entry.Next;
                Entry.Next.Prev = ListHead;

                Entry.Next = null;
                Entry.Prev = null;
                return Entry;
            }
            else
            {
                return null;
            }
        }

        //从一个队列，移动到另一个队列里
        static void CxPlatListMoveItems(CXPLAT_LIST_ENTRY Source, CXPLAT_LIST_ENTRY Destination)
        {
            EntryInQueueStateOk(Source);
            EntryInQueueStateOk(Destination);

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
                    CXPLAT_LIST_ENTRY AddEntryHead = Source.Next;
                    CXPLAT_LIST_ENTRY AddEntryTail = Source.Prev;

                    Destination.Prev.Next = AddEntryHead;
                    AddEntryHead.Prev = Destination.Prev;
                    Destination.Prev = AddEntryTail;
                    AddEntryTail.Next = Destination;
                }
                CxPlatListInitializeHead(Source);
            }
        }

        static int CxPlatListCount(CXPLAT_LIST_ENTRY ListHead)
        {
            int nCount = 0;
            CXPLAT_LIST_ENTRY mNode = ListHead.Next;
            while (mNode != ListHead)
            {
                nCount++;
                mNode = mNode.Next;
            }
            return nCount;
        }

    }
}
