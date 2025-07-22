#if TARGET_WINDOWS
using AKNet.Common;
using AKNet.Platform;
using AKNet.Platform.Socket;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace AKNet.Udp5MSQuic.Common
{
    using CXPLAT_CQE = OVERLAPPED_ENTRY;

    //一律强制转为IpV6地址
    internal class QUIC_ADDR
    {
        public const int sizeof_QUIC_ADDR = 12;
        public static readonly IPAddress IPAddressAnyMapToIPv6 = IPAddress.Any.MapToIPv6();

        public string ServerName;
        private IPAddress dont_use_this_field_Ip;
        public int nPort;
        private IPEndPoint mEndPoint;
        public int ScopeId;

        public QUIC_ADDR()
        {
            Ip = IPAddress.IPv6Any;
            nPort = 0;
            CheckFamilyError();
        }

        public QUIC_ADDR(IPAddress otherIp, int nPort)
        {
            this.Ip = otherIp;
            this.nPort = nPort;
            CheckFamilyError();
        }

        public QUIC_ADDR(IPEndPoint mIPEndPoint)
        {
            Ip = mIPEndPoint.Address;
            nPort = mIPEndPoint.Port;
            CheckFamilyError();
        }

        public byte[] GetBytes()
        {
            return Ip.GetAddressBytes();
        }

        public void SetIPEndPoint(IPEndPoint mIPEndPoint)
        {
            this.Ip = mIPEndPoint.Address;
            this.nPort = mIPEndPoint.Port;
            CheckFamilyError();
        }

        public IPEndPoint GetIPEndPoint()
        {
            if (mEndPoint == null || mEndPoint.Address != Ip || mEndPoint.Port != nPort)
            {
                mEndPoint = new IPEndPoint(Ip, nPort);
            }
            return mEndPoint;
        }

        public IPAddress Ip
        {
            get
            {
                return dont_use_this_field_Ip;
            }

            set
            {
                IPAddress tt = value;
                if (tt.Equals(IPAddress.Any) || tt.Equals(IPAddressAnyMapToIPv6))
                {
                    tt = IPAddress.IPv6Any;
                }
                else if (tt.AddressFamily == AddressFamily.InterNetwork)
                {
                    tt = tt.MapToIPv6();
                }

                dont_use_this_field_Ip = tt;
                CheckFamilyError();
            }
        }

        public AddressFamily Family
        {
            get
            {
                CheckFamilyError();
                return Ip.AddressFamily;
            }
        }

        public QUIC_ADDR MapToIPv6()
        {
            QUIC_ADDR OutAddr = new QUIC_ADDR();
            OutAddr.nPort = nPort;
            if (Ip.AddressFamily == AddressFamily.InterNetwork)
            {
                OutAddr.Ip = Ip.MapToIPv6();
            }
            else
            {
                OutAddr.Ip = Ip;
            }

            CheckFamilyError();
            return OutAddr;
        }

        public void CopyFrom(QUIC_ADDR other)
        {
            this.WriteFrom(other.ToSSBuffer().GetSpan());
        }

        public static implicit operator QUIC_ADDR(ReadOnlySpan<byte> ssBuffer)
        {
            QUIC_ADDR mm = new QUIC_ADDR();
            mm.WriteFrom(ssBuffer);
            return mm;
        }

        public int WriteTo(Span<byte> Buffer)
        {
            byte[] temp = Ip.MapToIPv6().GetAddressBytes();
            Buffer[0] = (byte)temp.Length;
            temp.AsSpan().CopyTo(Buffer.Slice(1));
            Buffer = Buffer.Slice(temp.Length + 1);
            EndianBitConverter.SetBytes(Buffer, 0, (ushort)nPort);
            return temp.Length + 1 + sizeof(ushort);
        }

        public void WriteFrom(ReadOnlySpan<byte> Buffer)
        {
            int nIpLength = Buffer[0];
            byte[] temp = Buffer.Slice(1, nIpLength).ToArray();
            Ip = new IPAddress(temp);
            Buffer = Buffer.Slice(temp.Length + 1);
            nPort = EndianBitConverter.ToUInt16(Buffer, 0);
            CheckFamilyError();
        }

        public QUIC_SSBuffer ToSSBuffer()
        {
            QUIC_SSBuffer qUIC_SSBuffer = new QUIC_SSBuffer(new byte[20]);
            int nLength = WriteTo(qUIC_SSBuffer.GetSpan());
            qUIC_SSBuffer.Length = nLength;
            return qUIC_SSBuffer;
        }

        public ReadOnlySpan<byte> GetBindAddr()
        {
            return SocketAddressHelper.GetBindAddr(GetIPEndPoint());
        }

        public void Reset()
        {
            Ip = IPAddress.IPv6Any;
            nPort = 0;
            ServerName = string.Empty;
            CheckFamilyError();
        }

        public override string ToString()
        {
            return $"{Ip}:{nPort}";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckFamilyError()
        {
#if DEBUG
            NetLog.Assert(dont_use_this_field_Ip.AddressFamily == AddressFamily.InterNetworkV6);
#endif
        }
    }

    internal class DATAPATH_RX_IO_BLOCK
    {
        public DATAPATH_RX_PACKET CXPLAT_CONTAINING_RECORD;
        public CXPLAT_POOL<DATAPATH_RX_PACKET> OwningPool = null;
        public CXPLAT_SOCKET_PROC SocketProc;
        public long ReferenceCount;
        public readonly CXPLAT_ROUTE Route = new CXPLAT_ROUTE();
        public readonly CXPLAT_SQE Sqe = new CXPLAT_SQE();
        public WSAMSG WsaMsgHdr;
        public WSABUF WsaControlBuf;
        public byte[] ControlBuf = new byte[100];

        public void Reset()
        {

        }
    }

    internal class DATAPATH_RX_PACKET : CXPLAT_POOL_Interface<DATAPATH_RX_PACKET>
    {
        public CXPLAT_POOL<DATAPATH_RX_PACKET> mPool = null;
        public readonly CXPLAT_POOL_ENTRY<DATAPATH_RX_PACKET> POOL_ENTRY = null;
        public readonly DATAPATH_RX_IO_BLOCK IoBlock;
        public readonly CXPLAT_RECV_DATA Data;

        public DATAPATH_RX_PACKET()
        {
            POOL_ENTRY = new CXPLAT_POOL_ENTRY<DATAPATH_RX_PACKET>(this);
            IoBlock = new DATAPATH_RX_IO_BLOCK();
            Data = new QUIC_RX_PACKET();
            IoBlock.CXPLAT_CONTAINING_RECORD = this;
            Data.CXPLAT_CONTAINING_RECORD = this;
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

            if (Data != null)
            {
                Data.Reset();
            }
        }

        public void SetPool(CXPLAT_POOL<DATAPATH_RX_PACKET> mPool)
        {
            this.mPool = mPool;
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
            Datapath.RecvDatagramLength = MessageCount * (BoolOk(Datapath.Features & CXPLAT_DATAPATH_FEATURE_RECV_COALESCING) ?
                MAX_URO_PAYLOAD_LENGTH : MAX_RECV_PAYLOAD_LENGTH);

            for (int i = 0; i < Datapath.PartitionCount; i++)
            {
                Datapath.Partitions[i] = new CXPLAT_DATAPATH_PROC();
                Datapath.Partitions[i].Datapath = Datapath;
                Datapath.Partitions[i].PartitionIndex = i;
                CxPlatRefInitialize(ref Datapath.Partitions[i].RefCount);

                Datapath.Partitions[i].SendDataPool.CxPlatPoolInitialize();
                Datapath.Partitions[i].SendBufferPool.CxPlatPoolInitialize(MAX_UDP_PAYLOAD_LENGTH);
                Datapath.Partitions[i].LargeSendBufferPool.CxPlatPoolInitialize(CXPLAT_LARGE_SEND_BUFFER_SIZE);
                Datapath.Partitions[i].RecvDatagramPool.CxPlatPoolInitialize();
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
                Socket.LocalAddress = Config.LocalAddress;
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
            }

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
                    sizeof(bool));

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
                    sizeof(bool));

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
                    sizeof(bool));

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
                    sizeof(bool));

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
                        sizeof(bool));

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
                        sizeof(bool));

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
                        sizeof(bool));

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
                        sizeof(bool));

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
                        sizeof(bool));

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
                        sizeof(bool));

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

                ReadOnlySpan<byte> mTemp = Socket.LocalAddress.GetBindAddr();
                fixed (byte* mTempPtr = mTemp)
                {
                    Result = Interop.Winsock.bind(SocketProc.Socket, mTempPtr, mTemp.Length);
                }

                if(Result == OSPlatformFunc.SOCKET_ERROR)
                {
                    Status = QUIC_STATUS_INTERNAL_ERROR;
                    goto Error;
                }

                if (Config.RemoteAddress != null)
                {
                    mTemp = Socket.RemoteAddress.GetBindAddr();
                    fixed (byte* mTempPtr = mTemp)
                    {
                        Interop.Winsock.connect(SocketProc.Socket, mTempPtr, mTemp.Length);
                    }

                    if (Result == OSPlatformFunc.SOCKET_ERROR)
                    {
                        Status = QUIC_STATUS_INTERNAL_ERROR;
                        goto Error;
                    }
                }
                
                if (i == 0)
                {
                    //如果客户端/服务器 没有指定端口,也就是端口==0的时候，Socket bind 后，会自动分配一个本地端口
                    var LocalEndPoint = SocketAddressHelper.GetLocalEndPoint(SocketProc.Socket, AddressFamily.InterNetworkV6);
                    Socket.LocalAddress.SetIPEndPoint(LocalEndPoint);
                    if (Config.LocalAddress != null && Config.LocalAddress.nPort != 0)
                    {
                        NetLog.Assert(Config.LocalAddress.nPort == Socket.LocalAddress.nPort);
                    }
                }

            }
        Skip:

            if (Config.RemoteAddress != null)
            {
                Socket.RemoteAddress = Config.RemoteAddress;
            }

            NewSocket = Socket;
            for (int i = 0; i < SocketCount; i++)
            {
                CxPlatDataPathStartReceive(Socket.PerProcSockets[i]);
                Socket.PerProcSockets[i].IoStarted = true;
            }
            Status = QUIC_STATUS_SUCCESS;
        Error:
            return Status;
        }

        static bool CxPlatDataPathStartReceive(CXPLAT_SOCKET_PROC SocketProc, 
            out ulong IoResult,
            out int InlineBytesTransferred,
            out DATAPATH_RX_IO_BLOCK IoBlock)
        {
            const int MAX_RECV_RETRIES = 10;
            int RetryCount = 0;
            int Status;
            do
            {
                Status = CxPlatSocketStartReceive(SocketProc,
                        out IoResult,
                        out InlineBytesTransferred,
                        out IoBlock);
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
            out ulong SyncIoResult,
            out int SyncBytesReceived,
            out DATAPATH_RX_IO_BLOCK SyncIoBlock)
        {
            int Status = 0;
            if (SocketProc.Parent.UseRio)
            {
                //Status = CxPlatSocketStartRioReceives(SocketProc);
                NetLog.Assert(Status != QUIC_STATUS_SUCCESS);
            }
            else
            {
                Status = CxPlatSocketStartWinsockReceive(SocketProc, out SyncIoResult, out SyncBytesReceived, out SyncIoBlock);
            }
            return Status;
        }

        static int CxPlatSocketStartWinsockReceive(CXPLAT_SOCKET_PROC SocketProc,
            ref ulong SyncIoResult,
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
            ReadOnlySpan<byte> mAdd = IoBlock.Route.RemoteAddress.GetBindAddr();
            int Result = 0;
            int BytesRecv = 0;
            fixed (void* mAddPtr = mAdd)
            fixed (void* ControlBufPtr = IoBlock.ControlBuf)
            fixed (void* WsaControlBufPtr = &IoBlock.WsaControlBuf)
            {
                IoBlock.WsaControlBuf.buf = (IntPtr)IoBlock.CXPLAT_CONTAINING_RECORD.Data.Buffer.GetBufferPtr();
                IoBlock.WsaControlBuf.len = SocketProc.Parent.RecvBufLen;

                IoBlock.WsaMsgHdr.name = (IntPtr)mAddPtr;
                IoBlock.WsaMsgHdr.namelen = mAdd.Length;
                IoBlock.WsaMsgHdr.lpBuffers = WsaControlBufPtr;
                IoBlock.WsaMsgHdr.dwBufferCount = 1;
                IoBlock.WsaMsgHdr.Control.buf = (IntPtr)ControlBufPtr;
                IoBlock.WsaMsgHdr.Control.len = IoBlock.ControlBuf.Length;
                IoBlock.WsaMsgHdr.dwFlags = 0;

                BytesRecv = 0;
                WSARecvMsg WSARecvMsg = DynamicWinsockMethods.GetWSARecvMsgDelegate(SocketProc.Socket);
                fixed (void* ptr2 = &IoBlock.WsaMsgHdr)
                {
                    Result = WSARecvMsg(
                              SocketProc.Socket,
                              (IntPtr)ptr2,
                              out BytesRecv,
                              &IoBlock.Sqe.sqePtr->Overlapped,
                              IntPtr.Zero);
                }
            }

            if (Result == OSPlatformFunc.SOCKET_ERROR)
            {
                int WsaError = Interop.Winsock.WSAGetLastError();
                NetLog.Assert(WsaError != OSPlatformFunc.NO_ERROR);
                if (WsaError == OSPlatformFunc.WSA_IO_PENDING)
                {
                    return QUIC_STATUS_PENDING;
                }
                if (SyncBytesReceived == 0)
                {
                    IoBlock.Sqe.Completion = CxPlatIoRecvFailureEventComplete;
                }
            }

            int Status = CxPlatSocketEnqueueSqe(SocketProc, IoBlock.Sqe, BytesRecv);
            if (QUIC_FAILED(Status))
            {
                NetLog.Assert(false); // We don't expect tests to hit this.
                CxPlatCancelDatapathIo(SocketProc);
                CxPlatSocketFreeRxIoBlock(IoBlock);
                return Status;
            }
            return QUIC_STATUS_PENDING;
        }

        static bool CxPlatDataPathUdpRecvComplete(CXPLAT_SOCKET_PROC SocketProc, DATAPATH_RX_IO_BLOCK IoBlock, ulong IoResult,
            int NumberOfBytesTransferred)
        {
            if (arg.SocketError == SocketError.NotSocket || arg.SocketError == SocketError.OperationAborted)
            {
                CxPlatSocketFreeRxIoBlock(IoBlock);
                return false;
            }

            IoBlock.Route.RemoteAddress = new QUIC_ADDR(arg.RemoteEndPoint as IPEndPoint);
            IoBlock.Route.LocalAddress = new QUIC_ADDR();
            QUIC_ADDR LocalAddr = IoBlock.Route.LocalAddress;
            QUIC_ADDR RemoteAddr = IoBlock.Route.RemoteAddress;
            IoBlock.Route.Queue = SocketProc;

            if (IsUnreachableErrorCode(arg.SocketError))
            {
                if (!SocketProc.Parent.PcpBinding)
                {
                    SocketProc.Parent.Datapath.UdpHandlers.Unreachable(SocketProc.Parent, SocketProc.Parent.ClientContext, RemoteAddr);
                }
            }
            else if (arg.SocketError == SocketError.Success)
            {
                if (arg.BytesTransferred == 0)
                {
                    NetLog.Assert(false);
                    goto Drop;
                }

                CXPLAT_RECV_DATA RecvDataChain = null;
                CXPLAT_RECV_DATA DatagramChainTail = null;

                CXPLAT_DATAPATH Datapath = SocketProc.Parent.Datapath;

                int NumberOfBytesTransferred = arg.BytesTransferred;
                bool FoundLocalAddr = false;
                int MessageLength = arg.BytesTransferred;
                int MessageCount = 0;
                bool IsCoalesced = false;
                byte TOS = 0;

                IPPacketInformation mIPPacketInformation = arg.ReceiveMessageFromPacketInfo;
                if (mIPPacketInformation != null)
                {
                    IPAddress Ip = mIPPacketInformation.Address;
                    LocalAddr.Ip = Ip;
                    LocalAddr.nPort = SocketProc.Parent.LocalAddress.nPort;
                    LocalAddr.ScopeId = mIPPacketInformation.Interface;

                    FoundLocalAddr = true;

                    int TypeOfService = (int)SocketProc.Socket.GetSocketOption(SocketOptionLevel.IP, SocketOptionName.TypeOfService);
                    TOS = (byte)TypeOfService;
                }

                if (!FoundLocalAddr)
                {
                    NetLog.Assert(false); // Not expected in tests
                    goto Drop;
                }

                NetLog.Assert(arg.BytesTransferred <= SocketProc.Parent.RecvBufLen);

                CXPLAT_RECV_DATA Datagram = IoBlock.CXPLAT_CONTAINING_RECORD.Data;
                for (; NumberOfBytesTransferred != 0; NumberOfBytesTransferred -= MessageLength)
                {
                    Datagram.CXPLAT_CONTAINING_RECORD = IoBlock.CXPLAT_CONTAINING_RECORD;

                    if (MessageLength > NumberOfBytesTransferred)
                    {
                        MessageLength = NumberOfBytesTransferred;
                    }

                    Datagram.Next = null;
                    Datagram.Buffer.Buffer = arg.Buffer;
                    Datagram.Buffer.Offset = arg.Offset;
                    Datagram.Buffer.Length = arg.BytesTransferred;
                    Datagram.Route = IoBlock.Route;
                    Datagram.PartitionIndex = SocketProc.DatapathProc.PartitionIndex % SocketProc.DatapathProc.Datapath.PartitionCount;
                    Datagram.TypeOfService = TOS;
                    Datagram.Allocated = true;
                    Datagram.Route.DatapathType = Datagram.DatapathType = CXPLAT_DATAPATH_TYPE.CXPLAT_DATAPATH_TYPE_NORMAL;
                    Datagram.QueuedOnConnection = false;

                    if (DatagramChainTail == null)
                    {
                        RecvDataChain = DatagramChainTail = Datagram;
                    }
                    else
                    {
                        DatagramChainTail.Next = Datagram;
                        DatagramChainTail = DatagramChainTail.Next;
                    }
                    IoBlock.ReferenceCount++;
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

        static void CxPlatSendDataComplete(CXPLAT_SEND_DATA SendData)
        {
            CXPLAT_SOCKET_PROC SocketProc = SendData.SocketProc;
            SendDataFree(SendData);
        }

        static void CxPlatSocketFreeRxIoBlock(DATAPATH_RX_IO_BLOCK IoBlock)
        {
            IoBlock.ReceiveArgs.Completed -= DataPathProcessCqe;
            IoBlock.OwningPool.CxPlatPoolFree(IoBlock.CXPLAT_CONTAINING_RECORD);
        }

        static void SendDataFree(CXPLAT_SEND_DATA SendData)
        {
            for (int i = 0; i < SendData.WsaBuffers.Count; ++i)
            {
                SendData.BufferPool.CxPlatPoolFree(SendData.WsaBuffers[i]);
            }
            SendData.WsaBuffers.Clear();
            SendData.Sqe.Completed -= DataPathProcessCqe;
            SendData.SendDataPool.CxPlatPoolFree(SendData);
        }

        static bool IsUnreachableErrorCode(SocketError ErrorCode)
        {
            return ErrorCode == SocketError.NetworkUnreachable || //10051
                ErrorCode == SocketError.HostUnreachable || //10065
                ErrorCode == SocketError.ConnectionReset; //10054
        }

        static int CxPlatSocketSendEnqueue(CXPLAT_ROUTE Route, CXPLAT_SEND_DATA SendData)
        {
            IList<ArraySegment<byte>> mList = SendData.Sqe.BufferList;
            mList.Clear();
            foreach (var v in SendData.WsaBuffers)
            {
                NetLog.Assert(v.Offset == 0);
                mList.Add(new ArraySegment<byte>(v.Buffer, v.Offset, v.Length));
            }


            //NetLog.Log("SendData.WsaBuffers.Count: " + SendData.WsaBuffers.Count);
            SendData.Sqe.RemoteEndPoint = SendData.MappedRemoteAddress.GetIPEndPoint();
            SendData.Sqe.UserToken = SendData;
            SendData.Sqe.BufferList = mList;
            CxPlatSocketEnqueueSqe(SendData.SocketProc, SendData.Sqe);
            return 0;
        }

        static int CxPlatSocketEnqueueSqe(CXPLAT_SOCKET_PROC SocketProc, SocketAsyncEventArgs arg)
        {
            NetLog.Assert(!SocketProc.Uninitialized);

            //NetLog.Log($"SendToAsync Length:  {arg.BufferList[0].Count}， {arg.RemoteEndPoint}");
            //NetLogHelper.PrintByteArray("SendToAsync Length", arg.BufferList[0].AsSpan());
            SocketProc.Socket.SendToAsync(arg);
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

                if (SendData.Sqe == null)
                {
                    SendData.Sqe = new SocketAsyncEventArgs();
                    SendData.Sqe.BufferList = new List<ArraySegment<byte>>();
                }

                SendData.Sqe.Completed += DataPathProcessCqe;
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

                if (IoBlock.ReceiveArgs == null)
                {
                    IoBlock.ReceiveArgs = new SSocketAsyncEventArgs();
                    IoBlock.ReceiveArgs.RemoteEndPoint = SocketProc.Parent.RemoteAddress.GetIPEndPoint();
                    byte[] mBuf = new byte[SocketProc.Parent.Datapath.RecvDatagramLength];
                    IoBlock.ReceiveArgs.SetBuffer(mBuf, 0, mBuf.Length);
                }
                IoBlock.ReceiveArgs.Completed += DataPathProcessCqe;
            }
            return IoBlock;
        }

        static void CxPlatSendDataComplete(SocketAsyncEventArgs arg)
        {
            CXPLAT_SEND_DATA SendData = arg.UserToken as CXPLAT_SEND_DATA;
            SendDataFree(SendData);
        }

        static void CxPlatDataPathSocketProcessReceive(SocketAsyncEventArgs arg)
        {
            //NetLog.Log($"ReceiveMessageFrom BytesTransferred:  {arg.BytesTransferred}");
            //NetLogHelper.PrintByteArray($"ReceiveMessageFrom BytesTransferred", arg.Buffer.AsSpan().Slice(arg.Offset, arg.BytesTransferred));
            NetLog.Assert(arg.BytesTransferred <= ushort.MaxValue);

            DATAPATH_RX_IO_BLOCK IoBlock = arg.UserToken as DATAPATH_RX_IO_BLOCK;
            int BytesTransferred = arg.BytesTransferred;
            SocketError IoResult = arg.SocketError;

            CXPLAT_SOCKET_PROC SocketProc = IoBlock.SocketProc;
            NetLog.Assert(!SocketProc.Uninitialized);

            if (!CxPlatDataPathUdpRecvComplete(arg))
            {
                return;
            }
            CxPlatDataPathStartReceiveAsync(SocketProc);
        }

        static void CxPlatDataPathSocketProcessReceive(DATAPATH_RX_IO_BLOCK IoBlock, int BytesTransferred,ulong IoResult)
        {
            CXPLAT_SOCKET_PROC SocketProc = IoBlock.SocketProc;
            NetLog.Assert(!SocketProc.Freed);
            if (!CxPlatRundownAcquire(SocketProc.RundownRef))
            {
                return;
            }

            NetLog.Assert(!SocketProc.Uninitialized);
            for (int InlineReceiveCount = 10; InlineReceiveCount > 0; InlineReceiveCount--)
            {
                if (!CxPlatDataPathUdpRecvComplete(SocketProc, IoBlock, IoResult, BytesTransferred) ||
                    !CxPlatDataPathStartReceiveAsync(
                        SocketProc,
                        InlineReceiveCount > 1 ? IoResult : null,
                        InlineReceiveCount > 1 ? BytesTransferred : null,
                        InlineReceiveCount > 1 ? IoBlock : null))
                {
                    break;
                }
            }

            CxPlatRundownRelease(SocketProc.RundownRef);
        }

        static void CxPlatStartDatapathIo(CXPLAT_SOCKET_PROC SocketProc,CXPLAT_SQE Sqe, DATAPATH_RX_IO_BLOCK contex, Action<CXPLAT_CQE> Completion)
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
                Interop.Kernel32.RtlNtStatusToDosError((long)Cqe.Internal));
        }

        static void DataPathProcessCqe(object Cqe, SocketAsyncEventArgs arg)
        {
            DATAPATH_RX_IO_BLOCK IoBlock = arg.UserToken as DATAPATH_RX_IO_BLOCK;
            var mWorker = IoBlock.SocketProc.Parent.Datapath.WorkerPool.Workers[IoBlock.SocketProc.DatapathProc.PartitionIndex];

            arg.Completed -= DataPathProcessCqe;
            arg.Completed += DataPathProcessCqe2;
            //mWorker.EventQ.Enqueue(arg as SSocketAsyncEventArgs);
        }

        static void DataPathProcessCqe2(object Cqe, SocketAsyncEventArgs arg)
        {
            arg.Completed -= DataPathProcessCqe2;
            switch (arg.LastOperation)
            {
                case SocketAsyncOperation.ReceiveMessageFrom:
                    CxPlatDataPathSocketProcessReceive(arg);
                    break;

                case SocketAsyncOperation.SendTo:
                    CxPlatSendDataComplete(arg);
                    break;

                default:
                    NetLog.Assert(false, arg.LastOperation);
                    break;
            }
        }
    }
}
#endif









