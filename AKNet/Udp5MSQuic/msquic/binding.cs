using AKNet.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace AKNet.Udp5MSQuic.Common
{
    internal class QUIC_BINDING
    {
        public readonly CXPLAT_LIST_ENTRY Link;
        public bool Exclusive; //独占，唯一拥有者, 即可能有多个连接使用这个绑定
        public bool ServerOwned;
        public bool Connected;
        public uint RefCount;
        public uint RandomReservedVersion;
        public uint CompartmentId;
        public CXPLAT_SOCKET Socket;
        public readonly ReaderWriterLockSlim RwLock = new ReaderWriterLockSlim();
        public readonly CXPLAT_LIST_ENTRY<QUIC_LISTENER> Listeners = new CXPLAT_LIST_ENTRY<QUIC_LISTENER>(null);
        public readonly QUIC_LOOKUP Lookup = new QUIC_LOOKUP();

        public readonly object StatelessOperLock = new object();
        public readonly Dictionary<QUIC_ADDR, QUIC_STATELESS_CONTEXT> StatelessOperTable = new Dictionary<QUIC_ADDR, QUIC_STATELESS_CONTEXT>(128);
        public readonly CXPLAT_LIST_ENTRY StatelessOperList = new CXPLAT_LIST_ENTRY<QUIC_STATELESS_CONTEXT>(null);
        public uint StatelessOperCount;
        public Stats_DATA Stats;

        public struct Stats_DATA
        {
            public Recv_DATA Recv;
            public struct Recv_DATA
            {
                public long DroppedPackets;
            }
        }

        public QUIC_BINDING()
        {
            Link = new CXPLAT_LIST_ENTRY<QUIC_BINDING>(this);
        }
    }

    internal class QUIC_RX_PACKET : CXPLAT_RECV_DATA
    {
        public const int sizeof_Length = 96;

        public ulong PacketId;
        public ulong PacketNumber;
        public long SendTimestamp;

        public readonly QUIC_BUFFER DestCid = new QUIC_BUFFER();
        public readonly QUIC_BUFFER SourceCid = new QUIC_BUFFER();
        public int HeaderLength; // 头部长度
        public int PayloadLength;// 负载长度，也就是最顶层玩家发送的消息

        public QUIC_PACKET_KEY_TYPE KeyType;

        public uint Flags;
        public bool AssignedToConnection;   //是否已分配到某个连接
        public bool ValidatedHeaderInv;    //不变头部是否已验证
        public bool IsShortHeader;  //是否是短头部数据包
        public bool ValidatedHeaderVer; //版本特定头部是否已验证
        public bool ValidToken; //初始数据包是否有有效的 Token
        public bool PacketNumberSet; //数据包编号是否已设置
        public bool Encrypted; //数据包是否已加密
        public bool EncryptedWith0Rtt; //是否使用 0-RTT 加密
        public bool ReleaseDeferred; //数据包是否需要延迟释放（因为尚未完全处理）
        public bool CompletelyValid; //数据包是否已完全解析并验证成功
        public bool NewLargestPacketNumber; //是否是目前为止最大的数据包编号
        public bool HasNonProbingFrame; //数据包是否包含非探测帧

        public QUIC_BUFFER AvailBuffer = null; //指向当前可用的数据缓冲区
        private QUIC_HEADER_INVARIANT m_Invariant;  //指向不变的头部结构
        private QUIC_VERSION_NEGOTIATION_PACKET m_VerNeg;   //版本协商数据包的指针。
        private QUIC_LONG_HEADER_V1 m_LH;       //长头部结构的指针。
        private QUIC_RETRY_PACKET_V1 m_Retry;   //重试数据包的指针。
        private QUIC_SHORT_HEADER_V1 m_SH;      //短头部结构的指针。

        public QUIC_HEADER_INVARIANT Invariant
        {
            get
            {
                if (m_Invariant == null)
                {
                    m_Invariant = new QUIC_HEADER_INVARIANT();
                    m_Invariant.WriteFrom(AvailBuffer);
                }
                return m_Invariant;
            }
        }

        public QUIC_VERSION_NEGOTIATION_PACKET VerNeg
        {
            get
            {
                if (m_VerNeg == null)
                {
                    m_VerNeg = new QUIC_VERSION_NEGOTIATION_PACKET();
                    m_VerNeg.WriteFrom(AvailBuffer);
                }
                return m_VerNeg;
            }
        }

        public QUIC_LONG_HEADER_V1 LH
        {
            get
            {
                if (m_LH == null)
                {
                    m_LH = new QUIC_LONG_HEADER_V1();
                    m_LH.WriteFrom(AvailBuffer);
                }
                return m_LH;
            }
        }

        public QUIC_RETRY_PACKET_V1 Retry
        {
            get
            {
                if (m_Retry == null)
                {
                    m_Retry = new QUIC_RETRY_PACKET_V1();
                    m_Retry.WriteFrom(AvailBuffer);
                }
                return m_Retry;
            }
        }

        public QUIC_SHORT_HEADER_V1 SH
        {
            get
            {
                if (m_SH == null)
                {
                    m_SH = new QUIC_SHORT_HEADER_V1();
                    m_SH.WriteFrom(AvailBuffer);
                }
                return m_SH;
            }
        }
    }

    internal class QUIC_TOKEN_CONTENTS
    {
        public const int sizeof_QUIC_TOKEN_CONTENTS = byte.MaxValue;

        public readonly Authenticated_DATA Authenticated = new Authenticated_DATA();
        public readonly Encrypted_DATA Encrypted = new Encrypted_DATA();
        public readonly byte[] Authenticated_Buffer = new byte[byte.MaxValue];
        public readonly byte[] Encrypted_Buffer = new byte[byte.MaxValue];
        public readonly byte[] EncryptionTag = new byte[MSQuicFunc.CXPLAT_ENCRYPTION_OVERHEAD];
        public readonly byte[] QUIC_TOKEN_CONTENTS_Buffer = new byte[byte.MaxValue];

        public class Authenticated_DATA
        {
            public bool IsNewToken;
            public long Timestamp;
        }

        public class Encrypted_DATA
        {
            public QUIC_ADDR RemoteAddress;
            public readonly QUIC_BUFFER OrigConnId = new QUIC_BUFFER(MSQuicFunc.QUIC_MAX_CONNECTION_ID_LENGTH_V1);
        }

        public void WriteFrom(QUIC_SSBuffer buffer)
        {

        }

        public void WriteFrom(byte[] buffer)
        {

        }

        public void WriteTo(byte[] buffer)
        {

        }
    }

    internal static partial class MSQuicFunc
    {
        static void QuicBindingUnregisterListener(QUIC_BINDING Binding, QUIC_LISTENER Listener)
        {
            CxPlatDispatchRwLockAcquireExclusive(Binding.RwLock);
            CxPlatListEntryRemove(Listener.Link);
            CxPlatDispatchRwLockReleaseExclusive(Binding.RwLock);
        }

        static int QuicBindingInitialize(CXPLAT_UDP_CONFIG UdpConfig, ref QUIC_BINDING NewBinding)
        {
            int Status;
            QUIC_BINDING Binding = new QUIC_BINDING();
            if (Binding == null)
            {
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                goto Error;
            }

            Binding.RefCount = 0;
            Binding.Exclusive = !BoolOk(UdpConfig.Flags & CXPLAT_SOCKET_FLAG_SHARE);
            Binding.ServerOwned = BoolOk(UdpConfig.Flags & CXPLAT_SOCKET_SERVER_OWNED);
            Binding.Connected = UdpConfig.RemoteAddress != null;
            Binding.StatelessOperCount = 0;
            CxPlatListInitializeHead(Binding.Listeners);
            Binding.StatelessOperTable.Clear();
            CxPlatListInitializeHead(Binding.StatelessOperList);
            CxPlatRandom.Random(ref Binding.RandomReservedVersion);

            Binding.RandomReservedVersion = (Binding.RandomReservedVersion & ~QUIC_VERSION_RESERVED_MASK) | QUIC_VERSION_RESERVED;
            UdpConfig.CallbackContext = Binding;
            Status = CxPlatSocketCreateUdp(MsQuicLib.Datapath, UdpConfig, ref Binding.Socket);

            if (QUIC_FAILED(Status))
            {
                goto Error;
            }
            
            NewBinding = Binding;
            Status = QUIC_STATUS_SUCCESS;
        Error:

            if (QUIC_FAILED(Status))
            {
                if (Binding != null)
                {
                    QuicLookupUninitialize(Binding.Lookup);
                }

            }
            return Status;
        }

        public static void QuicBindingReceive(CXPLAT_SOCKET Socket, object RecvCallbackContext, CXPLAT_RECV_DATA DatagramChain)
        {
            NetLog.Assert(RecvCallbackContext != null);
            NetLog.Assert(DatagramChain != null);

            QUIC_BINDING Binding = RecvCallbackContext as QUIC_BINDING;

            //子链列表
            CXPLAT_RECV_DATA ReleaseChain = null;
            CXPLAT_RECV_DATA ReleaseChainTail = ReleaseChain;

            CXPLAT_RECV_DATA SubChain = null;
            CXPLAT_RECV_DATA SubChainTail = SubChain;
            int SubChainLength = 0;
            int SubChainBytes = 0;

            int TotalChainLength = 0;
            int TotalDatagramBytes = 0;

            NetLog.Assert(Socket == Binding.Socket);
            int Partition = DatagramChain.PartitionIndex;
            ulong PartitionShifted = ((ulong)Partition + 1) << 40;

            CXPLAT_RECV_DATA Datagram;
            while ((Datagram = DatagramChain) != null)
            {
                TotalChainLength++;
                TotalDatagramBytes += Datagram.Buffer.Length;
                DatagramChain = Datagram.Next;
                Datagram.Next = null;

                QUIC_RX_PACKET Packet = Datagram as QUIC_RX_PACKET;
                Packet.PacketId = PartitionShifted | InterlockedEx.Increment(ref QuicLibraryGetPerProc().ReceivePacketId);
                Packet.PacketNumber = 0;
                Packet.SendTimestamp = long.MaxValue;
                Packet.AvailBuffer = Datagram.Buffer;
                Packet.HeaderLength = 0;
                Packet.PayloadLength = 0;
                Packet.KeyType = QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL;
                Packet.Flags = 0;

                NetLog.Assert(Packet.PacketId != 0);
                NetLog.Log($"QuicBindingReceive Received: {Packet.PacketId}");

                bool ReleaseDatagram = false;
                if (!QuicBindingPreprocessPacket(Binding, Packet, ref ReleaseDatagram))
                {
                    if (ReleaseDatagram)
                    {
                        if (ReleaseChainTail == null)
                        {
                            ReleaseChain = ReleaseChainTail = Datagram;
                        }
                        else
                        {
                            ReleaseChainTail.Next = Datagram;
                            ReleaseChainTail = Datagram;
                        }
                    }
                    continue;
                }

                NetLog.Assert(Packet.DestCid != null);
                NetLog.Assert(Packet.DestCid.Length != 0 || Binding.Exclusive);
                NetLog.Assert(Packet.ValidatedHeaderInv);

                //如果下一个接收到的数据报文与当前“子链”不匹配，则先提交当前的子链，然后开始一个新的子链。
                //如果绑定（binding）是独占的（exclusively owned），那么所有数据包都会被发送到同一个连接，不需要拆分子链。
                if (!Binding.Exclusive && SubChain != null)
                {
                    QUIC_RX_PACKET SubChainPacket = (QUIC_RX_PACKET)SubChain;

                    ////如果不同，说明属于不同的连接，不能继续挂在这个子链上。
                    if (!orBufferEqual(Packet.DestCid.GetSpan(), SubChainPacket.DestCid.GetSpan()))
                    {
                        if (!QuicBindingDeliverPackets(Binding, (QUIC_RX_PACKET)SubChain, SubChainLength, SubChainBytes))
                        {
                            if (ReleaseChainTail == null)
                            {
                                ReleaseChain = ReleaseChainTail = SubChain;
                            }
                            else
                            {
                                ReleaseChainTail.Next = SubChain;
                                ReleaseChainTail = SubChainTail;
                            }
                        }

                        SubChain = null;
                        SubChainTail = SubChain;
                        SubChainLength = 0;
                        SubChainBytes = 0;
                    }
                }

                //：当一个 UDP 数据报（datagram）包含多个 QUIC 数据包时，在将这些数据包加入到当前链表中时，
                //需要确保握手阶段的数据包（handshake packets）排在前面。
                //在 QUIC 中，客户端通常会在第一个数据包中发送 Initial 包（属于握手阶段），服务端需要优先处理这类包以建立连接。
                //在一个数据报内部，握手阶段的数据包不会出现在非握手阶段的数据包之后。
                //这是设计这个顺序的根本原因：优先处理握手包，有助于快速判断是否能够创建一个新的 QUIC 连接。
                //因为 QUIC 的连接建立依赖于初始握手包（如 Initial 和 Retry 类型），如果这些包被延迟处理，可能会导致连接建立失败或效率降低。

                SubChainLength++;
                SubChainBytes += Datagram.Buffer.Length;
                if (!QuicPacketIsHandshake(Packet))
                {
                    if (SubChainTail == null)
                    {
                        //初始化头部
                        SubChain = SubChainTail = Datagram;
                    }
                    else
                    {
                        SubChainTail.Next = Datagram;
                        SubChainTail = Datagram;
                    }
                }
                else
                {
                    if (SubChainTail == null)
                    {
                        //初始化头部
                        SubChain = SubChainTail = Datagram;
                    }
                    else
                    {
                        //把握手包，放前面
                        Datagram.Next = SubChain.Next;
                        SubChain = Datagram;
                    }
                }
            }

            if (SubChain != null)
            {
                // 分发最后一个子链
                if (!QuicBindingDeliverPackets(Binding, (QUIC_RX_PACKET)SubChain, SubChainLength, SubChainBytes))
                {
                    if (ReleaseChainTail == null)
                    {
                        ReleaseChain = ReleaseChainTail = SubChain;
                    }
                    else
                    {
                        ReleaseChainTail.Next = SubChain;
                        ReleaseChainTail = SubChainTail;
                    }
                }
            }

            if (ReleaseChain != null)
            {
                CxPlatRecvDataReturn(ReleaseChain);
            }

            QuicPerfCounterAdd(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_UDP_RECV, TotalChainLength);
            QuicPerfCounterAdd(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_UDP_RECV_BYTES, TotalDatagramBytes);
            QuicPerfCounterAdd(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_UDP_RECV_EVENTS);
        }

        static void QuicBindingUnreachable(CXPLAT_SOCKET Socket, object Context, QUIC_ADDR RemoteAddress)
        {
            NetLog.Assert(Context != null);
            NetLog.Assert(RemoteAddress != null);

            QUIC_BINDING Binding = Context as QUIC_BINDING;
            QUIC_CONNECTION Connection = QuicLookupFindConnectionByRemoteAddr(Binding.Lookup, RemoteAddress);

            if (Connection != null)
            {
                QuicConnQueueUnreachable(Connection, RemoteAddress);
                QuicConnRelease(Connection,  QUIC_CONNECTION_REF.QUIC_CONN_REF_LOOKUP_RESULT);
            }
        }

        static bool QuicBindingDropBlockedSourcePorts(QUIC_BINDING Binding, QUIC_RX_PACKET Packet)
        {
            int SourcePort = QuicAddrGetPort(Packet.Route.RemoteAddress);
            ushort[] BlockedPorts = new ushort[]
            {
                    11211,  // memcache
                    5353,   // mDNS
                    1900,   // SSDP
                    500,    // IKE
                    389,    // CLDAP
                    161,    // SNMP
                    138,    // NETBIOS Datagram Service
                    137,    // NETBIOS Name Service
                    123,    // NTP
                    111,    // Portmap
                    53,     // DNS
                    19,     // Chargen
                    17,     // Quote of the Day
                    0,      // Unusable
            };

            for (int i = 0; i < BlockedPorts.Length && SourcePort <= BlockedPorts[i]; ++i)
            {
                if (BlockedPorts[i] == SourcePort) 
                {
                    QuicPacketLogDrop(Binding, Packet, "Blocked source port: " + SourcePort);
                    return true;
                }
            }

            return false;
        }

        static bool QuicBindingQueueStatelessReset(QUIC_BINDING Binding, QUIC_RX_PACKET Packet)
        {
            NetLog.Assert(!Binding.Exclusive);
            if (Packet.Buffer.Length <= QUIC_MIN_STATELESS_RESET_PACKET_LENGTH)
            {
                QuicPacketLogDrop(Binding, Packet, "Packet too short for stateless reset");
                return false;
            }

            if (Binding.Exclusive)
            {
                QuicPacketLogDrop(Binding, Packet, "No stateless reset on exclusive binding");
                return false;
            }
            return QuicBindingQueueStatelessOperation(Binding, QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_STATELESS_RESET, Packet);
        }

        static bool QuicBindingShouldRetryConnection(QUIC_BINDING Binding, QUIC_RX_PACKET Packet, QUIC_SSBuffer Token, ref bool DropPacket)
        {
            if (Token.Length != 0)
            {
                if (QuicPacketValidateInitialToken(Binding, Packet, Token, ref DropPacket))
                {
                    Packet.ValidToken = true;
                    return false;
                }

                if (DropPacket)
                {
                    return false;
                }
            }

            long CurrentMemoryLimit = (MsQuicLib.Settings.RetryMemoryLimit * SystemInfo.TotalMemory()) / ushort.MaxValue;
            return MsQuicLib.CurrentHandshakeMemoryUsage >= CurrentMemoryLimit;
        }

        static bool QuicBindingDeliverPackets(QUIC_BINDING Binding, QUIC_RX_PACKET Packets, int PacketChainLength, int PacketChainByteLength)
        {
            NetLog.Assert(Packets.ValidatedHeaderInv);

            QUIC_CONNECTION Connection;
            if (!Binding.ServerOwned || Packets.IsShortHeader)
            {
                Connection = QuicLookupFindConnectionByLocalCid(Binding.Lookup, new QUIC_CID(Packets.DestCid));
            }
            else
            {
                Connection = QuicLookupFindConnectionByRemoteHash(Binding.Lookup, new QUIC_CID(Packets.SourceCid, Packets.Route.RemoteAddress));
            }

            if (Connection == null)
            {
                if (!Binding.ServerOwned)
                {
                    QuicPacketLogDrop(Binding, Packets, "No matching client connection");
                    return false;
                }

                if (Binding.Exclusive)
                {
                    QuicPacketLogDrop(Binding, Packets, "No connection on exclusive binding");
                    return false;
                }

                if (QuicBindingDropBlockedSourcePorts(Binding, Packets))
                {
                    return false;
                }

                if (Packets.IsShortHeader)
                {
                    return QuicBindingQueueStatelessReset(Binding, Packets);
                }

                if (Packets.Invariant.LONG_HDR.Version == QUIC_VERSION_VER_NEG)
                {
                    QuicPacketLogDrop(Binding, Packets, "Version negotiation packet not matched with a connection");
                    return false;
                }

                NetLog.Assert(QuicIsVersionSupported(Packets.Invariant.LONG_HDR.Version));
                switch (Packets.Invariant.LONG_HDR.Version)
                {
                    case QUIC_VERSION_1:
                        if (Packets.LH.Type != (byte)QUIC_LONG_HEADER_TYPE_V1.QUIC_INITIAL_V1)
                        {
                            QuicPacketLogDrop(Binding, Packets, "Non-initial packet not matched with a connection");
                            return false;
                        }
                        break;
                    case QUIC_VERSION_2:
                        if (Packets.LH.Type != (byte)QUIC_LONG_HEADER_TYPE_V2.QUIC_INITIAL_V2)
                        {
                            QuicPacketLogDrop(Binding, Packets, "Non-initial packet not matched with a connection");
                            return false;
                        }
                        break;
                }

                QUIC_SSBuffer Token = new QUIC_SSBuffer();
                if (!QuicPacketValidateLongHeaderV1(Binding, true, Packets, ref Token, false))
                {
                    return false;
                }

                NetLog.Assert(Token != QUIC_SSBuffer.Empty);
                if (!QuicBindingHasListenerRegistered(Binding))
                {
                    QuicPacketLogDrop(Binding, Packets, "No listeners registered to accept new connection.");
                    return false;
                }

                NetLog.Assert(Binding.ServerOwned);
                bool DropPacket = false;
                if (QuicBindingShouldRetryConnection(Binding, Packets, Token, ref DropPacket))
                {
                    return QuicBindingQueueStatelessOperation(Binding, QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_RETRY, Packets);
                }
                if (!DropPacket)
                {
                    Connection = QuicBindingCreateConnection(Binding, Packets);
                }
            }

            if (Connection == null)
            {
                return false;
            }

            QuicConnQueueRecvPackets(Connection, Packets, PacketChainLength, PacketChainByteLength);
            QuicConnRelease(Connection, QUIC_CONNECTION_REF.QUIC_CONN_REF_LOOKUP_RESULT);
            return true;
        }

        static QUIC_CONNECTION QuicBindingCreateConnection(QUIC_BINDING Binding, QUIC_RX_PACKET Packet)
        {
            QUIC_WORKER Worker = QuicLibraryGetWorker(Packet);
            if (QuicWorkerIsOverloaded(Worker))
            {
                QuicPacketLogDrop(Binding, Packet, "Stateless worker overloaded");
                return null;
            }

            QUIC_CONNECTION Connection = null;
            QUIC_CONNECTION NewConnection = null;
            int Status = QuicConnAlloc(MsQuicLib.StatelessRegistration, Worker, Packet, out NewConnection);
            if (QUIC_FAILED(Status))
            {
                QuicPacketLogDrop(Binding, Packet, "Failed to initialize new connection");
                return null;
            }

            bool BindingRefAdded = false;
            NetLog.Assert(NewConnection.SourceCids.Next != null);
            QuicConnAddRef(NewConnection, QUIC_CONNECTION_REF.QUIC_CONN_REF_LOOKUP_RESULT);
            if (!QuicLibraryTryAddRefBinding(Binding))
            {
                QuicPacketLogDrop(Binding, Packet, "Clean up in progress");
                goto Exit;
            }

            BindingRefAdded = true;
            NewConnection.Paths[0].Binding = Binding;

            if (!QuicLookupAddRemoteHash(
                    Binding.Lookup,
                    NewConnection,
                    new QUIC_CID(Packet.SourceCid, Packet.Route.RemoteAddress),
                    ref Connection))
            {
                if (Connection == null)
                {
                    QuicPacketLogDrop(Binding, Packet, "Failed to insert remote hash");
                }
                goto Exit;
            }

            QuicWorkerQueueConnection(NewConnection.Worker, NewConnection);
            return NewConnection;
        Exit:
            if (BindingRefAdded)
            {
                QuicConnRelease(NewConnection,  QUIC_CONNECTION_REF.QUIC_CONN_REF_LOOKUP_RESULT);
                if (Interlocked.CompareExchange(ref NewConnection.BackUpOperUsed, 1, 0) == 0)
                {
                    QUIC_OPERATION Oper = NewConnection.BackUpOper;
                    Oper.FreeAfterProcess = false;
                    Oper.Type =  QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_API_CALL;
                    Oper.API_CALL.Context = NewConnection.BackupApiContext;
                    Oper.API_CALL.Context.Type =  QUIC_API_TYPE.QUIC_API_TYPE_CONN_SHUTDOWN;
                    Oper.API_CALL.Context.CONN_SHUTDOWN.Flags =  QUIC_CONNECTION_SHUTDOWN_FLAGS.QUIC_CONNECTION_SHUTDOWN_FLAG_SILENT;
                    Oper.API_CALL.Context.CONN_SHUTDOWN.ErrorCode = QUIC_STATUS_INTERNAL_ERROR;
                    Oper.API_CALL.Context.CONN_SHUTDOWN.RegistrationShutdown = false;
                    Oper.API_CALL.Context.CONN_SHUTDOWN.TransportShutdown = true;
                    QuicConnQueueOper(NewConnection, Oper);
                }
            }
            else
            {
                NewConnection.SourceCids.Next = null;
                QuicConnRelease(NewConnection,  QUIC_CONNECTION_REF.QUIC_CONN_REF_LOOKUP_RESULT);
                QuicConnRelease(NewConnection,  QUIC_CONNECTION_REF.QUIC_CONN_REF_HANDLE_OWNER);
            }

            return Connection;
        }

        static bool QuicBindingHasListenerRegistered(QUIC_BINDING Binding)
        {
            return !CxPlatListIsEmpty(Binding.Listeners);
        }

        static bool QuicBindingPreprocessPacket(QUIC_BINDING Binding, QUIC_RX_PACKET Packet, ref bool ReleaseDatagram)
        {
            Packet.AvailBuffer = Packet.Buffer;

            ReleaseDatagram = true;
            if (!QuicPacketValidateInvariant(Binding, Packet, Binding.Exclusive))
            {
                return false;
            }

            if (BoolOk(Packet.Invariant.IsLongHeader))
            {
                if (Packet.Invariant.LONG_HDR.Version != QUIC_VERSION_VER_NEG &&
                    !QuicVersionNegotiationExtIsVersionServerSupported(Packet.Invariant.LONG_HDR.Version))
                {
                    if (!QuicBindingHasListenerRegistered(Binding))
                    {
                        QuicPacketLogDrop(Binding, Packet, "No listener to send VN");
                    }
                    else if (Packet.Buffer.Length < QUIC_MIN_UDP_PAYLOAD_LENGTH_FOR_VN)
                    {
                        QuicPacketLogDrop(Binding, Packet, "Too small to send VN");

                    }
                    else
                    {
                        ReleaseDatagram = !QuicBindingQueueStatelessOperation(Binding, QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_VERSION_NEGOTIATION, Packet);
                    }

                    NetLog.Assert(false, Packet.Invariant.LONG_HDR.Version);
                    return false;
                }

                if (Binding.Exclusive)
                {
                    if (Packet.DestCid.Length != 0)
                    {
                        QuicPacketLogDrop(Binding, Packet, "Non-zero length CID on exclusive binding");
                        return false;
                    }
                }
                else
                {
                    if (Packet.DestCid.Length == 0)
                    {
                        QuicPacketLogDrop(Binding, Packet, "Zero length DestCid");
                        return false;
                    }
                    if (Packet.DestCid.Length < QUIC_MIN_INITIAL_CONNECTION_ID_LENGTH)
                    {
                        QuicPacketLogDrop(Binding, Packet, "Less than min length CID on non-exclusive binding");
                        return false;
                    }
                }
            }

            ReleaseDatagram = false;
            return true;
        }

        static void QuicBindingRemoveConnection(QUIC_BINDING Binding, QUIC_CONNECTION Connection)
        {
            if (Connection.RemoteHashEntry != null)
            {
                QuicLookupRemoveRemoteHash(Binding.Lookup, Connection.RemoteHashEntry);
            }
            QuicLookupRemoveLocalCids(Binding.Lookup, Connection);
        }

        static QUIC_STATELESS_CONTEXT QuicBindingCreateStatelessOperation(QUIC_BINDING Binding, QUIC_WORKER Worker, QUIC_RX_PACKET Packet)
        {
            long TimeMs = CxPlatTime();
            QUIC_ADDR RemoteAddress = Packet.Route.RemoteAddress;
            uint Hash = QuicAddrHash(RemoteAddress);
            QUIC_STATELESS_CONTEXT StatelessCtx = null;

            CxPlatDispatchLockAcquire(Binding.StatelessOperLock);

            if (Binding.RefCount == 0)
            {
                goto Exit;
            }

            while (!CxPlatListIsEmpty(Binding.StatelessOperList))
            {
                QUIC_STATELESS_CONTEXT OldStatelessCtx = CXPLAT_CONTAINING_RECORD<QUIC_STATELESS_CONTEXT>(Binding.StatelessOperList.Next);

                if (CxPlatTimeDiff(OldStatelessCtx.CreationTimeMs, TimeMs) < MsQuicLib.Settings.StatelessOperationExpirationMs)
                {
                    break;
                }

                OldStatelessCtx.IsExpired = true;
                Binding.StatelessOperTable.Remove(RemoteAddress);

                CxPlatListEntryRemove(OldStatelessCtx.ListEntry);
                Binding.StatelessOperCount--;
                if (OldStatelessCtx.IsProcessed)
                {
                    OldStatelessCtx.Worker.StatelessContextPool.CxPlatPoolFree(OldStatelessCtx);
                }
            }

            if (Binding.StatelessOperCount >= MsQuicLib.Settings.MaxBindingStatelessOperations)
            {
                QuicPacketLogDrop(Binding, Packet, "Max binding operations reached");
                goto Exit;
            }

            if (Binding.StatelessOperTable.ContainsKey(RemoteAddress))
            {
                QuicPacketLogDrop(Binding, Packet, "Already in stateless oper table");
                goto Exit;
            }

            StatelessCtx = Worker.StatelessContextPool.CxPlatPoolAlloc();
            if (StatelessCtx == null)
            {
                QuicPacketLogDrop(Binding, Packet, "Alloc failure for stateless oper ctx");
                goto Exit;
            }

            StatelessCtx.Binding = Binding;
            StatelessCtx.Worker = Worker;
            StatelessCtx.Packet = Packet;
            StatelessCtx.CreationTimeMs = TimeMs;
            StatelessCtx.HasBindingRef = false;
            StatelessCtx.IsProcessed = false;
            StatelessCtx.IsExpired = false;
            StatelessCtx.RemoteAddress = RemoteAddress;

            Binding.StatelessOperTable.Add(RemoteAddress, StatelessCtx);
            CxPlatListInsertTail(Binding.StatelessOperList, StatelessCtx.ListEntry);
            Binding.StatelessOperCount++;
        Exit:
            CxPlatDispatchLockRelease(Binding.StatelessOperLock);
            return StatelessCtx;
        }

        static void QuicBindingReleaseStatelessOperation(QUIC_STATELESS_CONTEXT StatelessCtx, bool ReturnDatagram)
        {
            QUIC_BINDING Binding = StatelessCtx.Binding;
            if (ReturnDatagram)
            {
                CxPlatRecvDataReturn((CXPLAT_RECV_DATA)StatelessCtx.Packet);
            }
            StatelessCtx.Packet = null;

            CxPlatDispatchLockAcquire(Binding.StatelessOperLock);
            StatelessCtx.IsProcessed = true;

            bool FreeCtx = StatelessCtx.IsExpired;
            CxPlatDispatchLockRelease(Binding.StatelessOperLock);

            if (StatelessCtx.HasBindingRef)
            {
                QuicLibraryReleaseBinding(Binding);
            }

            if (FreeCtx)
            {
                StatelessCtx.Worker.StatelessContextPool.CxPlatPoolFree(StatelessCtx);
            }
        }

        static bool QuicBindingQueueStatelessOperation(QUIC_BINDING Binding, QUIC_OPERATION_TYPE OperType, QUIC_RX_PACKET Packet)
        {
            if (MsQuicLib.StatelessRegistration == null)
            {
                QuicPacketLogDrop(Binding, Packet, "NULL stateless registration");
                return false;
            }

            QUIC_WORKER Worker = QuicLibraryGetWorker(Packet);
            if (QuicWorkerIsOverloaded(Worker))
            {
                QuicPacketLogDrop(Binding, Packet, "Stateless worker overloaded (stateless oper)");
                return false;
            }

            QUIC_STATELESS_CONTEXT Context = QuicBindingCreateStatelessOperation(Binding, Worker, Packet);
            if (Context == null)
            {
                return false;
            }

            QUIC_OPERATION Oper = QuicOperationAlloc(Worker, OperType);
            if (Oper == null)
            {
                QuicPacketLogDrop(Binding, Packet, "Alloc failure for stateless operation");
                QuicBindingReleaseStatelessOperation(Context, false);
                return false;
            }

            Oper.STATELESS.Context = Context;
            QuicWorkerQueueOperation(Worker, Oper);
            return true;
        }

        static QUIC_LISTENER QuicBindingGetListener(QUIC_BINDING Binding, QUIC_CONNECTION Connection, QUIC_NEW_CONNECTION_INFO Info)
        {
            QUIC_LISTENER Listener = null;

            QUIC_ADDR Addr = Info.LocalAddress;
            AddressFamily Family = QuicAddrGetFamily(Addr);

            bool FailedAlpnMatch = false;
            bool FailedAddrMatch = true;

            CxPlatDispatchRwLockAcquireShared(Binding.RwLock);
            for (CXPLAT_LIST_ENTRY Link = Binding.Listeners.Next; Link != Binding.Listeners; Link = Link.Next)
            {
                QUIC_LISTENER ExistingListener = CXPLAT_CONTAINING_RECORD<QUIC_LISTENER>(Link);
                QUIC_ADDR ExistingAddr = ExistingListener.LocalAddress;
                bool ExistingWildCard = ExistingListener.WildCard;
                AddressFamily ExistingFamily = QuicAddrGetFamily(ExistingAddr);
                FailedAlpnMatch = false;

                if (ExistingFamily != AddressFamily.Unspecified)
                {
                    //{::ffff:127.0.0.1:0}   {::ffff:0:0:6000}
                    if (Family != ExistingFamily || (!ExistingWildCard && !QuicAddrCompareIp(Addr,ExistingAddr)))
                    {
                        FailedAddrMatch = true;
                        continue;
                    }
                }
                FailedAddrMatch = false;

                if (QuicListenerMatchesAlpn(ExistingListener, Info))
                {
                    if (CxPlatRefIncrementNonZero(ref ExistingListener.RefCount, 1))
                    {
                        Listener = ExistingListener;
                    }
                    goto Done;
                }
                else
                {
                    FailedAlpnMatch = true;
                }
            }

        Done:
            CxPlatDispatchRwLockReleaseShared(Binding.RwLock);
            if (FailedAddrMatch)
            {

            }
            else if (FailedAlpnMatch)
            {
                QuicPerfCounterIncrement(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_NO_ALPN);
            }

            return Listener;
        }

        static void QuicBindingAcceptConnection(QUIC_BINDING Binding, QUIC_CONNECTION Connection, QUIC_NEW_CONNECTION_INFO Info)
        {
            QUIC_LISTENER Listener = QuicBindingGetListener(Binding, Connection, Info);
            if (Listener == null)
            {
                QuicConnTransportError(Connection, QUIC_ERROR_CRYPTO_NO_APPLICATION_PROTOCOL);
                return;
            }

            int NegotiatedAlpnLength = 1 + Info.NegotiatedAlpn[0];

            QUIC_SSBuffer NegotiatedAlpn;
            if (NegotiatedAlpnLength <= TLS_SMALL_ALPN_BUFFER_SIZE)
            {
                NegotiatedAlpn = Connection.Crypto.TlsState.SmallAlpnBuffer;
            }
            else
            {
                NegotiatedAlpn = new byte[NegotiatedAlpnLength];
                if (NegotiatedAlpn.IsEmpty)
                {
                    QuicConnTransportError(Connection, QUIC_ERROR_INTERNAL_ERROR);
                    goto Error;
                }
            }

            Info.NegotiatedAlpn.CopyTo(NegotiatedAlpn);
            Connection.Crypto.TlsState.NegotiatedAlpn = NegotiatedAlpn;
            Connection.Crypto.TlsState.ClientAlpnList = Info.ClientAlpnList;
            QuicListenerAcceptConnection(Listener, Connection, Info);

        Error:
            QuicListenerRelease(Listener, true);
        }

        static void QuicBindingProcessStatelessOperation(QUIC_OPERATION_TYPE OperationType, QUIC_STATELESS_CONTEXT StatelessCtx)
        {
            QUIC_BINDING Binding = StatelessCtx.Binding;
            QUIC_RX_PACKET RecvPacket = StatelessCtx.Packet;
            QUIC_BUFFER SendDatagram = null;

            NetLog.Assert(RecvPacket.ValidatedHeaderInv);

            CXPLAT_SEND_CONFIG SendConfig = new CXPLAT_SEND_CONFIG()
            {
                Route = RecvPacket.Route,
                MaxPacketSize = 0,
                ECN = (byte)CXPLAT_ECN_TYPE.CXPLAT_ECN_NON_ECT,
                Flags = 0
            };

            CXPLAT_SEND_DATA SendData = CxPlatSendDataAlloc(Binding.Socket, SendConfig);
            if (SendData == null)
            {
                goto Exit;
            }

            if (OperationType == QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_VERSION_NEGOTIATION)
            {
                NetLog.Assert(RecvPacket.DestCid != null);
                NetLog.Assert(RecvPacket.SourceCid != null);

                var SupportedVersions = DefaultSupportedVersionsList;
                int SupportedVersionsLength = DefaultSupportedVersionsList.Count;

                int PacketLength = sizeof_QUIC_VERSION_NEGOTIATION_PACKET +
                    RecvPacket.SourceCid.Length +
                    sizeof(byte) +
                    RecvPacket.DestCid.Length +
                    sizeof(uint) +
                    (ushort)(SupportedVersionsLength * sizeof(uint));

                SendDatagram = CxPlatSendDataAllocBuffer(SendData, PacketLength);
                if (SendDatagram == null)
                {
                    goto Exit;
                }

                QUIC_VERSION_NEGOTIATION_PACKET VerNeg = new QUIC_VERSION_NEGOTIATION_PACKET();
                VerNeg.WriteFrom(SendDatagram.Buffer);
                NetLog.Assert(SendDatagram.Length == PacketLength);

                VerNeg.IsLongHeader = 1;
                VerNeg.Version = QUIC_VERSION_VER_NEG;

                QUIC_SSBuffer Buffer = VerNeg.DestCid;
                int nBufferOffset = 0;
                VerNeg.DestCid.Length = (byte)RecvPacket.SourceCid.Length;
                RecvPacket.SourceCid.CopyTo(Buffer);
                nBufferOffset += RecvPacket.SourceCid.Length;

                Buffer[nBufferOffset] = (byte)RecvPacket.DestCid.Length;
                nBufferOffset += RecvPacket.SourceCid.Length;
                RecvPacket.DestCid.CopyTo(Buffer);
                nBufferOffset += RecvPacket.DestCid.Length;

                byte RandomValue = 0;
                CxPlatRandom.Random(ref RandomValue);
                VerNeg.Unused = (byte)(0x7F & RandomValue);

                EndianBitConverter.SetBytes(Buffer.GetSpan(), nBufferOffset, Binding.RandomReservedVersion);
                nBufferOffset += sizeof(uint);

                for (int i = 0; i < SupportedVersionsLength; i++)
                {
                    EndianBitConverter.SetBytes(Buffer.GetSpan(), nBufferOffset, (uint)SupportedVersions[i]);
                }
                RecvPacket.ReleaseDeferred = false;
            }
            else if (OperationType == QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_STATELESS_RESET)
            {
                NetLog.Assert(RecvPacket.DestCid != null);
                NetLog.Assert(RecvPacket.SourceCid == null);

                int PacketLength = 0;
                CxPlatRandom.Random(ref PacketLength);
                PacketLength >>= 5;
                PacketLength += QUIC_RECOMMENDED_STATELESS_RESET_PACKET_LENGTH;

                if (PacketLength >= RecvPacket.AvailBuffer.Length)
                {
                    PacketLength = (byte)RecvPacket.AvailBuffer.Length - 1;
                }

                if (PacketLength < QUIC_MIN_STATELESS_RESET_PACKET_LENGTH)
                {
                    NetLog.Assert(false);
                    goto Exit;
                }

                SendDatagram = CxPlatSendDataAllocBuffer(SendData, PacketLength);
                if (SendDatagram == null)
                {
                    goto Exit;
                }

                QUIC_SHORT_HEADER_V1 ResetPacket = new QUIC_SHORT_HEADER_V1();
                ResetPacket.WriteFrom(SendDatagram.Buffer);
                NetLog.Assert(SendDatagram.Length == PacketLength);

                CxPlatRandom.Random(SendDatagram.Buffer.AsSpan().Slice(0, PacketLength - QUIC_STATELESS_RESET_TOKEN_LENGTH));
                ResetPacket.IsLongHeader = 0;
                ResetPacket.FixedBit = 1;
                ResetPacket.KeyPhase = RecvPacket.SH.KeyPhase;
                QuicLibraryGenerateStatelessResetToken(RecvPacket.DestCid, new QUIC_SSBuffer(SendDatagram.Buffer, PacketLength - QUIC_STATELESS_RESET_TOKEN_LENGTH));
                QuicPerfCounterIncrement(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_SEND_STATELESS_RESET);
            }
            else if (OperationType == QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_RETRY)
            {
                NetLog.Assert(RecvPacket.DestCid != null);
                NetLog.Assert(RecvPacket.SourceCid != null);
                int PacketLength = QuicPacketMaxBufferSizeForRetryV1();
                SendDatagram = CxPlatSendDataAllocBuffer(SendData, PacketLength);
                if (SendDatagram == null)
                {
                    goto Exit;
                }

                byte[] NewDestCid = new byte[QUIC_CID_MAX_LENGTH];
                NetLog.Assert(NewDestCid.Length >= MsQuicLib.CidTotalLength);
                CxPlatRandom.Random(NewDestCid);

                QUIC_TOKEN_CONTENTS Token = new QUIC_TOKEN_CONTENTS();
                Token.Authenticated.Timestamp = TimeTool.GetTimeStamp();
                Token.Authenticated.IsNewToken = false;
                Token.Encrypted.RemoteAddress = RecvPacket.Route.RemoteAddress;
                RecvPacket.DestCid.GetSpan().CopyTo(Token.Encrypted.OrigConnId.GetSpan());
                Token.Encrypted.OrigConnId.Length = RecvPacket.DestCid.Length;

                byte[] Iv = new byte[CXPLAT_MAX_IV_LENGTH];
                if (MsQuicLib.CidTotalLength >= CXPLAT_IV_LENGTH)
                {
                    Array.Copy(NewDestCid, Iv, CXPLAT_IV_LENGTH);
                    for (int i = CXPLAT_IV_LENGTH; i < MsQuicLib.CidTotalLength; ++i)
                    {
                        Iv[i % CXPLAT_IV_LENGTH] ^= NewDestCid[i];
                    }
                }
                else
                {
                    Array.Clear(Iv, 0, CXPLAT_IV_LENGTH);
                    Array.Copy(NewDestCid, Iv, MsQuicLib.CidTotalLength);
                }

                CxPlatDispatchLockAcquire(MsQuicLib.StatelessRetryKeysLock);

                CXPLAT_KEY StatelessRetryKey = QuicLibraryGetCurrentStatelessRetryKey();
                if (StatelessRetryKey == null)
                {
                    CxPlatDispatchLockRelease(MsQuicLib.StatelessRetryKeysLock);
                    goto Exit;
                }

                int Status = CxPlatEncrypt(StatelessRetryKey, Iv, Token.Authenticated_Buffer,Token.Encrypted_Buffer);
                CxPlatDispatchLockRelease(MsQuicLib.StatelessRetryKeysLock);
                if (QUIC_FAILED(Status))
                {
                    goto Exit;
                }

                SendDatagram.Length = QuicPacketEncodeRetryV1(RecvPacket.LH.Version,
                        RecvPacket.SourceCid,
                        new QUIC_SSBuffer(NewDestCid, MsQuicLib.CidTotalLength),
                        RecvPacket.DestCid,
                        Token.QUIC_TOKEN_CONTENTS_Buffer,
                        SendDatagram.Buffer);

                if (SendDatagram.Length == 0)
                {
                    goto Exit;
                }
                    
                QuicPerfCounterIncrement(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_SEND_STATELESS_RETRY);
            }
            else
            {
                NetLog.Assert(false); // Should be unreachable code.
                goto Exit;
            }

            QuicBindingSend(Binding, RecvPacket.Route, SendData, SendDatagram.Length, 1);
            SendData = null;

        Exit:
            return;
        }

        static void QuicBindingUninitialize(QUIC_BINDING Binding)
        {
            NetLog.Assert(Binding.RefCount == 0);
            NetLog.Assert(CxPlatListIsEmpty(Binding.Listeners));
            CxPlatSocketDelete(Binding.Socket);
            while (!CxPlatListIsEmpty(Binding.StatelessOperList))
            {
                QUIC_STATELESS_CONTEXT StatelessCtx = CXPLAT_CONTAINING_RECORD<QUIC_STATELESS_CONTEXT>(CxPlatListRemoveHead(Binding.StatelessOperList));
                Binding.StatelessOperCount--;
                Binding.StatelessOperTable.Remove(StatelessCtx.RemoteAddress);

                NetLog.Assert(StatelessCtx.IsProcessed);
                StatelessCtx.Worker.StatelessContextPool.CxPlatPoolFree(StatelessCtx);
            }
            NetLog.Assert(Binding.StatelessOperCount == 0);
            NetLog.Assert(Binding.StatelessOperTable.Count == 0);
            QuicLookupUninitialize(Binding.Lookup);
        }

        static bool QuicRetryTokenDecrypt(QUIC_RX_PACKET Packet, QUIC_SSBuffer TokenBuffer, ref QUIC_TOKEN_CONTENTS Token)
        {
            Token = new QUIC_TOKEN_CONTENTS();
            Token.WriteFrom(TokenBuffer);

            QUIC_SSBuffer Iv = new byte[CXPLAT_MAX_IV_LENGTH];
            if (MsQuicLib.CidTotalLength >= CXPLAT_IV_LENGTH)
            {
                Packet.DestCid.CopyTo(Iv);
                for (int i = CXPLAT_IV_LENGTH; i < MsQuicLib.CidTotalLength; ++i)
                {
                    Iv[i % CXPLAT_IV_LENGTH] ^= Packet.DestCid.Buffer[i];
                }
            }
            else
            {
                Iv.Clear();
                Packet.DestCid.CopyTo(Iv);
            }

            CxPlatDispatchLockAcquire(MsQuicLib.StatelessRetryKeysLock);
            CXPLAT_KEY StatelessRetryKey = QuicLibraryGetStatelessRetryKeyForTimestamp(Token.Authenticated.Timestamp);
            if (StatelessRetryKey == null)
            {
                CxPlatDispatchLockRelease(MsQuicLib.StatelessRetryKeysLock);
                return false;
            }

            int Status = CxPlatDecrypt(StatelessRetryKey, Iv, Token.Authenticated_Buffer, Token.Encrypted_Buffer);
            CxPlatDispatchLockRelease(MsQuicLib.StatelessRetryKeysLock);
            return QUIC_SUCCEEDED(Status);
        }

        static int QuicBindingSend(QUIC_BINDING Binding, CXPLAT_ROUTE Route, CXPLAT_SEND_DATA SendData, int BytesToSend, int DatagramsToSend)
        {
            int Status;
            Status = CxPlatSocketSend(Binding.Socket, Route, SendData);

            QuicPerfCounterAdd(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_UDP_SEND, DatagramsToSend);
            QuicPerfCounterAdd(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_UDP_SEND_BYTES, BytesToSend);
            QuicPerfCounterIncrement(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_UDP_SEND_CALLS);
            return Status;
        }

        static int QuicBindingRegisterListener(QUIC_BINDING Binding, QUIC_LISTENER NewListener)
        {
            int Status = QUIC_STATUS_SUCCESS;
            bool MaximizeLookup = false;

            QUIC_ADDR NewAddr = NewListener.LocalAddress;
            bool NewWildCard = NewListener.WildCard;
            AddressFamily NewFamily = QuicAddrGetFamily(NewAddr);

            CxPlatDispatchRwLockAcquireExclusive(Binding.RwLock);

            CXPLAT_LIST_ENTRY Link;
            for (Link = Binding.Listeners.Next; Link != Binding.Listeners; Link = Link.Next)
            {
                QUIC_LISTENER ExistingListener = CXPLAT_CONTAINING_RECORD<QUIC_LISTENER>(Link);
                QUIC_ADDR ExistingAddr = ExistingListener.LocalAddress;
                bool ExistingWildCard = ExistingListener.WildCard;
                AddressFamily ExistingFamily = QuicAddrGetFamily(ExistingAddr);

                if (NewFamily > ExistingFamily)
                {
                    break;
                }

                if (NewFamily != ExistingFamily)
                {
                    continue;
                }

                if (!NewWildCard && ExistingWildCard)
                {
                    break;
                }

                if (NewWildCard != ExistingWildCard)
                {
                    continue;
                }

                if (NewFamily != AddressFamily.Unspecified && NewAddr != ExistingAddr)
                {
                    continue;
                }

                if (QuicListenerHasAlpnOverlap(NewListener, ExistingListener))
                {
                    Status = QUIC_STATUS_ALPN_IN_USE;
                    break;
                }
            }

            if (Status == QUIC_STATUS_SUCCESS)
            {
                MaximizeLookup = CxPlatListIsEmpty(Binding.Listeners);
                if (Link == Binding.Listeners)
                {
                    CxPlatListInsertTail(Binding.Listeners, NewListener.Link);
                }
                else
                {
                    NewListener.Link.Next = Link;
                    NewListener.Link.Prev = Link.Prev;
                    NewListener.Link.Prev.Next = NewListener.Link;
                    Link.Prev = NewListener.Link;
                }
            }

            CxPlatDispatchRwLockReleaseExclusive(Binding.RwLock);
            QuicLookupMaximizePartitioning(Binding.Lookup);
            return Status;
        }

        static void QuicBindingGetLocalAddress(QUIC_BINDING Binding, out QUIC_ADDR Address)
        {
            CxPlatSocketGetLocalAddress(Binding.Socket, out Address);
        }

        static void QuicBindingGetRemoteAddress(QUIC_BINDING Binding, out QUIC_ADDR Address)
        {
            CxPlatSocketGetRemoteAddress(Binding.Socket, out Address);
        }

        static bool QuicBindingAddSourceConnectionID(QUIC_BINDING Binding, QUIC_CID SourceCid)
        {
            return QuicLookupAddLocalCid(Binding.Lookup, SourceCid, out _);
        }

        static void QuicBindingOnConnectionHandshakeConfirmed(QUIC_BINDING Binding, QUIC_CONNECTION Connection)
        {
            if (Connection.RemoteHashEntry != null)
            {
                QuicLookupRemoveRemoteHash(Binding.Lookup, Connection.RemoteHashEntry);
            }
        }

        static void QuicBindingRemoveSourceConnectionID(QUIC_BINDING Binding, QUIC_CID SourceCid)
        {
            QuicLookupRemoveLocalCid(Binding.Lookup, SourceCid);
        }

        static void QuicBindingMoveSourceConnectionIDs(QUIC_BINDING BindingSrc,QUIC_BINDING BindingDest, QUIC_CONNECTION Connection)
        {
            QuicLookupMoveLocalConnectionIDs(BindingSrc.Lookup, BindingDest.Lookup, Connection);
        }


    }
}
