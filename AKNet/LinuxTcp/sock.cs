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
    }

    internal static partial class LinuxTcpFunc
    {
        public static net sock_net(sock sk)
        {
            return sk.sk_net;
        }
    }
}
