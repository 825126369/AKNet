namespace AKNet.Udp4LinuxTcp.Common
{
    internal class scm_timestamping_internal
    {
        public long[] ts = new long[3];
    }

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
        public byte[] mBuffer;
        public int nLength;
        public int nOffset;
    }
}
