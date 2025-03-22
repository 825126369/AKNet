using System;

namespace AKNet.Udp5Quic.Common
{
    internal static partial class MSQuicFunc
    {
        static QUIC_WORKER_POOL new_QUIC_WORKER_POOL()
        {
            try
            {
                return new QUIC_WORKER_POOL();
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        static QUIC_REGISTRATION new_QUIC_REGISTRATION()
        {
            try
            {
                return new QUIC_REGISTRATION();
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
