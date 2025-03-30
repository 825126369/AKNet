using System.Net;
using System.Net.Sockets;

namespace AKNet.Udp5Quic.Common
{
    internal enum CXPLAT_DATAPATH_TYPE
    {
        CXPLAT_DATAPATH_TYPE_UNKNOWN = 0,
        CXPLAT_DATAPATH_TYPE_NORMAL,
        CXPLAT_DATAPATH_TYPE_RAW,
    }

    internal class CXPLAT_DATAPATH_COMMON
    {
        public CXPLAT_UDP_DATAPATH_CALLBACKS UdpHandlers;
        public CXPLAT_TCP_DATAPATH_CALLBACKS TcpHandlers;
        public CXPLAT_WORKER_POOL WorkerPool;
        public uint Features;
        public CXPLAT_DATAPATH_RAW RawDataPath;
    }

    internal class CXPLAT_DATAPATH_PROC
    {
        public CXPLAT_DATAPATH Datapath;
        public CXPLAT_EVENTQ EventQ;
        public CXPLAT_REF_COUNT RefCount;
        public int PartitionIndex;
        public bool Uninitialized;
        public CXPLAT_POOL SendDataPool;
        public CXPLAT_POOL RioSendDataPool;
        public CXPLAT_POOL SendBufferPool;
        public CXPLAT_POOL LargeSendBufferPool;
        public CXPLAT_POOL RioSendBufferPool;
        public CXPLAT_POOL RioLargeSendBufferPool;
        public CXPLAT_POOL_EX RecvDatagramPool;
        public CXPLAT_POOL RioRecvPool;
    }

    internal class CXPLAT_DATAPATH : CXPLAT_DATAPATH_COMMON
    {
        public LPFN_ACCEPTEX AcceptEx;
        public LPFN_CONNECTEX ConnectEx;
        public LPFN_WSASENDMSG WSASendMsg;
        public LPFN_WSARECVMSG WSARecvMsg;
        public RIO_EXTENSION_FUNCTION_TABLE RioDispatch;
        public CXPLAT_REF_COUNT RefCount;
        public uint DatagramStride;
        public uint RecvPayloadOffset;
        public int PartitionCount;
        public byte MaxSendBatchSize;
        public bool UseRio;
        public bool Uninitialized : 1;
        public bool Freed : 1;
        public bool UseTcp : 1;
        CXPLAT_DATAPATH_PARTITION Partitions[0];
    }

    internal class CXPLAT_SOCKET_PROC
    {
        public long RefCount;
        public CXPLAT_SQE IoSqe;
        public CXPLAT_SQE RioSqe;
        public CXPLAT_DATAPATH_PARTITION DatapathProc;
        public CXPLAT_SOCKET Parent;

        public Socket Socket;
        public CXPLAT_RUNDOWN_REF RundownRef;
        public bool IoStarted;
        public bool RecvFailure;
        public bool Uninitialized;
        public bool Freed;
        
        public RIO_CQ RioCq;
        public RIO_RQ RioRq;
        public long RioRecvCount;
        public long RioSendCount;
        public CXPLAT_LIST_ENTRY RioSendOverflow;
        public bool RioNotifyArmed;

        public CXPLAT_SOCKET AcceptSocket;
        public char AcceptAddrSpace[sizeof(SOCKADDR_INET) + 16 + sizeof(SOCKADDR_INET) + 16];
    }

    internal class CXPLAT_SOCKET_COMMON
    {
        public IPAddress LocalAddress;
        public IPAddress RemoteAddress;
        public CXPLAT_DATAPATH Datapath;
        public ushort Mtu;
    }
    
    internal class CXPLAT_SOCKET: CXPLAT_SOCKET_COMMON
    {
        public long RefCount;
        public int RecvBufLen;
        public bool Connected;
        public uint Type; // CXPLAT_SOCKET_TYPE
        public ushort NumPerProcessorSockets;
        public byte HasFixedRemoteAddress;
        public byte DisconnectIndicated;
        public byte PcpBinding;
        public byte UseRio;
        public byte Uninitialized;
        public byte Freed;
        public byte UseTcp;                  // Quic over TCP
        public bool RawSocketAvailable;
        public CXPLAT_SOCKET_PROC PerProcSockets[0];

    }

}
