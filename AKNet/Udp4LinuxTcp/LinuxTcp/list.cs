/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;

namespace AKNet.Udp4LinuxTcp.Common
{
    internal static partial class LinuxTcpFunc
    {

        static sk_buff list_entry(list_head list)
        {
            return list.value;
        }

        static void INIT_LIST_HEAD(list_head list)
        {
            list.next = list;
            list.prev = list;
        }

        static void __list_del(list_head prev, list_head next)
        {
            next.prev = prev;
            prev.next = next;
        }

        static void __list_del_entry(list_head entry)
        {
            NetLog.Assert(entry != null, "entry == null");
            NetLog.Assert(entry.prev != null, "__list_del_entry prev == null");
            NetLog.Assert(entry.next != null, "__list_del_entry next == null");
            __list_del(entry.prev, entry.next);
        }

        static void list_del(list_head entry)
        {
            __list_del_entry(entry);
            entry.next = null;
            entry.prev = null;
        }

        static void list_del_init(list_head entry)
        {
            __list_del_entry(entry);
            INIT_LIST_HEAD(entry);
        }

        static void __list_add(list_head newHead, list_head prev, list_head next)
        {
            next.prev = newHead;
            newHead.next = next;
            newHead.prev = prev;
            prev.next = newHead;
        }

        static void list_add(list_head newHead, list_head head)
        {
            __list_add(newHead, head, head.next);
        }

        static sk_buff list_first_entry(list_head ptr)
        {
            return list_entry(ptr.next);
        }
        
        static sk_buff list_next_entry(sk_buff entry)
        {
            NetLog.Assert(entry != null, "ptr == null");
            NetLog.Assert(entry.tcp_tsorted_anchor.prev != null, "list_next_entry prev == null");
            NetLog.Assert(entry.tcp_tsorted_anchor.next != null, "list_next_entry next == null");
            return list_entry(entry.tcp_tsorted_anchor.next);
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

        static int list_count_nodes(list_head head)
        {
            int count = 0;
            for (list_head pos = head.next; !list_is_head(pos, head); pos = pos.next)
            {
                count++;
            }
            return count;
        }

    }
}
