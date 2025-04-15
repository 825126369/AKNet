/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_ADDR
    {
        public AddressFamily AddressFamily;
        public IPAddress Ip;
        public int nPort;

        public IPEndPoint GetIPEndPoint()
        {
            return new IPEndPoint(Ip, nPort);
        }

        public QUIC_ADDR MapToIPv6()
        {
            QUIC_ADDR OutAddr = new QUIC_ADDR();
            OutAddr.nPort = nPort;
            OutAddr.AddressFamily = AddressFamily.InterNetworkV6;
            if (AddressFamily == AddressFamily.InterNetwork)
            {
                OutAddr.Ip = Ip.MapToIPv6();
            }
            else
            {
                OutAddr.Ip = Ip;
            }

            return OutAddr;
        }

        public QUIC_ADDR MapToIPv4()
        {
            QUIC_ADDR OutAddr = new QUIC_ADDR();
            OutAddr.nPort = nPort;
            OutAddr.AddressFamily = AddressFamily.InterNetwork;
            if (AddressFamily == AddressFamily.InterNetworkV6)
            {
                OutAddr.Ip = Ip.MapToIPv4();
            }
            else
            {
                OutAddr.Ip = Ip;
            }

            return OutAddr;
        }

        public void WriteTo(byte[] Buffer)
        {

        }

        public void WriteFrom(byte[] Buffer)
        {

        }
    }

    internal class INET_PORT_RANGE
    {
        public ushort StartPort;
        public ushort NumberOfPorts;
    }

    internal class DATAPATH_RX_IO_BLOCK:CXPLAT_POOL_Interface<DATAPATH_RX_IO_BLOCK>
    {
        public DATAPATH_RX_PACKET CXPLAT_CONTAINING_RECORD;

        public readonly CXPLAT_POOL_ENTRY<DATAPATH_RX_IO_BLOCK> POOL_ENTRY = null;
        public readonly CXPLAT_POOL<DATAPATH_RX_IO_BLOCK> OwningPool = new CXPLAT_POOL<DATAPATH_RX_IO_BLOCK>();
        public CXPLAT_SOCKET_PROC SocketProc;
        public long ReferenceCount;
        public CXPLAT_ROUTE Route;
        public QUIC_BUFFER WsaControlBuf;
        
        public DATAPATH_RX_IO_BLOCK()
        {
            POOL_ENTRY = new CXPLAT_POOL_ENTRY<DATAPATH_RX_IO_BLOCK>(this);
        }

        public CXPLAT_POOL_ENTRY<DATAPATH_RX_IO_BLOCK> GetEntry()
        {
            return POOL_ENTRY;
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }
    }

    internal class DATAPATH_RX_PACKET
    {
        public DATAPATH_RX_IO_BLOCK IoBlock;
        public CXPLAT_RECV_DATA Data;
    }

    internal static partial class MSQuicFunc
    {
        public static CXPLAT_DATAPATH_RECEIVE_CALLBACK CxPlatPcpRecvCallback;

        static ulong DataPathInitialize(int ClientRecvDataLength, CXPLAT_UDP_DATAPATH_CALLBACKS UdpCallbacks,
            QUIC_EXECUTION_CONFIG Config, ref CXPLAT_DATAPATH NewDatapath)
        {
            int WsaError;
            ulong Status;
            WSADATA WsaData;
            int PartitionCount = CxPlatProcCount();
            int DatapathLength;
            CXPLAT_DATAPATH Datapath = null;
            bool WsaInitialized = false;

            if (NewDatapath == null)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Exit;
            }

            if (UdpCallbacks != null)
            {
                if (UdpCallbacks.Receive == null || UdpCallbacks.Unreachable == null)
                {
                    Status = QUIC_STATUS_INVALID_PARAMETER;
                    goto Exit;
                }
            }

            if (!CxPlatWorkersLazyStart(Config))
            {
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                goto Exit;
            }

            WsaInitialized = true;
            if (Config != null && Config.ProcessorCount > 0)
            {
                PartitionCount = Config.ProcessorCount;
            }

            DatapathLength =
                sizeof(CXPLAT_DATAPATH) +
                PartitionCount * sizeof(CXPLAT_DATAPATH_PARTITION);

            Datapath = (CXPLAT_DATAPATH*)CXPLAT_ALLOC_PAGED(DatapathLength, QUIC_POOL_DATAPATH);
            if (Datapath == NULL)
            {
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                goto Error;
            }

            RtlZeroMemory(Datapath, DatapathLength);
            if (UdpCallbacks != null)
            {
                Datapath.UdpHandlers = UdpCallbacks;
            }

            Datapath.PartitionCount = PartitionCount;
            CxPlatRefInitializeEx(Datapath.RefCount, Datapath.PartitionCount);
            Datapath.UseRio = Config != null && BoolOk(Config.Flags & QUIC_EXECUTION_CONFIG_FLAG_RIO);

            CxPlatDataPathQueryRssScalabilityInfo(Datapath);
            Status = CxPlatDataPathQuerySockoptSupport(Datapath);
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            if (BoolOk(Datapath.Features & CXPLAT_DATAPATH_FEATURE_SEND_SEGMENTATION))
            {
                Datapath.MaxSendBatchSize = CXPLAT_MAX_BATCH_SEND;
            }
            else
            {
                Datapath.MaxSendBatchSize = 1;
            }

            int MessageCount = BoolOk(Datapath.Features & CXPLAT_DATAPATH_FEATURE_RECV_COALESCING) ? URO_MAX_DATAGRAMS_PER_INDICATION : 1;
            for (int i = 0; i < Datapath.PartitionCount; i++)
            {
                Datapath.Partitions[i].Datapath = Datapath;
                Datapath.Partitions[i].PartitionIndex = i;
                CxPlatRefInitialize(ref Datapath.Partitions[i].RefCount);

                Datapath.Partitions[i].SendDataPool.CxPlatPoolInitialize();
                Datapath.Partitions[i].RioSendDataPool.CxPlatPoolInitialize();
                Datapath.Partitions[i].SendBufferPool.CxPlatPoolInitialize();
                Datapath.Partitions[i].LargeSendBufferPool.CxPlatPoolInitialize();
                Datapath.Partitions[i].RioSendBufferPool.CxPlatPoolInitialize();
                Datapath.Partitions[i].RioLargeSendBufferPool.CxPlatPoolInitialize();
                Datapath.Partitions[i].RecvDatagramPool.CxPlatPoolInitialize();
                Datapath.Partitions[i].RioRecvPool.CxPlatPoolInitialize();
            }

            NetLog.Assert(CxPlatRundownAcquire(CxPlatWorkerRundown));
            NewDatapath = Datapath;
            Status = QUIC_STATUS_SUCCESS;

        Error:
            if (QUIC_FAILED(Status))
            {
                if (Datapath != null)
                {
                    CXPLAT_FREE(Datapath, QUIC_POOL_DATAPATH);
                }

                if (WsaInitialized)
                {

                }
            }

        Exit:
            return Status;
        }

        static ulong SocketCreateUdp(CXPLAT_DATAPATH Datapath, CXPLAT_UDP_CONFIG Config, ref CXPLAT_SOCKET NewSocket)
        {
            ulong Status;
            bool IsServerSocket = Config.RemoteAddress == null;
            bool NumPerProcessorSockets = IsServerSocket && Datapath.PartitionCount > 1;
            int SocketCount = NumPerProcessorSockets ? CxPlatProcCount() : 1;
            object PortReservation;
            int Result;
            bool Option = false;

            NetLog.Assert(Datapath.UdpHandlers.Receive != null || BoolOk(Config.Flags & CXPLAT_SOCKET_FLAG_PCP));
            NetLog.Assert(IsServerSocket || Config.PartitionIndex < Datapath.PartitionCount);

            CXPLAT_SOCKET_RAW[] RawSocket = new CXPLAT_SOCKET_RAW[SocketCount];
            if (RawSocket == null)
            {
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                goto Error;
            }

            CXPLAT_SOCKET Socket = CxPlatRawToSocket(RawSocket);

            ZeroMemory(RawSocket, RawSocketLength);
            Socket.Datapath = Datapath;
            Socket.ClientContext = Config.CallbackContext;
            Socket.NumPerProcessorSockets = NumPerProcessorSockets ? 1 : 0;
            Socket.HasFixedRemoteAddress = Config.RemoteAddress != null;
            Socket.Type = CXPLAT_SOCKET_TYPE.CXPLAT_SOCKET_UDP;
            Socket.UseRio = Datapath.UseRio;
            Socket.UseTcp = Datapath.UseTcp;
            if (Config.LocalAddress != null)
            {
                Socket.LocalAddress = Socket.LocalAddress.MapToIPv6();
            }
            else
            {
                Socket.LocalAddress.AddressFamily = AddressFamily.InterNetworkV6;
            }
            Socket.Mtu = CXPLAT_MAX_MTU;
            if (BoolOk(Config.Flags & CXPLAT_SOCKET_FLAG_PCP))
            {
                Socket.PcpBinding = true;
            }

            CxPlatRefInitializeEx(ref Socket.RefCount, Socket.UseTcp ? 1 : SocketCount);

            if (Datapath.UseTcp)
            {
                goto Skip;
            }

            Socket.RecvBufLen = BoolOk(Datapath.Features & CXPLAT_DATAPATH_FEATURE_RECV_COALESCING) ?
                    MAX_URO_PAYLOAD_LENGTH :
                    Socket.Mtu - CXPLAT_MIN_IPV4_HEADER_SIZE - CXPLAT_UDP_HEADER_SIZE;

            for (int i = 0; i < SocketCount; i++)
            {
                CxPlatRefInitialize(ref Socket.PerProcSockets[i].RefCount);
                Socket.PerProcSockets[i].Parent = Socket;
                Socket.PerProcSockets[i].Socket = null;
                CxPlatDatapathSqeInitialize(Socket.PerProcSockets[i].IoSqe.DatapathSqe, CXPLAT_CQE_TYPE_SOCKET_IO);
                CxPlatRundownInitialize(Socket.PerProcSockets[i].RundownRef);
                Socket.PerProcSockets[i].RioCq = RIO_INVALID_CQ;
                Socket.PerProcSockets[i].RioRq = RIO_INVALID_RQ;
                CxPlatListInitializeHead(Socket.PerProcSockets[i].RioSendOverflow);
            }

            for (int i = 0; i < SocketCount; i++)
            {
                CXPLAT_SOCKET_PROC SocketProc = Socket.PerProcSockets[i];
                int PartitionIndex = Config.RemoteAddress != null ? Config.PartitionIndex : i % Datapath.PartitionCount;
                uint BytesReturned;

                SocketProc.Socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                SocketProc.Socket.UseOnlyOverlappedIO = true;

                if (SocketProc.Socket == null)
                {
                    Status = QUIC_STATUS_SUCCESS;
                    goto Error;
                }

                Option = false;
                SocketProc.Socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, Option);
                if (Config.RemoteAddress == null)
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        //int Processor = i; // API only supports 16-bit proc index.
                        //Result = SocketProc.Socket.IOControl(IOControlCode.BindToInterface, ref Processor, sizeof(Processor), null, 0, out BytesReturned);
                        //SIO_CPU_AFFINITY,
                        //        &Processor,
                        //        sizeof(Processor),
                        //        NULL,
                        //        0,
                        //        &BytesReturned,
                        //        NULL,
                        //        NULL);
                        //if (Result != NO_ERROR)
                        //{
                        //    int WsaError = WSAGetLastError();
                        //    QuicTraceEvent(
                        //        DatapathErrorStatus,
                        //        "[data][%p] ERROR, %u, %s.",
                        //        Socket,
                        //        WsaError,
                        //        "SIO_CPU_AFFINITY");
                        //    Status = HRESULT_FROM_WIN32(WsaError);
                        //    goto Error;
                        //}
                    }
                }

                SocketProc.Socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DontFragment, true);
                SocketProc.Socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.DontFragment, true);
                SocketProc.Socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.PacketInformation, true);
                SocketProc.Socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
                //SocketProc.Socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.ECN, true);
                //SocketProc.Socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ECN, true);
                SocketProc.Socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReceiveBuffer, int.MaxValue);


                if (BoolOk(Datapath.Features & CXPLAT_DATAPATH_FEATURE_RECV_COALESCING))
                {
                    //    SocketProc.Socket.SetSocketOption(SocketOptionLevel.Udp, SocketOptionName.ReceiveLowWater, int.MaxValue);

                    //    Option = MAX_URO_PAYLOAD_LENGTH;
                    //Result =
                    //    setsockopt(
                    //        SocketProc->Socket,
                    //        IPPROTO_UDP,
                    //        UDP_RECV_MAX_COALESCED_SIZE,
                    //        (char*)&Option,
                    //        sizeof(Option));
                    //if (Result == SOCKET_ERROR)
                    //{
                    //    int WsaError = WSAGetLastError();
                    //    QuicTraceEvent(
                    //        DatapathErrorStatus,
                    //        "[data][%p] ERROR, %u, %s.",
                    //        Socket,
                    //        WsaError,
                    //        "Set UDP_RECV_MAX_COALESCED_SIZE");
                    //    Status = HRESULT_FROM_WIN32(WsaError);
                    //    goto Error;
                    //}
                }

                NetLog.Assert(PartitionIndex < Datapath.PartitionCount);
                SocketProc.DatapathProc = Datapath.Partitions[PartitionIndex];
                CxPlatRefIncrement(ref SocketProc.DatapathProc.RefCount);

                if (Socket.UseRio)
                {

                }

                if (Config.InterfaceIndex != 0)
                {
                    SocketProc.Socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastInterface, Config.InterfaceIndex);
                    SocketProc.Socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, Config.InterfaceIndex);
                }

                if (BoolOk(Datapath.Features & CXPLAT_DATAPATH_FEATURE_PORT_RESERVATIONS) && Config.LocalAddress != null && Config.LocalAddress.nPort != 0)
                {
                    if (i == 0)
                    {
                        INET_PORT_RANGE PortRange = new INET_PORT_RANGE();
                        PortRange.StartPort = (ushort)Config.LocalAddress.nPort;
                        PortRange.NumberOfPorts = 1;

                        //Result = SocketProc.Socket.IOControl(IOControlCode.PORT_RESERVATION)
                        //    WSAIoctl(
                        //        SocketProc->Socket,
                        //        SIO_ACQUIRE_PORT_RESERVATION,
                        //        &PortRange,
                        //        sizeof(PortRange),
                        //        &PortReservation,
                        //        sizeof(PortReservation),
                        //        &BytesReturned,
                        //        NULL,
                        //        NULL);
                        //if (Result == SOCKET_ERROR)
                        //{
                        //    int WsaError = WSAGetLastError();
                        //    QuicTraceEvent(
                        //        DatapathErrorStatus,
                        //        "[data][%p] ERROR, %u, %s.",
                        //        Socket,
                        //        WsaError,
                        //        "SIO_ACQUIRE_PORT_RESERVATION");
                        //    Status = HRESULT_FROM_WIN32(WsaError);
                        //    goto Error;
                        //}
                    }
                }

                try
                {
                    SocketProc.Socket.Bind(Socket.LocalAddress.GetIPEndPoint());
                }
                catch (Exception e)
                {
                    NetLog.LogError(e.ToString());
                    goto Error;
                }

                if (Config.RemoteAddress != null)
                {
                    var MappedRemoteAddress = Config.RemoteAddress.MapToIPv6();

                    try
                    {
                        SocketProc.Socket.Connect(MappedRemoteAddress.GetIPEndPoint());

                    }
                    catch (Exception e)
                    {
                        NetLog.LogError(e.ToString());
                        goto Error;
                    }
                }

                if (i == 0)
                {
                    if (Config.LocalAddress != null && Config.LocalAddress.nPort != 0)
                    {
                        NetLog.Assert(Config.LocalAddress.nPort == Socket.LocalAddress.nPort);
                    }
                }
            }

            Socket.LocalAddress = Socket.LocalAddress.MapToIPv4();
        Skip:

            if (Config.RemoteAddress != null)
            {
                Socket.RemoteAddress = Config.RemoteAddress;
            }
            else
            {
                Socket.RemoteAddress.nPort = 0;
            }

            NewSocket = Socket;
            for (int i = 0; i < SocketCount; i++)
            {
                CxPlatDataPathStartReceiveAsync(Socket.PerProcSockets[i]);
                Socket.PerProcSockets[i].IoStarted = true;
            }

            Socket = null;
            RawSocket = null;
            Status = QUIC_STATUS_SUCCESS;
        Error:
            return Status;
        }

        static void CxPlatDataPathStartReceiveAsync(CXPLAT_SOCKET_PROC SocketProc)
        {
            SocketProc.ReceiveArgs.Completed -= CxPlatDataPathProcessReceive;
            SocketProc.ReceiveArgs.Completed += CxPlatDataPathProcessReceive;
            SocketProc.ReceiveArgs.UserToken = SocketProc;
            bool bIOSyncCompleted = false;
            if (SocketProc.Socket != null)
            {
                int MAX_RECV_RETRIES = 10;
                int RetryCount = 0;
                ulong Status = QUIC_STATUS_SUCCESS;
                while (Status == QUIC_STATUS_OUT_OF_MEMORY && ++RetryCount < MAX_RECV_RETRIES)
                {
                    try
                    {
                        bIOSyncCompleted = !SocketProc.Socket.ReceiveFromAsync(SocketProc.ReceiveArgs);
                    }
                    catch (OutOfMemoryException e)
                    {
                        Status = QUIC_STATUS_OUT_OF_MEMORY;
                    }
                    catch (Exception)
                    {
                        Status = QUIC_STATUS_SOCKET_ERROR;
                    }
                }

                if (Status == QUIC_STATUS_OUT_OF_MEMORY)
                {
                    NetLog.Assert(RetryCount == MAX_RECV_RETRIES);
                    SocketProc.RecvFailure = true;
                    Status = QUIC_STATUS_PENDING;
                }
            }
            else
            {
                SocketProc.IoStarted = false;
            }

            if (bIOSyncCompleted)
            {
                CxPlatDataPathProcessReceive(SocketProc, SocketProc.ReceiveArgs);
            }
        }

        static void CxPlatDataPathProcessReceive(object sender, SocketAsyncEventArgs IoResult)
        {
            CXPLAT_SOCKET_PROC SocketProc = sender as CXPLAT_SOCKET_PROC;
            DATAPATH_RX_PACKET M_RX_PACKET = new DATAPATH_RX_PACKET();

            QUIC_ADDR RemoteAddr = SocketProc.Parent.RemoteAddress;
            if (IoResult.SocketError != SocketError.Success)
            {
                if (IsUnreachableErrorCode(IoResult.SocketError))
                {
                    if (!SocketProc.Parent.PcpBinding)
                    {
                        SocketProc.Parent.Datapath.UdpHandlers.Unreachable(
                            SocketProc.Parent,
                            SocketProc.Parent.ClientContext,
                            RemoteAddr);
                    }
                }

                return;
            }

            QUIC_ADDR LocalAddr = SocketProc.Parent.LocalAddress;
            QUIC_ADDR RemoteAddr = SocketProc.Parent.RemoteAddress;
            RemoteAddr = RemoteAddr.MapToIPv4();

            if (IoResult.BytesTransferred == 0)
            {
                NetLog.Assert(false);
                goto Drop;
            }

            CXPLAT_RECV_DATA RecvDataChain = new CXPLAT_RECV_DATA();
            CXPLAT_RECV_DATA DatagramChainTail = RecvDataChain;
            CXPLAT_DATAPATH Datapath = SocketProc.Parent.Datapath;
            CXPLAT_RECV_DATA Datagram;

            int MessageLength = IoResult.BytesTransferred;
            int MessageCount = 0;
            bool IsCoalesced = false;
            int ECN = 0;

            NetLog.Assert(IoResult.BytesTransferred <= SocketProc.Parent.RecvBufLen);
            Datagram = M_RX_PACKET.Data;
            Datagram.Next = null;
            Datagram.Buffer = IoResult.Buffer;
            Datagram.BufferLength = MessageLength;
            Datagram.Route = IoBlock.Route;
            Datagram.PartitionIndex = SocketProc.DatapathProc.PartitionIndex % SocketProc.DatapathProc.Datapath.PartitionCount;
            Datagram.TypeOfService = (byte)ECN;
            Datagram.Allocated = true;
            Datagram.Route.DatapathType = Datagram.DatapathType = CXPLAT_DATAPATH_TYPE.CXPLAT_DATAPATH_TYPE_USER;
            Datagram.QueuedOnConnection = false;

            DatagramChainTail = Datagram;
            DatagramChainTail = Datagram.Next;

            if (IsCoalesced && ++MessageCount == URO_MAX_DATAGRAMS_PER_INDICATION)
            {

            }

            NetLog.Assert(RecvDataChain != null);
            if (!SocketProc.Parent.PcpBinding)
            {
                SocketProc.Parent.Datapath.UdpHandlers.Receive(SocketProc.Parent, SocketProc.Parent.ClientContext, ref RecvDataChain);
            }
            else
            {
                CxPlatPcpRecvCallback(SocketProc.Parent, SocketProc.Parent.ClientContext, ref RecvDataChain);
            }

        Drop:
            return;
        }

        static void CxPlatSendDataComplete(CXPLAT_SEND_DATA SendData, ulong IoResult)
        {
            CXPLAT_SOCKET_PROC SocketProc = SendData.SocketProc;
            if (IoResult != QUIC_STATUS_SUCCESS)
            {

            }
            SendDataFree(SendData);
        }

        static void CxPlatSocketContextRelease(CXPLAT_SOCKET_PROC SocketProc)
        {
            NetLog.Assert(!SocketProc.Freed);
            if (CxPlatRefDecrement(ref SocketProc.RefCount))
            {
                if (SocketProc.Parent.Type != CXPLAT_SOCKET_TYPE.CXPLAT_SOCKET_TCP_LISTENER)
                {
                    NetLog.Assert(SocketProc.RioRecvCount == 0);
                    NetLog.Assert(SocketProc.RioSendCount == 0);
                    NetLog.Assert(SocketProc.RioNotifyArmed == false);

                    while (!CxPlatListIsEmpty(SocketProc.RioSendOverflow))
                    {
                        CXPLAT_LIST_ENTRY Entry = CxPlatListRemoveHead(SocketProc.RioSendOverflow);
                        CxPlatSendDataComplete(CXPLAT_CONTAINING_RECORD<CXPLAT_SEND_DATA>(Entry), WSA_OPERATION_ABORTED);
                    }
                }
                else
                {
                    if (SocketProc.AcceptSocket != null)
                    {
                        SocketDelete(SocketProc.AcceptSocket);
                        SocketProc.AcceptSocket = null;
                    }
                }

                CxPlatRundownUninitialize(SocketProc.RundownRef);
                if (SocketProc.DatapathProc != null)
                {
                    CxPlatProcessorContextRelease(SocketProc.DatapathProc);
                }

                SocketProc.Freed = true;
                CxPlatSocketRelease(SocketProc.Parent);
            }
        }

        static void SendDataFree(CXPLAT_SEND_DATA SendData)
        {
            for (int i = 0; i < SendData.WsaBufferCount; ++i)
            {
                SendData.BufferPool.CxPlatPoolFree(SendData.WsaBuffers[i]);
            }

            SendData.SendDataPool.CxPlatPoolFree(SendData);
        }

        static void CxPlatSocketFreeRxIoBlock(DATAPATH_RX_IO_BLOCK IoBlock)
        {
            IoBlock.OwningPool.CxPlatPoolFree(IoBlock);
        }

        static bool IsUnreachableErrorCode(SocketError ErrorCode)
        {
            return ErrorCode == SocketError.NetworkUnreachable ||
            ErrorCode == SocketError.HostUnreachable ||
            ErrorCode == SocketError.ConnectionReset;
        }

        static bool CxPlatSendDataCanAllocSendSegment(CXPLAT_SEND_DATA SendData, int MaxBufferLength)
        {
            if (SendData.ClientBuffer.Buffer == null)
            {
                return false;
            }

            NetLog.Assert(SendData.SegmentSize > 0);
            NetLog.Assert(SendData.WsaBufferCount > 0);
            int BytesAvailable = CXPLAT_LARGE_SEND_BUFFER_SIZE - SendData.WsaBuffers[SendData.WsaBufferCount - 1].Length - SendData.ClientBuffer.Length;
            return MaxBufferLength <= BytesAvailable;
        }

        static bool CxPlatSendDataCanAllocSend(CXPLAT_SEND_DATA SendData, int MaxBufferLength)
        {
            return (SendData.WsaBufferCount < SendData.Owner.Datapath.MaxSendBatchSize) ||
                ((SendData.SegmentSize > 0) && CxPlatSendDataCanAllocSendSegment(SendData, MaxBufferLength));
        }

        static QUIC_BUFFER CxPlatSendDataAllocDataBuffer(CXPLAT_SEND_DATA SendData)
        {
            NetLog.Assert(SendData.WsaBufferCount < SendData.Owner.Datapath.MaxSendBatchSize);
            QUIC_BUFFER WsaBuffer = SendData.WsaBuffers[SendData.WsaBufferCount];
            WsaBuffer.Buffer = SendData.BufferPool.CxPlatPoolAlloc();
            if (WsaBuffer.Buffer == null)
            {
                return null;
            }
            ++SendData.WsaBufferCount;
            return WsaBuffer;
        }

        static QUIC_BUFFER CxPlatSendDataAllocPacketBuffer(CXPLAT_SEND_DATA SendData,int MaxBufferLength)
        {
            QUIC_BUFFER WsaBuffer = CxPlatSendDataAllocDataBuffer(SendData);
            if (WsaBuffer != null)
            {
                WsaBuffer.Length = MaxBufferLength;
            }
            return (QUIC_BUFFER)WsaBuffer;
        }

        static QUIC_BUFFER CxPlatSendDataAllocSegmentBuffer(CXPLAT_SEND_DATA SendData, int MaxBufferLength)
        {
            NetLog.Assert(SendData.SegmentSize > 0);
            NetLog.Assert(MaxBufferLength <= SendData.SegmentSize);

            if (CxPlatSendDataCanAllocSendSegment(SendData, MaxBufferLength))
            {
                SendData.ClientBuffer.Length = MaxBufferLength;
                return (QUIC_BUFFER)SendData.ClientBuffer;
            }

            QUIC_BUFFER WsaBuffer = CxPlatSendDataAllocDataBuffer(SendData);
            if (WsaBuffer == null)
            {
                return null;
            }
            
            WsaBuffer.Length = 0;
            SendData.ClientBuffer.Buffer = WsaBuffer.Buffer;
            SendData.ClientBuffer.Length = MaxBufferLength;
            return (QUIC_BUFFER)SendData.ClientBuffer;
        }

    }
}









