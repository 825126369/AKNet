/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:18
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Net.Sockets;

namespace MSQuic1
{
    internal class CXPLAT_DATAPATH_COMMON
    {
        public CXPLAT_UDP_DATAPATH_CALLBACKS UdpHandlers;
        public uint Features;
    }

    internal class CXPLAT_DATAPATH_PROC
    {
        public CXPLAT_DATAPATH Datapath;
        public long RefCount;
        public int PartitionIndex;
        public bool Uninitialized;
        public readonly CXPLAT_POOL<CXPLAT_SEND_DATA> SendDataPool = new CXPLAT_POOL<CXPLAT_SEND_DATA>();
        public readonly CXPLAT_Buffer_POOL SendBufferPool = new CXPLAT_Buffer_POOL();
        public readonly CXPLAT_Buffer_POOL LargeSendBufferPool = new CXPLAT_Buffer_POOL();
        public readonly CXPLAT_POOL<DATAPATH_RX_PACKET> RecvDatagramPool = new CXPLAT_POOL<DATAPATH_RX_PACKET>();
    }

    internal class CXPLAT_DATAPATH : CXPLAT_DATAPATH_COMMON
    {
        public long RefCount;
        public int PartitionCount;
        public byte MaxSendBatchSize;
        public bool Uninitialized;
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
        public Socket Socket;
        public bool Freed;
        public bool RecvFailure;
        public bool IoStarted;
        public bool Uninitialized;
    }

    internal class CXPLAT_SOCKET_COMMON
    {
        public QUIC_ADDR LocalAddress = new QUIC_ADDR();
        public QUIC_ADDR RemoteAddress = new QUIC_ADDR();
        public CXPLAT_DATAPATH Datapath;
        public ushort Mtu;
    }
    
    internal class CXPLAT_SOCKET: CXPLAT_SOCKET_COMMON
    {
        public int RecvBufLen;
        public bool HasFixedRemoteAddress;
        public byte DisconnectIndicated;
        public bool Uninitialized;
        public CXPLAT_SOCKET_PROC[] PerProcSockets = null;
        public object ClientContext;
    }
}
