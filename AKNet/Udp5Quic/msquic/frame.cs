using AKNet.Udp4LinuxTcp.Common;

namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_ACK_ECN_EX
    {
        public ulong ECT_0_Count;
        public ulong ECT_1_Count;
        public ulong CE_Count;
    }

    internal static partial class MSQuicFunc
    {
        static bool QuicErrorIsProtocolError(ulong ErrorCode)
        {
            return ErrorCode >= QUIC_ERROR_FLOW_CONTROL_ERROR && ErrorCode <= QUIC_ERROR_AEAD_LIMIT_REACHED;
        }
    }
}
