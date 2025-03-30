using AKNet.Udp5Quic.Common;

namespace AKNet.Udp5Quic.Common
{
    internal class CXPLAT_SLIST_ENTRY
    {
        public CXPLAT_SLIST_ENTRY Next;
    }

    internal static partial class MSQuicFunc
    {
        static void InitializeSListHead(CXPLAT_SLIST_ENTRY ListHead)
        {
            ListHead.Next = null;
        }

        static void CxPlatListPushEntry(CXPLAT_SLIST_ENTRY ListHead, CXPLAT_SLIST_ENTRY Entry)
        {
            Entry.Next = ListHead.Next;
            ListHead.Next = Entry;
        }
    }
}
