/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/14 8:56:48
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Platform;
using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace MSQuic2
{
    internal enum CXPLAT_DATAPATH_TYPE
    {
        CXPLAT_DATAPATH_TYPE_UNKNOWN = 0,
        CXPLAT_DATAPATH_TYPE_NORMAL,
        CXPLAT_DATAPATH_TYPE_RAW,
    }

    internal enum CXPLAT_SOCKET_TYPE
    {
        CXPLAT_SOCKET_UDP = 0,
    }

    internal class CXPLAT_DATAPATH_COMMON
    {
        public CXPLAT_UDP_DATAPATH_CALLBACKS UdpHandlers;
        public CXPLAT_WORKER_POOL WorkerPool;
        public uint Features;
        public CXPLAT_DATAPATH_RAW RawDataPath;
    }

    internal class CXPLAT_DATAPATH_PROC
    {
        public CXPLAT_DATAPATH Datapath;
        public long RefCount;
        public int PartitionIndex;
        public bool Uninitialized;
        public readonly CXPLAT_SEND_DATA_POOL SendDataPool = new CXPLAT_SEND_DATA_POOL();
        public readonly CXPLAT_Buffer_POOL SendBufferPool = new CXPLAT_Buffer_POOL();
        public readonly CXPLAT_Buffer_POOL LargeSendBufferPool = new CXPLAT_Buffer_POOL();
        public readonly DATAPATH_RX_PACKET_POOL RecvDatagramPool = new DATAPATH_RX_PACKET_POOL();
        public CXPLAT_EVENTQ EventQ;
    }

    internal class CXPLAT_DATAPATH : CXPLAT_DATAPATH_COMMON
    {
        public long RefCount;
        public int PartitionCount;
        public byte MaxSendBatchSize;
        public bool UseRio;
        public bool Uninitialized;
        public bool Freed;
        public readonly CXPLAT_DATAPATH_PROC[] Partitions = null;
        public int RecvDatagramLength;
        public int RecvPayloadOffset;
        public CXPLAT_DATAPATH(int nWorkCount)
        {
            Partitions = new CXPLAT_DATAPATH_PROC[nWorkCount];
            for (int i = 0; i < Partitions.Length; i++)
            {
                Partitions[i] = new CXPLAT_DATAPATH_PROC();
            }
        }
    }

    internal class CXPLAT_SOCKET_PROC
    {
        public CXPLAT_DATAPATH_PROC DatapathProc;
        public CXPLAT_SOCKET Parent;
        public CxPlatSocketHandle Socket;
        public byte[] AcceptAddrSpace = new byte[4 + 16 + 4 + 16];
        public readonly SocketAsyncEventArgs SendArgs = new SocketAsyncEventArgs();
        public bool bReceiveIOContexUsed = false;
        public bool bSendIOContexUsed = false;
        public bool Freed;
        public bool RecvFailure;
        public bool IoStarted;
        public bool Uninitialized;
        public readonly CXPLAT_RUNDOWN_REF RundownRef = new CXPLAT_RUNDOWN_REF();
        public int RioSendCount;
    }

    internal class CXPLAT_SOCKET_RAW
    {
        //public Dictionary<ushort, > Entry;
        public CXPLAT_RUNDOWN_REF Rundown;
        public CXPLAT_DATAPATH_RAW RawDatapath;
        public Socket AuxSocket;
        public bool Wildcard;                // Using a wildcard local address. Optimization
                                             // to avoid always reading LocalAddress.
        public byte CibirIdLength;           // CIBIR ID length. Value of 0 indicates CIBIR isn't used
        public byte CibirIdOffsetSrc;        // CIBIR ID offset in source CID
        public byte CibirIdOffsetDst;        // CIBIR ID offset in destination CID
        public byte[] CibirId = new byte[6];              // CIBIR ID data

        public CXPLAT_SEND_DATA PausedTcpSend; // Paused TCP send data *before* framing
        public CXPLAT_SEND_DATA CachedRstSend; // Cached TCP RST send data *after* framing
    }

    internal class CXPLAT_SOCKET_COMMON: CXPLAT_SOCKET_RAW
    {
        public readonly QUIC_ADDR LocalAddress = new QUIC_ADDR();
        public readonly QUIC_ADDR RemoteAddress = new QUIC_ADDR();
        public CXPLAT_DATAPATH Datapath;
        public ushort Mtu;
    }
    
    internal class CXPLAT_SOCKET: CXPLAT_SOCKET_COMMON
    {
        public long RefCount;
        public int RecvBufLen;
        public bool Connected;
        public CXPLAT_SOCKET_TYPE Type;
        public int NumPerProcessorSockets;
        public bool HasFixedRemoteAddress;
        public byte DisconnectIndicated;
        public bool PcpBinding;
        public bool Uninitialized;
        public bool UseRio;
        public bool Freed;
        public bool RawSocketAvailable;
        public CXPLAT_SOCKET_PROC[] PerProcSockets = null;
        public object ClientContext;
    }
}
