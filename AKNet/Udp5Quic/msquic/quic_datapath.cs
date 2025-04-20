using AKNet.Common;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace AKNet.Udp5Quic.Common
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
        public CXPLAT_SOCKET_PROC Queue;
        public QUIC_ADDR RemoteAddress;
        public QUIC_ADDR LocalAddress;
        public byte[] LocalLinkLayerAddress = new byte[6];
        public byte[] NextHopLinkLayerAddress = new byte[6];
        public CXPLAT_DATAPATH_TYPE DatapathType;
        public CXPLAT_ROUTE_STATE State;
        public CXPLAT_RAW_TCP_STATE TcpState;

        public void CopyFrom(CXPLAT_ROUTE other)
        {
            this.Queue = other.Queue;
            this.RemoteAddress = other.RemoteAddress;
            this.LocalAddress = other.LocalAddress;
            Array.Copy(other.LocalLinkLayerAddress, LocalLinkLayerAddress, LocalLinkLayerAddress.Length);
            Array.Copy(other.NextHopLinkLayerAddress, NextHopLinkLayerAddress, NextHopLinkLayerAddress.Length);
            DatapathType = other.DatapathType;
            State = other.State;
            TcpState = other.TcpState;
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
        public QUIC_BUFFER Buffer = new QUIC_BUFFER();
        public int PartitionIndex;
        public byte TypeOfService;
        public byte HopLimitTTL;
        public bool Allocated;          // Used for debugging. Set to FALSE on free.
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
    
    internal class CXPLAT_SEND_DATA_COMMON
    {
        public CXPLAT_DATAPATH_TYPE DatapathType;
        public byte ECN;
    }

    internal class CXPLAT_SEND_DATA : CXPLAT_SEND_DATA_COMMON, CXPLAT_POOL_Interface<CXPLAT_SEND_DATA>
    {
        public CXPLAT_SOCKET_PROC SocketProc;
        public CXPLAT_POOL<CXPLAT_SEND_DATA> SendDataPool = null;
        public CXPLAT_POOL<QUIC_BUFFER> BufferPool;
        public int TotalSize;
        public int SegmentSize;
        public byte SendFlags;
        public byte WsaBufferCount;
        public QUIC_BUFFER[] WsaBuffers = new QUIC_BUFFER[MSQuicFunc.CXPLAT_MAX_BATCH_SEND];
        public QUIC_BUFFER ClientBuffer;
        public CXPLAT_LIST_ENTRY RioOverflowEntry;

        //char CtrlBuf[
        //    RIO_CMSG_BASE_SIZE +
        //    WSA_CMSG_SPACE(sizeof(IN6_PKTINFO)) +   // IP_PKTINFO
        //    WSA_CMSG_SPACE(sizeof(INT)) +           // IP_ECN
        //    WSA_CMSG_SPACE(sizeof(DWORD))           // UDP_SEND_MSG_SIZE
        //    ];
        
        public QUIC_ADDR LocalAddress;
        public QUIC_ADDR MappedRemoteAddress;

        public CXPLAT_SEND_DATA()
        {
            SendDataPool = new CXPLAT_POOL<CXPLAT_SEND_DATA>();
        }

        public CXPLAT_POOL_ENTRY<CXPLAT_SEND_DATA> GetEntry()
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }
    }

    internal static partial class MSQuicFunc
    {
        static QUIC_BUFFER CxPlatSendDataAllocBuffer(CXPLAT_SEND_DATA SendData,int MaxBufferLength)
        {
            NetLog.Assert(DatapathType(SendData) == CXPLAT_DATAPATH_TYPE.CXPLAT_DATAPATH_TYPE_USER || 
                DatapathType(SendData) == CXPLAT_DATAPATH_TYPE.CXPLAT_DATAPATH_TYPE_RAW);

            return DatapathType(SendData) == CXPLAT_DATAPATH_TYPE.CXPLAT_DATAPATH_TYPE_USER ?
                SendDataAllocBuffer(SendData, MaxBufferLength) : RawSendDataAllocBuffer(SendData, MaxBufferLength);
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

            NetLog.Assert(RecvDataChain.DatapathType == CXPLAT_DATAPATH_TYPE.CXPLAT_DATAPATH_TYPE_USER ||
                RecvDataChain.DatapathType == CXPLAT_DATAPATH_TYPE.CXPLAT_DATAPATH_TYPE_RAW);

            if (RecvDataChain.DatapathType == CXPLAT_DATAPATH_TYPE.CXPLAT_DATAPATH_TYPE_USER)
            {
                RecvDataReturn(RecvDataChain);
            }
            else
            {
                RawRecvDataReturn(RecvDataChain);
            }
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

        static ulong CxPlatResolveRoute(CXPLAT_ROUTE Route)
        {
            Route.State = CXPLAT_ROUTE_STATE.RouteResolved;
            return QUIC_STATUS_SUCCESS;
        }

        static CXPLAT_SEND_DATA CxPlatSendDataAlloc(CXPLAT_SOCKET Socket,CXPLAT_SEND_CONFIG Config)
        {
            CXPLAT_SEND_DATA SendData = SendDataAlloc(Socket, Config);
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
            CXPLAT_DATAPATH_PROC DatapathProc = SocketProc.DatapathProc;
            CXPLAT_POOL<CXPLAT_SEND_DATA> SendDataPool = DatapathProc.SendDataPool;

            CXPLAT_SEND_DATA SendData = SendDataPool.CxPlatPoolAlloc();
            if (SendData != null)
            {
                SendData.Owner = DatapathProc;
                SendData.SendDataPool = SendDataPool;
                SendData.ECN = Config.ECN;
                SendData.SendFlags = Config.Flags;
                SendData.SegmentSize = 0;
                SendData.TotalSize = 0;
                SendData.WsaBufferCount = 0;
                SendData.ClientBuffer.len = 0;
                SendData.ClientBuffer.buf = null;
                SendData.DatapathType = Config.Route.DatapathType = CXPLAT_DATAPATH_TYPE.CXPLAT_DATAPATH_TYPE_USER;
                SendData.BufferPool = SendData.SegmentSize > 0 ? DatapathProc.LargeSendBufferPool : DatapathProc.SendBufferPool;
            }

            return SendData;
        }

        static QUIC_BUFFER SendDataAllocBuffer(CXPLAT_SEND_DATA SendData, int MaxBufferLength)
        {
            NetLog.Assert(SendData != null);
            NetLog.Assert(MaxBufferLength > 0);

            CxPlatSendDataFinalizeSendBuffer(SendData);
            if (!CxPlatSendDataCanAllocSend(SendData, MaxBufferLength))
            {
                return null;
            }

            if (SendData.SegmentSize == 0)
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
                if (SendData.WsaBufferCount > 0)
                {
                    NetLog.Assert(SendData.WsaBuffers[SendData.WsaBufferCount - 1].Length < ushort.MaxValue);
                    SendData.TotalSize += SendData.WsaBuffers[SendData.WsaBufferCount - 1].Length;
                }
                return;
            }

            NetLog.Assert(SendData.SegmentSize > 0 && SendData.WsaBufferCount > 0);
            NetLog.Assert(SendData.ClientBuffer.Length > 0 && SendData.ClientBuffer.Length <= SendData.SegmentSize);
            NetLog.Assert(CxPlatSendDataCanAllocSendSegment(SendData, 0));
            
            SendData.WsaBuffers[SendData.WsaBufferCount - 1].Length += SendData.ClientBuffer.Length;
            SendData.TotalSize += SendData.ClientBuffer.Length;

            if (SendData.ClientBuffer.Length == SendData.SegmentSize)
            {
                SendData.ClientBuffer.Length += SendData.SegmentSize;
                SendData.ClientBuffer.Length = 0;
            }
            else
            {
                SendData.ClientBuffer.Buffer = null;
                SendData.ClientBuffer.Length = 0;
            }
        }

        static ulong CxPlatSocketSend(CXPLAT_SOCKET Socket,CXPLAT_ROUTE Route,CXPLAT_SEND_DATA SendData)
        {
            return SocketSend(Socket, Route, SendData);
        }

        static ulong CxPlatSocketCreateUdp(CXPLAT_DATAPATH Datapath, CXPLAT_UDP_CONFIG Config, ref CXPLAT_SOCKET NewSocket)
        {
            ulong Status = QUIC_STATUS_SUCCESS;
            Status = SocketCreateUdp(Datapath, Config,ref NewSocket);
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            NewSocket.RawSocketAvailable = false;
        Error:
            return Status;
        }

        static ulong SocketSend(CXPLAT_SOCKET Socket, CXPLAT_ROUTE Route, CXPLAT_SEND_DATA SendData)
        {
            CXPLAT_SOCKET_PROC SocketProc = Route.Queue;
            SendData.SocketProc = SocketProc;

            CxPlatSendDataFinalizeSendBuffer(SendData);
            SendData.MappedRemoteAddress = Route.RemoteAddress.MapToIPv6();
            return CxPlatSocketSendEnqueue(Route, SendData);
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
            NetLog.Assert(DatapathType(SendData) == CXPLAT_DATAPATH_TYPE.CXPLAT_DATAPATH_TYPE_USER || DatapathType(SendData) == CXPLAT_DATAPATH_TYPE.CXPLAT_DATAPATH_TYPE_RAW);
            return DatapathType(SendData) == CXPLAT_DATAPATH_TYPE.CXPLAT_DATAPATH_TYPE_USER ? DataPathIsPaddingPreferred(Datapath) : RawDataPathIsPaddingPreferred(Datapath);
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

        static void CxPlatSocketGetLocalAddress(CXPLAT_SOCKET Socket, ref QUIC_ADDR Address)
        {
            NetLog.Assert(Socket != null);
            Address = Socket.LocalAddress;
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

    }
}
