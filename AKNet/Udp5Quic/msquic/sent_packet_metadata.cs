using System.Linq;

namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_SENT_PACKET_POOL
    {
        public readonly CXPLAT_POOL[] Pools = new CXPLAT_POOL[MSQuicFunc.QUIC_MAX_FRAMES_PER_PACKET];
    }

    internal class QUIC_SEND_PACKET_FLAGS
    {
        public QUIC_PACKET_KEY_TYPE KeyType;
        public bool IsAckEliciting;
        public bool IsMtuProbe;
        public bool KeyPhase;
        public bool SuspectedLost;
        public bool IsAppLimited;
        public bool HasLastAckedPacketInfo;
        public bool EcnEctSet;
#if DEBUG
        public bool Freed;
#endif
    }

    internal class LAST_ACKED_PACKET_INFO
    {
        public ulong TotalBytesSent;
        public ulong TotalBytesAcked;
        public long SentTime;
        public long AckTime;
        public long AdjustedAckTime;
    }



    internal class QUIC_SENT_PACKET_METADATA : CXPLAT_POOL_Interface<QUIC_SENT_PACKET_METADATA>
    {
        public readonly CXPLAT_POOL_ENTRY<QUIC_SENT_PACKET_METADATA> POOL_ENTRY = null;

        public QUIC_SENT_PACKET_METADATA Next;
        public ulong PacketId;
        public ulong PacketNumber;
        public ulong TotalBytesSent;
        public long SentTime;
        public ushort PacketLength;
        public byte PathId;

        public LAST_ACKED_PACKET_INFO LastAckedPacketInfo;
        public QUIC_SEND_PACKET_FLAGS Flags;
        public byte FrameCount;
        public QUIC_SENT_FRAME_METADATA[] Frames = null;

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
            throw new System.NotImplementedException();
        }
    }

    internal class QUIC_SENT_FRAME_METADATA
    {
        public class ACK_Class
        {
            public ulong LargestAckedPacketNumber;
        }

        public class RESET_STREAM_CLASS
        {
            public QUIC_STREAM Stream;
        }

        public class RELIABLE_RESET_STREAM_Class
        {
            public QUIC_STREAM Stream;
        }

        public class STOP_SENDING_Class
        {
            public QUIC_STREAM Stream;
        }

        public class CRYPTO_Class
        {
            public int Offset;
            public int Length;
        }

        public class STREAM_Class
        {
            public QUIC_STREAM Stream;
        }

        public class MAX_STREAM_DATA_Class
        {
            public QUIC_STREAM Stream;
        }

        public class STREAM_DATA_BLOCKED_Class
        {
            public QUIC_STREAM Stream;
        }
        
        public class NEW_CONNECTION_ID_Class
        {
            public ulong Sequence;
        }

        public class RETIRE_CONNECTION_ID_Class
        {
            public ulong Sequence;
        }

        public class PATH_CHALLENGE_Class
        {
            public byte[] Data = new byte[8];
        }

        public class PATH_RESPONSE
        {
            public byte[] Data = new byte[8];
        }

        public class DATAGRAM_Class
        {
            void* ClientContext;
        }

        public class ACK_FREQUENCY_Class
        {
            public ulong Sequence;
        }

            
        public ACK_Class ACK;
        public RESET_STREAM_CLASS RESET_STREAM;
        public RELIABLE_RESET_STREAM_Class RELIABLE_RESET_STREAM;
        public STOP_SENDING_Class STOP_SENDING;
        public CRYPTO_Class CRYPTO;
        public STREAM_Class STREAM;
        public MAX_STREAM_DATA_Class MAX_STREAM_DATA;
        public STREAM_DATA_BLOCKED_Class STREAM_DATA_BLOCKED;
        public NEW_CONNECTION_ID_Class NEW_CONNECTION_ID;
        public PATH_CHALLENGE_Class PATH_CHALLENGE;
        public DATAGRAM_Class DATAGRAM;

        public ACK_FREQUENCY_Class ACK_FREQUENCY;

        public int StreamOffset;
        public int StreamLength;
        public QUIC_FRAME_TYPE Type;
        public int Flags;
    }

    public class QUIC_MAX_SENT_PACKET_METADATA
    {
        public QUIC_SENT_PACKET_METADATA Metadata;
        public readonly byte[] Raw = new byte[sizeof(QUIC_SENT_PACKET_METADATA) + sizeof(QUIC_SENT_FRAME_METADATA) * MSQuicFunc.QUIC_MAX_FRAMES_PER_PACKET];
    }

    internal static partial class MSQuicFunc
    {
        static void QuicSentPacketPoolInitialize(QUIC_SENT_PACKET_POOL Pool)
        {
            for (int i = 0; i < Pool.Pools.Length; i++)
            {
                //int PacketMetadataSize = (i + 1) * sizeof(QUIC_SENT_FRAME_METADATA) + sizeof(QUIC_SENT_PACKET_METADATA);
                //CxPlatPoolInitialize(false, PacketMetadataSize, QUIC_POOL_META, Pool.Pools.Count + i);
            }
        }
    }

}
