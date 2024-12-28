namespace AKNet.LinuxTcp
{
    internal class sk_buff_list
    {
        public sk_buff next;
        public sk_buff prev;
    }

    internal class sk_buff_head
    {
	    public uint qlen;
        public sk_buff next;
        public sk_buff prev;
    }

}
