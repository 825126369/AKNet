namespace AKNet.Udp4LinuxTcp.Common
{
    internal static partial class LinuxTcpFunc
    {
        static void INIT_LIST_HEAD(list_head list)
        {
            list.next = list;
            list.prev = list;
        }

        static bool __list_add_valid(list_head newHead, list_head prev, list_head next)
        {
	        return true;
        }

        static bool __list_del_entry_valid(list_head entry)
        {
	        return true;
        }

        static void __list_del(list_head prev, list_head next)
        {
            next.prev = prev;
            prev.next = next;
        }

        static void __list_del_entry(list_head entry)
        {
            __list_del(entry.prev, entry.next);
        }

        static void list_del(list_head entry)
        {
            __list_del_entry(entry);
            entry.next = null;
            entry.prev = null;
        }
        
        static void __list_add(list_head newHead, list_head prev, list_head next)
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

        static void list_add(list_head newHead, list_head head)
        {
            __list_add(newHead, head, head.next);
        }

        static void list_del_init(list_head entry)
        {
	        __list_del_entry(entry);
            entry.next = null;
            entry.prev = null;
        }

        static sk_buff list_first_entry(list_head ptr)
        {
            return ptr.next.value;
        }

        static sk_buff list_next_entry(sk_buff ptr)
        {
            return ptr.tcp_tsorted_anchor.next.value;
        }


        static bool list_is_head(list_head list, list_head head)
        {
	        return list == head;
        }

        static bool list_entry_is_head(sk_buff skb, list_head  head)
        {
            return list_is_head(skb.tcp_tsorted_anchor, head);
        }

        static void list_add_tail(list_head newHead, list_head head)
        {
	        __list_add(newHead, head.prev, head);
        }

        // list_move_tail
        static void list_move_tail(list_head list, list_head head)
        {
	        __list_del_entry(list);
            list_add_tail(list, head);
        }

    }
}
