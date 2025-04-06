using AKNet.Common;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace AKNet.Udp5Quic.Common
{

    internal delegate void CXPLAT_DATAPATH_RECEIVE_CALLBACK(CXPLAT_SOCKET Socket, QUIC_BINDING Context, CXPLAT_RECV_DATA RecvDataChain);
    internal delegate void CXPLAT_DATAPATH_UNREACHABLE_CALLBACK(CXPLAT_SOCKET Socket, QUIC_BINDING Context, IPAddress RemoteAddress);
    internal class CXPLAT_UDP_DATAPATH_CALLBACKS
    {
        public CXPLAT_DATAPATH_RECEIVE_CALLBACK Receive;
        public CXPLAT_DATAPATH_UNREACHABLE_CALLBACK Unreachable;
    }

    internal class CXPLAT_TCP_DATAPATH_CALLBACKS
    {
        CXPLAT_DATAPATH_ACCEPT_CALLBACK Accept;
        CXPLAT_DATAPATH_CONNECT_CALLBACK Connect;
        CXPLAT_DATAPATH_RECEIVE_CALLBACK Receive;
        CXPLAT_DATAPATH_SEND_COMPLETE_CALLBACK SendComplete;
    }

    internal delegate CXPLAT_DATAPATH_ACCEPT_CALLBACK(CXPLAT_SOCKET, Action, CXPLAT_SOCKET AcceptSocket, Action AcceptClientContext);

    internal enum CXPLAT_ROUTE_STATE
    {
        RouteUnresolved,
        RouteResolving,
        RouteSuspected,
        RouteResolved,
    }

    internal enum CXPLAT_QEO_OPERATION
    {
        CXPLAT_QEO_OPERATION_ADD,     // Add (or modify) a QUIC connection offload
        CXPLAT_QEO_OPERATION_REMOVE,  // Remove a QUIC connection offload
    }

    internal enum CXPLAT_QEO_DIRECTION
    {
        CXPLAT_QEO_DIRECTION_TRANSMIT, // An offload for the transmit path
        CXPLAT_QEO_DIRECTION_RECEIVE,  // An offload for the receive path
    }

    internal enum CXPLAT_QEO_DECRYPT_FAILURE_ACTION
    {
        CXPLAT_QEO_DECRYPT_FAILURE_ACTION_DROP,     // Drop the packet on decryption failure
        CXPLAT_QEO_DECRYPT_FAILURE_ACTION_CONTINUE, // Continue and pass the packet up on decryption failure
    }

    internal enum CXPLAT_QEO_CIPHER_TYPE
    {
        CXPLAT_QEO_CIPHER_TYPE_AEAD_AES_128_GCM,
        CXPLAT_QEO_CIPHER_TYPE_AEAD_AES_256_GCM,
        CXPLAT_QEO_CIPHER_TYPE_AEAD_CHACHA20_POLY1305,
        CXPLAT_QEO_CIPHER_TYPE_AEAD_AES_128_CCM,
    }

    internal class CXPLAT_RAW_TCP_STATE
    {
        public bool Syncd;
        public uint AckNumber;
        public uint SequenceNumber;
    }

    public class CXPLAT_SOCKET_POOL
    {
        public readonly object Lock = new object();
        public CXPLAT_HASHTABLE Sockets;
    }

    internal class CXPLAT_ROUTE_RESOLUTION_WORKER
    {
        public bool Enabled;
        public CXPLAT_EVENT Ready;
        public Thread Thread;
        public CXPLAT_POOL OperationPool;
        public readonly object Lock = new object();
        public CXPLAT_LIST_ENTRY Operations;
    }

    internal class CXPLAT_DATAPATH_RAW
    {
        public CXPLAT_DATAPATH ParentDataPath;
        public CXPLAT_WORKER_POOL WorkerPool;
        public CXPLAT_SOCKET_POOL SocketPool;
        public CXPLAT_ROUTE_RESOLUTION_WORKER RouteResolutionWorker;

        public CXPLAT_LIST_ENTRY Interfaces;
        public bool UseTcp;
    }

    internal class CXPLAT_ROUTE
    {
        void* Queue;
        public IPEndPoint RemoteAddress;
        public IPEndPoint LocalAddress;
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

    internal class CXPLAT_UDP_CONFIG
    {
        public IPEndPoint LocalAddress;      // optional
        public IPEndPoint RemoteAddress;     // optional
        public uint Flags;                     // CXPLAT_SOCKET_FLAG_*
        public int InterfaceIndex;            // 0 means any/all
        public int PartitionIndex;            // Client-only
        public void* CallbackContext;              // optional
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
        public bool QueuedOnConnection; // Used for debugging.
        public CXPLAT_DATAPATH_TYPE DatapathType;       // CXPLAT_DATAPATH_TYPE
        public ushort Reserved;           // PACKET_TYPE (at least 3 bits)
        public ushort ReservedEx;         // Header length
    }

    internal class CXPLAT_QEO_CONNECTION
    {
        public CXPLAT_QEO_OPERATION Operation;  // CXPLAT_QEO_OPERATION
        public CXPLAT_QEO_DIRECTION Direction;  // CXPLAT_QEO_DIRECTION
        public CXPLAT_QEO_DECRYPT_FAILURE_ACTION DecryptFailureAction;  // CXPLAT_QEO_DECRYPT_FAILURE_ACTION
        public uint KeyPhase;
        public uint RESERVED; // Must be set to 0. Don't read.
        public CXPLAT_QEO_CIPHER_TYPE CipherType; // CXPLAT_QEO_CIPHER_TYPE
        public ulong NextPacketNumber;
        public IPAddress Address;
        public byte ConnectionIdLength;
        public byte[] ConnectionId = new byte[20]; // QUIC v1 and v2 max CID size
        public byte[] PayloadKey = new byte[32];   // Length determined by CipherType
        public byte[] HeaderKey = new byte[32];    // Length determined by CipherType
        public byte[] PayloadIv = new byte[12];
    }

    internal class CXPLAT_SEND_CONFIG
    {
        public CXPLAT_ROUTE Route;
        public int MaxPacketSize;
        public byte ECN; // CXPLAT_ECN_TYPE
        public byte Flags; // CXPLAT_SEND_FLAGS
    }

    internal enum CXPLAT_ECN_TYPE
    {
        CXPLAT_ECN_NON_ECT = 0x0, // Non ECN-Capable Transport, Non-ECT
        CXPLAT_ECN_ECT_1 = 0x1, // ECN Capable Transport, ECT(1)
        CXPLAT_ECN_ECT_0 = 0x2, // ECN Capable Transport, ECT(0)
        CXPLAT_ECN_CE = 0x3  // Congestion Encountered, CE
    }

    internal class CXPLAT_DATAPATH_PROC
    {
        public CXPLAT_DATAPATH Datapath;
        public CXPLAT_EVENTQ EventQ;
        public int RefCount;
        public int PartitionIndex;
        public bool Uninitialized;
        public CXPLAT_POOL SendDataPool;
        public CXPLAT_POOL RioSendDataPool;
        public CXPLAT_POOL SendBufferPool;
        public CXPLAT_POOL LargeSendBufferPool;
        public CXPLAT_POOL RioSendBufferPool;
        public CXPLAT_POOL RioLargeSendBufferPool;
        public CXPLAT_POOL RecvDatagramPool;
        public CXPLAT_POOL RioRecvPool;
    }

    internal class CXPLAT_SEND_DATA_COMMON
    {
        public int DatapathType;
        public byte ECN;
    }

    public class CXPLAT_SEND_DATA : CXPLAT_SEND_DATA_COMMON
    {
        CXPLAT_SOCKET_PROC SocketProc;
        DATAPATH_IO_SQE Sqe;

        CXPLAT_DATAPATH_PARTITION Owner;
        CXPLAT_POOL* SendDataPool;

        //
        // The pool for send buffers within this send data.
        //
        CXPLAT_POOL* BufferPool;

        //
        // The total buffer size for WsaBuffers.
        //
        uint32_t TotalSize;

        //
        // The send segmentation size; zero if segmentation is not performed.
        //
        uint16_t SegmentSize;

        //
        // Set of flags set to configure the send behavior.
        //
        uint8_t SendFlags; // CXPLAT_SEND_FLAGS

        //
        // The current number of WsaBuffers used.
        //
        uint8_t WsaBufferCount;

        //
        // Contains all the datagram buffers to pass to the socket.
        //
        WSABUF WsaBuffers[CXPLAT_MAX_BATCH_SEND];

        //
        // The WSABUF returned to the client for segmented sends.
        //
        WSABUF ClientBuffer;

        //
        // The RIO buffer ID, or RIO_INVALID_BUFFERID if not registered.
        //
        RIO_BUFFERID RioBufferId;

        //
        // The RIO send overflow entry. Used when the RIO send RQ is full.
        //
        CXPLAT_LIST_ENTRY RioOverflowEntry;

        //
        // The buffer for send control data.
        //
        char CtrlBuf[
            RIO_CMSG_BASE_SIZE +
            WSA_CMSG_SPACE(sizeof(IN6_PKTINFO)) +   // IP_PKTINFO
            WSA_CMSG_SPACE(sizeof(INT)) +           // IP_ECN
            WSA_CMSG_SPACE(sizeof(DWORD))           // UDP_SEND_MSG_SIZE
            ];

        //
        // The local address to bind to.
        //
        QUIC_ADDR LocalAddress;

        //
        // The V6-mapped remote address to send to.
        //
        QUIC_ADDR MappedRemoteAddress;
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

        static ulong CxPlatDataPathInitialize(CXPLAT_UDP_DATAPATH_CALLBACKS UdpCallbacks,
            CXPLAT_TCP_DATAPATH_CALLBACKS TcpCallbacks, CXPLAT_WORKER_POOL WorkerPool,
            QUIC_EXECUTION_CONFIG Config, CXPLAT_DATAPATH NewDataPath)
        {
            ulong Status = QUIC_STATUS_SUCCESS;
            if (NewDataPath == null)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Error;
            }

            Status = DataPathInitialize(UdpCallbacks, TcpCallbacks, WorkerPool, Config, NewDataPath);
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            if (Config != null && Config.Flags & QUIC_EXECUTION_CONFIG_FLAG_XDP)
            {
                Status = RawDataPathInitialize(ClientRecvContextLength, Config, NewDataPath, WorkerPool, NewDataPath.RawDataPath);
                if (QUIC_FAILED(Status))
                {
                    NewDataPath.RawDataPath = null;
                    CxPlatDataPathUninitialize(NewDataPath);
                    NewDataPath = null;
                }
            }
        Error:
            return Status;
        }

        static void QuicCopyRouteInfo(CXPLAT_ROUTE DstRoute, CXPLAT_ROUTE SrcRoute)
        {
            if (SrcRoute.DatapathType == CXPLAT_DATAPATH_TYPE.CXPLAT_DATAPATH_TYPE_RAW)
            {
                DstRoute.CopyFrom(SrcRoute);
                CxPlatUpdateRoute(DstRoute, SrcRoute);
            }
            else if (SrcRoute.DatapathType == CXPLAT_DATAPATH_TYPE.CXPLAT_DATAPATH_TYPE_NORMAL)
            {
                DstRoute = SrcRoute;
            }
            else
            {
                NetLog.Assert(false);
            }
        }

        static void CxPlatUpdateRoute(CXPLAT_ROUTE DstRoute, CXPLAT_ROUTE SrcRoute)
        {
            if (SrcRoute.DatapathType == CXPLAT_DATAPATH_TYPE.CXPLAT_DATAPATH_TYPE_RAW)
            {
                RawUpdateRoute(DstRoute, SrcRoute);
            }

            if (DstRoute.DatapathType != SrcRoute.DatapathType ||
                (DstRoute.State == CXPLAT_ROUTE_STATE.RouteResolved &&
                 DstRoute.Queue != SrcRoute.Queue))
            {
                DstRoute.Queue = SrcRoute.Queue;
                DstRoute.DatapathType = SrcRoute.DatapathType;
            }
        }

        static void CxPlatRecvDataReturn(CXPLAT_RECV_DATA RecvDataChain)
        {
            if (RecvDataChain == null)
            {
                return;
            }

            NetLog.Assert(RecvDataChain.DatapathType == CXPLAT_DATAPATH_TYPE.CXPLAT_DATAPATH_TYPE_NORMAL ||
                RecvDataChain.DatapathType == CXPLAT_DATAPATH_TYPE.CXPLAT_DATAPATH_TYPE_RAW);

            RecvDataChain.DatapathType == CXPLAT_DATAPATH_TYPE.CXPLAT_DATAPATH_TYPE_NORMAL ? RecvDataReturn(RecvDataChain) : RawRecvDataReturn(RecvDataChain);
        }

        static void RecvDataReturn(CXPLAT_RECV_DATA RecvDataChain)
        {
            long BatchedBufferCount = 0;
            DATAPATH_RX_IO_BLOCK BatchIoBlock = null;
            CXPLAT_RECV_DATA Datagram;
            while ((Datagram = RecvDataChain) != null)
            {
                RecvDataChain = RecvDataChain.Next;

                DATAPATH_RX_IO_BLOCK IoBlock = CXPLAT_CONTAINING_RECORD<DATAPATH_RX_PACKET>(Datagram, DATAPATH_RX_PACKET, Data).IoBlock;

                if (BatchIoBlock == IoBlock)
                {
                    BatchedBufferCount++;
                }
                else
                {
                    if (BatchIoBlock != null && Interlocked.Add(BatchIoBlock.ReferenceCount, -BatchedBufferCount) == 0)
                    {
                        CxPlatSocketFreeRxIoBlock(BatchIoBlock);
                    }

                    BatchIoBlock = IoBlock;
                    BatchedBufferCount = 1;
                }
            }

            if (BatchIoBlock != null && Interlocked.Add(BatchIoBlock.ReferenceCount, -BatchedBufferCount) == 0)
            {
                CxPlatSocketFreeRxIoBlock(BatchIoBlock);
            }
        }

        static ulong CxPlatResolveRoute(CXPLAT_ROUTE Route)
        {
            Route.State = CXPLAT_ROUTE_STATE.RouteResolved;
            return QUIC_STATUS_SUCCESS;
        }

        static CXPLAT_SEND_DATA CxPlatSendDataAlloc(CXPLAT_SOCKET Socket,CXPLAT_SEND_CONFIG Config)
        {
            CXPLAT_SEND_DATA SendData = null;
            if (Socket->UseTcp || Config->Route->DatapathType == CXPLAT_DATAPATH_TYPE_RAW ||
                (Config->Route->DatapathType == CXPLAT_DATAPATH_TYPE_UNKNOWN &&
                Socket->RawSocketAvailable && !IS_LOOPBACK(Config->Route->RemoteAddress)))
            {
                SendData = RawSendDataAlloc(CxPlatSocketToRaw(Socket), Config);
            }
            else
            {
                SendData = SendDataAlloc(Socket, Config);
            }
            return SendData;
        }

        static CXPLAT_SEND_DATA SendDataAlloc(CXPLAT_SOCKET Socket, CXPLAT_SEND_CONFIG Config)
        {
            NetLog.Assert(Socket != null);

            if (Config.Route.Queue == null)
            {
                Config.Route.Queue = Socket.PerProcSockets[0];
            }

            CXPLAT_SOCKET_PROC SocketProc = Config.Route.Queue;
            CXPLAT_DATAPATH_PARTITION DatapathProc = SocketProc.DatapathProc;
            CXPLAT_POOL SendDataPool = Socket.UseRio ? &DatapathProc->RioSendDataPool : &DatapathProc->SendDataPool;

            CXPLAT_SEND_DATA* SendData = CxPlatPoolAlloc(SendDataPool);

            if (SendData != NULL)
            {
                SendData->Owner = DatapathProc;
                SendData->SendDataPool = SendDataPool;
                SendData->ECN = Config->ECN;
                SendData->SendFlags = Config->Flags;
                SendData->SegmentSize =
                    (Socket->Type != CXPLAT_SOCKET_UDP ||
                     Socket->Datapath->Features & CXPLAT_DATAPATH_FEATURE_SEND_SEGMENTATION)
                        ? Config->MaxPacketSize : 0;
                SendData->TotalSize = 0;
                SendData->WsaBufferCount = 0;
                SendData->ClientBuffer.len = 0;
                SendData->ClientBuffer.buf = NULL;
                SendData->DatapathType = Config->Route->DatapathType = CXPLAT_DATAPATH_TYPE_USER;
#if DEBUG
                SendData->Sqe.IoType = 0;
#endif

                if (Socket->UseRio)
                {
                    SendData->BufferPool =
                        SendData->SegmentSize > 0 ?
                            &DatapathProc->RioLargeSendBufferPool :
                            &DatapathProc->RioSendBufferPool;
                }
                else
                {
                    SendData->BufferPool =
                        SendData->SegmentSize > 0 ?
                            &DatapathProc->LargeSendBufferPool :
                            &DatapathProc->SendBufferPool;
                }
            }

            return SendData;
        }
    }
}
