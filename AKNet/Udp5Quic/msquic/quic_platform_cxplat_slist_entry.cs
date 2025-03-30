namespace AKNet.Udp5Quic.Common
{
    internal class CXPLAT_SLIST_ENTRY
    {
        public CXPLAT_SLIST_ENTRY Next;
    }

    internal static partial class MSQuicFunc
    {
        static void CxPlatListPushEntry(CXPLAT_SLIST_ENTRY ListHead, CXPLAT_SLIST_ENTRY Entry)
        {
            Entry.Next = ListHead.Next;
            ListHead.Next = Entry;
        }
    }
}
