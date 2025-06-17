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
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace AKNet.Udp5MSQuic.Common
{
    //一律强制转为IpV6地址
    internal class QUIC_ADDR
    {
        public const int sizeof_QUIC_ADDR = 12;

        public string ServerName;
        public IPAddress Ip = IPAddress.IPv6Any;
        public int nPort;

        public QUIC_ADDR()
        {
            
        }

        public QUIC_ADDR(IPAddress Ip, int nPort)
        {
            this.Ip = Ip.MapToIPv6();
            this.nPort = nPort;
        }

        public QUIC_ADDR(IPEndPoint mIPEndPoint)
        {
            Ip = mIPEndPoint.Address;
            nPort = mIPEndPoint.Port;
        }

        public byte[] GetBytes()
        {
            return Ip.GetAddressBytes();
        }

        public IPEndPoint GetIPEndPoint()
        {
            return new IPEndPoint(Ip, nPort);
        }

        public AddressFamily Family
        {
            get { return Ip.AddressFamily; }
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

            return OutAddr;
        }

        public QUIC_ADDR MapToIPv4()
        {
            QUIC_ADDR OutAddr = new QUIC_ADDR();
            OutAddr.nPort = nPort;
            if (Ip.AddressFamily == AddressFamily.InterNetworkV6)
            {
                OutAddr.Ip = Ip.MapToIPv4();
            }
            else
            {
                OutAddr.Ip = Ip;
            }

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
            byte[] temp = Ip.GetAddressBytes();
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
        }

        public QUIC_SSBuffer ToSSBuffer()
        {
            QUIC_SSBuffer qUIC_SSBuffer = new QUIC_SSBuffer(new byte[byte.MaxValue]);
            int nLength = WriteTo(qUIC_SSBuffer.GetSpan());
            qUIC_SSBuffer.Length = nLength;
            return qUIC_SSBuffer;
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
        public CXPLAT_POOL<DATAPATH_RX_IO_BLOCK> OwningPool = null;
        public CXPLAT_SOCKET_PROC SocketProc;
        public long ReferenceCount;

        public readonly CXPLAT_ROUTE Route = new CXPLAT_ROUTE();
        public readonly SocketAsyncEventArgs ReceiveArgs = new SocketAsyncEventArgs();

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
        public readonly DATAPATH_RX_IO_BLOCK IoBlock;
        public readonly CXPLAT_RECV_DATA Data;

        public DATAPATH_RX_PACKET()
        {
            IoBlock = new DATAPATH_RX_IO_BLOCK();
            Data = new CXPLAT_RECV_DATA();
        }

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
            Datapath.RecvDatagramLength = Datapath.RecvPayloadOffset + (BoolOk(Datapath.Features & CXPLAT_DATAPATH_FEATURE_RECV_COALESCING) ?
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

        Exit:
            return Status;
        }

        static ulong SocketCreateUdp(CXPLAT_DATAPATH Datapath, CXPLAT_UDP_CONFIG Config, ref CXPLAT_SOCKET NewSocket)
        {
            ulong Status = 0;
            bool IsServerSocket = Config.RemoteAddress == null;
            bool NumPerProcessorSockets = IsServerSocket && Datapath.PartitionCount > 1;
            int SocketCount = NumPerProcessorSockets ? CxPlatProcCount() : 1;
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
                Socket.LocalAddress = Config.LocalAddress;
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
            Status = QUIC_STATUS_SUCCESS;
        Error:
            return Status;
        }

        static bool CxPlatDataPathStartReceiveAsync(CXPLAT_SOCKET_PROC SocketProc)
        {
            CXPLAT_DATAPATH Datapath = SocketProc.Parent.Datapath;

            DATAPATH_RX_IO_BLOCK IoBlock = CxPlatSocketAllocRxIoBlock(SocketProc);
            IoBlock.ReceiveArgs.Completed += DataPathProcessCqe;
            IoBlock.ReceiveArgs.UserToken = IoBlock;
            IoBlock.ReceiveArgs.SetBuffer(Datapath.RecvPayloadOffset, SocketProc.Parent.RecvBufLen);
            IoBlock.ReceiveArgs.RemoteEndPoint = SocketProc.Parent.RemoteAddress.GetIPEndPoint();
            bool bIOSyncCompleted = false;
            try
            {
                bIOSyncCompleted = !SocketProc.Socket.ReceiveFromAsync(IoBlock.ReceiveArgs);
            }
            catch (Exception e)
            {
                NetLog.LogError(e.ToString());
                return false;
            }

            if (bIOSyncCompleted)
            {
                NetLog.Assert(IoBlock.ReceiveArgs.BytesTransferred < ushort.MaxValue);
                CxPlatDataPathUdpRecvComplete(IoBlock.ReceiveArgs);
            }
            return true;
        }

        static bool CxPlatDataPathUdpRecvComplete(SocketAsyncEventArgs arg)
        {
            DATAPATH_RX_IO_BLOCK IoBlock = arg.UserToken as DATAPATH_RX_IO_BLOCK;
            CXPLAT_SOCKET_PROC SocketProc = IoBlock.SocketProc;
            DATAPATH_RX_PACKET M_RX_PACKET = new DATAPATH_RX_PACKET();

            QUIC_ADDR LocalAddr = IoBlock.Route.LocalAddress;
            QUIC_ADDR RemoteAddr = IoBlock.Route.RemoteAddress;

            if (arg.SocketError != SocketError.Success)
            {
                if (IsUnreachableErrorCode(arg.SocketError))
                {
                    if (!SocketProc.Parent.PcpBinding)
                    {
                        SocketProc.Parent.Datapath.UdpHandlers.Unreachable(SocketProc.Parent, SocketProc.Parent.ClientContext, RemoteAddr);
                    }
                }
                goto Drop;
            }
            else if (arg.SocketError == SocketError.Success)
            {
                if (arg.BytesTransferred == 0)
                {
                    NetLog.Assert(false);
                    goto Drop;
                }

                CXPLAT_RECV_DATA RecvDataChain = new CXPLAT_RECV_DATA();
                CXPLAT_RECV_DATA DatagramChainTail = RecvDataChain;
                CXPLAT_DATAPATH Datapath = SocketProc.Parent.Datapath;
                CXPLAT_RECV_DATA Datagram = null;
                
                bool IsCoalesced = false;
                int ECN = 0;
                
                NetLog.Assert(arg.BytesTransferred <= SocketProc.Parent.RecvBufLen);

                Datagram = M_RX_PACKET.Data;
               // Datagram.CXPLAT_CONTAINING_RECORD.IoBlock = IoBlock;
                Datagram.Next = null;
                Datagram.Buffer.Buffer = arg.Buffer;
                Datagram.Buffer.Offset = arg.Offset;
                Datagram.Buffer.Length = arg.BytesTransferred;
                Datagram.Route = IoBlock.Route;
                Datagram.PartitionIndex = SocketProc.DatapathProc.PartitionIndex % SocketProc.DatapathProc.Datapath.PartitionCount;
                Datagram.TypeOfService = (byte)ECN;
                Datagram.Allocated = true;
                Datagram.Route.DatapathType = Datagram.DatapathType = CXPLAT_DATAPATH_TYPE.CXPLAT_DATAPATH_TYPE_USER;
                Datagram.QueuedOnConnection = false;

                DatagramChainTail = Datagram;
                DatagramChainTail = Datagram.Next;

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

        Drop:
            return false;
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

        static ulong CxPlatSocketSendEnqueue(CXPLAT_ROUTE Route, CXPLAT_SEND_DATA SendData)
        {
            List<ArraySegment<byte>> mList = new List<ArraySegment<byte>>();
            foreach (var v in SendData.WsaBuffers)
            {
                mList.Add(new ArraySegment<byte>(v.Buffer, 0, v.Buffer.Length));
            }

            SendData.Sqe.BufferList = mList;
            SendData.Sqe.Completed += DataPathProcessCqe;
            SendData.Sqe.UserToken = SendData;
            SendData.Sqe.RemoteEndPoint = SendData.SocketProc.Socket.RemoteEndPoint;
            CxPlatSocketEnqueueSqe(SendData.SocketProc, SendData.Sqe);
            return 0;
        }

        static ulong CxPlatSocketEnqueueSqe(CXPLAT_SOCKET_PROC SocketProc, SocketAsyncEventArgs Sqe)
        {
            NetLog.Assert(!SocketProc.Uninitialized);
            NetLog.Assert(!SocketProc.Freed);
            SocketProc.Socket.SendToAsync(Sqe);
            return QUIC_STATUS_SUCCESS;
        }

        static DATAPATH_RX_IO_BLOCK CxPlatSocketAllocRxIoBlock(CXPLAT_SOCKET_PROC SocketProc)
        {
            CXPLAT_DATAPATH_PROC DatapathProc = SocketProc.DatapathProc;
            DATAPATH_RX_IO_BLOCK IoBlock;
            CXPLAT_POOL<DATAPATH_RX_IO_BLOCK> OwningPool = DatapathProc.RecvDatagramPool;

            IoBlock = OwningPool.CxPlatPoolAlloc();
            if (IoBlock != null)
            {
                IoBlock.Route.State =  CXPLAT_ROUTE_STATE.RouteResolved;
                IoBlock.OwningPool = OwningPool;
                IoBlock.ReferenceCount = 0;
                IoBlock.SocketProc = SocketProc;

                if (IoBlock.ReceiveArgs.Buffer == null)
                {
                    byte[] Buffer = new byte[SocketProc.Parent.Datapath.RecvDatagramLength];
                    IoBlock.ReceiveArgs.SetBuffer(Buffer, 0, Buffer.Length);
                }
            }
            return IoBlock;
        }

        static void SendDataFree(CXPLAT_SEND_DATA SendData)
        {
            for (int i = 0; i < SendData.WsaBuffers.Count; ++i)
            {
                SendData.BufferPool.CxPlatPoolFree(SendData.WsaBuffers[i]);
            }
            SendData.WsaBuffers.Clear();
            SendData.SendDataPool.CxPlatPoolFree(SendData);
        }
         
        static void CxPlatSendDataComplete(SocketAsyncEventArgs arg)
        {
            CXPLAT_SEND_DATA SendData = arg.UserToken as CXPLAT_SEND_DATA;
            SendData.Sqe.Completed -= DataPathProcessCqe;

            SendDataFree(SendData);
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
                if (!CxPlatDataPathUdpRecvComplete(arg) ||!CxPlatDataPathStartReceiveAsync(SocketProc))
                {
                    break;
                }
            }

            CxPlatRundownRelease(SocketProc.RundownRef);
        }

        static void DataPathProcessCqe(object Cqe, SocketAsyncEventArgs arg)
        {
            switch (arg.LastOperation)
            {
                case  SocketAsyncOperation.ReceiveFrom:
                    NetLog.Assert(arg.BytesTransferred <= ushort.MaxValue);
                    CxPlatDataPathSocketProcessReceive(arg);
                    break;

                case SocketAsyncOperation.SendTo:
                    CxPlatSendDataComplete(arg);
                    break;

                default:
                    NetLog.Assert(false);
                    break;
            }
        }
    }
}









