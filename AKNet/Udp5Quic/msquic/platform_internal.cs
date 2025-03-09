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

    internal class CXPLAT_DATAPATH : CXPLAT_DATAPATH_COMMON
    {
        //
        // Function pointer to AcceptEx.
        //
        LPFN_ACCEPTEX AcceptEx;

        //
        // Function pointer to ConnectEx.
        //
        LPFN_CONNECTEX ConnectEx;

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
        uint32_t RecvBufLen;

        //
        // Indicates the binding connected to a remote IP address.
        //
        BOOLEAN Connected : 1;

        //
        // Socket type.
        //
        uint8_t Type : 2; // CXPLAT_SOCKET_TYPE

        //
        // Flag indicates the socket has more than one socket, affinitized to all
        // the processors.
        //
        uint16_t NumPerProcessorSockets : 1;

        //
        // Flag indicates the socket has a default remote destination.
        //
        uint8_t HasFixedRemoteAddress : 1;

        //
        // Flag indicates the socket indicated a disconnect event.
        //
        uint8_t DisconnectIndicated : 1;

        //
        // Flag indicates the binding is being used for PCP.
        //
        uint8_t PcpBinding : 1;

        //
        // Flag indicates the socket is using RIO instead of traditional Winsock.
        //
        uint8_t UseRio : 1;

        //
        // Debug flags.
        //
        uint8_t Uninitialized : 1;
        uint8_t Freed : 1;

        uint8_t UseTcp : 1;                  // Quic over TCP

        uint8_t RawSocketAvailable : 1;

        //
        // Per-processor socket contexts.
        //
        CXPLAT_SOCKET_PROC PerProcSockets[0];

    }

}
