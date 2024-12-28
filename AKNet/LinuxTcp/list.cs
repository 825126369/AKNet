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
            entry.value = null;
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

        static void list_del_init<T>(list_head<T> entry)
        {
	        __list_del_entry(entry);
            entry.next = null;
            entry.prev = null;
            entry.value = default;
        }

        static sk_buff list_first_entry(list_head<sk_buff> ptr)
        {
            return ptr.next.value;
        }

        static sk_buff list_next_entry(sk_buff ptr)
        {
            return ptr.tcp_tsorted_anchor.next.value;
        }


        static bool list_is_head(list_head<sk_buff> list, list_head<sk_buff> head)
        {
	        return list == head;
        }

        static bool list_entry_is_head(sk_buff skb, list_head<sk_buff>  head)
        {
            return list_is_head(skb.tcp_tsorted_anchor, head);
        }

    }
}
