/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/14 8:56:49
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
#if TARGET_WINDOWS

using AKNet.Common;
using AKNet.Platform;
using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MSQuic2
{
    using CXPLAT_CQE = OVERLAPPED_ENTRY;

    //一律强制转为IpV6地址
    internal unsafe class QUIC_ADDR:IDisposable
    {
        public string ServerName;
        public SOCKADDR_INET* RawAddr;

        public void Dispose()
        {
            if(RawAddr != null)
            {
                OSPlatformFunc.CxPlatFree(RawAddr);
                RawAddr = null;
            }
        }

        //C# 会调用析构函数的哦
        ~QUIC_ADDR()
        {
            Dispose();
        }

        public QUIC_ADDR()
        {
            RawAddr = (SOCKADDR_INET*)OSPlatformFunc.CxPlatAllocAndClear(sizeof(SOCKADDR_INET));
        }

        public QUIC_ADDR(IPAddress otherIp, int nPort) : this(new IPEndPoint(otherIp, nPort))
        {

        }

        public QUIC_ADDR(IPEndPoint mIPEndPoint)
        {
            RawAddr = SocketAddressHelper.GetRawAddr(mIPEndPoint, out _);
        }

        public ReadOnlySpan<byte> GetIPAddressSpan()
        {
            if (RawAddr->si_family == OSPlatformFunc.AF_INET)
            {
                return new ReadOnlySpan<byte>(RawAddr->Ipv4.sin_addr.u, 4);
            }
            else
            {
                return new ReadOnlySpan<byte>(RawAddr->Ipv6.sin6_addr.u, 16);
            }
        }

        public IPEndPoint GetIPEndPoint()
        { 
            var mIpEndPoint = SocketAddressHelper.RawAddrTo(RawAddr);
            return mIpEndPoint;
        }

        public AddressFamily Family
        {
            get
            {
                return (AddressFamily)RawAddr->si_family;
            }

            set
            {
                RawAddr->si_family = (ushort)value;
            }
        }

        public ushort nPort
        {
            get
            {
                return (ushort)IPAddress.NetworkToHostOrder((short)RawAddr->Ipv6.sin6_port);
            }

            set
            {
                RawAddr->Ipv6.sin6_port = (ushort)IPAddress.HostToNetworkOrder((short)value);
            }
        }

        public static implicit operator QUIC_ADDR(ReadOnlySpan<byte> ssBuffer)
        {
            QUIC_ADDR mm = new QUIC_ADDR();
            mm.WriteFrom(ssBuffer);
            return mm;
        }

        public int WriteTo(Span<byte> Buffer)
        {
            ReadOnlySpan<byte> mSpan = UnSafeTool.GetSpan(RawAddr);
            mSpan.CopyTo(Buffer);
            return mSpan.Length;
        }

        public void WriteFrom(ReadOnlySpan<byte> Buffer)
        {
            RawAddr = (SOCKADDR_INET*)OSPlatformFunc.CxPlatAlloc(sizeof(SOCKADDR_INET));
            Span<byte> mTarget = new Span<byte>(RawAddr, sizeof(SOCKADDR_INET));
            Buffer.CopyTo(mTarget);
        }

        public void WriteFrom(QUIC_ADDR other)
        {
            *RawAddr = *other.RawAddr;
        }

        public QUIC_SSBuffer ToSSBuffer()
        {
            QUIC_SSBuffer qUIC_SSBuffer = new QUIC_SSBuffer(new byte[28]);
            int nLength = WriteTo(qUIC_SSBuffer.GetSpan());
            qUIC_SSBuffer.Length = nLength;
            return qUIC_SSBuffer;
        }

        public SOCKADDR_INET* GetRawAddr()
        {
            return RawAddr;
        }

        public void Reset()
        {
            RawAddr = null;
            ServerName = string.Empty;
        }

        public override string ToString()
        {
            var mIpEndPoint = GetIPEndPoint();
            if (mIpEndPoint != null)
            {
                return GetIPEndPoint().ToString();
            }
            else
            {
                return $"mIpEndPoint = null: {Family}, {nPort}";
            }
        }
    }

    internal unsafe class DATAPATH_RX_IO_BLOCK:IDisposable
    {
        public readonly DATAPATH_RX_PACKET CXPLAT_CONTAINING_RECORD;
        public CXPLAT_POOL<DATAPATH_RX_PACKET> OwningPool = null;
        public CXPLAT_SOCKET_PROC SocketProc;
        public long ReferenceCount;
        public readonly CXPLAT_ROUTE Route = new CXPLAT_ROUTE();
        public readonly CXPLAT_SQE Sqe = new CXPLAT_SQE();
        public readonly WSAMSG* WsaMsgHdr;            //这是消息头
        public readonly WSABUF* WsaControlBuf;        //这是实际数据

        public readonly MemoryHandle ControlBufHandle;
        public readonly Memory<byte> ControlBuf = new byte[
                OSPlatformFunc.RIO_CMSG_BASE_SIZE() +
                OSPlatformFunc.WSA_CMSG_SPACE(sizeof(IN6_PKTINFO)) +   // IP_PKTINFO
                OSPlatformFunc.WSA_CMSG_SPACE(sizeof(int)) +         // UDP_COALESCED_INFO
                OSPlatformFunc.WSA_CMSG_SPACE(sizeof(int)) +           // IP_TOS, or IP_ECN if RECV_DSCP isn't supported
                OSPlatformFunc.WSA_CMSG_SPACE(sizeof(int))             // IP_HOP_LIMIT
            ];

        public readonly byte[] mCqeBuffer = null;
        public readonly Memory<byte> mCqeMemory = null;
        public readonly MemoryHandle mCqeMemoryHandle;
        private bool _disposed = false;

        public DATAPATH_RX_IO_BLOCK(DATAPATH_RX_PACKET mRxPackage, int nRecvDatagramLength)
        {
            this.CXPLAT_CONTAINING_RECORD = mRxPackage;
            this.WsaMsgHdr = (WSAMSG*)OSPlatformFunc.CxPlatAllocAndClear(sizeof(WSAMSG));
            this.WsaControlBuf = (WSABUF*)OSPlatformFunc.CxPlatAllocAndClear(sizeof(WSABUF));
            this.ControlBufHandle = ControlBuf.Pin();
            this.mCqeBuffer = new byte[nRecvDatagramLength];
            this.mCqeMemory = mCqeBuffer;
            this.mCqeMemoryHandle = mCqeMemory.Pin();
            this._disposed = false;
        }

        ~DATAPATH_RX_IO_BLOCK()
        {
            Dispose();
        }

        public void Reset()
        {
            
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (WsaMsgHdr != null)
            {
                OSPlatformFunc.CxPlatFree(WsaMsgHdr);
            }

            if (WsaControlBuf != null)
            {
                OSPlatformFunc.CxPlatFree(WsaControlBuf);
            }

            ControlBufHandle.Dispose();
            mCqeMemoryHandle.Dispose();
        }
    }

    internal class DATAPATH_RX_PACKET : CXPLAT_POOL_Interface<DATAPATH_RX_PACKET>, IDisposable
    {
        public CXPLAT_POOL<DATAPATH_RX_PACKET> mPool = null;
        public readonly CXPLAT_POOL_ENTRY<DATAPATH_RX_PACKET> POOL_ENTRY = null;
        public readonly DATAPATH_RX_IO_BLOCK IoBlock;
        public readonly QUIC_RX_PACKET[] DataList;

        public DATAPATH_RX_PACKET()
        {
            POOL_ENTRY = new CXPLAT_POOL_ENTRY<DATAPATH_RX_PACKET>(this);
        }

        public DATAPATH_RX_PACKET(int MessageCount, int RecvDatagramLength) :this()
        {
            DataList = new QUIC_RX_PACKET[MessageCount];
            for(int i = 0; i < MessageCount; i++)
            {
                DataList[i] = new QUIC_RX_PACKET(this);
            }

            IoBlock = new DATAPATH_RX_IO_BLOCK(this, RecvDatagramLength);
        }

        public CXPLAT_POOL_ENTRY<DATAPATH_RX_PACKET> GetEntry()
        {
            return POOL_ENTRY;
        }

        public CXPLAT_POOL<DATAPATH_RX_PACKET> GetPool()
        {
            return this.mPool;
        }

        public void Reset()
        {
            if (IoBlock != null)
            {
                IoBlock.Reset();
            }

            foreach(var v in DataList)
            {
                v.Reset();
            }
        }

        public void SetPool(CXPLAT_POOL<DATAPATH_RX_PACKET> mPool)
        {
            this.mPool = mPool;
        }

        public void Dispose()
        {
            if (IoBlock != null)
            {
                IoBlock.Dispose();
            }
        }
    }

    internal static unsafe partial class MSQuicFunc
    {
        public static CXPLAT_DATAPATH_RECEIVE_CALLBACK CxPlatPcpRecvCallback;
        static int DataPathInitialize(CXPLAT_UDP_DATAPATH_CALLBACKS UdpCallbacks, CXPLAT_WORKER_POOL WorkerPool, out CXPLAT_DATAPATH NewDatapath)
        {
            int WsaError;
            int Status;
            int PartitionCount = CxPlatProcCount();
            int DatapathLength;
            CXPLAT_DATAPATH Datapath = NewDatapath = null;

            if (UdpCallbacks != null)
            {
                if (UdpCallbacks.Receive == null || UdpCallbacks.Unreachable == null)
                {
                    return QUIC_STATUS_INVALID_PARAMETER;
                }
            }

            if (WorkerPool == null)
            {
                return QUIC_STATUS_INVALID_PARAMETER;
            }

            Datapath = new CXPLAT_DATAPATH(CxPlatWorkerPoolGetCount(WorkerPool));
            if (Datapath == null)
            {
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                goto Error;
            }

            if (UdpCallbacks != null)
            {
                Datapath.UdpHandlers = UdpCallbacks;
            }

            Datapath.WorkerPool = WorkerPool;
            Datapath.PartitionCount = CxPlatWorkerPoolGetCount(WorkerPool);
            CxPlatRefInitializeEx(ref Datapath.RefCount, Datapath.PartitionCount);

            if (BoolOk(Datapath.Features & CXPLAT_DATAPATH_FEATURE_SEND_SEGMENTATION))
            {
                Datapath.MaxSendBatchSize = CXPLAT_MAX_BATCH_SEND;
            }
            else
            {
                Datapath.MaxSendBatchSize = 1;
            }

            int MessageCount = BoolOk(Datapath.Features & CXPLAT_DATAPATH_FEATURE_RECV_COALESCING) ? URO_MAX_DATAGRAMS_PER_INDICATION : 1;
            int RecvDatagramLength = (BoolOk(Datapath.Features & CXPLAT_DATAPATH_FEATURE_RECV_COALESCING) ? MAX_URO_PAYLOAD_LENGTH : MAX_RECV_PAYLOAD_LENGTH);

            for (int i = 0; i < Datapath.PartitionCount; i++)
            {
                Datapath.Partitions[i] = new CXPLAT_DATAPATH_PROC();
                Datapath.Partitions[i].Datapath = Datapath;
                Datapath.Partitions[i].PartitionIndex = i;
                Datapath.Partitions[i].EventQ = CxPlatWorkerPoolGetEventQ(Datapath.WorkerPool, i);
                CxPlatRefInitialize(ref Datapath.Partitions[i].RefCount);

                Datapath.Partitions[i].SendDataPool.CxPlatPoolInitialize();
                Datapath.Partitions[i].SendBufferPool.CxPlatPoolInitialize(MAX_UDP_PAYLOAD_LENGTH);
                Datapath.Partitions[i].LargeSendBufferPool.CxPlatPoolInitialize(CXPLAT_LARGE_SEND_BUFFER_SIZE);
                Datapath.Partitions[i].RecvDatagramPool.CxPlatPoolInitialize(MessageCount, RecvDatagramLength);
            }

            NewDatapath = Datapath;
            Status = QUIC_STATUS_SUCCESS;
        Error:
            return Status;
        }

        static int SocketCreateUdp(CXPLAT_DATAPATH Datapath, CXPLAT_UDP_CONFIG Config, out CXPLAT_SOCKET NewSocket)
        {
            int Status = 0;
            bool IsServerSocket = Config.RemoteAddress == null;
            bool NumPerProcessorSockets = IsServerSocket && Datapath.PartitionCount > 1;
            int SocketCount = NumPerProcessorSockets ? CxPlatProcCount() : 1;
            NetLog.Assert(Datapath.UdpHandlers.Receive != null || BoolOk(Config.Flags & CXPLAT_SOCKET_FLAG_PCP));
            NetLog.Assert(IsServerSocket || Config.PartitionIndex < Datapath.PartitionCount);

            INET_PORT_RESERVATION_INSTANCE PortReservation;
            NewSocket = null;
            CXPLAT_SOCKET Socket = new CXPLAT_SOCKET();
            Socket.Datapath = Datapath;
            Socket.ClientContext = Config.CallbackContext;
            Socket.NumPerProcessorSockets = NumPerProcessorSockets ? 1 : 0;
            Socket.HasFixedRemoteAddress = Config.RemoteAddress != null;
            Socket.Type = CXPLAT_SOCKET_TYPE.CXPLAT_SOCKET_UDP;

            if (Config.LocalAddress != null)
            {
                SocketAddressHelper.CxPlatConvertToMappedV6(Config.LocalAddress.RawAddr, Socket.LocalAddress.RawAddr);
            }
            else
            {
                Socket.LocalAddress.RawAddr->si_family = OSPlatformFunc.AF_INET6;
            }

            Socket.Mtu = CXPLAT_MAX_MTU;
            if (BoolOk(Config.Flags & CXPLAT_SOCKET_FLAG_PCP))
            {
                Socket.PcpBinding = true;
            }

            CxPlatRefInitializeEx(ref Socket.RefCount, SocketCount);

            Socket.RecvBufLen = BoolOk(Datapath.Features & CXPLAT_DATAPATH_FEATURE_RECV_COALESCING) ? MAX_URO_PAYLOAD_LENGTH : MAX_RECV_PAYLOAD_LENGTH;
            Socket.PerProcSockets = new CXPLAT_SOCKET_PROC[SocketCount];
            for (int i = 0; i < SocketCount; i++)
            {
                Socket.PerProcSockets[i] = new CXPLAT_SOCKET_PROC();
                Socket.PerProcSockets[i].Parent = Socket;
                Socket.PerProcSockets[i].Socket = null;

                //CxPlatRefInitialize(Socket.PerProcSockets[i].RefCount);
                CxPlatRundownInitialize(Socket.PerProcSockets[i].RundownRef);
                //Socket.PerProcSockets[i].RioCq = OSPlatformFunc.RIO_INVALID_CQ;
                //Socket.PerProcSockets[i].RioRq = OSPlatformFunc.RIO_INVALID_RQ;
                //CxPlatListInitializeHead(Socket.PerProcSockets[i].RioSendOverflow);
            }

            Interop.Winsock.EnsureInitialized();
            for (int i = 0; i < SocketCount; i++)
            {
                CXPLAT_SOCKET_PROC SocketProc = Socket.PerProcSockets[i];
                int PartitionIndex = Config.RemoteAddress != null ? Config.PartitionIndex : i % Datapath.PartitionCount;
                int BytesReturned;

                uint SocketFlags = OSPlatformFunc.WSA_FLAG_OVERLAPPED;
                if (Socket.UseRio)
                {
                    SocketFlags |= OSPlatformFunc.WSA_FLAG_REGISTERED_IO;
                }

                SocketProc.Socket = Interop.Winsock.WSASocketW(
                    OSPlatformFunc.AF_INET6, 
                    OSPlatformFunc.SOCK_DGRAM, 
                    OSPlatformFunc.IPPROTO_UDP, 
                    IntPtr.Zero, 0, SocketFlags);

                if (SocketProc.Socket == null)
                {
                    Status = QUIC_STATUS_INTERNAL_ERROR;
                    goto Error;
                }

                int Option = 0;
                int Result = Interop.Winsock.setsockopt(
                    SocketProc.Socket, 
                    OSPlatformFunc.IPPROTO_IPV6, 
                    OSPlatformFunc.IPV6_V6ONLY,
                    (byte*)&Option,
                    sizeof(int));

                //同时接收IPV4 和IPV6数据包
                if (Result == OSPlatformFunc.SOCKET_ERROR)
                {
                    Status = QUIC_STATUS_INTERNAL_ERROR;
                    goto Error;
                }

                if (Config.RemoteAddress == null && Datapath.PartitionCount > 1)
                {
                    //设置CPU亲和性
                    int Processor = i;
                    Result = Interop.Winsock.WSAIoctl(
                        SocketProc.Socket, 
                        OSPlatformFunc.SIO_CPU_AFFINITY,
                            &Processor,
                            sizeof(int),
                            null,
                            0,
                            out BytesReturned,
                            null,
                            null);

                    if (Result != OSPlatformFunc.NO_ERROR)
                    {
                        Status = QUIC_STATUS_INTERNAL_ERROR;
                        goto Error;
                    }
                }

                Option = 1;
                Result = Interop.Winsock.setsockopt(
                    SocketProc.Socket, 
                    OSPlatformFunc.IPPROTO_IP, 
                    OSPlatformFunc.IP_DONTFRAGMENT,
                    (byte*)(&Option),
                    sizeof(int));

                if(Result == OSPlatformFunc.SOCKET_ERROR)
                {
                    Status = QUIC_STATUS_INTERNAL_ERROR;
                    goto Error;
                }

                Option = 1;
                Result = Interop.Winsock.setsockopt(
                    SocketProc.Socket,
                    OSPlatformFunc.IPPROTO_IPV6,
                    OSPlatformFunc.IPV6_DONTFRAG,
                    (byte*)(&Option),
                    sizeof(int));

                if (Result == OSPlatformFunc.SOCKET_ERROR)
                {
                    Status = QUIC_STATUS_INTERNAL_ERROR;
                    goto Error;
                }

                Option = 1;
                Result = Interop.Winsock.setsockopt(
                    SocketProc.Socket,
                    OSPlatformFunc.IPPROTO_IPV6,
                    OSPlatformFunc.IPV6_PKTINFO,
                    (byte*)(&Option),
                    sizeof(int));

                if (Result == OSPlatformFunc.SOCKET_ERROR)
                {
                    Status = QUIC_STATUS_INTERNAL_ERROR;
                    goto Error;
                }

                Option = 1;
                Result = Interop.Winsock.setsockopt(
                    SocketProc.Socket,
                    OSPlatformFunc.IPPROTO_IP,
                    OSPlatformFunc.IPV6_PKTINFO,
                    (byte*)(&Option),
                    sizeof(int));

                if (Result == OSPlatformFunc.SOCKET_ERROR)
                {
                    Status = QUIC_STATUS_INTERNAL_ERROR;
                    goto Error;
                }

                if (BoolOk(Datapath.Features & (uint)CXPLAT_DATAPATH_FEATURES.CXPLAT_DATAPATH_FEATURE_RECV_DSCP))
                {
                    Option = 1;
                    Result = Interop.Winsock.setsockopt(
                        SocketProc.Socket,
                        OSPlatformFunc.IPPROTO_IPV6,
                        OSPlatformFunc.IPV6_RECVTCLASS,
                        (byte*)(&Option),
                        sizeof(int));

                    if (Result == OSPlatformFunc.SOCKET_ERROR)
                    {
                        Status = QUIC_STATUS_INTERNAL_ERROR;
                        goto Error;
                    }

                    Option = 1;
                    Result = Interop.Winsock.setsockopt(
                        SocketProc.Socket,
                        OSPlatformFunc.IPPROTO_IP,
                        OSPlatformFunc.IP_RECVTOS,
                        (byte*)(&Option),
                        sizeof(int));

                    if (Result == OSPlatformFunc.SOCKET_ERROR)
                    {
                        Status = QUIC_STATUS_INTERNAL_ERROR;
                        goto Error;
                    }
                }
                else
                {
                    Option = 1;
                    Result = Interop.Winsock.setsockopt(
                        SocketProc.Socket,
                        OSPlatformFunc.IPPROTO_IPV6,
                        OSPlatformFunc.IPV6_ECN,
                        (byte*)(&Option),
                        sizeof(int));

                    if (Result == OSPlatformFunc.SOCKET_ERROR)
                    {
                        Status = QUIC_STATUS_INTERNAL_ERROR;
                        goto Error;
                    }

                    Option = 1;
                    Result = Interop.Winsock.setsockopt(
                        SocketProc.Socket,
                        OSPlatformFunc.IPPROTO_IP,
                        OSPlatformFunc.IP_ECN,
                        (byte*)(&Option),
                        sizeof(int));

                    if (Result == OSPlatformFunc.SOCKET_ERROR)
                    {
                        Status = QUIC_STATUS_INTERNAL_ERROR;
                        goto Error;
                    }
                }

                if (BoolOk(Datapath.Features & (uint)CXPLAT_DATAPATH_FEATURES.CXPLAT_DATAPATH_FEATURE_TTL))
                {
                    Option = 1;
                    Result = Interop.Winsock.setsockopt(
                        SocketProc.Socket,
                        OSPlatformFunc.IPPROTO_IP,
                        OSPlatformFunc.IP_HOPLIMIT,
                        (byte*)(&Option),
                        sizeof(int));

                    if (Result == OSPlatformFunc.SOCKET_ERROR)
                    {
                        Status = QUIC_STATUS_INTERNAL_ERROR;
                        goto Error;
                    }

                    Option = 1;
                    Result = Interop.Winsock.setsockopt(
                        SocketProc.Socket,
                        OSPlatformFunc.IPPROTO_IPV6,
                        OSPlatformFunc.IPV6_HOPLIMIT,
                        (byte*)(&Option),
                        sizeof(int));

                    if (Result == OSPlatformFunc.SOCKET_ERROR)
                    {
                        Status = QUIC_STATUS_INTERNAL_ERROR;
                        goto Error;
                    }
                }

                Option = Int32.MaxValue;
                Result = Interop.Winsock.setsockopt(
                    SocketProc.Socket,
                    OSPlatformFunc.SOL_SOCKET,
                    OSPlatformFunc.SO_RCVBUF,
                     (byte*)&Option,
                    sizeof(int));

                if (Result == OSPlatformFunc.SOCKET_ERROR)
                {
                    Status = QUIC_STATUS_INTERNAL_ERROR;
                    goto Error;
                }

                if (BoolOk(Datapath.Features & (uint)CXPLAT_DATAPATH_FEATURES.CXPLAT_DATAPATH_FEATURE_RECV_COALESCING))
                {
                    Option = MAX_URO_PAYLOAD_LENGTH;
                    Result = Interop.Winsock.setsockopt(
                        SocketProc.Socket,
                        OSPlatformFunc.IPPROTO_UDP,
                        OSPlatformFunc.UDP_RECV_MAX_COALESCED_SIZE,
                        (byte*)&Option,
                        sizeof(int));

                    if (Result == OSPlatformFunc.SOCKET_ERROR)
                    {
                        Status = QUIC_STATUS_INTERNAL_ERROR;
                        goto Error;
                    }
                }

                if (!Interop.Kernel32.SetFileCompletionNotificationModes(SocketProc.Socket,
                     OSPlatformFunc.FILE_SKIP_COMPLETION_PORT_ON_SUCCESS | OSPlatformFunc.FILE_SKIP_SET_EVENT_ON_HANDLE))
                {
                    Status = QUIC_STATUS_INTERNAL_ERROR;
                    goto Error;
                }

                NetLog.Assert(PartitionIndex < Datapath.PartitionCount);
                SocketProc.DatapathProc = Datapath.Partitions[PartitionIndex];
                CxPlatRefIncrement(ref SocketProc.DatapathProc.RefCount);

                if (!OSPlatformFunc.CxPlatEventQAssociateHandle(SocketProc.DatapathProc.EventQ, SocketProc.Socket.DangerousGetHandle()))
                {
                    Status = QUIC_STATUS_INTERNAL_ERROR;
                    goto Error;
                }

                if (Socket.UseRio)
                {
                    //TODO
                }

                if (Config.InterfaceIndex != 0)
                {
                    Option = Config.InterfaceIndex;
                    Result = Interop.Winsock.setsockopt(
                        SocketProc.Socket,
                        OSPlatformFunc.IPPROTO_IPV6,
                        OSPlatformFunc.IPV6_UNICAST_IF,
                        (byte*)&Option,
                        sizeof(int));

                    if (Result == OSPlatformFunc.SOCKET_ERROR)
                    {
                        Status = QUIC_STATUS_INTERNAL_ERROR;
                        goto Error;
                    }

                    Option = Config.InterfaceIndex;
                    Result = Interop.Winsock.setsockopt(
                        SocketProc.Socket,
                        OSPlatformFunc.IPPROTO_IP,
                        OSPlatformFunc.IP_UNICAST_IF,
                        (byte*)&Option,
                        sizeof(int));

                    if (Result == OSPlatformFunc.SOCKET_ERROR)
                    {
                        Status = QUIC_STATUS_INTERNAL_ERROR;
                        goto Error;
                    }
                }

                if (BoolOk(Datapath.Features & (uint)CXPLAT_DATAPATH_FEATURES.CXPLAT_DATAPATH_FEATURE_PORT_RESERVATIONS) &&
                    Config.LocalAddress != null && Config.LocalAddress.nPort != 0)
                {
                    if (i == 0)
                    {
                        INET_PORT_RANGE PortRange;
                        PortRange.StartPort = (ushort)Config.LocalAddress.nPort;
                        PortRange.NumberOfPorts = 1;

                        Result = Interop.Winsock.WSAIoctl(
                                SocketProc.Socket,
                                OSPlatformFunc.SIO_ACQUIRE_PORT_RESERVATION,
                                &PortRange,
                                sizeof(INET_PORT_RANGE),
                                &PortReservation,
                                sizeof(INET_PORT_RESERVATION_INSTANCE),
                                out BytesReturned,
                                null,
                                null);
                        if (Result == OSPlatformFunc.SOCKET_ERROR)
                        {
                            Status = QUIC_STATUS_INTERNAL_ERROR;
                            goto Error;
                        }
                    }

                    Result = Interop.Winsock.WSAIoctl(
                              SocketProc.Socket,
                              OSPlatformFunc.SIO_ASSOCIATE_PORT_RESERVATION,
                              &PortReservation.Token,
                              sizeof(INET_PORT_RESERVATION_TOKEN),
                              null,
                              0,
                              out BytesReturned,
                              null,
                              null);

                    if (Result == OSPlatformFunc.SOCKET_ERROR)
                    {
                        Status = QUIC_STATUS_INTERNAL_ERROR;
                        goto Error;
                    }
                }

                Result = Interop.Winsock.bind(SocketProc.Socket, (byte*)Socket.LocalAddress.RawAddr, sizeof(SOCKADDR_INET));
                if(Result == OSPlatformFunc.SOCKET_ERROR)
                {
                    int WSAError = Marshal.GetLastWin32Error();
                    NetLog.LogError("Error: " + (SocketError)WSAError);
                    Status = QUIC_STATUS_INTERNAL_ERROR;
                    goto Error;
                }

                if (Config.RemoteAddress != null)
                {
                    SOCKADDR_INET MappedRemoteAddress;
                    SocketAddressHelper.CxPlatConvertToMappedV6(Config.RemoteAddress.RawAddr, &MappedRemoteAddress);
                    Result = Interop.Winsock.connect(SocketProc.Socket, (byte*)&MappedRemoteAddress, sizeof(SOCKADDR_INET));
                    if (Result == OSPlatformFunc.SOCKET_ERROR)
                    {
                        int WSAError = Marshal.GetLastWin32Error();
                        NetLog.LogError("Error: " +(SocketError)WSAError);
                        Status = QUIC_STATUS_INTERNAL_ERROR;
                        goto Error;
                    }
                }
                
                if (i == 0)
                {
                    //如果客户端/服务器 没有指定端口,也就是端口==0的时候，Socket bind 后，会自动分配一个本地端口
                    int AssignedLocalAddressLength = Marshal.SizeOf<SOCKADDR_INET>();
                    Result = Interop.Winsock.getsockname(SocketProc.Socket, (byte*)Socket.LocalAddress.RawAddr, ref AssignedLocalAddressLength);
                    if (Result == OSPlatformFunc.SOCKET_ERROR)
                    {
                        Status = QUIC_STATUS_INTERNAL_ERROR;
                        goto Error;
                    }

                    if (Config.LocalAddress != null && Config.LocalAddress.RawAddr != null && Config.LocalAddress.RawAddr->Ipv6.sin6_port != 0)
                    {
                        NetLog.Assert(Config.LocalAddress.RawAddr->Ipv6.sin6_port == Socket.LocalAddress.RawAddr->Ipv6.sin6_port);
                    }
                }

            }

            SocketAddressHelper.CxPlatConvertFromMappedV6(Socket.LocalAddress.RawAddr, Socket.LocalAddress.RawAddr);
        Skip:

            if (Config.RemoteAddress != null)
            {
                Socket.RemoteAddress.WriteFrom(Config.RemoteAddress);
            }
            
            NewSocket = Socket;
            for (int i = 0; i < SocketCount; i++)
            {
                DATAPATH_RX_IO_BLOCK __IoBlock = null;
                int __BytesTransferred = 0;
                uint __IoResult = 0;
                CxPlatDataPathStartReceive(Socket.PerProcSockets[i], ref __IoResult, ref __BytesTransferred, ref __IoBlock);
                Socket.PerProcSockets[i].IoStarted = true;
            }
            Status = QUIC_STATUS_SUCCESS;
        Error:
            return Status;
        }

        static bool CxPlatDataPathStartReceive(CXPLAT_SOCKET_PROC SocketProc, 
            ref uint IoResult,
            ref int InlineBytesTransferred,
            ref DATAPATH_RX_IO_BLOCK IoBlock)
        {
            const int MAX_RECV_RETRIES = 10;
            int RetryCount = 0;
            int Status;
            do
            {
                Status = CxPlatSocketStartReceive(SocketProc,
                        ref IoResult,
                        ref InlineBytesTransferred,
                        ref IoBlock);
            } while (Status == QUIC_STATUS_OUT_OF_MEMORY && ++RetryCount < MAX_RECV_RETRIES);

            if (Status == QUIC_STATUS_OUT_OF_MEMORY)
            {
                NetLog.Assert(RetryCount == MAX_RECV_RETRIES);
                SocketProc.RecvFailure = true;
                Status = QUIC_STATUS_PENDING;
            }

            return Status != QUIC_STATUS_PENDING;
        }

        static int CxPlatSocketStartReceive(CXPLAT_SOCKET_PROC SocketProc,
            ref uint SyncIoResult,
            ref int SyncBytesReceived,
            ref DATAPATH_RX_IO_BLOCK SyncIoBlock)
        {
            int Status = 0;
            if (SocketProc.Parent.UseRio)
            {
                //Status = CxPlatSocketStartRioReceives(SocketProc);
                NetLog.Assert(Status != QUIC_STATUS_SUCCESS);
            }
            else
            {
                Status = CxPlatSocketStartWinsockReceive(SocketProc, ref SyncIoResult, ref SyncBytesReceived, ref SyncIoBlock);
            }
            return Status;
        }

        static int CxPlatSocketStartWinsockReceive(CXPLAT_SOCKET_PROC SocketProc,
            ref uint SyncIoResult,
            ref int SyncBytesReceived,
            ref DATAPATH_RX_IO_BLOCK SyncIoBlock)
        {
            CXPLAT_DATAPATH Datapath = SocketProc.Parent.Datapath;
            DATAPATH_RX_IO_BLOCK IoBlock = CxPlatSocketAllocRxIoBlock(SocketProc);
            if (IoBlock == null)
            {
                return QUIC_STATUS_OUT_OF_MEMORY;
            }

            CxPlatStartDatapathIo(SocketProc, IoBlock.Sqe, IoBlock, CxPlatIoRecvEventComplete);
            int Result = 0;
            int BytesRecv = 0;

            IoBlock.WsaControlBuf->buf = (byte*)IoBlock.mCqeMemoryHandle.Pointer;
            IoBlock.WsaControlBuf->len = SocketProc.Parent.RecvBufLen;
            IoBlock.WsaMsgHdr->name = IoBlock.Route.RemoteAddress.RawAddr;
            IoBlock.WsaMsgHdr->namelen = sizeof(SOCKADDR_INET);
            IoBlock.WsaMsgHdr->lpBuffers = IoBlock.WsaControlBuf;
            IoBlock.WsaMsgHdr->dwBufferCount = 1;
            IoBlock.WsaMsgHdr->Control.buf = (byte*)IoBlock.ControlBufHandle.Pointer;
            IoBlock.WsaMsgHdr->Control.len = IoBlock.ControlBuf.Length;
            IoBlock.WsaMsgHdr->dwFlags = 0;

            BytesRecv = 0;
            WSARecvMsg WSARecvMsg = DynamicWinsockMethods.GetWSARecvMsgDelegate(SocketProc.Socket);
            Result = WSARecvMsg(
                        SocketProc.Socket,
                        IoBlock.WsaMsgHdr,
                        &BytesRecv,
                        &IoBlock.Sqe.sqePtr->Overlapped,
                        null);

            int WsaError = OSPlatformFunc.NO_ERROR;
            if (Result == OSPlatformFunc.SOCKET_ERROR)
            {
                WsaError = Marshal.GetLastWin32Error();
                NetLog.Assert(WsaError != OSPlatformFunc.NO_ERROR);
                if ((ulong)WsaError == OSPlatformFunc.WSA_IO_PENDING)
                {
                    return QUIC_STATUS_PENDING;
                }

                if (SyncIoBlock != null)
                {
                    IoBlock.Sqe.Completion = CxPlatIoRecvFailureEventComplete;
                    BytesRecv = (int)WsaError;
                }
            }

            if (SyncIoBlock != null)
            {
                NetLog.Assert(BytesRecv < ushort.MaxValue);
                IoBlock.Sqe.sqePtr->Overlapped.Internal = 0;
                SyncBytesReceived = (ushort)BytesRecv;
                SyncIoResult = (uint)WsaError;
                SyncIoBlock = IoBlock;
                return QUIC_STATUS_SUCCESS;
            }

            int Status = CxPlatSocketEnqueueSqe(SocketProc, IoBlock.Sqe, BytesRecv);
            if (QUIC_FAILED(Status))
            {
                NetLog.Assert(false);
                CxPlatCancelDatapathIo(SocketProc);
                CxPlatSocketFreeRxIoBlock(IoBlock);
                return Status;
            }
            return QUIC_STATUS_PENDING;
        }
        
        static void CxPlatCancelDatapathIo(CXPLAT_SOCKET_PROC SocketProc)
        {
            
        }

        static void CxPlatIoRecvFailureEventComplete(CXPLAT_CQE Cqe)
        {
            CXPLAT_SQE Sqe = OSPlatformFunc.CxPlatCqeGetSqe(Cqe);
            NetLog.Assert(Sqe.sqePtr->Overlapped.Internal != 0x103); // STATUS_PENDING
            NetLog.Assert(Cqe.dwNumberOfBytesTransferred <= ushort.MaxValue);
            CxPlatDataPathSocketProcessReceive(Sqe.Contex as DATAPATH_RX_IO_BLOCK, 0, (uint)Cqe.dwNumberOfBytesTransferred);
        }

        static bool CxPlatDataPathUdpRecvComplete(CXPLAT_SOCKET_PROC SocketProc, DATAPATH_RX_IO_BLOCK IoBlock, ulong IoResult,
            int NumberOfBytesTransferred)
        {
            if (IoResult == OSPlatformFunc.WSAENOTSOCK || IoResult == OSPlatformFunc.WSA_OPERATION_ABORTED)
            {
                CxPlatSocketFreeRxIoBlock(IoBlock);
                return false;
            }

            SOCKADDR_INET* LocalRawAddr = IoBlock.Route.LocalAddress.RawAddr;
            SOCKADDR_INET* RemoteAddr = IoBlock.Route.RemoteAddress.RawAddr;
            SocketAddressHelper.CxPlatConvertFromMappedV6(RemoteAddr, RemoteAddr);
            IoBlock.Route.Queue = SocketProc;

            if (IsUnreachableErrorCode(IoResult))
            {
                if (!SocketProc.Parent.PcpBinding)
                {
                    SocketProc.Parent.Datapath.UdpHandlers.Unreachable(SocketProc.Parent, SocketProc.Parent.ClientContext, IoBlock.Route.RemoteAddress);
                }
            }
            else if (IoResult == OSPlatformFunc.ERROR_MORE_DATA ||
                   (IoResult == OSPlatformFunc.NO_ERROR && SocketProc.Parent.RecvBufLen < NumberOfBytesTransferred))
            {
                
            }
            else if (IoResult == OSPlatformFunc.NO_ERROR)
            {
                if (NumberOfBytesTransferred == 0)
                {
                    NetLog.Assert(false);
                    goto Drop;
                }

                CXPLAT_RECV_DATA RecvDataChain = null;
                CXPLAT_RECV_DATA DatagramChainTail = null;
                CXPLAT_DATAPATH Datapath = SocketProc.Parent.Datapath;

                bool FoundLocalAddr = false;
                int MessageLength = NumberOfBytesTransferred;
                int MessageCount = 0;
                bool IsCoalesced = false;
                int TypeOfService = 0;
                int HopLimitTTL = 0;

                if (SocketProc.Parent.UseRio)
                {
                    RIO_CMSG_BUFFER* RioRcvMsg = (RIO_CMSG_BUFFER*)IoBlock.ControlBufHandle.Pointer;
                    IoBlock.WsaMsgHdr->Control.buf = (byte*)IoBlock.ControlBufHandle.Pointer + OSPlatformFunc.RIO_CMSG_BASE_SIZE();
                    IoBlock.WsaMsgHdr->Control.len = (int)RioRcvMsg->TotalLength - (int)OSPlatformFunc.RIO_CMSG_BASE_SIZE();
                }

                for (WSACMSGHDR* CMsg = OSPlatformFunc.WSA_CMSG_FIRSTHDR(IoBlock.WsaMsgHdr); CMsg != null;
                    CMsg = OSPlatformFunc.WSA_CMSG_NXTHDR(IoBlock.WsaMsgHdr, CMsg))
                {
                    if (CMsg->cmsg_level == OSPlatformFunc.IPPROTO_IPV6)
                    {
                        if (CMsg->cmsg_type == OSPlatformFunc.IPV6_PKTINFO)
                        {
                            IN6_PKTINFO* PktInfo6 = (IN6_PKTINFO*)OSPlatformFunc.WSA_CMSG_DATA(CMsg);
                            LocalRawAddr->Ipv6.sin6_family = OSPlatformFunc.AF_INET6;
                            LocalRawAddr->Ipv6.sin6_addr = PktInfo6->ipi6_addr;
                            LocalRawAddr->Ipv6.sin6_port = (ushort)SocketProc.Parent.LocalAddress.nPort;
                            SocketAddressHelper.CxPlatConvertFromMappedV6(LocalRawAddr, LocalRawAddr);
                            LocalRawAddr->Ipv6.sin6_scope_id = PktInfo6->ipi6_ifindex;
                            FoundLocalAddr = true;
                        }
                        else if (CMsg->cmsg_type == OSPlatformFunc.IPV6_TCLASS)
                        {
                            TypeOfService = *(int*)OSPlatformFunc.WSA_CMSG_DATA(CMsg);
                            NetLog.Assert(TypeOfService < byte.MaxValue);
                        }
                        else if (CMsg->cmsg_type == OSPlatformFunc.IPV6_ECN)
                        {
                            TypeOfService = *(int*)OSPlatformFunc.WSA_CMSG_DATA(CMsg);
                            NetLog.Assert(TypeOfService <= (int)CXPLAT_ECN_TYPE.CXPLAT_ECN_CE);
                        }
                        else if (CMsg->cmsg_type == OSPlatformFunc.IPV6_HOPLIMIT)
                        {
                            HopLimitTTL = *(int*)OSPlatformFunc.WSA_CMSG_DATA(CMsg);
                            NetLog.Assert(HopLimitTTL < 256);
                            NetLog.Assert(HopLimitTTL > 0);
                        }
                    }
                    else if (CMsg->cmsg_level == OSPlatformFunc.IPPROTO_IP)
                    {
                        if (CMsg->cmsg_type == OSPlatformFunc.IP_PKTINFO)
                        {
                            IN_PKTINFO* PktInfo = (IN_PKTINFO*)OSPlatformFunc.WSA_CMSG_DATA(CMsg);
                            LocalRawAddr->si_family = OSPlatformFunc.AF_INET;
                            LocalRawAddr->Ipv4.sin_addr = PktInfo->ipi_addr;
                            LocalRawAddr->Ipv4.sin_port = SocketProc.Parent.LocalAddress.RawAddr->Ipv6.sin6_port;
                            LocalRawAddr->Ipv6.sin6_scope_id = PktInfo->ipi_ifindex;
                            FoundLocalAddr = true;
                        }
                        else if (CMsg->cmsg_type == OSPlatformFunc.IP_TOS)
                        {
                            TypeOfService = *(int*)OSPlatformFunc.WSA_CMSG_DATA(CMsg);
                            NetLog.Assert(TypeOfService < byte.MaxValue);
                        }
                        else if (CMsg->cmsg_type == OSPlatformFunc.IP_ECN)
                        {
                            TypeOfService = *(int*)OSPlatformFunc.WSA_CMSG_DATA(CMsg);
                            NetLog.Assert(TypeOfService <= (int)CXPLAT_ECN_TYPE.CXPLAT_ECN_CE);
                        }
                        else if (CMsg->cmsg_type == OSPlatformFunc.IP_TTL)
                        {
                            HopLimitTTL = *(int*)OSPlatformFunc.WSA_CMSG_DATA(CMsg);
                            NetLog.Assert(HopLimitTTL < 256);
                            NetLog.Assert(HopLimitTTL > 0);
                        }
                    }
                    else if (CMsg->cmsg_level == OSPlatformFunc.IPPROTO_UDP)
                    {
                        if (CMsg->cmsg_type == UDP_COALESCED_INFO)
                        {
                            NetLog.Assert(*(int*)OSPlatformFunc.WSA_CMSG_DATA(CMsg) <= SocketProc.Parent.RecvBufLen);
                            MessageLength = (ushort) *(int*)OSPlatformFunc.WSA_CMSG_DATA(CMsg);
                            IsCoalesced = true;
                        }
                    }
                }

                if (!FoundLocalAddr)
                {
                    NetLog.Assert(false); // Not expected in tests
                    goto Drop;
                }
                NetLog.Assert(NumberOfBytesTransferred <= SocketProc.Parent.RecvBufLen);

                int RecvPayload = 0;
                int nDatagramOffset = 0;
                for (; NumberOfBytesTransferred != 0; NumberOfBytesTransferred -= MessageLength)
                {
                    if (MessageLength > NumberOfBytesTransferred)
                    {
                        MessageLength = NumberOfBytesTransferred;
                    }

                    CXPLAT_RECV_DATA Datagram = (CXPLAT_RECV_DATA)(IoBlock.CXPLAT_CONTAINING_RECORD.DataList[nDatagramOffset]);
                    Datagram.Next = null;
                    Datagram.Buffer.Buffer = IoBlock.mCqeBuffer;
                    Datagram.Buffer.Offset = RecvPayload;
                    Datagram.Buffer.Length = MessageLength;
                    Datagram.Route = IoBlock.Route;
                    Datagram.PartitionIndex = SocketProc.DatapathProc.PartitionIndex % SocketProc.DatapathProc.Datapath.PartitionCount;
                    Datagram.TypeOfService = (byte)TypeOfService;
                    Datagram.HopLimitTTL = (byte)HopLimitTTL;
                    Datagram.Allocated = true;
                    Datagram.Route.DatapathType = Datagram.DatapathType = CXPLAT_DATAPATH_TYPE.CXPLAT_DATAPATH_TYPE_NORMAL;
                    Datagram.QueuedOnConnection = false;

                    //NetLog.Log("Receive: " +  Datagram.Buffer.GetSpan().Length);
                    // NetLogHelper.PrintByteArray("Receive: ", Datagram.Buffer.GetSpan());

                    if (DatagramChainTail == null)
                    {
                        RecvDataChain = DatagramChainTail = Datagram;
                    }
                    else
                    {
                        DatagramChainTail.Next = Datagram;
                        DatagramChainTail = Datagram;
                    }
                    IoBlock.ReferenceCount++;
                    nDatagramOffset++;
                    RecvPayload += MessageLength;

                    if (IsCoalesced && ++MessageCount == URO_MAX_DATAGRAMS_PER_INDICATION)
                    {
                        NetLog.LogWarning($"[data][{SocketProc.Parent}] Exceeded URO preallocation capacity.");
                        break;
                    }
                }

                IoBlock = null; //不加这个，会导致多个地方释放
                NetLog.Assert(RecvDataChain != null);
                if (SocketProc.Parent.PcpBinding)
                {
                    CxPlatPcpRecvCallback(SocketProc.Parent, SocketProc.Parent.ClientContext, RecvDataChain);
                }
                else
                {
                    SocketProc.Parent.Datapath.UdpHandlers.Receive(SocketProc.Parent, SocketProc.Parent.ClientContext, RecvDataChain);
                }
            }
            else
            {
                NetLog.Assert(false); // Not expected in test scenarios
            }

        Drop:
            if (IoBlock != null)
            {
                CxPlatSocketFreeRxIoBlock(IoBlock);
            }
            return true;
        }

        static void CxPlatSendDataComplete(CXPLAT_SEND_DATA SendData, uint IoResult)
        {
            CXPLAT_SOCKET_PROC SocketProc = SendData.SocketProc;
            if (IoResult != QUIC_STATUS_SUCCESS)
            {
                NetLog.LogError("CxPlatSendDataComplete Error: " + (SocketError)IoResult);
            }
            SendDataFree(SendData);
        }

        static void CxPlatSocketFreeRxIoBlock(DATAPATH_RX_IO_BLOCK IoBlock)
        {
            IoBlock.OwningPool.CxPlatPoolFree(IoBlock.CXPLAT_CONTAINING_RECORD);
        }

        static void SendDataFree(CXPLAT_SEND_DATA SendData)
        {
            for (int i = 0; i < SendData.WsaBuffers.Count; ++i)
            {
                SendData.WsaBuffers2[i].Dispose();
                SendData.BufferPool.CxPlatPoolFree(SendData.WsaBuffers[i]);
            }
            SendData.WsaBuffers2.Clear();
            SendData.WsaBuffers.Clear();
            SendData.SendDataPool.CxPlatPoolFree(SendData);
        }

        static bool IsUnreachableErrorCode(ulong ErrorCode)
        {
            return ErrorCode == OSPlatformFunc.ERROR_NETWORK_UNREACHABLE || //10051
                ErrorCode == OSPlatformFunc.ERROR_HOST_UNREACHABLE ||       //10065
                ErrorCode == OSPlatformFunc.ERROR_PROTOCOL_UNREACHABLE ||   //10054
                ErrorCode == OSPlatformFunc.ERROR_PORT_UNREACHABLE ||       //10065
                ErrorCode == OSPlatformFunc.WSAENETUNREACH ||               //10065
                ErrorCode == OSPlatformFunc.WSAEHOSTUNREACH ||              //10065
                ErrorCode == OSPlatformFunc.WSAECONNRESET;                  //10065
        }

        static void CxPlatIoSendEventComplete(CXPLAT_CQE Cqe)
        {
            CXPLAT_SQE Sqe = OSPlatformFunc.CxPlatCqeGetSqe(Cqe);
            NetLog.Assert(Sqe.sqePtr->Overlapped.Internal != 0x103); // STATUS_PENDING
            CXPLAT_SEND_DATA SendData = Sqe.Contex as CXPLAT_SEND_DATA;
            CXPLAT_SOCKET_PROC SocketProc = SendData.SocketProc;
            CxPlatSendDataComplete(SendData, Interop.Kernel32.RtlNtStatusToDosError((int)Cqe.Internal));
            //CxPlatSocketContextRelease(SocketProc);
        }

        static void CxPlatIoQueueSendEventComplete(CXPLAT_CQE Cqe)
        {
            CXPLAT_SQE Sqe = OSPlatformFunc.CxPlatCqeGetSqe(Cqe);
            NetLog.Assert(Sqe.sqePtr->Overlapped.Internal != 0x103); // STATUS_PENDING
            CXPLAT_SEND_DATA SendData = Sqe.Contex as CXPLAT_SEND_DATA;
            CXPLAT_SOCKET_PROC SocketProc = SendData.SocketProc;
            CxPlatDataPathSocketProcessQueuedSend(SendData);
            //CxPlatSocketContextRelease(SocketProc);
        }

        static void CxPlatDataPathSocketProcessQueuedSend(CXPLAT_SEND_DATA SendData)
        {
            CXPLAT_SOCKET_PROC SocketProc = SendData.SocketProc;
            if (CxPlatRundownAcquire(SocketProc.RundownRef))
            {
                CxPlatSocketSendInline(SendData.LocalAddress, SendData);
                CxPlatRundownRelease(SocketProc.RundownRef);
            }
            else
            {
                CxPlatSendDataComplete(SendData, OSPlatformFunc.WSAESHUTDOWN);
            }
        }

        static void CxPlatSocketSendEnqueue(CXPLAT_ROUTE Route, CXPLAT_SEND_DATA SendData)
        {
            *SendData.LocalAddress.RawAddr = *Route.LocalAddress.RawAddr;
            CxPlatStartDatapathIo(SendData.SocketProc, SendData.Sqe, SendData, CxPlatIoQueueSendEventComplete);
            int Status = CxPlatSocketEnqueueSqe(SendData.SocketProc, SendData.Sqe, 0);
            if (QUIC_FAILED(Status))
            {
                CxPlatCancelDatapathIo(SendData.SocketProc);
            }
        }

        static void CxPlatSocketSendInline(QUIC_ADDR LocalAddress, CXPLAT_SEND_DATA SendData)
        {
            CXPLAT_SOCKET_PROC SocketProc = SendData.SocketProc;
            //if (SocketProc.RioSendCount == OSPlatformFunc.RIO_SEND_QUEUE_DEPTH)
            //{
            //    CxPlatListInsertTail(SocketProc.RioSendOverflow, &SendData.RioOverflowEntry);
            //    return;
            //}

            int Result;
            int BytesSent;
            CXPLAT_DATAPATH Datapath = SocketProc.Parent.Datapath;
            CXPLAT_SOCKET Socket = SocketProc.Parent;

            WSAMSG WSAMhdr;
            WSAMhdr.dwFlags = 0;
            if (Socket.HasFixedRemoteAddress) 
            {
                WSAMhdr.name = null;
                WSAMhdr.namelen = 0;
            }
            else
            {
                WSAMhdr.name = SendData.MappedRemoteAddress.RawAddr;
                WSAMhdr.namelen = sizeof(SOCKADDR_INET);
            }

            for (int i = 0; i < SendData.WsaBuffers.Count; i++)
            {
                MemoryHandle mHandle = new Memory<byte>(SendData.WsaBuffers[i].Buffer).Pin();
                SendData.WsaBuffers2.Add(mHandle);
                SendData.WsaBuffersInner.Span[i].buf = (byte*)mHandle.Pointer;
                SendData.WsaBuffersInner.Span[i].len = SendData.WsaBuffers[i].Length;

                //NetLog.Log("Send: " + SendData.WsaBuffers[i].GetSpan().Length);
                //NetLogHelper.PrintByteArray("Send: ", SendData.WsaBuffers[i].GetSpan());
            }

            WSAMhdr.lpBuffers = (WSABUF*)SendData.WsaBuffersInnerMemoryHandle.Pointer;
            WSAMhdr.dwBufferCount = SendData.WsaBuffers.Count;
            WSAMhdr.Control.buf = OSPlatformFunc.RIO_CMSG_BASE_SIZE() + (byte*)SendData.CtrlBufHandle.Pointer;
            WSAMhdr.Control.len = 0;

            WSACMSGHDR* CMsg = null;
            if (LocalAddress.Family == AddressFamily.InterNetwork)
            {
                if (!Socket.HasFixedRemoteAddress)
                {
                    WSAMhdr.Control.len += OSPlatformFunc.WSA_CMSG_SPACE(sizeof(IN_PKTINFO));
                    CMsg = OSPlatformFunc.WSA_CMSG_FIRSTHDR(&WSAMhdr);
                    CMsg->cmsg_level = OSPlatformFunc.IPPROTO_IP;
                    CMsg->cmsg_type = OSPlatformFunc.IP_PKTINFO;
                    CMsg->cmsg_len = OSPlatformFunc.WSA_CMSG_LEN(sizeof(IN_PKTINFO));
                    IN_PKTINFO* PktInfo = (IN_PKTINFO*)OSPlatformFunc.WSA_CMSG_DATA(CMsg);
                    PktInfo->ipi_ifindex = LocalAddress.RawAddr->Ipv6.sin6_scope_id;
                    PktInfo->ipi_addr = LocalAddress.RawAddr->Ipv4.sin_addr;
                }

                if (BoolOk(Socket.Datapath.Features & (ulong)CXPLAT_DATAPATH_FEATURES.CXPLAT_DATAPATH_FEATURE_SEND_DSCP))
                {
                    if (SendData.ECN != (byte)CXPLAT_ECN_TYPE.CXPLAT_ECN_NON_ECT || SendData.DSCP != (byte)CXPLAT_DSCP_TYPE.CXPLAT_DSCP_CS0)
                    {
                        WSAMhdr.Control.len += OSPlatformFunc.WSA_CMSG_SPACE(sizeof(int));
                        CMsg = OSPlatformFunc.WSA_CMSG_NXTHDR(&WSAMhdr, CMsg);
                        NetLog.Assert(CMsg != null);
                        CMsg->cmsg_level = OSPlatformFunc.IPPROTO_IP;
                        CMsg->cmsg_type = OSPlatformFunc.IP_TOS;
                        CMsg->cmsg_len = OSPlatformFunc.WSA_CMSG_LEN(sizeof(int));
                        *(int*)OSPlatformFunc.WSA_CMSG_DATA(CMsg) = SendData.ECN | (SendData.DSCP << 2);
                    }
                }
                else
                {
                    if (SendData.ECN != (byte)CXPLAT_ECN_TYPE.CXPLAT_ECN_NON_ECT)
                    {
                        WSAMhdr.Control.len += OSPlatformFunc.WSA_CMSG_SPACE(sizeof(int));
                        CMsg = OSPlatformFunc.WSA_CMSG_NXTHDR(&WSAMhdr, CMsg);
                        NetLog.Assert(CMsg != null);
                        CMsg->cmsg_level = OSPlatformFunc.IPPROTO_IP;
                        CMsg->cmsg_type = OSPlatformFunc.IP_ECN;
                        CMsg->cmsg_len = OSPlatformFunc.WSA_CMSG_LEN(sizeof(int));
                        *(int*)OSPlatformFunc.WSA_CMSG_DATA(CMsg) = SendData.ECN;
                    }
                }
            }
            else
            {
                if (!Socket.HasFixedRemoteAddress)
                {
                    WSAMhdr.Control.len += OSPlatformFunc.WSA_CMSG_SPACE(sizeof(IN6_PKTINFO));
                    CMsg = OSPlatformFunc.WSA_CMSG_FIRSTHDR(&WSAMhdr);
                    CMsg->cmsg_level = OSPlatformFunc.IPPROTO_IPV6;
                    CMsg->cmsg_type = OSPlatformFunc.IPV6_PKTINFO;
                    CMsg->cmsg_len = OSPlatformFunc.WSA_CMSG_LEN(sizeof(IN6_PKTINFO));
                    IN6_PKTINFO* PktInfo6 = (IN6_PKTINFO*)OSPlatformFunc.WSA_CMSG_DATA(CMsg);
                    PktInfo6->ipi6_ifindex = LocalAddress.RawAddr->Ipv6.sin6_scope_id;
                    PktInfo6->ipi6_addr = LocalAddress.RawAddr->Ipv6.sin6_addr;
                }

                if (BoolOk(Socket.Datapath.Features & (ulong)CXPLAT_DATAPATH_FEATURES.CXPLAT_DATAPATH_FEATURE_SEND_DSCP))
                {
                    if (SendData.ECN != (byte)CXPLAT_ECN_TYPE.CXPLAT_ECN_NON_ECT || SendData.DSCP != (byte)CXPLAT_DSCP_TYPE.CXPLAT_DSCP_CS0)
                    {
                        WSAMhdr.Control.len += OSPlatformFunc.WSA_CMSG_SPACE(sizeof(int));
                        CMsg = OSPlatformFunc.WSA_CMSG_NXTHDR(&WSAMhdr, CMsg);
                        NetLog.Assert(CMsg != null);
                        CMsg->cmsg_level = OSPlatformFunc.IPPROTO_IPV6;
                        CMsg->cmsg_type = OSPlatformFunc.IPV6_TCLASS;
                        CMsg->cmsg_len = OSPlatformFunc.WSA_CMSG_LEN(sizeof(int));
                        *(int*)OSPlatformFunc.WSA_CMSG_DATA(CMsg) = SendData.ECN | (SendData.DSCP << 2);
                    }
                }
                else
                {
                    if (SendData.ECN != (byte)CXPLAT_ECN_TYPE.CXPLAT_ECN_NON_ECT)
                    {
                        WSAMhdr.Control.len += OSPlatformFunc.WSA_CMSG_SPACE(sizeof(int));
                        CMsg = OSPlatformFunc.WSA_CMSG_NXTHDR(&WSAMhdr, CMsg);
                        NetLog.Assert(CMsg != null);
                        CMsg->cmsg_level = OSPlatformFunc.IPPROTO_IPV6;
                        CMsg->cmsg_type = OSPlatformFunc.IPV6_ECN;
                        CMsg->cmsg_len = OSPlatformFunc.WSA_CMSG_LEN(sizeof(int));
                        *(int*)OSPlatformFunc.WSA_CMSG_DATA(CMsg) = SendData.ECN;
                    }
                }
            }

            if (SendData.SegmentSize > 0)
            {
                WSAMhdr.Control.len += OSPlatformFunc.WSA_CMSG_SPACE(sizeof(int));
                CMsg = OSPlatformFunc.WSA_CMSG_NXTHDR(&WSAMhdr, CMsg);
                NetLog.Assert(CMsg != null);
                CMsg->cmsg_level = OSPlatformFunc.IPPROTO_UDP;
                CMsg->cmsg_type = OSPlatformFunc.UDP_SEND_MSG_SIZE;
                CMsg->cmsg_len = OSPlatformFunc.WSA_CMSG_LEN(sizeof(int));
                *(int*)OSPlatformFunc.WSA_CMSG_DATA(CMsg) = SendData.SegmentSize;
            }
            
            if (WSAMhdr.Control.len == 0)
            {
                WSAMhdr.Control.buf = null;
            }

            if (Socket.UseRio)
            {
                //CxPlatSocketSendWithRio(SendData, &WSAMhdr);
                return;
            }
            
            CxPlatStartDatapathIo(SocketProc, SendData.Sqe, SendData, CxPlatIoSendEventComplete);
            WSASendMsg mFunc = DynamicWinsockMethods.GetWSASendMsgDelegate(SocketProc.Socket);
            Result = mFunc(SocketProc.Socket, &WSAMhdr,0, &BytesSent, &SendData.Sqe.sqePtr->Overlapped, null);
            int WsaError = OSPlatformFunc.NO_ERROR;
            if (Result == OSPlatformFunc.SOCKET_ERROR)
            {
                WsaError = Marshal.GetLastWin32Error();
                if ((uint)WsaError == OSPlatformFunc.WSA_IO_PENDING)
                {
                    return;
                }
            }
            CxPlatCancelDatapathIo(SocketProc);
            CxPlatSendDataComplete(SendData, (uint)WsaError);
        }

        static int CxPlatSocketEnqueueSqe(CXPLAT_SOCKET_PROC SocketProc, CXPLAT_SQE Sqe, int NumBytes)
        {
            NetLog.Assert(!SocketProc.Uninitialized);
            NetLog.Assert(!SocketProc.Freed);
            if (!OSPlatformFunc.CxPlatEventQEnqueueEx(SocketProc.DatapathProc.EventQ, Sqe, NumBytes))
            {
                return QUIC_STATUS_INTERNAL_ERROR;
            }
            return QUIC_STATUS_SUCCESS;
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
                SendData.ECN = (byte)Config.ECN;
                SendData.SendFlags = Config.Flags;
                SendData.SegmentSize = HasFlag(Socket.Datapath.Features, CXPLAT_DATAPATH_FEATURE_SEND_SEGMENTATION) ? Config.MaxPacketSize : 0;
                SendData.TotalSize = 0;
                SendData.WsaBuffers.Clear();
                SendData.ClientBuffer.Buffer = null;
                SendData.ClientBuffer.Length = 0;
                SendData.DatapathType = Config.Route.DatapathType = CXPLAT_DATAPATH_TYPE.CXPLAT_DATAPATH_TYPE_NORMAL;
                SendData.Owner = DatapathProc;
                SendData.SendDataPool = SendDataPool;
                SendData.BufferPool = SendData.SegmentSize > 0 ? DatapathProc.LargeSendBufferPool : DatapathProc.SendBufferPool;
            }

            return SendData;
        }

        static DATAPATH_RX_IO_BLOCK CxPlatSocketAllocRxIoBlock(CXPLAT_SOCKET_PROC SocketProc)
        {
            CXPLAT_DATAPATH_PROC DatapathProc = SocketProc.DatapathProc;
            CXPLAT_POOL<DATAPATH_RX_PACKET> OwningPool = DatapathProc.RecvDatagramPool;
            DATAPATH_RX_IO_BLOCK IoBlock = OwningPool.CxPlatPoolAlloc().IoBlock;
            if (IoBlock != null)
            {
                IoBlock.Route.State = CXPLAT_ROUTE_STATE.RouteResolved;
                IoBlock.OwningPool = OwningPool;
                IoBlock.ReferenceCount = 0;
                IoBlock.SocketProc = SocketProc;
            }
            return IoBlock;
        }

        static void CxPlatDataPathSocketProcessReceive(DATAPATH_RX_IO_BLOCK IoBlock, int BytesTransferred,uint IoResult)
        {
            CXPLAT_SOCKET_PROC SocketProc = IoBlock.SocketProc;
            NetLog.Assert(!SocketProc.Freed);
            if (!CxPlatRundownAcquire(SocketProc.RundownRef))
            {
                //CxPlatSocketContextRelease(SocketProc);
                return;
            }

            NetLog.Assert(!SocketProc.Uninitialized);

            //下面这个是处理同步完成的
            for (int InlineReceiveCount = 10; InlineReceiveCount > 0; InlineReceiveCount--)
            {
                //CxPlatSocketContextRelease(SocketProc);
                if (!CxPlatDataPathUdpRecvComplete(SocketProc, IoBlock, IoResult, BytesTransferred))
                {
                    break;
                }

                if (InlineReceiveCount > 1)
                {
                    if (!CxPlatDataPathStartReceive(SocketProc, ref IoResult, ref BytesTransferred, ref IoBlock))
                    {
                        break;
                    }
                }
                else
                {
                    DATAPATH_RX_IO_BLOCK __IoBlock = null;
                    int __BytesTransferred = 0;
                    uint __IoResult = 0;
                    if (!CxPlatDataPathStartReceive(SocketProc, ref __IoResult, ref __BytesTransferred, ref __IoBlock))
                    {
                        break;
                    }
                }
            }

            CxPlatRundownRelease(SocketProc.RundownRef);
        }

        static void CxPlatStartDatapathIo(CXPLAT_SOCKET_PROC SocketProc,CXPLAT_SQE Sqe, object contex, Action<CXPLAT_CQE> Completion)
        {
            NetLog.Assert(Sqe.sqePtr->Overlapped.Internal != 0x103); // STATUS_PENDING
            OSPlatformFunc.CxPlatSqeInitializeEx(Completion, contex, Sqe);
            //CxPlatRefIncrement(ref SocketProc.RefCount);
        }

        static void CxPlatIoRecvEventComplete(CXPLAT_CQE Cqe)
        {
            CXPLAT_SQE Sqe = OSPlatformFunc.CxPlatCqeGetSqe(Cqe);
            NetLog.Assert(Cqe.Internal != 0x103); // STATUS_PENDING
            NetLog.Assert(Cqe.dwNumberOfBytesTransferred <= ushort.MaxValue);

            CxPlatDataPathSocketProcessReceive(Sqe.Contex as DATAPATH_RX_IO_BLOCK,
                (ushort)Cqe.dwNumberOfBytesTransferred,
                Interop.Kernel32.RtlNtStatusToDosError((int)Cqe.Internal));
        }
    }
}
#endif









