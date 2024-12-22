using System.Collections.Generic;

namespace AKNet.LinuxTcp
{
    internal class sk_backlog
    {
        public LinkedList<sk_buff> mQueue = new LinkedList<sk_buff>();
        public long rmem_alloc;
        public int len;
    }
}
