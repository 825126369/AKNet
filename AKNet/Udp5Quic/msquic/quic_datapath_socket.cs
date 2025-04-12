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
using AKNet.Udp5Quic.Common;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static AKNet.Udp5Quic.Common.QUIC_CONN_STATS;

namespace AKNet.Udp5Quic.Common
{
    internal class SocketUdp
    {
        private readonly SocketAsyncEventArgs ReceiveArgs;
        private readonly SocketAsyncEventArgs SendArgs;
        private readonly object lock_mSocket_object = new object();

        readonly AkCircularSpanBuffer mSendStreamList = null;
        private Socket mSocket = null;
        private QUIC_ADDR remoteEndPoint = null;
        private string ip;
        private int port;

        bool bReceiveIOContexUsed = false;
        bool bSendIOContexUsed = false;

        ClientPeer mClientPeer;
        public SocketUdp(ClientPeer mClientPeer)
        {
            this.mClientPeer = mClientPeer;
            mClientPeer.SetSocketState(SOCKET_PEER_STATE.NONE);

            mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            mSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            NetLog.Log("Default: ReceiveBufferSize: " + mSocket.ReceiveBufferSize);
            mSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, mClientPeer.GetConfig().client_socket_receiveBufferSize);
            NetLog.Log("Fix ReceiveBufferSize: " + mSocket.ReceiveBufferSize);

            ReceiveArgs = new SocketAsyncEventArgs();
            ReceiveArgs.SetBuffer(new byte[Config.nUdpPackageFixedSize], 0, Config.nUdpPackageFixedSize);
            ReceiveArgs.Completed += ProcessReceive;

            SendArgs = new SocketAsyncEventArgs();
            SendArgs.SetBuffer(new byte[Config.nUdpPackageFixedSize], 0, Config.nUdpPackageFixedSize);
            SendArgs.Completed += ProcessSend;

            bReceiveIOContexUsed = false;
            bSendIOContexUsed = false;

            mSendStreamList = new AkCircularSpanBuffer();
        }

        public void ConnectServer(string ip, int nPort)
        {
            this.port = nPort;
            this.ip = ip;
            remoteEndPoint = new QUIC_ADDR(IPAddress.Parse(ip), port);
            ReceiveArgs.RemoteEndPoint = remoteEndPoint;
            SendArgs.RemoteEndPoint = remoteEndPoint;

            ConnectServer();
            StartReceiveEventArg();
        }

        public void ConnectServer()
        {
            mClientPeer.mUDPLikeTCPMgr.SendConnect();
        }

        public void ReConnectServer()
        {
            mClientPeer.mUDPLikeTCPMgr.SendConnect();
        }

        public QUIC_ADDR GetIPEndPoint()
        {
            return remoteEndPoint;
        }

        public bool DisConnectServer()
        {
            var mSocketPeerState = mClientPeer.GetSocketState();
            if (mSocketPeerState == SOCKET_PEER_STATE.CONNECTED || mSocketPeerState == SOCKET_PEER_STATE.CONNECTING)
            {
                mClientPeer.mUDPLikeTCPMgr.SendDisConnect();
                return false;
            }
            else
            {
                return true;
            }
        }

        private void StartReceiveEventArg()
        {
            bool bIOSyncCompleted = false;
            if (mSocket != null)
            {
                try
                {
                    bIOSyncCompleted = !mSocket.ReceiveFromAsync(ReceiveArgs);
                }
                catch (Exception e)
                {
                    bReceiveIOContexUsed = false;
                    DisConnectedWithException(e);
                }
            }
            else
            {
                bReceiveIOContexUsed = false;
            }

            if (bIOSyncCompleted)
            {
                ProcessReceive(null, ReceiveArgs);
            }
        }

        private void StartSendEventArg()
        {
            bool bIOSyncCompleted = false;
            if (mSocket != null)
            {
                try
                {
                    bIOSyncCompleted = !mSocket.SendToAsync(SendArgs);
                }
                catch (Exception e)
                {
                    bSendIOContexUsed = false;
                    DisConnectedWithException(e);
                }
            }
            else
            {
                bSendIOContexUsed = false;
            }

            if (bIOSyncCompleted)
            {
                ProcessSend(null, SendArgs);
            }
        }

        private void ProcessReceive(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
            {
                mClientPeer.mMsgReceiveMgr.MultiThreading_ReceiveWaitCheckNetPackage(e);
            }
            StartReceiveEventArg();
        }

        private void ProcessSend(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                SendNetStream2(e.BytesTransferred);
            }
            else
            {
                bSendIOContexUsed = false;
                DisConnectedWithSocketError(e.SocketError);
            }
        }

        public void SendNetPackage(ReadOnlySpan<byte> mPackage)
        {
            MainThreadCheck.Check();

            lock (mSendStreamList)
            {
                mSendStreamList.WriteFrom(mPackage);
            }

            if (!bSendIOContexUsed)
            {
                bSendIOContexUsed = true;
                SendNetStream2();
            }
        }

        int nLastSendBytesCount = 0;
        private void SendNetStream2(int BytesTransferred = -1)
        {
            if (BytesTransferred >= 0)
            {
                if (BytesTransferred != nLastSendBytesCount)
                {
                    NetLog.LogError("UDP 发生短写");
                }
            }

            var mSendArgSpan = SendArgs.Buffer.AsSpan();
            int nSendBytesCount = 0;
            lock (mSendStreamList)
            {
                nSendBytesCount += mSendStreamList.WriteTo(mSendArgSpan);
            }

            if (nSendBytesCount > 0)
            {
                nLastSendBytesCount = nSendBytesCount;
                SendArgs.SetBuffer(0, nSendBytesCount);
                StartSendEventArg();
            }
            else
            {
                bSendIOContexUsed = false;
            }
        }

        public void DisConnectedWithNormal()
        {
            NetLog.Log("客户端 正常 断开服务器 ");
            mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
        }

        private void DisConnectedWithException(Exception e)
        {
            if (mSocket != null)
            {
                NetLog.LogException(e);
            }
            DisConnectedWithError();
        }

        private void DisConnectedWithSocketError(SocketError e)
        {
            DisConnectedWithError();
        }

        private void DisConnectedWithError()
        {
            var mSocketPeerState = mClientPeer.GetSocketState();
            if (mSocketPeerState == SOCKET_PEER_STATE.DISCONNECTING)
            {
                mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
            }
            else if (mSocketPeerState == SOCKET_PEER_STATE.CONNECTED || mSocketPeerState == SOCKET_PEER_STATE.CONNECTING)
            {
                mClientPeer.SetSocketState(SOCKET_PEER_STATE.RECONNECTING);
            }
        }

        private void CloseSocket()
        {
            if (mSocket != null)
            {
                Socket mSocket2 = mSocket;
                mSocket = null;

                try
                {
                    mSocket2.Close();
                }
                catch (Exception) { }
            }
        }

        public void Reset()
        {
            lock (mSendStreamList)
            {
                mSendStreamList.reset();
            }
        }

        public void Release()
        {
            DisConnectServer();
            CloseSocket();
            NetLog.Log("--------------- Client Release ----------------");
        }
    }

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

    internal class DATAPATH_RX_IO_BLOCK
    {
        public CXPLAT_POOL OwningPool;
        public CXPLAT_SOCKET_PROC SocketProc;
        public long ReferenceCount;
        public RIO_BUFFERID RioBufferId;

        public CXPLAT_ROUTE Route;
        public DATAPATH_IO_SQE Sqe;
        WSAMSG WsaMsgHdr;
        WSABUF WsaControlBuf;

        //
        // Contains the control data resulting from the receive.
        //
        char ControlBuf[
            RIO_CMSG_BASE_SIZE +
            WSA_CMSG_SPACE(sizeof(IN6_PKTINFO)) +   // IP_PKTINFO
            WSA_CMSG_SPACE(sizeof(DWORD)) +         // UDP_COALESCED_INFO
            WSA_CMSG_SPACE(sizeof(INT))             // IP_ECN
            ];

    }

    internal static partial class MSQuicFunc
    {
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
                CxPlatConvertToMappedV6(Config->LocalAddress, &Socket->LocalAddress);
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

                    //
                    // Associate the port reservation with the socket.
                    //
                    //Result =
                    //    WSAIoctl(
                    //        SocketProc->Socket,
                    //        SIO_ASSOCIATE_PORT_RESERVATION,
                    //        &PortReservation.Token,
                    //        sizeof(PortReservation.Token),
                    //        NULL,
                    //        0,
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
                    //        "SIO_ASSOCIATE_PORT_RESERVATION");
                    //    Status = HRESULT_FROM_WIN32(WsaError);
                    //    goto Error;
                    //}
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
                    //        int AssignedLocalAddressLength = sizeof(Socket->LocalAddress);
                    //Result =
                    //    getsockname(
                    //        SocketProc->Socket,
                    //        (PSOCKADDR) & Socket->LocalAddress,
                    //        &AssignedLocalAddressLength);
                    //if (Result == SOCKET_ERROR)
                    //{
                    //    int WsaError = WSAGetLastError();
                    //    QuicTraceEvent(
                    //        DatapathErrorStatus,
                    //        "[data][%p] ERROR, %u, %s.",
                    //        Socket,
                    //        WsaError,
                    //        "getsockaddress");
                    //    Status = HRESULT_FROM_WIN32(WsaError);
                    //    goto Error;
                    //}

                    if (Config.LocalAddress && Config.LocalAddress.nPort != 0)
                    {
                        NetLog.Assert(Config.LocalAddress.nPort == Socket.LocalAddress.nPort);
                    }
                }

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

                if (!Socket.UseTcp)
                {
                    for (int i = 0; i < SocketCount; i++)
                    {
                        CxPlatDataPathStartReceiveAsync(Socket.PerProcSockets[i]);
                        Socket.PerProcSockets[i].IoStarted = true;
                    }
                }

                Socket = null;
                RawSocket = null;
                Status = QUIC_STATUS_SUCCESS;
            Error:
                if (RawSocket != null)
                {
                    SocketDelete(CxPlatRawToSocket(RawSocket));
                }
                return Status;
            }
        }

        static void CxPlatDataPathStartReceiveAsync(CXPLAT_SOCKET_PROC SocketProc)
        {
            CxPlatDataPathStartReceive(SocketProc);
        }

        static bool CxPlatDataPathStartReceive(CXPLAT_SOCKET_PROC SocketProc)
        {
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

                return Status != QUIC_STATUS_PENDING;
            }
            else
            {
                SocketProc.IoStarted = false;
            }

            if (bIOSyncCompleted)
            {
                if (SocketProc.ReceiveArgs.SocketError == SocketError.Success && SocketProc.ReceiveArgs.BytesTransferred > 0)
                {
                    mClientPeer.mMsgReceiveMgr.MultiThreading_ReceiveWaitCheckNetPackage(SocketProc.ReceiveArgs);
                }
                StartReceiveEventArg();

            }
        }

        static void CxPlatDataPathSocketProcessReceive(DATAPATH_RX_IO_BLOCK IoBlock)
        {
            CXPLAT_SOCKET_PROC SocketProc = IoBlock.UserToken as CXPLAT_SOCKET_PROC;
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
                if (!CxPlatDataPathRecvComplete(SocketProc, IoBlock) || !CxPlatDataPathStartReceive(
                        SocketProc,
                        InlineReceiveCount > 1 ? &IoResult : NULL,
                        InlineReceiveCount > 1 ? &BytesTransferred : NULL,
                        InlineReceiveCount > 1 ? &IoBlock : NULL))
                {
                    break;
                }
            }

            CxPlatRundownRelease(&SocketProc->RundownRef);
        }

        static bool CxPlatDataPathRecvComplete(CXPLAT_SOCKET_PROC SocketProc, DATAPATH_RX_IO_BLOCK IoBlock)
        {
            if (SocketProc.Parent.Type == CXPLAT_SOCKET_TYPE.CXPLAT_SOCKET_UDP)
            {
                return
                    CxPlatDataPathUdpRecvComplete(
                        SocketProc,
                        IoBlock,
                        IoResult,
                        BytesTransferred);
            }
            else
            {
                Debug.Assert(false);
            }
        }

        static bool CxPlatDataPathUdpRecvComplete(CXPLAT_SOCKET_PROC SocketProc, DATAPATH_RX_IO_BLOCK IoBlock, SocketAsyncEventArgs IoResult)
        {
            if (IoBlock.SocketError != SocketError.Success)
            {
                CxPlatSocketFreeRxIoBlock(IoBlock);
                return false;
            }

            QUIC_ADDR LocalAddr = IoBlock.Route.LocalAddress;
            QUIC_ADDR RemoteAddr = IoBlock.Route.RemoteAddress;
            RemoteAddr = RemoteAddr.MapToIPv4();
            IoBlock.Route.Queue = SocketProc;

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
            else if (IoResult == ERROR_MORE_DATA || (IoResult.SocketError == SocketError.Success && SocketProc.Parent.RecvBufLen < IoResult.BytesTransferred))
            {

            }
            else if (IoResult.SocketError == SocketError.Success)
            {
                if (IoResult.BytesTransferred == 0)
                {
                    NetLog.Assert(false); // Not expected in tests
                    goto Drop;
                }

                CXPLAT_RECV_DATA RecvDataChain = null;
                CXPLAT_RECV_DATA DatagramChainTail = RecvDataChain;

                CXPLAT_DATAPATH Datapath = SocketProc.Parent.Datapath;
                CXPLAT_RECV_DATA Datagram;
                PUCHAR RecvPayload = ((PUCHAR)IoBlock) + Datapath->RecvPayloadOffset;

                bool FoundLocalAddr = false;
                ushort MessageLength = IoResult.BytesTransferred;
                int MessageCount = 0;
                bool IsCoalesced = false;
                int ECN = 0;

                if (SocketProc.Parent.UseRio)
                {
                    PRIO_CMSG_BUFFER RioRcvMsg = (PRIO_CMSG_BUFFER)IoBlock->ControlBuf;
                    IoBlock->WsaMsgHdr.Control.buf = IoBlock->ControlBuf + RIO_CMSG_BASE_SIZE;
                    IoBlock->WsaMsgHdr.Control.len = RioRcvMsg->TotalLength - RIO_CMSG_BASE_SIZE;
                }

                for (WSACMSGHDR* CMsg = CMSG_FIRSTHDR(&IoBlock->WsaMsgHdr);
                    CMsg != NULL;
                    CMsg = CMSG_NXTHDR(&IoBlock->WsaMsgHdr, CMsg))
                {

                    if (CMsg->cmsg_level == IPPROTO_IPV6)
                    {
                        if (CMsg->cmsg_type == IPV6_PKTINFO)
                        {
                            PIN6_PKTINFO PktInfo6 = (PIN6_PKTINFO)WSA_CMSG_DATA(CMsg);
                            LocalAddr->si_family = QUIC_ADDRESS_FAMILY_INET6;
                            LocalAddr->Ipv6.sin6_addr = PktInfo6->ipi6_addr;
                            LocalAddr->Ipv6.sin6_port = SocketProc->Parent->LocalAddress.Ipv6.sin6_port;
                            CxPlatConvertFromMappedV6(LocalAddr, LocalAddr);
                            LocalAddr->Ipv6.sin6_scope_id = PktInfo6->ipi6_ifindex;
                            FoundLocalAddr = TRUE;
                        }
                        else if (CMsg->cmsg_type == IPV6_ECN)
                        {
                            ECN = *(PINT)WSA_CMSG_DATA(CMsg);
                            CXPLAT_DBG_ASSERT(ECN < UINT8_MAX);
                        }
                    }
                    else if (CMsg->cmsg_level == IPPROTO_IP)
                    {
                        if (CMsg->cmsg_type == IP_PKTINFO)
                        {
                            PIN_PKTINFO PktInfo = (PIN_PKTINFO)WSA_CMSG_DATA(CMsg);
                            LocalAddr->si_family = QUIC_ADDRESS_FAMILY_INET;
                            LocalAddr->Ipv4.sin_addr = PktInfo->ipi_addr;
                            LocalAddr->Ipv4.sin_port = SocketProc->Parent->LocalAddress.Ipv6.sin6_port;
                            LocalAddr->Ipv6.sin6_scope_id = PktInfo->ipi_ifindex;
                            FoundLocalAddr = TRUE;
                        }
                        else if (CMsg->cmsg_type == IP_ECN)
                        {
                            ECN = *(PINT)WSA_CMSG_DATA(CMsg);
                            CXPLAT_DBG_ASSERT(ECN < UINT8_MAX);
                        }
                    }
                    else if (CMsg->cmsg_level == IPPROTO_UDP)
                    {
                        if (CMsg->cmsg_type == UDP_COALESCED_INFO)
                        {
                            CXPLAT_DBG_ASSERT(*(PDWORD)WSA_CMSG_DATA(CMsg) <= SocketProc->Parent->RecvBufLen);
                            MessageLength = (UINT16) * (PDWORD)WSA_CMSG_DATA(CMsg);
                            IsCoalesced = TRUE;
                        }
                    }
                }

                if (!FoundLocalAddr)
                {
                    NetLog.Assert(false); // Not expected in tests
                    goto Drop;
                }

                NetLog.Assert(IoResult.BytesTransferred <= SocketProc.Parent.RecvBufLen);
                Datagram = (CXPLAT_RECV_DATA)(IoBlock + 1);

                for (;
                    NumberOfBytesTransferred != 0;
                    NumberOfBytesTransferred -= MessageLength)
                {

                    CXPLAT_CONTAINING_RECORD(
                        Datagram, DATAPATH_RX_PACKET, Data)->IoBlock = IoBlock;

                    if (MessageLength > NumberOfBytesTransferred)
                    {
                        //
                        // The last message is smaller than all the rest.
                        //
                        MessageLength = NumberOfBytesTransferred;
                    }

                    Datagram->Next = NULL;
                    Datagram->Buffer = RecvPayload;
                    Datagram->BufferLength = MessageLength;
                    Datagram->Route = &IoBlock->Route;
                    Datagram->PartitionIndex =
                        SocketProc->DatapathProc->PartitionIndex % SocketProc->DatapathProc->Datapath->PartitionCount;
                    Datagram->TypeOfService = (uint8_t)ECN;
                    Datagram->Allocated = TRUE;
                    Datagram->Route->DatapathType = Datagram->DatapathType = CXPLAT_DATAPATH_TYPE_USER;
                    Datagram->QueuedOnConnection = FALSE;

                    RecvPayload += MessageLength;

                    //
                    // Add the datagram to the end of the current chain.
                    //
                    *DatagramChainTail = Datagram;
                    DatagramChainTail = &Datagram->Next;
                    IoBlock->ReferenceCount++;

                    Datagram = (CXPLAT_RECV_DATA*)
                        (((PUCHAR)Datagram) +
                            SocketProc->Parent->Datapath->DatagramStride);

                    if (IsCoalesced && ++MessageCount == URO_MAX_DATAGRAMS_PER_INDICATION)
                    {
                        QuicTraceLogWarning(
                            DatapathUroPreallocExceeded,
                            "[data][%p] Exceeded URO preallocation capacity.",
                            SocketProc->Parent);
                        break;
                    }
                }

                IoBlock = NULL;
                NetLog.Assert(RecvDataChain != null);
                if (!SocketProc.Parent.PcpBinding)
                {
                    SocketProc.Parent.Datapath.UdpHandlers.Receive(SocketProc.Parent,SocketProc.Parent.ClientContext, RecvDataChain);
                }
                else
                {
                    CxPlatPcpRecvCallback(SocketProc->Parent,SocketProc->Parent->ClientContext,RecvDataChain);
                }
            }
            else
            {
                NetLog.Assert(false);
            }

        Drop:
            if (IoBlock != null)
            {
                CxPlatSocketFreeRxIoBlock(IoBlock);
            }
            return true;
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

                    if (SocketProc.RioCq != RIO_INVALID_CQ)
                    {
                        SocketProc.DatapathProc.Datapath.RioDispatch.RIOCloseCompletionQueue(SocketProc.RioCq);
                        SocketProc.RioCq = RIO_INVALID_CQ;
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

        static bool IsUnreachableErrorCode(SocketError ErrorCode)
        {
            return ErrorCode == SocketError.NetworkUnreachable ||
            ErrorCode == SocketError.HostUnreachable ||
            ErrorCode == SocketError.ConnectionReset;
        }
    }
}









