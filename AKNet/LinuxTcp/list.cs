namespace AKNet.LinuxTcp
{
    internal static partial class LinuxTcpFunc
    {
        static bool __list_add_valid<T>(list_head<T> newHead, list_head<T> prev, list_head<T> next)
        {
	        return true;
        }

        static bool __list_del_entry_valid<T>(list_head<T> entry)
        {
	        return true;
        }

        static void __list_del<T>(list_head<T> prev, list_head<T> next)
        {
            next.prev = prev;
            prev.next = next;
        }

        static void __list_del_entry<T>(list_head<T> entry)
        {
            __list_del(entry.prev, entry.next);
        }

        static void list_del<T>(list_head<T> entry)
        {
            __list_del_entry(entry);
            entry.next = null;
            entry.prev = null;
        }
        
        static void __list_add<T>(list_head<T> newHead, list_head<T> prev, list_head<T> next)
        {
            if (!__list_add_valid(newHead, prev, next))
            {
                return;
            }

            next.prev = newHead;
            newHead.next = next;
            newHead.prev = prev;
            prev.next = newHead;
        }

        static void list_add<T>(list_head<T> newHead, list_head<T> head)
        {
            __list_add(newHead, head, head.next);
        }
    }
}
