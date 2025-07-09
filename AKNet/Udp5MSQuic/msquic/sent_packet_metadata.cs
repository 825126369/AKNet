using AKNet.Common;
using System;

namespace AKNet.Udp5MSQuic.Common
{
    internal class QUIC_SENT_PACKET_POOL
    {
        public readonly CXPLAT_POOL<QUIC_SENT_PACKET_METADATA>[] Pools = new CXPLAT_POOL<QUIC_SENT_PACKET_METADATA>[MSQuicFunc.QUIC_MAX_FRAMES_PER_PACKET];
    }

    internal struct QUIC_SEND_PACKET_FLAGS
    {
        public QUIC_PACKET_KEY_TYPE KeyType;
        public bool IsAckEliciting; //如果为 TRUE，表示该包需要对方回复 ACK
        public bool IsMtuProbe;//是否是一个 MTU 探测包（用于路径 MTU 发现）
        public bool KeyPhase;//指示当前使用的是哪个加密密钥阶段（用于密钥更新）
        public bool SuspectedLost; //包是否被怀疑已丢失（用于丢包检测）
        public bool IsAppLimited; //是否因为应用层速率限制而延迟发送
        public bool HasLastAckedPacketInfo; //是否包含上次确认的数据包信息
        public bool EcnEctSet;//是否设置了 ECN（显式拥塞通知）标记
        public bool Freed; //调试用标志，表示该包是否已被释放
    }

    internal struct LAST_ACKED_PACKET_INFO
    {
        public ulong TotalBytesSent;
        public ulong TotalBytesAcked;
        public long SentTime;
        public long AckTime;
        public long AdjustedAckTime;
    }



    internal class QUIC_SENT_PACKET_METADATA : CXPLAT_POOL_Interface<QUIC_SENT_PACKET_METADATA>
    {
        public CXPLAT_POOL<QUIC_SENT_PACKET_METADATA> mPool = null;
        public readonly CXPLAT_POOL_ENTRY<QUIC_SENT_PACKET_METADATA> POOL_ENTRY = null;
        public QUIC_SENT_PACKET_METADATA Next;
        public ulong PacketId;
        public ulong PacketNumber;
        public ulong TotalBytesSent;
        public long SentTime;
        public int PacketLength;
        public byte PathId;
        public LAST_ACKED_PACKET_INFO LastAckedPacketInfo;
        public QUIC_SEND_PACKET_FLAGS Flags;
        public byte FrameCount;
        public readonly QUIC_SENT_FRAME_METADATA[] Frames = new QUIC_SENT_FRAME_METADATA[MSQuicFunc.QUIC_MAX_FRAMES_PER_PACKET];

        public QUIC_SENT_PACKET_METADATA()
        {
            POOL_ENTRY = new CXPLAT_POOL_ENTRY<QUIC_SENT_PACKET_METADATA>(this);
        }

        public CXPLAT_POOL_ENTRY<QUIC_SENT_PACKET_METADATA> GetEntry()
        {
            return POOL_ENTRY;
        }

        public void Reset()
        {
            this.Next = null;
            this.PacketId = default;
            this.PacketNumber = default;
            this.TotalBytesSent = default;
            this.SentTime = default;
            this.PacketLength = default;
            this.PathId = default;
            this.LastAckedPacketInfo = default;
            this.Flags = default;
            this.FrameCount = default;
            for (int i = 0; i < this.FrameCount; i++)
            {
                this.Frames[i].Reset();
            }
        }

        public void CopyFrom(QUIC_SENT_PACKET_METADATA other)
        {
            this.PacketId = other.PacketId;
            this.PacketNumber = other.PacketNumber;
            this.TotalBytesSent = other.TotalBytesSent;
            this.SentTime = other.SentTime;
            this.PacketLength = other.PacketLength;
            this.PathId = other.PathId;
            this.LastAckedPacketInfo = other.LastAckedPacketInfo;
            this.Flags = other.Flags;
            this.FrameCount = other.FrameCount;
            for (int i = 0; i < this.FrameCount; i++)
            {
                this.Frames[i].CopyFrom(other.Frames[i]);
            }
        }

        public void SetPool(CXPLAT_POOL<QUIC_SENT_PACKET_METADATA> mPool)
        {
            this.mPool = mPool;
        }

        public CXPLAT_POOL<QUIC_SENT_PACKET_METADATA> GetPool()
        {
            return this.mPool;
        }
    }

    internal struct QUIC_SENT_FRAME_METADATA
    {
        public struct ACK_DATA
        {
            public ulong LargestAckedPacketNumber;
        }

        public struct RESET_STREAM_DATA
        {
            public QUIC_STREAM Stream;
        }

        public struct RELIABLE_RESET_STREAM_DATA
        {
            public QUIC_STREAM Stream;
        }

        public struct STOP_SENDING_DATA
        {
            public QUIC_STREAM Stream;
        }

        public struct CRYPTO_DATA
        {
            public int Offset;
            public int Length;
        }

        public struct STREAM_DATA
        {
            public QUIC_STREAM Stream;
        }

        public struct MAX_STREAM_DATA_DATA
        {
            public QUIC_STREAM Stream;
        }

        public struct STREAM_DATA_BLOCKED_DATA
        {
            public QUIC_STREAM Stream;
        }

        public struct NEW_CONNECTION_ID_DATA
        {
            public ulong Sequence;
        }

        public struct RETIRE_CONNECTION_ID_DATA
        {
            public ulong Sequence;
        }

        public struct PATH_CHALLENGE_DATA
        {
            private byte[] m_Data;
            public byte[] Data
            {
                get
                {
                    if (m_Data == null)
                    {
                        m_Data = new byte[8];
                    }
                    return m_Data;
                }
            }

            public bool IsDataNull()
            {
                return m_Data == null;
            }
        }

        public struct PATH_RESPONSE_DATA
        {
            public byte[] m_Data;
            public byte[] Data
            {
                get
                {
                    if (m_Data == null)
                    {
                        m_Data = new byte[8];
                    }
                    return m_Data;
                }
            }
            public bool IsDataNull()
            {
                return m_Data == null;
            }
        }

        public struct DATAGRAM_DATA
        {
            public object ClientContext;
        }

        public struct ACK_FREQUENCY_DATA
        {
            public ulong Sequence;
        }

        public ACK_DATA ACK;
        public RESET_STREAM_DATA RESET_STREAM;
        public RELIABLE_RESET_STREAM_DATA RELIABLE_RESET_STREAM;
        public STOP_SENDING_DATA STOP_SENDING;
        public CRYPTO_DATA CRYPTO;
        public STREAM_DATA STREAM;
        public MAX_STREAM_DATA_DATA MAX_STREAM_DATA;
        public STREAM_DATA_BLOCKED_DATA STREAM_DATA_BLOCKED;
        public NEW_CONNECTION_ID_DATA NEW_CONNECTION_ID;
        public PATH_CHALLENGE_DATA PATH_CHALLENGE;
        public DATAGRAM_DATA DATAGRAM;
        public RETIRE_CONNECTION_ID_DATA RETIRE_CONNECTION_ID;
        public ACK_FREQUENCY_DATA ACK_FREQUENCY;
        public PATH_RESPONSE_DATA PATH_RESPONSE;
        public int StreamOffset;
        public int StreamLength;
        public QUIC_FRAME_TYPE Type;
        public int Flags;

        public void CopyFrom(QUIC_SENT_FRAME_METADATA other)
        {
            this = other;
            if (!PATH_CHALLENGE.IsDataNull())
            {
                this.PATH_CHALLENGE = new PATH_CHALLENGE_DATA();
                Array.Copy(other.PATH_CHALLENGE.Data, this.PATH_CHALLENGE.Data, this.PATH_CHALLENGE.Data.Length);
            }
            if (!PATH_RESPONSE.IsDataNull())
            {
                this.PATH_RESPONSE = new PATH_RESPONSE_DATA();
                Array.Copy(other.PATH_RESPONSE.Data, this.PATH_RESPONSE.Data, this.PATH_RESPONSE.Data.Length);
            }
        }

        public void Reset()
        {

        }
    }

    internal class QUIC_MAX_SENT_PACKET_METADATA
    {
        public readonly QUIC_SENT_PACKET_METADATA Metadata = new QUIC_SENT_PACKET_METADATA();
    }

    internal static partial class MSQuicFunc
    {
        static void QuicSentPacketPoolInitialize(QUIC_SENT_PACKET_POOL Pool)
        {
            for (int i = 0; i < Pool.Pools.Length; i++)
            {
                Pool.Pools[i] = new CXPLAT_POOL<QUIC_SENT_PACKET_METADATA>();
                Pool.Pools[i].CxPlatPoolInitialize();
            }
        }

        static void QuicSentPacketPoolUninitialize(QUIC_SENT_PACKET_POOL Pool)
        {
            for (int i = 0; i < Pool.Pools.Length; i++)
            {
                Pool.Pools[i].CxPlatPoolUninitialize();
            }
        }

        static QUIC_SENT_PACKET_METADATA QuicSentPacketPoolGetPacketMetadata(QUIC_SENT_PACKET_POOL Pool, int FrameCount)
        {
            QUIC_SENT_PACKET_METADATA Metadata = Pool.Pools[FrameCount - 1].CxPlatPoolAlloc();
            return Metadata;
        }

        static void QuicSentPacketPoolReturnPacketMetadata(QUIC_SENT_PACKET_METADATA Metadata,QUIC_CONNECTION Connection)
        {
            NetLog.Assert(Metadata.FrameCount > 0 && Metadata.FrameCount <= QUIC_MAX_FRAMES_PER_PACKET);
            QuicSentPacketMetadataReleaseFrames(Metadata, Connection);
            Connection.Partition.SentPacketPool.Pools[Metadata.FrameCount - 1].CxPlatPoolFree(Metadata);
        }

        static void QuicSentPacketMetadataReleaseFrames(QUIC_SENT_PACKET_METADATA Metadata, QUIC_CONNECTION Connection)
        {
            for (int i = 0; i < Metadata.FrameCount; i++)
            {
                switch (Metadata.Frames[i].Type)
                {
                    case QUIC_FRAME_TYPE.QUIC_FRAME_RESET_STREAM:
                        QuicStreamSentMetadataDecrement(Metadata.Frames[i].RESET_STREAM.Stream);
                        break;
                    case QUIC_FRAME_TYPE.QUIC_FRAME_MAX_STREAM_DATA:
                        QuicStreamSentMetadataDecrement(Metadata.Frames[i].MAX_STREAM_DATA.Stream);
                        break;
                    case QUIC_FRAME_TYPE.QUIC_FRAME_STREAM_DATA_BLOCKED:
                        QuicStreamSentMetadataDecrement(Metadata.Frames[i].STREAM_DATA_BLOCKED.Stream);
                        break;
                    case QUIC_FRAME_TYPE.QUIC_FRAME_STOP_SENDING:
                        QuicStreamSentMetadataDecrement(Metadata.Frames[i].STOP_SENDING.Stream);
                        break;
                    case QUIC_FRAME_TYPE.QUIC_FRAME_STREAM:
                        QuicStreamSentMetadataDecrement(Metadata.Frames[i].STREAM.Stream);
                        break;
                    case QUIC_FRAME_TYPE.QUIC_FRAME_RELIABLE_RESET_STREAM:
                        QuicStreamSentMetadataDecrement(Metadata.Frames[i].RELIABLE_RESET_STREAM.Stream);
                        break;
                    case QUIC_FRAME_TYPE.QUIC_FRAME_DATAGRAM:
                    case QUIC_FRAME_TYPE.QUIC_FRAME_DATAGRAM_1:
                        if (Metadata.Frames[i].DATAGRAM.ClientContext != null)
                        {
                            QuicDatagramIndicateSendStateChange(Connection, ref Metadata.Frames[i].DATAGRAM.ClientContext, QUIC_DATAGRAM_SEND_STATE.QUIC_DATAGRAM_SEND_LOST_DISCARDED);
                        }
                        break;
                    default:
                        break;
                }
            }
        }


    }

}
