using System.Collections.Generic;

namespace AKNet.LinuxTcp
{
    struct rb_root
    {
        public SortedSet<sk_buff> rb_node;
    };

    internal class sock
    {
        public int sk_err;
        public int sk_err_soft;

        public LinkedList<sk_buff> sk_send_head;
        public rb_root tcp_rtx_queue;
        public sk_buff_head sk_write_queue;

        public net sk_net;
        public ulong sk_flags;
        public uint sk_txhash;
        public int sk_refcnt;
    }

    internal static partial class LinuxTcpFunc
    {
        public static net sock_net(sock sk)
        {
            return sk.sk_net;
        }

        public static bool sock_flag(sock sk, sock_flags flag)
        {
	        return ((ulong)flag & sk.sk_flags) > 0;
        }

        public static void __sock_put(sock sk)
        {
            sk.sk_refcnt--;
        }
    }
}
