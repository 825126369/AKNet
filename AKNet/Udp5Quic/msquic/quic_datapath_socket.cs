/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp5Quic.Common;
using System;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.InteropServices;

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

    internal class INET_PORT_RANGE
    {
        public ushort StartPort;
        public ushort NumberOfPorts;
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
                uint SocketFlags = WSA_FLAG_OVERLAPPED;
                uint BytesReturned;

                if (Socket.UseRio)
                {
                    SocketFlags |= WSA_FLAG_REGISTERED_IO;
                }

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
    }
}









