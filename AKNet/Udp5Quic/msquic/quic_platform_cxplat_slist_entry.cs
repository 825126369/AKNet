namespace AKNet.Udp5Quic.Common
{
    internal class CXPLAT_SLIST_ENTRY
    {
        public CXPLAT_SLIST_ENTRY Next;
    }

    internal static partial class MSQuicFunc
    {
        public static void InitializeSListHead(CXPLAT_SLIST_ENTRY ListHead)
        {
            ListHead.Next = null;
        }

        public static void CxPlatListPushEntry(CXPLAT_SLIST_ENTRY ListHead, CXPLAT_SLIST_ENTRY Entry)
        {
            Entry.Next = ListHead.Next;
            ListHead.Next = Entry;
        }

        public static CXPLAT_SLIST_ENTRY CxPlatListPopEntry(CXPLAT_SLIST_ENTRY ListHead)
        {
            CXPLAT_SLIST_ENTRY FirstEntry = ListHead.Next;
            if (FirstEntry != null)
            {
                ListHead.Next = FirstEntry.Next;
            }
            return FirstEntry;
        }

    }
}
