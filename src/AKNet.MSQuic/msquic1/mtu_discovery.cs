/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:56
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;

namespace MSQuic1
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
            MtuDiscovery.SearchCompleteEnterTimeUs = CxPlatTimeUs();
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

        static bool QuicMtuDiscoveryOnAckedPacket(QUIC_MTU_DISCOVERY MtuDiscovery, int PacketMtu, QUIC_CONNECTION Connection)
        {
            QUIC_PATH Path = MtuDiscovery.mQUIC_PATH;
            if (PacketMtu != MtuDiscovery.ProbeSize)
            {
                return false;
            }

            Path.Mtu = MtuDiscovery.ProbeSize;
            if (Path.Mtu == MtuDiscovery.MaxMtu)
            {
                QuicMtuDiscoveryMoveToSearchComplete(MtuDiscovery, Connection);
                return true;
            }

            QuicMtuDiscoveryMoveToSearching(MtuDiscovery, Connection);
            return true;
        }

        static void QuicMtuDiscoveryProbePacketDiscarded(QUIC_MTU_DISCOVERY MtuDiscovery, QUIC_CONNECTION Connection, ushort PacketMtu)
        {
            QUIC_PATH Path = MtuDiscovery.mQUIC_PATH;
            if (PacketMtu != MtuDiscovery.ProbeSize)
            {
                return;
            }

            if (MtuDiscovery.ProbeCount >= (ushort)Connection.Settings.MtuDiscoveryMissingProbeCount - 1)
            {
                QuicMtuDiscoveryMoveToSearchComplete(MtuDiscovery, Connection);
                return;
            }
            MtuDiscovery.ProbeCount++;
            QuicMtuDiscoverySendProbePacket(Connection);
        }
    }
}
