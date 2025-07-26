using AKNet.Common;
using AKNet.Platform;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace AKNet.Udp1MSQuic.Common
{
    internal delegate void CXPLAT_DATAPATH_ACCEPT_CALLBACK(CXPLAT_SOCKET Socket, QUIC_BINDING Context, CXPLAT_RECV_DATA RecvDataChain);
    internal delegate void CXPLAT_DATAPATH_CONNECT_CALLBACK(CXPLAT_SOCKET Socket, QUIC_BINDING Context, CXPLAT_RECV_DATA RecvDataChain);
    internal delegate void CXPLAT_DATAPATH_RECEIVE_CALLBACK(CXPLAT_SOCKET Socket, object Context, CXPLAT_RECV_DATA RecvDataChain);
    internal delegate void CXPLAT_DATAPATH_SEND_COMPLETE_CALLBACK(CXPLAT_SOCKET Socket, object Context, CXPLAT_RECV_DATA RecvDataChain);
    internal delegate void CXPLAT_DATAPATH_UNREACHABLE_CALLBACK(CXPLAT_SOCKET Socket, object Context, QUIC_ADDR RemoteAddress);

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

    internal enum CXPLAT_SEND_FLAGS
    {
        CXPLAT_SEND_FLAGS_NONE = 0,
        CXPLAT_SEND_FLAGS_MAX_THROUGHPUT = 1,
    }

    internal class CXPLAT_RAW_TCP_STATE
    {
        public bool Syncd;
        public uint AckNumber;
        public uint SequenceNumber;
    }

    internal class CXPLAT_SOCKET_POOL
    {
        public readonly object Lock = new object();
        public readonly Dictionary<ushort, CXPLAT_SOCKET> Sockets = new Dictionary<ushort, CXPLAT_SOCKET>();
    }

    internal class CXPLAT_ROUTE_RESOLUTION_WORKER
    {
        public bool Enabled;
        public EventWaitHandle Ready;
        public Thread Thread;
        public CXPLAT_POOL<CXPLAT_ROUTE_RESOLUTION_OPERATION> OperationPool;
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
        public CXPLAT_SOCKET_PROC Queue;
        public CXPLAT_DATAPATH_TYPE DatapathType;
        public CXPLAT_ROUTE_STATE State;
        public QUIC_ADDR RemoteAddress = new QUIC_ADDR();
        public QUIC_ADDR LocalAddress = new QUIC_ADDR();

        public void CopyFrom(CXPLAT_ROUTE other)
        {
            this.Queue = other.Queue;
            this.RemoteAddress.CopyFrom(other.RemoteAddress);
            this.LocalAddress.CopyFrom(other.LocalAddress);
            DatapathType = other.DatapathType;
            State = other.State;
        }
    }

    internal class CXPLAT_UDP_CONFIG
    {
        public QUIC_ADDR LocalAddress;      // optional
        public QUIC_ADDR RemoteAddress;     // optional
        public uint Flags;                     // CXPLAT_SOCKET_FLAG_*
        public int InterfaceIndex;            // 0 means any/all
        public int PartitionIndex;            // Client-only
        public object CallbackContext;              // optional

        public int CibirIdLength;              // CIBIR ID length. Value of 0 indicates CIBIR isn't used
        public int CibirIdOffsetSrc;           // CIBIR ID offset in source CID
        public int CibirIdOffsetDst;           // CIBIR ID offset in destination CID
        public readonly byte[] CibirId = new byte[6];                 // CIBIR ID data
    }

    internal class CXPLAT_RECV_DATA
    {
        public DATAPATH_RX_PACKET CXPLAT_CONTAINING_RECORD;

        public CXPLAT_RECV_DATA Next;
        public CXPLAT_ROUTE Route;
        public readonly QUIC_BUFFER Buffer = new QUIC_BUFFER();
        public int PartitionIndex;
        public byte TypeOfService;
        public byte HopLimitTTL;
        public bool Allocated;          // Used for debugging. Set to FALSE on free.
        public bool QueuedOnConnection; // Used for debugging.
        public CXPLAT_DATAPATH_TYPE DatapathType;       // CXPLAT_DATAPATH_TYPE
        public ushort Reserved;           // PACKET_TYPE (at least 3 bits)
        public ushort ReservedEx;         // Header length

        public virtual void Reset()
        {
            Next = null;
            Route = null;
            Buffer.Reset();
            PartitionIndex = 0;
            TypeOfService = 0;
            HopLimitTTL = 0;
            Allocated = false;
            QueuedOnConnection = false;
            DatapathType = CXPLAT_DATAPATH_TYPE.CXPLAT_DATAPATH_TYPE_UNKNOWN; // Default to user datapath
            Reserved = 0;
            ReservedEx = 0;
        }
    }

    internal class CXPLAT_QEO_CONNECTION
    {
        public CXPLAT_QEO_OPERATION Operation;  // CXPLAT_QEO_OPERATION
        public CXPLAT_QEO_DIRECTION Direction;  // CXPLAT_QEO_DIRECTION
        public CXPLAT_QEO_DECRYPT_FAILURE_ACTION DecryptFailureAction;  // CXPLAT_QEO_DECRYPT_FAILURE_ACTION
        public bool KeyPhase;
        public uint RESERVED; // Must be set to 0. Don't read.
        public CXPLAT_QEO_CIPHER_TYPE CipherType; // CXPLAT_QEO_CIPHER_TYPE
        public ulong NextPacketNumber;
        public QUIC_ADDR Address;
        public int ConnectionIdLength;
        public byte[] ConnectionId = new byte[20]; // QUIC v1 and v2 max CID size
        public byte[] PayloadKey = new byte[32];   // Length determined by CipherType
        public byte[] HeaderKey = new byte[32];    // Length determined by CipherType
        public byte[] PayloadIv = new byte[12];
    }

    internal class CXPLAT_SEND_CONFIG
    {
        public CXPLAT_ROUTE Route;
        public int MaxPacketSize;
        public CXPLAT_ECN_TYPE ECN; // CXPLAT_ECN_TYPE
        public byte Flags; // CXPLAT_SEND_FLAGS
        public CXPLAT_DSCP_TYPE DSCP; // 它用于在网络中实现差异化服务（Differentiated Services），即为数据包打上不同的优先级标签，以便网络设备（如路由器、交换机）进行优先转发；
    }

    internal enum CXPLAT_ECN_TYPE
    {
        CXPLAT_ECN_NON_ECT = 0x0, // Non ECN-Capable Transport, Non-ECT
        CXPLAT_ECN_ECT_1 = 0x1, // ECN Capable Transport, ECT(1)
        CXPLAT_ECN_ECT_0 = 0x2, // ECN Capable Transport, ECT(0)
        CXPLAT_ECN_CE = 0x3  // Congestion Encountered, CE
    }

    internal enum CXPLAT_DSCP_TYPE
    {
        CXPLAT_DSCP_CS0 = 0,
        CXPLAT_DSCP_LE = 1,
        CXPLAT_DSCP_CS1 = 8,
        CXPLAT_DSCP_CS2 = 16,
        CXPLAT_DSCP_CS3 = 24,
        CXPLAT_DSCP_CS4 = 32,
        CXPLAT_DSCP_CS5 = 40,
        CXPLAT_DSCP_EF = 46,
    }

    internal enum CXPLAT_DATAPATH_FEATURES
    {
        CXPLAT_DATAPATH_FEATURE_NONE = 0x00000000,
        CXPLAT_DATAPATH_FEATURE_RECV_SIDE_SCALING = 0x00000001,
        CXPLAT_DATAPATH_FEATURE_RECV_COALESCING = 0x00000002,
        CXPLAT_DATAPATH_FEATURE_SEND_SEGMENTATION = 0x00000004,
        CXPLAT_DATAPATH_FEATURE_LOCAL_PORT_SHARING = 0x00000008,
        CXPLAT_DATAPATH_FEATURE_PORT_RESERVATIONS = 0x00000010,
        CXPLAT_DATAPATH_FEATURE_TCP = 0x00000020,
        CXPLAT_DATAPATH_FEATURE_RAW = 0x00000040,
        CXPLAT_DATAPATH_FEATURE_TTL = 0x00000080,
        CXPLAT_DATAPATH_FEATURE_SEND_DSCP = 0x00000100,
        CXPLAT_DATAPATH_FEATURE_RIO = 0x00000200,
        CXPLAT_DATAPATH_FEATURE_RECV_DSCP = 0x00000400,
    }
    
    internal class CXPLAT_SEND_DATA_COMMON
    {
        public CXPLAT_DATAPATH_TYPE DatapathType;
        public byte ECN;
        public byte DSCP;
    }


    //应用程序数据 → ClientBuffer → 分割为多个片段 → WsaBuffers[0..n] → 网络发送
    internal unsafe class CXPLAT_SEND_DATA : CXPLAT_SEND_DATA_COMMON, CXPLAT_POOL_Interface<CXPLAT_SEND_DATA>
    {
        public CXPLAT_POOL<CXPLAT_SEND_DATA> mPool = null;
        public readonly CXPLAT_POOL_ENTRY<CXPLAT_SEND_DATA> POOL_ENTRY = null;

        public CXPLAT_DATAPATH_PROC Owner = null;
        public CXPLAT_SOCKET_PROC SocketProc;
        public CXPLAT_POOL<CXPLAT_SEND_DATA> SendDataPool;
        public CXPLAT_POOL<QUIC_Pool_BUFFER> BufferPool;
        public int TotalSize;
        public int SegmentSize; //是否分区，如果为0，则不分区
        public byte SendFlags;
        public List<QUIC_Pool_BUFFER> WsaBuffers = new List<QUIC_Pool_BUFFER>();
        public readonly QUIC_Pool_BUFFER ClientBuffer = new QUIC_Pool_BUFFER();
        public QUIC_ADDR LocalAddress;
        public QUIC_ADDR MappedRemoteAddress;

        public SSocketAsyncEventArgs Sqe = null;
        public CXPLAT_SEND_DATA()
        {
            POOL_ENTRY = new CXPLAT_POOL_ENTRY<CXPLAT_SEND_DATA>(this);
        }

        public CXPLAT_POOL_ENTRY<CXPLAT_SEND_DATA> GetEntry()
        {
            return POOL_ENTRY;
        }

        public void Reset()
        {
            
        }

        public void SetPool(CXPLAT_POOL<CXPLAT_SEND_DATA> mPool)
        {
            this.mPool = mPool;
        }

        public CXPLAT_POOL<CXPLAT_SEND_DATA> GetPool()
        {
            return this.mPool;
        }
    }

    internal delegate void CXPLAT_ROUTE_RESOLUTION_CALLBACK (object Context, byte[] PhysicalAddress, int PathId, bool Succeeded);

    internal class CXPLAT_ROUTE_RESOLUTION_OPERATION:CXPLAT_POOL_Interface<CXPLAT_ROUTE_RESOLUTION_OPERATION>
    {
        public CXPLAT_POOL<CXPLAT_ROUTE_RESOLUTION_OPERATION> mPool = null;
        public readonly CXPLAT_POOL_ENTRY<CXPLAT_ROUTE_RESOLUTION_OPERATION> POOL_ENTRY = null;
        public readonly CXPLAT_LIST_ENTRY WorkerLink;
        public object Context;
        public int PathId;
        public CXPLAT_ROUTE_RESOLUTION_CALLBACK Callback;

        public CXPLAT_ROUTE_RESOLUTION_OPERATION()
        {
            POOL_ENTRY = new CXPLAT_POOL_ENTRY<CXPLAT_ROUTE_RESOLUTION_OPERATION>(this);
            WorkerLink = new CXPLAT_LIST_ENTRY<CXPLAT_ROUTE_RESOLUTION_OPERATION>(this);
        }

        public CXPLAT_POOL_ENTRY<CXPLAT_ROUTE_RESOLUTION_OPERATION> GetEntry()
        {
            return POOL_ENTRY;
        }

        public CXPLAT_POOL<CXPLAT_ROUTE_RESOLUTION_OPERATION> GetPool()
        {
            return this.mPool;
        }

        public void Reset()
        {
            
        }

        public void SetPool(CXPLAT_POOL<CXPLAT_ROUTE_RESOLUTION_OPERATION> mPool)
        {
            this.mPool = mPool;
        }
    }

    internal static partial class MSQuicFunc
    {
        //给发送数据分配Buffer
        static QUIC_Pool_BUFFER CxPlatSendDataAllocBuffer(CXPLAT_SEND_DATA SendData, int MaxBufferLength)
        {
            NetLog.Assert(DatapathType(SendData) == CXPLAT_DATAPATH_TYPE.CXPLAT_DATAPATH_TYPE_NORMAL ||
                DatapathType(SendData) == CXPLAT_DATAPATH_TYPE.CXPLAT_DATAPATH_TYPE_RAW);

            return SendDataAllocBuffer(SendData, MaxBufferLength);
        }

        //SendDAta 分配一个数据报
        static QUIC_Pool_BUFFER SendDataAllocBuffer(CXPLAT_SEND_DATA SendData, int MaxBufferLength)
        {
            NetLog.Assert(SendData != null);
            NetLog.Assert(MaxBufferLength > 0);

            CxPlatSendDataFinalizeSendBuffer(SendData);
            if (!CxPlatSendDataCanAllocSend(SendData, MaxBufferLength))
            {
                return null;
            }

            if (SendData.SegmentSize == 0) //不分区
            {
                return CxPlatSendDataAllocPacketBuffer(SendData, MaxBufferLength);
            }
            else
            {
                return CxPlatSendDataAllocSegmentBuffer(SendData, MaxBufferLength);
            }
        }

        static void CxPlatSendDataFinalizeSendBuffer(CXPLAT_SEND_DATA SendData)
        {
            if (SendData.ClientBuffer.Length == 0)
            {
                if (SendData.WsaBuffers.Count > 0)
                {
                    NetLog.Assert(SendData.WsaBuffers[SendData.WsaBuffers.Count - 1].Length < ushort.MaxValue);
                    SendData.TotalSize += SendData.WsaBuffers[SendData.WsaBuffers.Count - 1].Length;
                }
                return;
            }

            NetLog.Assert(SendData.SegmentSize > 0 && SendData.WsaBuffers.Count > 0);
            NetLog.Assert(SendData.ClientBuffer.Length > 0 && SendData.ClientBuffer.Length <= SendData.SegmentSize);
            NetLog.Assert(CxPlatSendDataCanAllocSendSegment(SendData, 0));

            SendData.WsaBuffers[SendData.WsaBuffers.Count - 1].Length += SendData.ClientBuffer.Length;
            SendData.TotalSize += SendData.ClientBuffer.Length;

            if (SendData.ClientBuffer.Length == SendData.SegmentSize)
            {
                SendData.ClientBuffer.Offset = SendData.SegmentSize;
                SendData.ClientBuffer.Length = 0;
            }
            else
            {
                SendData.ClientBuffer.Buffer = null;
                SendData.ClientBuffer.Length = 0;
            }
        }

        static QUIC_Pool_BUFFER CxPlatSendDataAllocPacketBuffer(CXPLAT_SEND_DATA SendData, int MaxBufferLength)
        {
            QUIC_Pool_BUFFER WsaBuffer = CxPlatSendDataAllocDataBuffer(SendData);
            if (WsaBuffer != null)
            {
                WsaBuffer.Length = MaxBufferLength;
            }
            return WsaBuffer;
        }

        static QUIC_Pool_BUFFER CxPlatSendDataAllocSegmentBuffer(CXPLAT_SEND_DATA SendData, int MaxBufferLength)
        {
            NetLog.Assert(SendData.SegmentSize > 0);
            NetLog.Assert(MaxBufferLength <= SendData.SegmentSize);

            if (CxPlatSendDataCanAllocSendSegment(SendData, MaxBufferLength))
            {
                SendData.ClientBuffer.Length = MaxBufferLength;
                return SendData.ClientBuffer;
            }

            QUIC_Pool_BUFFER WsaBuffer = CxPlatSendDataAllocDataBuffer(SendData);
            if (WsaBuffer == null)
            {
                return null;
            }

            WsaBuffer.Length = 0;
            SendData.ClientBuffer.Buffer = WsaBuffer.Buffer;
            SendData.ClientBuffer.Length = MaxBufferLength;
            return SendData.ClientBuffer;
        }

        static bool CxPlatSendDataCanAllocSendSegment(CXPLAT_SEND_DATA SendData, int MaxBufferLength)
        {
            if (SendData.ClientBuffer.Buffer == null)
            {
                return false;
            }

            NetLog.Assert(SendData.SegmentSize > 0);
            NetLog.Assert(SendData.WsaBuffers.Count > 0);
            int BytesAvailable = CXPLAT_LARGE_SEND_BUFFER_SIZE - SendData.WsaBuffers[SendData.WsaBuffers.Count - 1].Length - SendData.ClientBuffer.Length;
            return MaxBufferLength <= BytesAvailable;
        }

        static bool CxPlatSendDataCanAllocSend(CXPLAT_SEND_DATA SendData, int MaxBufferLength)
        {
            return (SendData.WsaBuffers.Count < SendData.Owner.Datapath.MaxSendBatchSize) ||
                ((SendData.SegmentSize > 0) && CxPlatSendDataCanAllocSendSegment(SendData, MaxBufferLength));
        }

        static QUIC_Pool_BUFFER CxPlatSendDataAllocDataBuffer(CXPLAT_SEND_DATA SendData)
        {
            NetLog.Assert(SendData.WsaBuffers.Count < SendData.Owner.Datapath.MaxSendBatchSize);
            QUIC_Pool_BUFFER WsaBuffer = SendData.BufferPool.CxPlatPoolAlloc();
            if (WsaBuffer.Buffer == null)
            {
                return null;
            }

            SendData.WsaBuffers.Add(WsaBuffer);
            return WsaBuffer;
        }

        static ushort MaxUdpPayloadSizeForFamily(AddressFamily Family, ushort Mtu)
        {
            return Family == AddressFamily.InterNetwork ?
                (ushort)(Mtu - CXPLAT_MIN_IPV4_HEADER_SIZE - CXPLAT_UDP_HEADER_SIZE) :
                (ushort)(Mtu - CXPLAT_MIN_IPV6_HEADER_SIZE - CXPLAT_UDP_HEADER_SIZE);
        }

        static ushort PacketSizeFromUdpPayloadSize(AddressFamily Family, int UdpPayloadSize)
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

        static int CxPlatDataPathInitialize(CXPLAT_UDP_DATAPATH_CALLBACKS UdpCallbacks, CXPLAT_WORKER_POOL WorkerPool, out CXPLAT_DATAPATH NewDataPath)
        {
            int Status = QUIC_STATUS_SUCCESS;
            Status = DataPathInitialize(UdpCallbacks, WorkerPool, out NewDataPath);
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }
        Error:
            return Status;
        }

        static void QuicCopyRouteInfo(CXPLAT_ROUTE DstRoute, CXPLAT_ROUTE SrcRoute)
        {
            if (SrcRoute.DatapathType == CXPLAT_DATAPATH_TYPE.CXPLAT_DATAPATH_TYPE_NORMAL)
            {
                DstRoute.CopyFrom(SrcRoute);
            }
            else
            {
                NetLog.Assert(false);
            }
        }

        static void CxPlatUpdateRoute(CXPLAT_ROUTE DstRoute, CXPLAT_ROUTE SrcRoute)
        {
            if (DstRoute.DatapathType != SrcRoute.DatapathType || (DstRoute.State == CXPLAT_ROUTE_STATE.RouteResolved && DstRoute.Queue != SrcRoute.Queue))
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

            RecvDataReturn(RecvDataChain);
        }

        static void RecvDataReturn(CXPLAT_RECV_DATA RecvDataChain)
        {
            long BatchedBufferCount = 0;
            DATAPATH_RX_IO_BLOCK BatchIoBlock = null;
            CXPLAT_RECV_DATA Datagram;
            while ((Datagram = RecvDataChain) != null)
            {
                RecvDataChain = RecvDataChain.Next;
                DATAPATH_RX_IO_BLOCK IoBlock = Datagram.CXPLAT_CONTAINING_RECORD.IoBlock;

                if (BatchIoBlock == IoBlock)
                {
                    BatchedBufferCount++;
                }
                else
                {
                    if (BatchIoBlock != null && Interlocked.Add(ref BatchIoBlock.ReferenceCount, -BatchedBufferCount) == 0)
                    {
                        CxPlatSocketFreeRxIoBlock(BatchIoBlock);
                    }

                    BatchIoBlock = IoBlock;
                    BatchedBufferCount = 1;
                }
            }

            if (BatchIoBlock != null && Interlocked.Add(ref BatchIoBlock.ReferenceCount, -BatchedBufferCount) == 0)
            {
                CxPlatSocketFreeRxIoBlock(BatchIoBlock);
            }
        }

        static int CxPlatResolveRoute(CXPLAT_ROUTE Route)
        {
            Route.State = CXPLAT_ROUTE_STATE.RouteResolved;
            return QUIC_STATUS_SUCCESS;
        }

        static CXPLAT_SEND_DATA CxPlatSendDataAlloc(CXPLAT_SOCKET Socket, CXPLAT_SEND_CONFIG Config)
        {
            CXPLAT_SEND_DATA SendData = SendDataAlloc(Socket, Config);
            return SendData;
        }

        static void CxPlatSocketSend(CXPLAT_SOCKET Socket,CXPLAT_ROUTE Route,CXPLAT_SEND_DATA SendData)
        {
            SocketSend(Socket, Route, SendData);
        }

        static int CxPlatSocketCreateUdp(CXPLAT_DATAPATH Datapath, CXPLAT_UDP_CONFIG Config, out CXPLAT_SOCKET NewSocket)
        {
            int Status = QUIC_STATUS_SUCCESS;
            Status = SocketCreateUdp(Datapath, Config, out NewSocket);
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            NewSocket.RawSocketAvailable = false;
        Error:
            return Status;
        }

        static unsafe void SocketSend(CXPLAT_SOCKET Socket, CXPLAT_ROUTE Route, CXPLAT_SEND_DATA SendData)
        {
            CXPLAT_SOCKET_PROC SocketProc = Route.Queue;
            SendData.SocketProc = SocketProc;
            CxPlatSendDataFinalizeSendBuffer(SendData);
            SendData.MappedRemoteAddress = Route.RemoteAddress;

            if (!BoolOk(SendData.SendFlags & CXPLAT_SEND_FLAGS_MAX_THROUGHPUT))
            {
                CxPlatSocketSendInline(Route.LocalAddress, SendData);
            }
            else
            {
                CxPlatSocketSendEnqueue(Route, SendData);
            }
        }

        static void CxPlatSocketDelete(CXPLAT_SOCKET Socket)
        {
            SocketDelete(Socket);
        }

        static void SocketDelete(CXPLAT_SOCKET Socket)
        {
            NetLog.Assert(Socket != null);
            NetLog.Assert(!Socket.Uninitialized);
            Socket.Uninitialized = true;

            int SocketCount = Socket.NumPerProcessorSockets > 0 ? CxPlatProcCount() : 1;
            for (int i = 0; i < SocketCount; ++i)
            {
                Socket.PerProcSockets[i] = null;
            }
        }

        static bool CxPlatDataPathIsPaddingPreferred(CXPLAT_DATAPATH Datapath, CXPLAT_SEND_DATA SendData)
        {
            NetLog.Assert(DatapathType(SendData) == CXPLAT_DATAPATH_TYPE.CXPLAT_DATAPATH_TYPE_NORMAL || DatapathType(SendData) == CXPLAT_DATAPATH_TYPE.CXPLAT_DATAPATH_TYPE_RAW);
            return DatapathType(SendData) == CXPLAT_DATAPATH_TYPE.CXPLAT_DATAPATH_TYPE_NORMAL ? DataPathIsPaddingPreferred(Datapath) : RawDataPathIsPaddingPreferred(Datapath);
        }

        static bool DataPathIsPaddingPreferred(CXPLAT_DATAPATH Datapath)
        {
            return BoolOk(Datapath.Features & CXPLAT_DATAPATH_FEATURE_SEND_SEGMENTATION);
        }

        static bool RawDataPathIsPaddingPreferred(CXPLAT_DATAPATH Datapath)
        {
            return false;
        }

        static uint CxPlatDataPathGetSupportedFeatures(CXPLAT_DATAPATH Datapath)
        {
            return DataPathGetSupportedFeatures(Datapath);
        }

        static uint DataPathGetSupportedFeatures(CXPLAT_DATAPATH Datapath)
        {
            return Datapath.Features;
        }

        static void CxPlatSocketGetLocalAddress(CXPLAT_SOCKET Socket, out QUIC_ADDR Address)
        {
            NetLog.Assert(Socket != null);
            Address = Socket.LocalAddress;
        }

        static void CxPlatSocketGetRemoteAddress(CXPLAT_SOCKET Socket, out QUIC_ADDR Address)
        {
            NetLog.Assert(Socket != null);
            Address = Socket.RemoteAddress;
        }

        static CXPLAT_ECN_TYPE CXPLAT_ECN_FROM_TOS(byte ToS)
        {
            return (CXPLAT_ECN_TYPE)((ToS) & 0x3);
        }

        static void CxPlatSendDataFree(CXPLAT_SEND_DATA SendData)
        {
            SendDataFree(SendData);
        }

        static bool CxPlatSendDataIsFull(CXPLAT_SEND_DATA SendData)
        {
             return SendDataIsFull(SendData);
        }

        static bool SendDataIsFull(CXPLAT_SEND_DATA SendData)
        {
            return !CxPlatSendDataCanAllocSend(SendData, SendData.SegmentSize);
        }

        static void CxPlatDataPathQueryRssScalabilityInfo(CXPLAT_DATAPATH Datapath)
        {
           
        }

        static int MaxUdpPayloadSizeFromMTU(ushort Mtu)
        {
            return Mtu - CXPLAT_MIN_IPV4_HEADER_SIZE - CXPLAT_UDP_HEADER_SIZE;
        }
        
        static void CxPlatResolveRouteComplete(object Context, CXPLAT_ROUTE Route, byte[] PhysicalAddress, byte PathId)
        {
            NetLog.Assert(Route.DatapathType !=  CXPLAT_DATAPATH_TYPE.CXPLAT_DATAPATH_TYPE_NORMAL);
            if (Route.State !=  CXPLAT_ROUTE_STATE.RouteResolved) 
            {
              
            }
        }

        static void CxPlatSendDataFreeBuffer(CXPLAT_SEND_DATA SendData, QUIC_Pool_BUFFER Buffer)
        {
            SendDataFreeBuffer(SendData, Buffer);
        }

        static void SendDataFreeBuffer(CXPLAT_SEND_DATA SendData, QUIC_Pool_BUFFER Buffer)
        {
            QUIC_BUFFER TailBuffer = SendData.WsaBuffers[SendData.WsaBuffers.Count - 1];

            if (SendData.SegmentSize == 0)
            {
                NetLog.Assert(Buffer == TailBuffer);
                SendData.BufferPool.CxPlatPoolFree(Buffer);
                SendData.WsaBuffers.Remove(Buffer);
            }
            else
            {
                if (TailBuffer.Length == 0)
                {
                    SendData.BufferPool.CxPlatPoolFree(Buffer);
                    SendData.WsaBuffers.Remove(Buffer);
                }

                SendData.ClientBuffer.Buffer = null;
                SendData.ClientBuffer.Length = 0;
            }
        }

        static void CxPlatDataPathRelease(CXPLAT_DATAPATH Datapath)
        {
            if (CxPlatRefDecrement(ref Datapath.RefCount))
            {
                NetLog.Assert(!Datapath.Freed);
                NetLog.Assert(Datapath.Uninitialized);
                Datapath.Freed = true;
                CxPlatWorkerPoolRelease(Datapath.WorkerPool);
            }
        }

        static void CxPlatDataPathUninitialize(CXPLAT_DATAPATH Datapath)
        {
            DataPathUninitialize(Datapath);
        }

        static void CxPlatProcessorContextRelease(CXPLAT_DATAPATH_PROC DatapathProc)
        {
            if (CxPlatRefDecrement(ref DatapathProc.RefCount))
            {
                NetLog.Assert(!DatapathProc.Uninitialized);
                DatapathProc.Uninitialized = true;
                DatapathProc.SendDataPool.CxPlatPoolUninitialize();
                DatapathProc.SendBufferPool.CxPlatPoolUninitialize();
                DatapathProc.LargeSendBufferPool.CxPlatPoolUninitialize();
                DatapathProc.RecvDatagramPool.CxPlatPoolUninitialize();
                DatapathProc.RecvDatagramPool.CxPlatPoolUninitialize();
                CxPlatDataPathRelease(DatapathProc.Datapath);
            }
        }

        static void DataPathUninitialize(CXPLAT_DATAPATH Datapath)
        {
            if (Datapath != null)
            {
                NetLog.Assert(!Datapath.Uninitialized);
                Datapath.Uninitialized = true;
                int PartitionCount = Datapath.PartitionCount;
                for (int i = 0; i < PartitionCount; i++)
                {
                    CxPlatProcessorContextRelease(Datapath.Partitions[i]);
                }
            }
        }

    }
}
