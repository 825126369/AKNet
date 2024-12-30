namespace AKNet.LinuxTcp
{
    internal class iov_iter
    {
        public byte iter_type;
        public bool nofault;
        public bool data_source;
        public long iov_offset;
        public long count;
    }

    internal class msghdr
    {
        public void* msg_name;      /* ptr to socket address structure */
        public int msg_namelen;     /* size of socket address structure */
        public int msg_inq;         /* output, data left in socket */
        public iov_iter msg_iter;	/* data */
        public bool msg_get_inq;    /* return INQ after receive */
        public uint msg_flags;      /* flags on received message */
    }

    internal static partial class LinuxTcpFunc
    {
        public static long msg_data_left(msghdr msg)
        {
	        return iov_iter_count(msg.msg_iter);
        }
    }
}
