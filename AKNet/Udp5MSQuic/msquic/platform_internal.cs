using System.Net.Sockets;

namespace AKNet.Udp5MSQuic.Common
{
    internal enum CXPLAT_DATAPATH_TYPE
    {
        CXPLAT_DATAPATH_TYPE_UNKNOWN = 0,
        CXPLAT_DATAPATH_TYPE_USER,
        CXPLAT_DATAPATH_TYPE_RAW, // currently raw == xdp
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
        public CXPLAT_WORKER_POOL WorkerPool;
        public uint Features;
        public CXPLAT_DATAPATH_RAW RawDataPath;
    }

    internal class CXPLAT_DATAPATH_PROC
    {
        public CXPLAT_DATAPATH Datapath;
        public long RefCount;
        public int PartitionIndex;
        public bool Uninitialized;
        public readonly CXPLAT_POOL<CXPLAT_SEND_DATA> SendDataPool = new CXPLAT_POOL<CXPLAT_SEND_DATA>();
        public readonly CXPLAT_Buffer_POOL SendBufferPool = new CXPLAT_Buffer_POOL();
        public readonly CXPLAT_Buffer_POOL LargeSendBufferPool = new CXPLAT_Buffer_POOL();
        public readonly CXPLAT_POOL_EX<DATAPATH_RX_IO_BLOCK> RecvDatagramPool = new CXPLAT_POOL_EX<DATAPATH_RX_IO_BLOCK>();
    }

    internal class CXPLAT_DATAPATH : CXPLAT_DATAPATH_COMMON
    {
        public long RefCount;
        public int DatagramStride;
        public int PartitionCount;
        public byte MaxSendBatchSize;
        public bool UseRio;
        public bool Uninitialized;
        public bool Freed;
        public CXPLAT_DATAPATH_PROC[] Partitions = null;

        public int RecvPayloadOffset;
        public int RecvDatagramLength;
    }

    internal class CXPLAT_SOCKET_PROC
    {
        public long RefCount;
        public CXPLAT_DATAPATH_PROC DatapathProc;
        public CXPLAT_SOCKET Parent;

        public Socket Socket;
        public readonly CXPLAT_RUNDOWN_REF RundownRef = new CXPLAT_RUNDOWN_REF();
        public bool IoStarted;
        public bool RecvFailure;
        public bool Uninitialized;
        public bool Freed;

        public int RioRecvCount;

        public CXPLAT_SOCKET AcceptSocket;
        public byte[] AcceptAddrSpace = new byte[4 + 16 + 4 + 16];
        public readonly SocketAsyncEventArgs SendArgs = new SocketAsyncEventArgs();
        public bool bReceiveIOContexUsed = false;
        public bool bSendIOContexUsed = false;
    }

    internal class CXPLAT_SOCKET_RAW
    {
        //public Dictionary<ushort, > Entry;
        public CXPLAT_RUNDOWN_REF Rundown;
        public CXPLAT_DATAPATH_RAW RawDatapath;
        public Socket AuxSocket;
        public bool Wildcard;                // Using a wildcard local address. Optimization
                                             // to avoid always reading LocalAddress.
        public byte CibirIdLength;           // CIBIR ID length. Value of 0 indicates CIBIR isn't used
        public byte CibirIdOffsetSrc;        // CIBIR ID offset in source CID
        public byte CibirIdOffsetDst;        // CIBIR ID offset in destination CID
        public byte[] CibirId = new byte[6];              // CIBIR ID data

        public CXPLAT_SEND_DATA PausedTcpSend; // Paused TCP send data *before* framing
        public CXPLAT_SEND_DATA CachedRstSend; // Cached TCP RST send data *after* framing
    }

    internal class CXPLAT_SOCKET_COMMON: CXPLAT_SOCKET_RAW
    {
        public QUIC_ADDR LocalAddress = new QUIC_ADDR();
        public QUIC_ADDR RemoteAddress = new QUIC_ADDR();
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
        public bool Uninitialized;
        public byte Freed;
        public bool RawSocketAvailable;
        public CXPLAT_SOCKET_PROC[] PerProcSockets = null;
        public object ClientContext;
    }

}
