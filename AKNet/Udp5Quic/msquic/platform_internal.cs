using System;
using System.Net.Sockets;

namespace AKNet.Udp5Quic.Common
{
    internal enum CXPLAT_DATAPATH_TYPE
    {
        CXPLAT_DATAPATH_TYPE_UNKNOWN = 0,
        CXPLAT_DATAPATH_TYPE_NORMAL,
        CXPLAT_DATAPATH_TYPE_RAW,
    }

    internal enum CXPLAT_SOCKET_TYPE
    {
        CXPLAT_SOCKET_UDP = 0,
        CXPLAT_SOCKET_TCP_LISTENER = 1,
        CXPLAT_SOCKET_TCP = 2,
        CXPLAT_SOCKET_TCP_SERVER = 3
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
        public long RefCount;
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
        public bool Uninitialized;
        public bool Freed;
        public bool UseTcp;
        public CXPLAT_DATAPATH_PROC[] Partitions = null;
    }

    internal class CXPLAT_SOCKET_RAW
    {
        public CXPLAT_HASHTABLE_ENTRY Entry;
        public CXPLAT_RUNDOWN_REF Rundown;
        public CXPLAT_DATAPATH_RAW RawDatapath;
        public Socket AuxSocket;
        public bool Wildcard;                // Using a wildcard local address. Optimization
                                         // to avoid always reading LocalAddress.
        byte CibirIdLength;           // CIBIR ID length. Value of 0 indicates CIBIR isn't used
        byte CibirIdOffsetSrc;        // CIBIR ID offset in source CID
        byte CibirIdOffsetDst;        // CIBIR ID offset in destination CID
        byte CibirId[6];              // CIBIR ID data

        public CXPLAT_SEND_DATA PausedTcpSend; // Paused TCP send data *before* framing
        public CXPLAT_SEND_DATA CachedRstSend; // Cached TCP RST send data *after* framing
    }

    internal class CXPLAT_SOCKET_PROC
    {
        public long RefCount;
        public CXPLAT_SQE IoSqe;
        public CXPLAT_SQE RioSqe;
        public CXPLAT_DATAPATH_PROC DatapathProc;
        public CXPLAT_SOCKET Parent;

        public Socket Socket;
        public CXPLAT_RUNDOWN_REF RundownRef;
        public bool IoStarted;
        public bool RecvFailure;
        public bool Uninitialized;
        public bool Freed;
        
        //public RIO_CQ RioCq;
        //public RIO_RQ RioRq;
        public long RioRecvCount;
        public long RioSendCount;
        public CXPLAT_LIST_ENTRY RioSendOverflow;
        public bool RioNotifyArmed;

        public CXPLAT_SOCKET AcceptSocket;
        public byte[] AcceptAddrSpace = new byte[4 + 16 + 4 + 16];
        public readonly SocketAsyncEventArgs ReceiveArgs;
        public readonly SocketAsyncEventArgs SendArgs;
        public bool bReceiveIOContexUsed = false;
        public bool bSendIOContexUsed = false;
    }

    internal class CXPLAT_SOCKET_COMMON
    {
        public QUIC_ADDR LocalAddress;
        public QUIC_ADDR RemoteAddress;
        public CXPLAT_DATAPATH Datapath;
        public ushort Mtu;
    }
    
    internal class CXPLAT_SOCKET: CXPLAT_SOCKET_COMMON
    {
        public long RefCount;
        public int RecvBufLen;
        public bool Connected;
        public CXPLAT_SOCKET_TYPE Type;
        public int NumPerProcessorSockets;
        public bool HasFixedRemoteAddress;
        public byte DisconnectIndicated;
        public bool PcpBinding;
        public bool UseRio;
        public bool Uninitialized;
        public byte Freed;
        public bool UseTcp;
        public bool RawSocketAvailable;
        public CXPLAT_SOCKET_PROC[] PerProcSockets = null;
        public object ClientContext;
    }

}
