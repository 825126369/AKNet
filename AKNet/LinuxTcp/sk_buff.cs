using System;
using System.Collections.Generic;
using System.Text;

namespace AKNet.LinuxTcp
{
    struct sk_buff_head
    {
        public sk_buff  next;
		public sk_buff  prev;
	    public uint qlen;
    }

    internal class sk_buff
    {

    }
}
