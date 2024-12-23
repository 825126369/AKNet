namespace AKNet.LinuxTcp
{
    internal partial class LinuxTcpFunc
    {
        public static readonly int OPTION_SACK_ADVERTISE = (int)LinuxTcpFunc.BIT(0);
        public static readonly int OPTION_TS = (int)LinuxTcpFunc.BIT(1);
        public static readonly int OPTION_MD5 = (int)LinuxTcpFunc.BIT(2);
        public static readonly int OPTION_WSCALE = (int)LinuxTcpFunc.BIT(3);
        public static readonly int OPTION_FAST_OPEN_COOKIE = (int)LinuxTcpFunc.BIT(8);
        public static readonly int OPTION_SMC = (int)LinuxTcpFunc.BIT(9);
        public static readonly int OPTION_MPTCP = (int)LinuxTcpFunc.BIT(10);
        public static readonly int OPTION_AO = (int)LinuxTcpFunc.BIT(11);

        /* use zcopy routines */
        public static readonly int SKBFL_ZEROCOPY_ENABLE = (int)LinuxTcpFunc.BIT(0);

        /* This indicates at least one fragment might be overwritten
         * (as in vmsplice(), sendfile() ...)
         * If we need to compute a TX checksum, we'll need to copy
         * all frags to avoid possible bad checksum
         */
        public static readonly int SKBFL_SHARED_FRAG = (int)LinuxTcpFunc.BIT(1);

        /* segment contains only zerocopy data and should not be
         * charged to the kernel memory.
         */
        public static readonly int SKBFL_PURE_ZEROCOPY = (int)LinuxTcpFunc.BIT(2);

        public static readonly int SKBFL_DONT_ORPHAN = (int)LinuxTcpFunc.BIT(3);

        /* page references are managed by the ubuf_info, so it's safe to
         * use frags only up until ubuf_info is released
         */
        public static readonly int SKBFL_MANAGED_FRAG_REFS = (int)LinuxTcpFunc.BIT(4);
    }
}
