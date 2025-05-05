/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp4LinuxTcp.Common;
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_ADDR
    {
        public AddressFamily AddressFamily;
        public IPAddress Ip;
        public int nPort;

        public QUIC_ADDR()
        {
            
        }

        public QUIC_ADDR(IPEndPoint mIPEndPoint)
        {
            AddressFamily = mIPEndPoint.AddressFamily;
            Ip = mIPEndPoint.Address;
            nPort = mIPEndPoint.Port;
        }

        public Byte[] GetBytes()
        {
            return Ip.GetAddressBytes();
        }

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
        public const int sizeof_Length = 100;
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
        public const int sizeof_Length = 100;
        public DATAPATH_RX_IO_BLOCK IoBlock;
        public CXPLAT_RECV_DATA Data;
    }

    internal static partial class MSQuicFunc
    {
        public static CXPLAT_DATAPATH_RECEIVE_CALLBACK CxPlatPcpRecvCallback;
        static ulong DataPathInitialize(int ClientRecvDataLength, CXPLAT_UDP_DATAPATH_CALLBACKS UdpCallbacks, QUIC_EXECUTION_CONFIG Config, ref CXPLAT_DATAPATH NewDatapath)
        {
            int WsaError;
            ulong Status;
            int PartitionCount = CxPlatProcCount();
            int DatapathLength;
            CXPLAT_DATAPATH Datapath = null;
            bool WsaInitialized = false;

            if (UdpCallbacks != null)
            {
                if (UdpCallbacks.Receive == null || UdpCallbacks.Unreachable == null)
                {
                    Status = QUIC_STATUS_INVALID_PARAMETER;
                    goto Exit;
                }
            }

            //if (!CxPlatWorkersLazyStart(Config))
            //{
            //    Status = QUIC_STATUS_OUT_OF_MEMORY;
            //    goto Exit;
            //}

            WsaInitialized = true;
            if (Config != null && Config.ProcessorList.Count > 0)
            {
                PartitionCount = Config.ProcessorList.Count;
            }

            Datapath = new CXPLAT_DATAPATH();
            Datapath.Partitions = new CXPLAT_DATAPATH_PROC[PartitionCount];
            if (Datapath == null)
            {
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                goto Error;
            }

            if (UdpCallbacks != null)
            {
                Datapath.UdpHandlers = UdpCallbacks;
            }

            Datapath.PartitionCount = PartitionCount;
            CxPlatRefInitializeEx(ref Datapath.RefCount, Datapath.PartitionCount);
            Datapath.UseRio = Config != null && BoolOk(Config.Flags & QUIC_EXECUTION_CONFIG_FLAG_RIO);

            if (BoolOk(Datapath.Features & CXPLAT_DATAPATH_FEATURE_SEND_SEGMENTATION))
            {
                Datapath.MaxSendBatchSize = CXPLAT_MAX_BATCH_SEND;
            }
            else
            {
                Datapath.MaxSendBatchSize = 1;
            }

            int MessageCount = BoolOk(Datapath.Features & CXPLAT_DATAPATH_FEATURE_RECV_COALESCING) ? URO_MAX_DATAGRAMS_PER_INDICATION : 1;
            Datapath.DatagramStride = DATAPATH_RX_PACKET.sizeof_Length + ClientRecvDataLength;
            Datapath.RecvPayloadOffset = DATAPATH_RX_IO_BLOCK.sizeof_Length + MessageCount * Datapath.DatagramStride;
            int RecvDatagramLength = Datapath.RecvPayloadOffset + (BoolOk(Datapath.Features & CXPLAT_DATAPATH_FEATURE_RECV_COALESCING) ?
                    MAX_URO_PAYLOAD_LENGTH : MAX_RECV_PAYLOAD_LENGTH);

            for (int i = 0; i < Datapath.PartitionCount; i++)
            {
                Datapath.Partitions[i] = new CXPLAT_DATAPATH_PROC();
                Datapath.Partitions[i].Datapath = Datapath;
                Datapath.Partitions[i].PartitionIndex = i;
                CxPlatRefInitialize(ref Datapath.Partitions[i].RefCount);

                Datapath.Partitions[i].SendDataPool.CxPlatPoolInitialize();
                Datapath.Partitions[i].SendBufferPool.CxPlatPoolInitialize();
                Datapath.Partitions[i].LargeSendBufferPool.CxPlatPoolInitialize();
                Datapath.Partitions[i].RecvDatagramPool.CxPlatPoolInitialize();
            }

            NewDatapath = Datapath;
            Status = QUIC_STATUS_SUCCESS;
        Error:

        Exit:
            return Status;
        }

        static ulong SocketCreateUdp(CXPLAT_DATAPATH Datapath, CXPLAT_UDP_CONFIG Config, ref CXPLAT_SOCKET NewSocket)
        {
            ulong Status = 0;
            bool IsServerSocket = Config.RemoteAddress == null;
            bool NumPerProcessorSockets = IsServerSocket && Datapath.PartitionCount > 1;
            int SocketCount = NumPerProcessorSockets ? CxPlatProcCount() : 1;
            object PortReservation;
            int Result;
            bool Option = false;

            NetLog.Assert(Datapath.UdpHandlers.Receive != null || BoolOk(Config.Flags & CXPLAT_SOCKET_FLAG_PCP));
            NetLog.Assert(IsServerSocket || Config.PartitionIndex < Datapath.PartitionCount);

            CXPLAT_SOCKET Socket = new CXPLAT_SOCKET();
            Socket.Datapath = Datapath;
            Socket.ClientContext = Config.CallbackContext;
            Socket.NumPerProcessorSockets = NumPerProcessorSockets ? 1 : 0;
            Socket.HasFixedRemoteAddress = Config.RemoteAddress != null;
            Socket.Type = CXPLAT_SOCKET_TYPE.CXPLAT_SOCKET_UDP;

            if (Config.LocalAddress != null)
            {
                Socket.LocalAddress = Config.LocalAddress.MapToIPv6();
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

            CxPlatRefInitializeEx(ref Socket.RefCount, SocketCount);

            Socket.RecvBufLen = BoolOk(Datapath.Features & CXPLAT_DATAPATH_FEATURE_RECV_COALESCING) ? 
                MAX_URO_PAYLOAD_LENGTH : Socket.Mtu - CXPLAT_MIN_IPV4_HEADER_SIZE - CXPLAT_UDP_HEADER_SIZE;

            Socket.PerProcSockets = new CXPLAT_SOCKET_PROC[SocketCount];
            for (int i = 0; i < SocketCount; i++)
            {
                Socket.PerProcSockets[i] = new CXPLAT_SOCKET_PROC();
                CxPlatRefInitialize(ref Socket.PerProcSockets[i].RefCount);
                Socket.PerProcSockets[i].Parent = Socket;
                Socket.PerProcSockets[i].Socket = null;
                CxPlatRundownInitialize(Socket.PerProcSockets[i].RundownRef);
            }

            for (int i = 0; i < SocketCount; i++)
            {
                CXPLAT_SOCKET_PROC SocketProc = Socket.PerProcSockets[i];
                int PartitionIndex = Config.RemoteAddress != null ? Config.PartitionIndex : i % Datapath.PartitionCount;
                uint BytesReturned;

                SocketProc.Socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                SocketProc.Socket.UseOnlyOverlappedIO = true;
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

                SocketProc.Socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
                SocketProc.Socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.PacketInformation, true);

                //SocketProc.Socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ECN, true);
                //SocketProc.Socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.ECN, true);
                
                SocketProc.Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, int.MaxValue);
                SocketProc.Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

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
            
            NewSocket = Socket;
            for (int i = 0; i < SocketCount; i++)
            {
                CxPlatDataPathStartReceiveAsync(Socket.PerProcSockets[i]);
                Socket.PerProcSockets[i].IoStarted = true;
            }

            Socket = null;
            //RawSocket = null;
            Status = QUIC_STATUS_SUCCESS;
        Error:
            return Status;
        }

        static void CxPlatDataPathStartReceiveAsync(CXPLAT_SOCKET_PROC SocketProc)
        {
            SocketProc.ReceiveArgs.Completed -= CxPlatDataPathUdpRecvComplete;
            SocketProc.ReceiveArgs.Completed += CxPlatDataPathUdpRecvComplete;
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
                CxPlatDataPathUdpRecvComplete(SocketProc, SocketProc.ReceiveArgs);
            }
        }

        static bool CxPlatDataPathUdpRecvComplete(SocketAsyncEventArgs arg)
        {
            DATAPATH_RX_IO_BLOCK IoBlock = arg.UserToken as DATAPATH_RX_IO_BLOCK;
            CXPLAT_SOCKET_PROC SocketProc = IoBlock.SocketProc;
            DATAPATH_RX_PACKET M_RX_PACKET = new DATAPATH_RX_PACKET();

            QUIC_ADDR LocalAddr = IoBlock.Route.LocalAddress;
            QUIC_ADDR RemoteAddr = IoBlock.Route.RemoteAddress;
            RemoteAddr = RemoteAddr.MapToIPv4();

            if (IoResult.SocketError != SocketError.Success)
            {
                if (IsUnreachableErrorCode(IoResult.SocketError))
                {
                    if (!SocketProc.Parent.PcpBinding)
                    {
                        SocketProc.Parent.Datapath.UdpHandlers.Unreachable(SocketProc.Parent, SocketProc.Parent.ClientContext, RemoteAddr);
                    }
                }

                return;
            }

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
            Datagram.Buffer.Buffer =  IoResult.Buffer;
            Datagram.Buffer.Length = MessageLength;
            //Datagram.Route = IoBlock.Route;
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
                SocketProc.Parent.Datapath.UdpHandlers.Receive(SocketProc.Parent, SocketProc.Parent.ClientContext, RecvDataChain);
            }
            else
            {
                CxPlatPcpRecvCallback(SocketProc.Parent, SocketProc.Parent.ClientContext, RecvDataChain);
            }

        Drop:
            return;
        }

        static void CxPlatSendDataComplete(CXPLAT_SEND_DATA SendData)
        {
            CXPLAT_SOCKET_PROC SocketProc = SendData.SocketProc;
            SendDataFree(SendData);
        }

        static void CxPlatSocketContextRelease(CXPLAT_SOCKET_PROC SocketProc)
        {
            NetLog.Assert(!SocketProc.Freed);
            if (CxPlatRefDecrement(ref SocketProc.RefCount))
            {
                if (SocketProc.Parent.Type != CXPLAT_SOCKET_TYPE.CXPLAT_SOCKET_TCP_LISTENER)
                {
                   
                }
                else
                {
                    if (SocketProc.AcceptSocket != null)
                    {
                        SocketDelete(SocketProc.AcceptSocket);
                        SocketProc.AcceptSocket = null;
                    }
                }

                //CxPlatRundownUninitialize(SocketProc.RundownRef);
                //if (SocketProc.DatapathProc != null)
                //{
                //    CxPlatProcessorContextRelease(SocketProc.DatapathProc);
                //}

                //SocketProc.Freed = true;
                //CxPlatSocketRelease(SocketProc.Parent);
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
            WsaBuffer = SendData.BufferPool.CxPlatPoolAlloc();
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

        static ulong CxPlatSocketSendEnqueue(CXPLAT_ROUTE Route, CXPLAT_SEND_DATA SendData)
        {
            SendData.LocalAddress = Route.LocalAddress;
            //CxPlatDatapathSqeInitialize(&SendData->Sqe.DatapathSqe, CXPLAT_CQE_TYPE_SOCKET_IO);
            //    CxPlatStartDatapathIo(SendData->SocketProc, &SendData->Sqe, DATAPATH_IO_QUEUE_SEND);
            //    QUIC_STATUS Status = CxPlatSocketEnqueueSqe(SendData->SocketProc, &SendData->Sqe, 0);
            //if (QUIC_FAILED(Status)) {
            //    CxPlatCancelDatapathIo(SendData->SocketProc, &SendData->Sqe);
            //}
            //return Status;
            return 0;
        }

        static void CxPlatDataPathSocketProcessReceive(SocketAsyncEventArgs arg)
        {
            DATAPATH_RX_IO_BLOCK IoBlock = arg.UserToken as DATAPATH_RX_IO_BLOCK;
            int BytesTransferred = arg.BytesTransferred;
            SocketError IoResult = arg.SocketError;

            CXPLAT_SOCKET_PROC SocketProc = IoBlock.SocketProc;

            NetLog.Assert(!SocketProc.Freed);
            if (!CxPlatRundownAcquire(SocketProc.RundownRef))
            {
                CxPlatSocketContextRelease(SocketProc);
                return;
            }

            NetLog.Assert(!SocketProc.Uninitialized);

            for (int InlineReceiveCount = 10; InlineReceiveCount > 0; InlineReceiveCount--)
            {
                CxPlatSocketContextRelease(SocketProc);
                if (!CxPlatDataPathUdpRecvComplete(arg) ||!CxPlatDataPathStartReceive(
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

        static void DataPathProcessCqe(object Cqe, SocketAsyncEventArgs arg)
        {
            CXPLAT_SOCKET_PROC SocketProc = null;
            switch (arg.LastOperation)
            {
                case  SocketAsyncOperation.ReceiveFrom:
                    NetLog.Assert(arg.BytesTransferred <= ushort.MaxValue);
                    CxPlatDataPathSocketProcessReceive(arg);
                    break;

                case DATAPATH_IO_SEND:
                    SocketProc = CONTAINING_RECORD(Sqe, CXPLAT_SEND_DATA, Sqe)->SocketProc;
                    CxPlatSendDataComplete(
                    CONTAINING_RECORD(Sqe, CXPLAT_SEND_DATA, Sqe),
                        RtlNtStatusToDosError((NTSTATUS)Cqe->Internal));
                    break;

                case DATAPATH_IO_QUEUE_SEND:
                    SocketProc = CONTAINING_RECORD(Sqe, CXPLAT_SEND_DATA, Sqe)->SocketProc;
                    CxPlatDataPathSocketProcessQueuedSend(Sqe, Cqe);
                    break;

                case DATAPATH_IO_ACCEPTEX:
                    SocketProc = CONTAINING_RECORD(Sqe, CXPLAT_SOCKET_PROC, IoSqe);
                    CxPlatDataPathSocketProcessAcceptCompletion(Sqe, Cqe);
                    break;

                case DATAPATH_IO_CONNECTEX:
                    SocketProc = CONTAINING_RECORD(Sqe, CXPLAT_SOCKET_PROC, IoSqe);
                    CxPlatDataPathSocketProcessConnectCompletion(Sqe, Cqe);
                    break;

                case DATAPATH_IO_RIO_NOTIFY:
                    SocketProc = CONTAINING_RECORD(Sqe, CXPLAT_SOCKET_PROC, RioSqe);
                    CxPlatDataPathSocketProcessRioCompletion(Sqe, Cqe);
                    break;

                case DATAPATH_IO_RECV_FAILURE:
                    //
                    // N.B. We don't set SocketProc here because receive completions are
                    // special (they loop internally).
                    //
                    CxPlatDataPathSocketProcessReceive(
                        CONTAINING_RECORD(Sqe, DATAPATH_RX_IO_BLOCK, Sqe),
                    0,
                        (ULONG)Cqe->dwNumberOfBytesTransferred);
                    break;

                default:
                    CXPLAT_DBG_ASSERT(FALSE);
                    break;
            }

            if (SocketProc)
            {
                CxPlatSocketContextRelease(SocketProc);
            }

        }
    }
}









