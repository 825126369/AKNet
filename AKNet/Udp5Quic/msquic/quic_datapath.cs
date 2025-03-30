using System;
using System.Net;
using System.Net.Sockets;

namespace AKNet.Udp5Quic.Common
{
    internal delegate void CXPLAT_DATAPATH_RECEIVE_CALLBACK(CXPLAT_SOCKET Socket, void* Context, CXPLAT_RECV_DATA* RecvDataChain);

    internal class CXPLAT_UDP_DATAPATH_CALLBACKS
    {
        CXPLAT_DATAPATH_RECEIVE_CALLBACK_HANDLER Receive;
        CXPLAT_DATAPATH_UNREACHABLE_CALLBACK_HANDLER Unreachable;
    }

    internal class CXPLAT_TCP_DATAPATH_CALLBACKS
    {
        CXPLAT_DATAPATH_ACCEPT_CALLBACK_HANDLER Accept;
        CXPLAT_DATAPATH_CONNECT_CALLBACK_HANDLER Connect;
        CXPLAT_DATAPATH_RECEIVE_CALLBACK_HANDLER Receive;
        CXPLAT_DATAPATH_SEND_COMPLETE_CALLBACK_HANDLER SendComplete;
    }

    internal delegate CXPLAT_DATAPATH_ACCEPT_CALLBACK(CXPLAT_SOCKET, Action, CXPLAT_SOCKET AcceptSocket, Action AcceptClientContext);

    internal enum CXPLAT_ROUTE_STATE
    {
        RouteUnresolved,
        RouteResolving,
        RouteSuspected,
        RouteResolved,
    }

    internal class CXPLAT_RAW_TCP_STATE
    {
        public bool Syncd;
        public uint AckNumber;
        public uint SequenceNumber;
    }

    internal class CXPLAT_ROUTE
    {
        void* Queue;
        public IPAddress RemoteAddress;
        public IPAddress LocalAddress;
        public byte[] LocalLinkLayerAddress = new byte[6];
        public byte[] NextHopLinkLayerAddress = new byte[6];
        public CXPLAT_DATAPATH_TYPE DatapathType;
        public CXPLAT_ROUTE_STATE State;
        public CXPLAT_RAW_TCP_STATE TcpState;

        public void CopyFrom(CXPLAT_ROUTE other)
        {
            this.Queue = other.Queue;
            RemoteAddress = IPAddress.Parse(other.RemoteAddress.ToString());
            LocalAddress = IPAddress.Parse(other.LocalAddress.ToString());
            Array.Copy(other.LocalLinkLayerAddress, LocalLinkLayerAddress, LocalLinkLayerAddress.Length);
            Array.Copy(other.NextHopLinkLayerAddress, NextHopLinkLayerAddress, NextHopLinkLayerAddress.Length);
            DatapathType = other.DatapathType;
            State = other.State;
            TcpState = other.TcpState;
        }
    }

    internal class CXPLAT_RECV_DATA
    {
        public CXPLAT_RECV_DATA Next;
        public CXPLAT_ROUTE Route;
        public byte[] Buffer;
        public int BufferLength;
        public ushort PartitionIndex;
        public byte TypeOfService;
        public byte HopLimitTTL;
        public ushort Allocated;          // Used for debugging. Set to FALSE on free.
        public ushort QueuedOnConnection; // Used for debugging.
        public ushort DatapathType;       // CXPLAT_DATAPATH_TYPE
        public ushort Reserved;           // PACKET_TYPE (at least 3 bits)
        public ushort ReservedEx;         // Header length
    }

    internal static partial class MSQuicFunc
    {
        static ushort MaxUdpPayloadSizeForFamily(AddressFamily Family, ushort Mtu)
        {
            return Family == AddressFamily.InterNetwork ?
                (ushort)(Mtu - CXPLAT_MIN_IPV4_HEADER_SIZE - CXPLAT_UDP_HEADER_SIZE) :
                (ushort)(Mtu - CXPLAT_MIN_IPV6_HEADER_SIZE - CXPLAT_UDP_HEADER_SIZE);
        }

        static ushort PacketSizeFromUdpPayloadSize(AddressFamily Family, ushort UdpPayloadSize)
        {
            int PayloadSize = Family == AddressFamily.InterNetwork ?
                UdpPayloadSize + CXPLAT_MIN_IPV4_HEADER_SIZE + CXPLAT_UDP_HEADER_SIZE :
                UdpPayloadSize + CXPLAT_MIN_IPV6_HEADER_SIZE + CXPLAT_UDP_HEADER_SIZE;
            if (PayloadSize > ushort.MaxValue)
            {
                PayloadSize = ushort.MaxValue;
            }
            return (ushort)PayloadSize;
        }
    }
}
