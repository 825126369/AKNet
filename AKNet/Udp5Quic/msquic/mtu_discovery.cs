using AKNet.Common;
using System;

namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_MTU_DISCOVERY
    {
        public long SearchCompleteEnterTimeUs;
        public ushort MaxMtu;
        public ushort ProbeSize;
        public byte ProbeCount;
        public bool IsSearchComplete;
        public bool HasProbed1500;
        public QUIC_PATH mQUIC_PATH;
    }

    internal static partial class MSQuicFunc
    {
        static void QuicMtuDiscoverySendProbePacket(QUIC_CONNECTION Connection)
        {
            QuicSendSetSendFlag(Connection.Send, QUIC_CONN_SEND_FLAG_DPLPMTUD);
        }

        static void QuicMtuDiscoveryMoveToSearchComplete(QUIC_MTU_DISCOVERY MtuDiscovery, QUIC_CONNECTION Connection)
        {
            QUIC_PATH Path = MtuDiscovery.mQUIC_PATH;
            MtuDiscovery.IsSearchComplete = true;
            MtuDiscovery.SearchCompleteEnterTimeUs = mStopwatch.ElapsedMilliseconds;
        }

        static ushort QuicGetNextProbeSize(QUIC_MTU_DISCOVERY MtuDiscovery)
        {
            QUIC_PATH Path = MtuDiscovery.mQUIC_PATH;
            if (Path.Mtu < 1280)
            {
                return (ushort)Math.Min(1280, (int)MtuDiscovery.MaxMtu);
            }

            ushort Mtu = (ushort)(Path.Mtu + QUIC_DPLPMTUD_INCREMENT);
            if (Mtu > MtuDiscovery.MaxMtu)
            {
                Mtu = MtuDiscovery.MaxMtu;
            }

            if (!MtuDiscovery.HasProbed1500 && Mtu >= 1500)
            {
                MtuDiscovery.HasProbed1500 = true;
                Mtu = 1500;
            }
            return Mtu;
        }

        static void QuicMtuDiscoveryMoveToSearching(QUIC_MTU_DISCOVERY MtuDiscovery, QUIC_CONNECTION Connection)
        {
            QUIC_PATH Path = MtuDiscovery.mQUIC_PATH;
            MtuDiscovery.IsSearchComplete = false;
            MtuDiscovery.ProbeCount = 0;
            
            MtuDiscovery.ProbeSize = Path.IsMinMtuValidated ? QuicGetNextProbeSize(MtuDiscovery) : Path.Mtu;
            if (MtuDiscovery.ProbeSize == Path.Mtu && Path.IsMinMtuValidated)
            {
                QuicMtuDiscoveryMoveToSearchComplete(MtuDiscovery, Connection);
                return;
            }

            QuicMtuDiscoverySendProbePacket(Connection);
        }

        static void QuicMtuDiscoveryPeerValidated(QUIC_MTU_DISCOVERY MtuDiscovery, QUIC_CONNECTION Connection)
        {
            QUIC_PATH Path = MtuDiscovery.mQUIC_PATH;
            MtuDiscovery.MaxMtu = QuicConnGetMaxMtuForPath(Connection, Path);
            MtuDiscovery.HasProbed1500 = Path.Mtu >= 1500;
            NetLog.Assert(Path.Mtu <= MtuDiscovery.MaxMtu);
            QuicMtuDiscoveryMoveToSearching(MtuDiscovery, Connection);
        }
    }
}
