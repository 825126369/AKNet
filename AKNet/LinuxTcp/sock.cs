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
        public LinkedList<sk_buff> sk_send_head;
        public rb_root tcp_rtx_queue;
        public sk_buff_head sk_write_queue;
    }
}
