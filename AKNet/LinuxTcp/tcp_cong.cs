namespace AKNet.LinuxTcp
{
    internal static partial class LinuxTcpFunc
    {
        public static void tcp_set_ca_state(tcp_sock tp, tcp_ca_state ca_state)
        {
            if (tp.icsk_ca_ops.set_state != null)
            {
                tp.icsk_ca_ops.set_state(tp, ca_state);
                tp.icsk_ca_state = (byte)ca_state;
            }
        }
    }
}
