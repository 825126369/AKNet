/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:19
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Platform;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MSQuic1
{
    //一律强制转为IpV6地址
    internal class QUIC_ADDR
    {
        public const int sizeof_Length = 28;
        public string ServerName;
        private IPEndPoint mEndPoint;
        private byte[] mAddressCache = new byte[16];
        long m_ScopeId = 0;

        public QUIC_ADDR()
        {
            mEndPoint = new IPEndPoint(IPAddress.IPv6Any, 0);
            CheckFamilyError();
        }

        public QUIC_ADDR(IPAddress otherIp, int nPort)
        {
            mEndPoint = new IPEndPoint(otherIp, nPort);
            Ip = otherIp;
            CheckFamilyError();
        }

        public QUIC_ADDR(IPEndPoint mIPEndPoint)
        {
            mEndPoint = mIPEndPoint;
            if (mEndPoint.AddressFamily == AddressFamily.InterNetwork)
            {
                mEndPoint.Address = mEndPoint.Address.MapToIPv6();
            }
            CheckFamilyError();
        }

        public IPEndPoint GetIPEndPoint()
        {
            return mEndPoint;
        }

        public ReadOnlySpan<byte> GetAddressSpan()
        {
            int nLength = 0;
            mEndPoint.Address.TryWriteBytes(mAddressCache, out nLength);
            return mAddressCache.AsSpan().Slice(0, nLength);
        }

        public int nPort
        {
            get
            {
                return mEndPoint.Port;
            }

            set
            {
                mEndPoint.Port = value;
            }
        }

        //C# IPAddress 会比较 ScopeId，而 QUIC地址比较时，没有考虑这个字段，故而，不直接赋值给IPAddress
        public long ScopeId
        {
            get
            {
                return m_ScopeId;
            }

            set
            {
                m_ScopeId = value;
            }
        }

        public IPAddress Ip
        {
            get
            {
                return mEndPoint.Address;
            }

            set
            {
                IPAddress tt = value;
                if (tt.Equals(IPAddress.Any) || tt.Equals(IPAddress.Any.MapToIPv6()))
                {
                    tt = IPAddress.IPv6Any;
                }
                else if (tt.AddressFamily == AddressFamily.InterNetwork)
                {
                    tt = tt.MapToIPv6();
                }
                mEndPoint.Address = tt;
                CheckFamilyError();
            }
        }

        public AddressFamily Family
        {
            get
            {
                CheckFamilyError();
                return mEndPoint.AddressFamily;
            }
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
            SocketAddress m = mEndPoint.Serialize();
            for(int i = 0; i< m.Size; i++)
            {
                Buffer[i] = m[i];
            }
            return m.Size;
        }

        public void WriteFrom(ReadOnlySpan<byte> Buffer)
        {
            SocketAddress m = new SocketAddress(AddressFamily.InterNetworkV6, sizeof_Length);
            for (int i = 0; i < m.Size; i++)
            {
                m[i] = Buffer[i];
            }
            mEndPoint = (IPEndPoint)mEndPoint.Create(m);
            CheckFamilyError();
        }

        public QUIC_SSBuffer ToSSBuffer()
        {
            QUIC_SSBuffer qUIC_SSBuffer = new QUIC_SSBuffer(new byte[sizeof_Length]);
            int nLength = WriteTo(qUIC_SSBuffer.GetSpan());
            qUIC_SSBuffer.Length = nLength;
            return qUIC_SSBuffer;
        }

        public void Reset()
        {
            mEndPoint = new IPEndPoint(IPAddress.IPv6Any, 0);
            ServerName = string.Empty;
            CheckFamilyError();
        }
        
        public override string ToString()
        {
            return mEndPoint.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckFamilyError()
        {
#if DEBUG
            NetLog.Assert(mEndPoint.AddressFamily == AddressFamily.InterNetworkV6);
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
        public SSocketAsyncEventArgs ReceiveArgs;
        public long nReceiveArgsSyncCount;

        public void Reset()
        {
            nReceiveArgsSyncCount = 0;
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

    internal static partial class MSQuicFunc
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
                Datapath.Partitions[i].mWorker = CxPlatWorkerPoolGetWorker(Datapath.WorkerPool, i);
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

        static unsafe int SocketCreateUdp(CXPLAT_DATAPATH Datapath, CXPLAT_UDP_CONFIG Config, out CXPLAT_SOCKET NewSocket)
        {
            int Status = 0;
            bool IsServerSocket = Config.RemoteAddress == null;
            bool NumPerProcessorSockets = IsServerSocket && Datapath.PartitionCount > 1;
            int SocketCount = NumPerProcessorSockets ? CxPlatProcCount() : 1;
            int Result;
            bool Option = false;

            NetLog.Assert(Datapath.UdpHandlers.Receive != null || BoolOk(Config.Flags & CXPLAT_SOCKET_FLAG_PCP));
            NetLog.Assert(IsServerSocket || Config.PartitionIndex < Datapath.PartitionCount);

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

                SocketProc.Socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);

                if (Config.RemoteAddress == null && Datapath.PartitionCount > 1)
                {
                    //这里是把每个Socket 分配到不同的CPU 核心上，否则只会第一个Socket 接收数据
                    uint SIO_CPU_AFFINITY = 0x98000015;
                    NetLog.Assert(SIO_CPU_AFFINITY == OSPlatformFunc.SIO_CPU_AFFINITY);
                    ushort Processor = (ushort)i; // API only supports 16-bit proc index.
                    Result = Interop.Winsock.WSAIoctl(
                            SocketProc.Socket.Handle,
                            OSPlatformFunc.SIO_CPU_AFFINITY,
                            &Processor,
                            sizeof(ushort),
                            null,
                            0,
                            out _,
                            null,
                            null);

                    if (Result != OSPlatformFunc.NO_ERROR)
                    {
                        int WsaError = Marshal.GetLastWin32Error();
                        Status = QUIC_STATUS_INTERNAL_ERROR;
                        goto Error;
                    }
                }

                SocketProc.Socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false); //同时接收IPV4 和IPV6数据包
                SocketProc.Socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DontFragment, true);
                SocketProc.Socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.DontFragment, true);
                SocketProc.Socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
                SocketProc.Socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.PacketInformation, true);
                //SocketProc.Socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.TypeOfService, 46);
                //SocketProc.Socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.TypeOfService, 46);
                //SocketProc.Socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HopLimit, true);
                //SocketProc.Socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.HopLimit, true);
                SocketProc.Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, int.MaxValue);
                //SocketProc.Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                NetLog.Assert(PartitionIndex < Datapath.PartitionCount);
                SocketProc.DatapathProc = Datapath.Partitions[PartitionIndex]; //这里设置 Socket分区
                CxPlatRefIncrement(ref SocketProc.DatapathProc.RefCount);

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
                        Status = QUIC_STATUS_INTERNAL_ERROR;
                        goto Error;
                    }
                }
                else if (Config.LocalAddress != null)
                {
                    try
                    {
                        IPEndPoint bindPoint = Socket.LocalAddress.GetIPEndPoint();
                        SocketProc.Socket.Bind(bindPoint);
                    }
                    catch (Exception e)
                    {
                        NetLog.LogError(e.ToString());
                        Status = QUIC_STATUS_INTERNAL_ERROR;
                        goto Error;
                    }
                }
                else
                {
                    NetLog.Assert(false);
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

            IoBlock.ReceiveArgs.UserToken = IoBlock;
            IoBlock.ReceiveArgs.SetBuffer(0, SocketProc.Parent.RecvBufLen);
            try
            {
                bool bIOPending = SocketProc.Socket.ReceiveMessageFromAsync(IoBlock.ReceiveArgs);
                if (!bIOPending)
                {
                    NetLog.Assert(IoBlock.ReceiveArgs.BytesTransferred < ushort.MaxValue);
                    DataPathProcessCqe2(null, IoBlock.ReceiveArgs);
                }
            }
            catch (Exception e)
            {
                NetLog.LogError(e.ToString());
                return false;
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
                bool FoundLocalAddr = false;
                byte TOS = 0;

                IPPacketInformation mIPPacketInformation = arg.ReceiveMessageFromPacketInfo;
                if (mIPPacketInformation != null)
                {
                    IPAddress Ip = mIPPacketInformation.Address;
                    LocalAddr.Ip = Ip;
                    LocalAddr.nPort = SocketProc.Parent.LocalAddress.nPort;
                    LocalAddr.ScopeId = mIPPacketInformation.Interface;

                    FoundLocalAddr = true;

                    int TypeOfService = (int)SocketProc.Socket.GetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.TypeOfService);
                    TOS = (byte)TypeOfService;
                }

                if (!FoundLocalAddr)
                {
                    NetLog.Assert(false); // Not expected in tests
                    goto Drop;
                }

                NetLog.Assert(arg.BytesTransferred <= SocketProc.Parent.RecvBufLen);

                CXPLAT_RECV_DATA Datagram = IoBlock.CXPLAT_CONTAINING_RECORD.Data;
                Datagram.CXPLAT_CONTAINING_RECORD = IoBlock.CXPLAT_CONTAINING_RECORD;
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
                    DatagramChainTail = Datagram;
                }
                IoBlock.ReferenceCount++;
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
                NetLog.Assert(false, arg.SocketError); // Not expected in test scenarios
            }

        Drop:
            if (IoBlock != null)
            {
                CxPlatSocketFreeRxIoBlock(IoBlock);
            }
            return true;
        }

        static void CxPlatSocketFreeRxIoBlock(DATAPATH_RX_IO_BLOCK IoBlock)
        {
            IoBlock.OwningPool.CxPlatPoolFree(IoBlock.CXPLAT_CONTAINING_RECORD);
        }

        static bool IsUnreachableErrorCode(SocketError ErrorCode)
        {
            return ErrorCode == SocketError.NetworkUnreachable || //10051
                ErrorCode == SocketError.HostUnreachable || //10065
                ErrorCode == SocketError.ConnectionReset; //10054
        }

        static int CxPlatSocketSendInline(QUIC_ADDR LocalAddress, CXPLAT_SEND_DATA SendData)
        {
            //NetLog.Log($"SendData.WsaBuffers.Count: {SendData.WsaBuffers.Count} {mList[0].Count}");
            //NetLog.Log($"SendToAsync Length:  {arg.BufferList[0].Count}， {arg.RemoteEndPoint}");
            //NetLogHelper.PrintByteArray("SendToAsync Length", mList[0].Count);

            IList<ArraySegment<byte>> mList = SendData.Sqe.BufferList;
            mList.Clear();
            foreach (var v in SendData.WsaBuffers)
            {
                mList.Add(new ArraySegment<byte>(v.Buffer, 0, v.Length));
            }

            SendData.Sqe.RemoteEndPoint = SendData.MappedRemoteAddress.GetIPEndPoint();
            SendData.Sqe.BufferList = mList;
            SendData.Sqe.UserToken = SendData;

            try
            {
                bool bIOPending = SendData.SocketProc.Socket.SendToAsync(SendData.Sqe);
                if (!bIOPending)
                {
                    DataPathProcessCqe2(null, SendData.Sqe);
                }
            }
            catch(Exception e)
            {
                NetLog.LogError(e.ToString());
            }

            return 0;
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
            SendData.ECN = (byte)Config.ECN;
            SendData.DSCP = (byte)Config.DSCP;
            SendData.SendFlags = Config.Flags;
            SendData.TotalSize = 0;
            SendData.Owner = DatapathProc;
            SendData.SendDataPool = SendDataPool;
            SendData.BufferPool = DatapathProc.SendBufferPool;
            SendData.WsaBuffers.Clear();

            if (SendData.Sqe == null)
            {
                SendData.Sqe = new SSocketAsyncEventArgs();
                SendData.Sqe.BufferList = new List<ArraySegment<byte>>();
                SendData.Sqe.Completed += DataPathProcessCqe3;
                SendData.Sqe.Completed2 += DataPathProcessCqe2;
            }
            return SendData;
        }

        static DATAPATH_RX_IO_BLOCK CxPlatSocketAllocRxIoBlock(CXPLAT_SOCKET_PROC SocketProc)
        {
            CXPLAT_DATAPATH_PROC DatapathProc = SocketProc.DatapathProc;
            CXPLAT_POOL<DATAPATH_RX_PACKET> OwningPool = DatapathProc.RecvDatagramPool;
            DATAPATH_RX_IO_BLOCK IoBlock = OwningPool.CxPlatPoolAlloc().IoBlock;
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
                IoBlock.ReceiveArgs.Completed += DataPathProcessCqe;
                IoBlock.ReceiveArgs.Completed2 += DataPathProcessCqe2;
            }

            return IoBlock;
        }

        static void CxPlatSendDataComplete(SocketAsyncEventArgs arg)
        {
            CXPLAT_SEND_DATA SendData = arg.UserToken as CXPLAT_SEND_DATA;
            CxPlatSendDataFree(SendData);
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

        //这是接收
        static void DataPathProcessCqe(object Cqe, SocketAsyncEventArgs arg)
        {
            DATAPATH_RX_IO_BLOCK IoBlock = arg.UserToken as DATAPATH_RX_IO_BLOCK;
            var mWorker = IoBlock.SocketProc.DatapathProc.mWorker;
            CxPlatWorkerAddNetEvent(mWorker, arg as SSocketAsyncEventArgs);
        }

        //这是发送
        static void DataPathProcessCqe3(object Cqe, SocketAsyncEventArgs arg)
        {
            CXPLAT_SEND_DATA SendData = arg.UserToken as CXPLAT_SEND_DATA;
            var mWorker = SendData.SocketProc.DatapathProc.mWorker;
            CxPlatWorkerAddNetEvent(mWorker, arg as SSocketAsyncEventArgs);
        }

        static void DataPathProcessCqe2(object Cqe, SocketAsyncEventArgs arg)
        {
            if(arg.SocketError != SocketError.Success)
            {
                NetLog.LogError("arg.SocketError: " + arg.SocketError);
            }

            switch (arg.LastOperation)
            {
                case SocketAsyncOperation.ReceiveMessageFrom:
                case SocketAsyncOperation.ReceiveFrom:
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









