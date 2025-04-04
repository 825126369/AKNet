using AKNet.Common;
using System;
using System.Data;
using System.IO;
using System.Threading;

namespace AKNet.Udp5Quic.Common
{
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

    internal class CXPLAT_SEND_DATA_COMMON
    {
        public int DatapathType;
        public byte ECN;
    }

    public class CXPLAT_SEND_DATA : CXPLAT_SEND_DATA_COMMON
    {
        CXPLAT_SOCKET_PROC SocketProc;
        DATAPATH_IO_SQE Sqe;

        //
        // The owning processor context.
        //
        CXPLAT_DATAPATH_PARTITION* Owner;

        //
        // The pool for this send data.
        //
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
            else if (SrcRoute.DatapathType ==  CXPLAT_DATAPATH_TYPE.CXPLAT_DATAPATH_TYPE_NORMAL)
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
                RecvDataChain.DatapathType ==  CXPLAT_DATAPATH_TYPE.CXPLAT_DATAPATH_TYPE_RAW);

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
    }
}
