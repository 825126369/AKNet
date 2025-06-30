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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AKNet.Udp5MSQuic.Common
{
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

    internal class INET_PORT_RANGE
    {
        public ushort StartPort;
        public ushort NumberOfPorts;
    }

    internal class DATAPATH_RX_IO_BLOCK
    {
        public DATAPATH_RX_PACKET CXPLAT_CONTAINING_RECORD;
        public CXPLAT_POOL<DATAPATH_RX_PACKET> OwningPool = null;
        public CXPLAT_SOCKET_PROC SocketProc;
        public long ReferenceCount;

        public readonly CXPLAT_ROUTE Route = new CXPLAT_ROUTE();
        public SocketAsyncEventArgs ReceiveArgs;
    }

    internal class DATAPATH_RX_PACKET : CXPLAT_POOL_Interface<DATAPATH_RX_PACKET>
    {
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

        public void Reset()
        {
            
        }
    }

    internal static partial class MSQuicFunc
    {
        public static CXPLAT_DATAPATH_RECEIVE_CALLBACK CxPlatPcpRecvCallback;
        static int DataPathInitialize(int ClientRecvDataLength, CXPLAT_UDP_DATAPATH_CALLBACKS UdpCallbacks, QUIC_EXECUTION_CONFIG Config, ref CXPLAT_DATAPATH NewDatapath)
        {
            int WsaError;
            int Status;
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
            Datapath.RecvDatagramLength = MessageCount * (BoolOk(Datapath.Features & CXPLAT_DATAPATH_FEATURE_RECV_COALESCING) ? MAX_URO_PAYLOAD_LENGTH : MAX_RECV_PAYLOAD_LENGTH);

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

        static int SocketCreateUdp(CXPLAT_DATAPATH Datapath, CXPLAT_UDP_CONFIG Config, ref CXPLAT_SOCKET NewSocket)
        {
            int Status = 0;
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
                uint BytesReturned;

                SocketProc.Socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                SocketProc.Socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false); //同时接收IPV4 和IPV6数据包    
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
                    //SocketProc.Socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastInterface, Config.InterfaceIndex);
                    //SocketProc.Socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, Config.InterfaceIndex);
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
                    var MappedRemoteAddress = Config.RemoteAddress;

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
                    //如果客户端/服务器 没有指定端口,也就是端口==0的时候，Socket bind 后，会自动分配一个本地端口
                    Socket.LocalAddress = new QUIC_ADDR(SocketProc.Socket.LocalEndPoint as IPEndPoint);
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
            DATAPATH_RX_IO_BLOCK IoBlock = CxPlatSocketAllocRxIoBlock(SocketProc);
            if (IoBlock == null)
            {
                return false;
            }
            //NetLog.Log("分配");
            IoBlock.ReceiveArgs.UserToken = IoBlock;
            IoBlock.ReceiveArgs.SetBuffer(0, SocketProc.Parent.RecvBufLen);
            bool bIOSyncCompleted = false;
            try
            {
                bIOSyncCompleted = !SocketProc.Socket.ReceiveMessageFromAsync(IoBlock.ReceiveArgs);
            }
            catch (Exception e)
            {
                NetLog.LogError(e.ToString());
                return false;
            }

            if (bIOSyncCompleted)
            {
                NetLog.Assert(IoBlock.ReceiveArgs.BytesTransferred < ushort.MaxValue);
                CxPlatDataPathSocketProcessReceive(IoBlock.ReceiveArgs);
            }
            return true;
        }

        static bool CxPlatDataPathUdpRecvComplete(SocketAsyncEventArgs arg)
        {
            DATAPATH_RX_IO_BLOCK IoBlock = arg.UserToken as DATAPATH_RX_IO_BLOCK;
            CXPLAT_SOCKET_PROC SocketProc = IoBlock.SocketProc;

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
                if(mIPPacketInformation != null)
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
                for (;NumberOfBytesTransferred != 0; NumberOfBytesTransferred -= MessageLength)
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
                    Datagram.Route.DatapathType = Datagram.DatapathType = CXPLAT_DATAPATH_TYPE.CXPLAT_DATAPATH_TYPE_USER;
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
            //NetLog.Log("释放");
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
                mList.Add(new ArraySegment<byte>(v.Buffer, v.Offset, v.Buffer.Length));
            }

            NetLog.Log("SendData.WsaBuffers.Count: " + SendData.WsaBuffers.Count);

            SendData.Sqe.RemoteEndPoint = SendData.MappedRemoteAddress.GetIPEndPoint();
            SendData.Sqe.UserToken = SendData;
            SendData.Sqe.BufferList = mList;
            CxPlatSocketEnqueueSqe(SendData.SocketProc, SendData.Sqe);
            return 0;
        }

        static int CxPlatSocketEnqueueSqe(CXPLAT_SOCKET_PROC SocketProc, SocketAsyncEventArgs arg)
        {
            NetLog.Assert(!SocketProc.Uninitialized);

            NetLog.Log($"SendToAsync Length:  {arg.BufferList[0].Count}， {arg.RemoteEndPoint}");
            NetLogHelper.PrintByteArray("SendToAsync Length", arg.BufferList[0].AsSpan());
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
                SendData.ECN = Config.ECN;
                SendData.SendFlags = Config.Flags;
                SendData.SegmentSize = HasFlag(Socket.Datapath.Features, CXPLAT_DATAPATH_FEATURE_SEND_SEGMENTATION) ? Config.MaxPacketSize : 0;
                SendData.TotalSize = 0;
                SendData.WsaBuffers.Clear();
                SendData.ClientBuffer.Buffer = null;
                SendData.ClientBuffer.Length = 0;
                SendData.DatapathType = Config.Route.DatapathType = CXPLAT_DATAPATH_TYPE.CXPLAT_DATAPATH_TYPE_USER;

                SendData.Owner = DatapathProc;
                SendData.SendDataPool = SendDataPool;
                SendData.BufferPool = SendData.SegmentSize > 0 ? DatapathProc.LargeSendBufferPool : DatapathProc.SendBufferPool;

                if (SendData.Sqe == null)
                {
                    SendData.Sqe = new SocketAsyncEventArgs();
                    SendData.Sqe.Completed += DataPathProcessCqe;
                    SendData.Sqe.BufferList = new List<ArraySegment<byte>>();
                }
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
                IoBlock.Route.State =  CXPLAT_ROUTE_STATE.RouteResolved;
                IoBlock.OwningPool = OwningPool;
                IoBlock.ReferenceCount = 0;
                IoBlock.SocketProc = SocketProc;

                if (IoBlock.ReceiveArgs == null)
                {
                    IoBlock.ReceiveArgs = new SocketAsyncEventArgs();
                    IoBlock.ReceiveArgs.RemoteEndPoint = SocketProc.Parent.RemoteAddress.GetIPEndPoint();
                    IoBlock.ReceiveArgs.Completed += DataPathProcessCqe;

                    byte[] mBuf = new byte[SocketProc.Parent.Datapath.RecvDatagramLength];
                    IoBlock.ReceiveArgs.SetBuffer(mBuf, 0, mBuf.Length);
                }
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

        static void DataPathProcessCqe(object Cqe, SocketAsyncEventArgs arg)
        {
            switch (arg.LastOperation)
            {
                case  SocketAsyncOperation.ReceiveMessageFrom:
                    NetLog.Log($"ReceiveMessageFrom BytesTransferred:  {arg.BytesTransferred}");
                    NetLogHelper.PrintByteArray($"ReceiveMessageFrom BytesTransferred", arg.Buffer.AsSpan().Slice(arg.Offset, arg.BytesTransferred));
                    NetLog.Assert(arg.BytesTransferred <= ushort.MaxValue);
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









