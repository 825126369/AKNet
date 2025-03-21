namespace AKNet.Udp5Quic.Common
{
    internal class CXPLAT_DATAPATH_COMMON
    {
        public CXPLAT_UDP_DATAPATH_CALLBACKS UdpHandlers;
        //public CXPLAT_TCP_DATAPATH_CALLBACKS TcpHandlers;
        //public CXPLAT_WORKER_POOL* WorkerPool;
       // public uint32_t Features;
        //public CXPLAT_DATAPATH_RAW* RawDataPath;
    }

        internal class CXPLAT_DATAPATH_PROC 
        {
            public CXPLAT_DATAPATH Datapath;
            //public CXPLAT_EVENTQ EventQ;
            public long RefCount;
            public ushort PartitionIndex;
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

        //
        // Function pointer to WSASendMsg.
        //
        LPFN_WSASENDMSG WSASendMsg;

        //
        // Function pointer to WSARecvMsg.
        //
        LPFN_WSARECVMSG WSARecvMsg;

        //
        // Function pointer table for RIO.
        //
        RIO_EXTENSION_FUNCTION_TABLE RioDispatch;

        //
        // Used to synchronize clean up.
        //
        CXPLAT_REF_COUNT RefCount;

        //
        // The size of each receive datagram array element, including client context,
        // internal context, and padding.
        //
        uint32_t DatagramStride;

        //
        // The offset of the receive payload buffer from the start of the receive
        // context.
        //
        uint32_t RecvPayloadOffset;

        //
        // The number of processors.
        //
        uint16_t PartitionCount;

        //
        // Maximum batch sizes supported for send.
        //
        uint8_t MaxSendBatchSize;

        //
        // Uses RIO interface instead of normal asyc IO.
        //
        uint8_t UseRio : 1;

        //
        // Debug flags
        //
        uint8_t Uninitialized : 1;
        uint8_t Freed : 1;

        uint8_t UseTcp : 1;

        //
        // Per-processor completion contexts.
        //
        CXPLAT_DATAPATH_PARTITION Partitions[0];

    }

    internal class CXPLAT_SOCKET_PROC 
    {
        public long RefCount;
        public CXPLAT_SQE IoSqe;
        public CXPLAT_SQE RioSqe;

    
    CXPLAT_DATAPATH_PARTITION DatapathProc;

    //
    // Parent CXPLAT_SOCKET.
    //
    CXPLAT_SOCKET* Parent;

    //
    // Socket handle to the networking stack.
    //
    SOCKET Socket;

    //
    // Rundown for synchronizing upcalls to the app and downcalls on the Socket.
    //
    CXPLAT_RUNDOWN_REF RundownRef;

    //
    // Flag indicates the socket started processing IO.
    //
    BOOLEAN IoStarted : 1;

    //
    // Flag indicates a persistent out-of-memory failure for the receive path.
    //
    BOOLEAN RecvFailure : 1;

    //
    // Debug Flags
    //
    uint8_t Uninitialized : 1;
    uint8_t Freed : 1;

    //
    // The set of parameters/state passed to WsaRecvMsg for the IP stack to
    // populate to indicate the result of the receive.
    //

    union {
    //
    // Normal TCP/UDP socket data
    //
    struct {
        RIO_CQ RioCq;
        RIO_RQ RioRq;
        ULONG RioRecvCount;
        ULONG RioSendCount;
        CXPLAT_LIST_ENTRY RioSendOverflow;
        BOOLEAN RioNotifyArmed;
    };
    //
    // TCP Listener socket data
    //
    struct {
        CXPLAT_SOCKET* AcceptSocket;
        char AcceptAddrSpace[
            sizeof(SOCKADDR_INET) + 16 +
            sizeof(SOCKADDR_INET) + 16
            ];
    };
};
} 

internal class CXPLAT_SOCKET_COMMON
    {
        //
        // The local address and port.
        //
        string LocalAddress;

        //
        // The remote address and port.
        //
        string RemoteAddress;

        //
        // Parent datapath.
        //
        CXPLAT_DATAPATH* Datapath;

        //
        // The client context for this binding.
        //
        void* ClientContext;

        //
        // The local interface's MTU.
        //
        uint16_t Mtu;
    }

    internal class CXPLAT_SOCKET: CXPLAT_SOCKET_COMMON
    {
        //
        // Synchronization mechanism for cleanup.
        //
        public long RefCount;

        //
        // The size of a receive buffer's payload.
        //
        public int RecvBufLen;

        //
        // Indicates the binding connected to a remote IP address.
        //
        public bool Connected;

        //
        // Socket type.
        //
        public uint Type : 2; // CXPLAT_SOCKET_TYPE

        //
        // Flag indicates the socket has more than one socket, affinitized to all
        // the processors.
        //
        public ushort NumPerProcessorSockets;

        //
        // Flag indicates the socket has a default remote destination.
        //
        public byte HasFixedRemoteAddress;

        //
        // Flag indicates the socket indicated a disconnect event.
        //
        public byte DisconnectIndicated;

        //
        // Flag indicates the binding is being used for PCP.
        //
        public byte PcpBinding;

        //
        // Flag indicates the socket is using RIO instead of traditional Winsock.
        //
        public byte UseRio;

        //
        // Debug flags.
        //
        public byte Uninitialized;
        public byte Freed;
        public byte UseTcp;                  // Quic over TCP
        public byte RawSocketAvailable;
        CXPLAT_SOCKET_PROC PerProcSockets[0];

    }

}
