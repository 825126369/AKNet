using AKNet.Common;
using System;
using System.Threading;

namespace AKNet.Udp1MSQuic.Common
{
    //QUIC 版本协商数据包
    internal class QUIC_VERSION_NEGOTIATION_PACKET
    {
        public byte Unused; //7位
        public byte IsLongHeader;//1位
        public uint Version;
        public byte DestCidLength;
        public QUIC_BUFFER m_DestCid = null;

        public QUIC_BUFFER DestCid
        {
            get
            {
                if (m_DestCid == null)
                {
                    m_DestCid = new QUIC_BUFFER();
                }
                return m_DestCid;
            }
        }

        public void WriteFrom(QUIC_SSBuffer buffer)
        {
            IsLongHeader = (byte)((buffer[0] & 0x80) >> 7);
            Version = EndianBitConverter.ToUInt32(buffer.GetSpan(), 1);
            DestCidLength = buffer[5];
            m_DestCid = buffer.Slice(6);
        }

        public void WriteTo(Span<byte> buffer)
        {

        }
    }

    internal class QUIC_LONG_HEADER_V1
    {
        public byte PnLength;  ////  2位 //数据包数据包编号 Packet Number Length  实际长度 = (PnLength + 1) 字节
        public byte Reserved;  //  2位 //必须为 0，用于将来扩展或对齐

        //  长头部类型
        //  取值代表不同的 QUIC 长头部格式：
        //  0b00: Initial
        //  0b01: 0-RTT
        //  0b10: Handshake
        //  0b11: Retry
        public byte Type; //  2位
        public byte FixedBit;  //  1位 //在标准 QUIC 实现中必须设为 1。
        public byte IsLongHeader;//  1位   1标识这是一个长头部。

        //0x00000001 → QUIC v1
        //0x00000002 → QUIC v2
        public uint Version;
        public byte DestCidLength;
        QUIC_BUFFER m_DestCid;

        //uint8_t SourceCidLength;
        //uint8_t SourceCid[SourceCidLength];
        //  QUIC_VAR_INT TokenLength;       {Initial}
        //  uint8_t Token[0];               {Initial}
        //QUIC_VAR_INT Length;
        //uint8_t PacketNumber[PnLength];
        //uint8_t Payload[0];

        public const int sizeof_Length = 6;
        public void WriteFrom(QUIC_SSBuffer buffer)
        {
            this.UpdateFirstByte(buffer[0]);
            Version = EndianBitConverter.ToUInt32(buffer.GetSpan(), 1);
            DestCidLength = buffer[5];
            m_DestCid = buffer.Slice(6);
        }

        private void UpdateFirstByte(byte buffer)
        {
            PnLength = (byte)(buffer & 0x03);
            Reserved = (byte)((buffer & 0x0C) >> 2);
            Type = (byte)((buffer & 0x30) >> 4);
            FixedBit = (byte)((buffer & 0x40) >> 6);
            IsLongHeader = (byte)(buffer & 0x80 >> 7);
        }

        public byte GetFirstByte()
        {
            return (byte)(
                   (PnLength & 0x03) |
                   ((Reserved & 0x03) << 2) |
                   ((Type & 0x03) << 4) |
                   ((FixedBit & 0x01) << 6) |
                   ((IsLongHeader & 0x01) << 7)
               );
        }

        public void WriteTo(Span<byte> buffer)
        {

        }
    }

    //短头部主要用于在[连接建立][之后]传输数据。
    internal class QUIC_SHORT_HEADER_V1
    {
        public byte PnLength; //2位，表示数据包编号（Packet Number）的长度，单位为字节。
        public byte KeyPhase; //1位，用于标识当前使用的密钥阶段，在 QUIC 中，密钥阶段用于区分不同的加密密钥。当密钥更新时，该位会切换
        public byte Reserved; //2位, 一定是0
        public byte SpinBit; //1位，用于测量往返时间（RTT）。客户端和服务器会交替翻转该位，以帮助检测网络延迟
        public byte FixedBit;   // 固定位（1位，必须为1, 用于标识这是一个有效的 QUIC 数据包
        public byte IsLongHeader;// 是否为长头部（1位，短头部为0）
        public QUIC_BUFFER m_DestCid; // 目标连接ID，
                                      // uint8_t PacketNumber[PnLength]; // 数据包编号（长度由PnLength决定）
                                      // uint8_t Payload[0];             // 数据包有效载荷
        
        public const int sizeof_Length = 1;
        public QUIC_BUFFER DestCid
        {
            get
            {
                if (m_DestCid == null)
                {
                    m_DestCid = new QUIC_BUFFER();
                }
                return m_DestCid;
            }
        }

        public void WriteFrom(QUIC_SSBuffer buffer)
        {
            this.UpdateFirstByte(buffer[0]);
            m_DestCid = buffer.Slice(1);
        }

        private void UpdateFirstByte(byte buffer)
        {
            PnLength = (byte)((buffer & 0x03));
            KeyPhase = (byte)((buffer & 0x04) >> 2);
            Reserved = (byte)((buffer & 0x18) >> 3);
            SpinBit = (byte)((buffer & 0x20) >> 5);
            FixedBit = (byte)((buffer & 0x40) >> 6);
            IsLongHeader = (byte)((buffer & 0x80) >> 7);
        }

        public byte GetFirstByte()
        {
            return (byte)
                (
                   (PnLength & 0x03) |
                   ((KeyPhase & 0x01) << 2) |
                   ((Reserved & 0x03) << 3) |
                   ((SpinBit & 0x01) << 5) |
                   ((FixedBit & 0x01) << 6) |
                   ((IsLongHeader & 0x01) << 7)
               );
        }

        public void WriteTo(Span<byte> buffer)
        {

        }
    }

    internal class QUIC_RETRY_PACKET_V1
    {
        public byte UNUSED; //4位

        //表示这个长头部的类型。
        //对于 Retry 包，该字段应为：
        //Type == 0b11 （二进制）
        //#define QUIC_LONG_HEADER_TYPE_RETRY 3
        public byte Type; //2位
        public byte FixedBit;//1位
        public byte IsLongHeader;//1位
        public uint Version;
        public byte DestCidLength;
        private QUIC_BUFFER m_DestCid;
        public QUIC_BUFFER DestCid
        {
            get
            {
                if (m_DestCid == null)
                {
                    m_DestCid = new QUIC_BUFFER();
                }
                return m_DestCid;
            }
        }

        public byte[] ToBytes()
        {
            return null;
        }

        public void WriteFrom(QUIC_SSBuffer buffer)
        {
            UNUSED = (byte)(buffer[0] & 0x0F);
            Type = (byte)((buffer[0] & 0x30) >> 4);
            FixedBit = (byte)((buffer[0] & 0x40) >> 6);
            IsLongHeader = (byte)((buffer[0] & 0x80) >> 7);

            Version = EndianBitConverter.ToUInt32(buffer.GetSpan(), 1);
            DestCidLength = buffer[5];
            m_DestCid = buffer.Slice(6);
        }

        public void WriteTo(Span<byte> buffer)
        {
            
        }
    }

    //头部不可变部分
    //在 QUIC 协议里，存在一些布局不变字段，这些字段不依赖特定版本，在不同版本中保持一致。
    internal class QUIC_HEADER_INVARIANT
    {
        public struct LONG_HDR_DATA
        {
            public byte VARIANT;// 7位;
            public byte IsLongHeader;// 1位;
            public uint Version;// 4个字节;
            public byte DestCidLength; //1个字节
            public QUIC_BUFFER DestCid;
        }

        public struct SHORT_HDR_DATA
        {
            public byte VARIANT; // 7位;
            public byte IsLongHeader; //1位
            public QUIC_BUFFER DestCid;       
        }

        public byte VARIANT;// 7位;
        public byte IsLongHeader;// 1位;
        public LONG_HDR_DATA LONG_HDR;
        public SHORT_HDR_DATA SHORT_HDR;

        public void WriteFrom(QUIC_SSBuffer buffer)
        {
            IsLongHeader = (byte)((buffer[0] & 0x80) >> 7);

            LONG_HDR.IsLongHeader = (byte)((buffer[0] & 0x80) >> 7);
            LONG_HDR.Version = EndianBitConverter.ToUInt32(buffer.GetSpan(), 1);
            LONG_HDR.DestCidLength = buffer[5];
            LONG_HDR.DestCid = buffer.Slice(6);
            
            SHORT_HDR.IsLongHeader = (byte)((buffer[0] & 0x80) >> 7);
            SHORT_HDR.DestCid = buffer.Slice(1);
        }
    }

    internal class QUIC_VERSION_INFO
    {
        public uint Number;
        public QUIC_BUFFER Salt = new byte[MSQuicFunc.CXPLAT_VERSION_SALT_LENGTH];
        public QUIC_BUFFER RetryIntegritySecret = new byte[MSQuicFunc.QUIC_VERSION_RETRY_INTEGRITY_SECRET_LENGTH];
        public QUIC_HKDF_LABELS HkdfLabels;
    }

    internal enum QUIC_LONG_HEADER_TYPE_V1
    {
        QUIC_INITIAL_V1 = 0,
        QUIC_0_RTT_PROTECTED_V1 = 1,
        QUIC_HANDSHAKE_V1 = 2,
        QUIC_RETRY_V1 = 3,
    }

    internal enum QUIC_LONG_HEADER_TYPE_V2
    {
        QUIC_RETRY_V2 = 0,
        QUIC_INITIAL_V2 = 1,
        QUIC_0_RTT_PROTECTED_V2 = 2,
        QUIC_HANDSHAKE_V2 = 3,
    }
    
    internal static partial class MSQuicFunc
    {
        public const int QUIC_VERSION_RETRY_INTEGRITY_SECRET_LENGTH = 32;
        public const int MIN_INV_LONG_HDR_LENGTH = 7;// 6个字节不可变部分  1个字节 + 4个字节（版本号）+ 2个字节（分别是源Cid和目标Cid的长度的1个字节）
        public const int MIN_INV_SHORT_HDR_LENGTH = 1; //1个字节不可变部分
        public const int QUIC_RETRY_INTEGRITY_TAG_LENGTH_V1 = CXPLAT_ENCRYPTION_OVERHEAD;

        public static readonly QUIC_VERSION_INFO[] QuicSupportedVersionList = new QUIC_VERSION_INFO[]{
             new QUIC_VERSION_INFO()
             {
                Number = QUIC_VERSION_2,
                Salt = new byte[]{ 0x0d, 0xed, 0xe3, 0xde, 0xf7, 0x00, 0xa6, 0xdb, 0x81, 0x93,0x81, 0xbe, 0x6e, 0x26, 0x9d, 0xcb, 0xf9, 0xbd, 0x2e, 0xd9 },
                RetryIntegritySecret = new byte[]{ 0x34, 0x25, 0xc2, 0x0c, 0xf8, 0x87, 0x79, 0xdf, 0x2f, 0xf7, 0x1e, 0x8a, 0xbf, 0xa7, 0x82, 0x49,0x89, 0x1e, 0x76, 0x3b, 0xbe, 0xd2, 0xf1, 0x3c, 0x04, 0x83, 0x43, 0xd3, 0x48, 0xc0, 0x60, 0xe2 },
                HkdfLabels = new QUIC_HKDF_LABELS()
                {
                    KeyLabel = "quicv2 key",
                    IvLabel = "quicv2 iv",
                    HpLabel = "quicv2 hp",
                    KuLabel = "quicv2 ku"
                }
             },
            new QUIC_VERSION_INFO()
            {
               Number = QUIC_VERSION_1,
              Salt = new byte[]{ 0x38, 0x76, 0x2c, 0xf7, 0xf5, 0x59, 0x34, 0xb3, 0x4d, 0x17,0x9a, 0xe6, 0xa4, 0xc8, 0x0c, 0xad, 0xcc, 0xbb, 0x7f, 0x0a },
              RetryIntegritySecret = new byte[]{ 0xd9, 0xc9, 0x94, 0x3e, 0x61, 0x01, 0xfd, 0x20, 0x00, 0x21, 0x50, 0x6b, 0xcc, 0x02, 0x81, 0x4c,
                0x73, 0x03, 0x0f, 0x25, 0xc7, 0x9d, 0x71, 0xce, 0x87, 0x6e, 0xca, 0x87, 0x6e, 0x6f, 0xca, 0x8e },
                HkdfLabels = new QUIC_HKDF_LABELS()
                {
                    KeyLabel = "quic key",
                    IvLabel = "quic iv",
                    HpLabel = "quic hp",
                    KuLabel = "quic ku"
                }
             }
        };

        //0：客户端 1: 服务器
        static readonly bool[,] QUIC_HEADER_TYPE_ALLOWED_V1 = new bool[2, 4]
        {
            {
                true,  // QUIC_INITIAL_V1
                false, // QUIC_0_RTT_PROTECTED_V1
                true,  // QUIC_HANDSHAKE_V1
                true,  // QUIC_RETRY_V1
            },
            {
                true,  // QUIC_INITIAL_V1
                true,  // QUIC_0_RTT_PROTECTED_V1
                true,  // QUIC_HANDSHAKE_V1
                false, // QUIC_RETRY_V1
             }

        };

        //0：客户端 1: 服务器
        static readonly bool[,] QUIC_HEADER_TYPE_ALLOWED_V2 = new bool[2, 4]
        {
            {
                true,  // QUIC_RETRY_V2
                true,  // QUIC_INITIAL_V2
                false, // QUIC_0_RTT_PROTECTED_V2
                true,  // QUIC_HANDSHAKE_V2
            },
            {
                false, // QUIC_RETRY_V2
                true,  // QUIC_INITIAL_V2
                true,  // QUIC_0_RTT_PROTECTED_V2
                true,  // QUIC_HANDSHAKE_V2
            },
        };

        static int QuicMinPacketLengths(byte IsLongHeader)
        {
            if (BoolOk(IsLongHeader))
            {
                return MIN_INV_LONG_HDR_LENGTH;
            }
            else
            {
                return MIN_INV_SHORT_HDR_LENGTH;
            }
        }

        static bool QuicPacketValidateInvariant(object Owner, QUIC_RX_PACKET Packet, bool IsBindingShared)
        {
            int DestCidLen, SourceCidLen;
            QUIC_SSBuffer DestCid, SourceCid;

            if (Packet.AvailBufferLength == 0 || Packet.AvailBufferLength < QuicMinPacketLengths(Packet.Invariant.IsLongHeader))
            {
                QuicPacketLogDrop(Owner, Packet, "Too small for Packet->Invariant");
                return false;
            }
            
            if (BoolOk(Packet.Invariant.IsLongHeader))
            {
                Packet.IsShortHeader = false;
                DestCidLen = Packet.Invariant.LONG_HDR.DestCidLength;
                if (Packet.AvailBufferLength < MIN_INV_LONG_HDR_LENGTH + DestCidLen)
                {
                    QuicPacketLogDrop(Owner, Packet, "LH no room for DestCid");
                    return false;
                }

                DestCid = Packet.Invariant.LONG_HDR.DestCid;
                SourceCidLen = DestCid.Slice(DestCidLen)[0];
                Packet.HeaderLength = MIN_INV_LONG_HDR_LENGTH + DestCidLen + SourceCidLen;
                if (Packet.AvailBufferLength < Packet.HeaderLength)
                {
                    QuicPacketLogDrop(Owner, Packet, "LH no room for SourceCid");
                    return false;
                }
                SourceCid = DestCid + DestCidLen + +sizeof(byte);
            }
            else
            {
                Packet.IsShortHeader = true;
                DestCidLen = IsBindingShared ? MsQuicLib.CidTotalLength : 0;
                SourceCidLen = 0;

                Packet.HeaderLength = sizeof(byte) + DestCidLen;
                if (Packet.AvailBufferLength < Packet.HeaderLength)
                {
                    QuicPacketLogDrop(Owner, Packet, "SH no room for DestCid");
                    return false;
                }

                DestCid = Packet.Invariant.SHORT_HDR.DestCid;
                SourceCid = QUIC_SSBuffer.Empty;
            }

            if (!Packet.DestCid.Data.IsEmpty)
            {
                if (!orBufferEqual(Packet.DestCid.Data, DestCid.Slice(0, DestCidLen)))
                {
                    // NetLogHelper.PrintByteArray("Packet.DestCid.Data", Packet.DestCid.Data.GetSpan());
                    // NetLogHelper.PrintByteArray("Packet.DestCid.Data2", DestCid.Slice(0, DestCidLen).GetSpan());
                    QuicPacketLogDrop(Owner, Packet, "DestCid don't match");
                    return false;
                }
                
                if (!Packet.IsShortHeader)
                {
                    NetLog.Assert(Packet.SourceCid != null);
                    if (!orBufferEqual(Packet.SourceCid.Data, SourceCid.Slice(0, SourceCidLen)))
                    {
                        // NetLogHelper.PrintByteArray("Packet.SourceCid.Data", Packet.SourceCid.Data.GetSpan());
                        // NetLogHelper.PrintByteArray("Packet.SourceCid.Data2", SourceCid.Slice(0, SourceCidLen).GetSpan());
                        QuicPacketLogDrop(Owner, Packet, "SourceCid don't match");
                        return false;
                    }
                }
            }
            else
            {
                Packet.DestCid.Data.SetData(DestCid.Slice(0, DestCidLen));
                Packet.SourceCid.Data.SetData(SourceCid.Slice(0, SourceCidLen));
            }

            Packet.ValidatedHeaderInv = true;
            return true;
        }

        static bool QuicPacketIsHandshake(QUIC_RX_PACKET Packet)
        {
            if (!BoolOk(Packet.Invariant.IsLongHeader))
            {
                return false;
            }

            switch (Packet.Invariant.LONG_HDR.Version)
            {
                case QUIC_VERSION_1:
                    return Packet.LH.Type != (byte)QUIC_LONG_HEADER_TYPE_V1.QUIC_0_RTT_PROTECTED_V1;
                case QUIC_VERSION_2:
                    return Packet.LH.Type != (byte)QUIC_LONG_HEADER_TYPE_V2.QUIC_0_RTT_PROTECTED_V2;
                default:
                    return true;
            }
        }

        static void QuicPacketLogDrop(object Owner, QUIC_RX_PACKET Packet, string Reason)
        {
            if (Packet.AssignedToConnection)
            {
                Interlocked.Increment(ref ((QUIC_CONNECTION)Owner).Stats.Recv.DroppedPackets);
            }
            else
            {
                Interlocked.Increment(ref ((QUIC_BINDING)Owner).Stats.Recv.DroppedPackets);
            }

            QuicPerfCounterIncrement(MsQuicLib.Partitions[Packet.PartitionIndex], QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_PKTS_DROPPED);
            NetLog.LogError(Reason);
        }

        public static int QuicPacketHash(QUIC_ADDR RemoteAddress, QUIC_SSBuffer RemoteCid)
        {
            uint Key;
            CxPlatToeplitzHashComputeAddr(MsQuicLib.ToeplitzHash, RemoteAddress, out Key);
            if (RemoteCid.Length != 0)
            {
                Key ^= CxPlatToeplitzHashCompute(MsQuicLib.ToeplitzHash, RemoteCid);
            }
            return (int)Key;
        }

        static bool QuicPacketValidateLongHeaderV1(object Owner, bool IsServer, QUIC_RX_PACKET Packet, ref QUIC_SSBuffer Token, bool IgnoreFixedBit)
        {
            NetLog.Assert(Packet.ValidatedHeaderInv);
            NetLog.Assert(Packet.AvailBufferLength >= Packet.HeaderLength);
            NetLog.Assert((Packet.LH.Version != QUIC_VERSION_2 && Packet.LH.Type != (byte)QUIC_LONG_HEADER_TYPE_V1.QUIC_RETRY_V1) ||
            (Packet.LH.Version == QUIC_VERSION_2 && Packet.LH.Type != (byte)QUIC_LONG_HEADER_TYPE_V2.QUIC_RETRY_V2));

            if (Packet.DestCid.Data.Length > QUIC_MAX_CONNECTION_ID_LENGTH_V1 || Packet.SourceCid.Data.Length > QUIC_MAX_CONNECTION_ID_LENGTH_V1)
            {
                QuicPacketLogDrop(Owner, Packet, "Greater than allowed max CID length");
                return false;
            }

            if ((Packet.LH.Version != QUIC_VERSION_2 && QUIC_HEADER_TYPE_ALLOWED_V1[IsServer ? 1 : 0, Packet.LH.Type] == false) ||
                (Packet.LH.Version == QUIC_VERSION_2 && QUIC_HEADER_TYPE_ALLOWED_V2[IsServer ? 1 : 0, Packet.LH.Type] == false))
            {
                QuicPacketLogDropWithValue(Owner, Packet, "Invalid client/server packet type", Packet.LH.Type);
                return false;
            }

            if (IgnoreFixedBit == false && Packet.LH.FixedBit == 0)
            {
                QuicPacketLogDrop(Owner, Packet, "Invalid LH FixedBit bits values: ");
                return false;
            }

            //这里包头长度，是刚好到Token这里, 这里刚好解析 Token
            int Offset = Packet.HeaderLength;
            QUIC_SSBuffer mBuf = Packet.AvailBuffer + Offset;
            if ((Packet.LH.Version != QUIC_VERSION_2 && Packet.LH.Type == (byte)QUIC_LONG_HEADER_TYPE_V1.QUIC_INITIAL_V1) ||
                (Packet.LH.Version == QUIC_VERSION_2 && Packet.LH.Type == (byte)QUIC_LONG_HEADER_TYPE_V2.QUIC_INITIAL_V2))
            {
                if (IsServer && Packet.AvailBufferLength < QUIC_MIN_INITIAL_PACKET_LENGTH)
                {
                    QuicPacketLogDropWithValue(Owner, Packet, "Client Long header Initial packet too short", Packet.AvailBufferLength);
                    return false;
                }

                int TokenLengthVarInt = 0;
                if (!QuicVarIntDecode(ref mBuf, ref TokenLengthVarInt))
                {
                    QuicPacketLogDrop(Owner, Packet, "Long header has invalid token length");
                    return false;
                }

                if (mBuf.Length < TokenLengthVarInt)
                {
                    QuicPacketLogDropWithValue(Owner, Packet, "Long header has token length larger than buffer length", (int)TokenLengthVarInt);
                    return false;
                }

                Token = mBuf;
                Token.Length = TokenLengthVarInt;
                mBuf += TokenLengthVarInt;
            }
            else
            {
                Token = QUIC_SSBuffer.Empty;
                //这里没有return，有可能其他长头包，没有Token
            }

            //解析包体长度，也就是负载长度
            int LengthVarInt = 0;
            if (!QuicVarIntDecode(ref mBuf, ref LengthVarInt))
            {
                QuicPacketLogDrop(Owner, Packet, "Long header has invalid payload length");
                return false;
            }

            int HeaderLength = mBuf.Offset - Packet.AvailBuffer.Offset;
            if (Packet.AvailBufferLength < HeaderLength + LengthVarInt)
            {
                QuicPacketLogDropWithValue(Owner, Packet, "Long header has length larger than buffer length", (int)LengthVarInt);
                return false;
            }

            if (Packet.AvailBufferLength < HeaderLength + sizeof(uint)) //判断是否有足够的空间来存储包编号
            {
                QuicPacketLogDropWithValue(Owner, Packet, "Long Header doesn't have enough room for packet number", Packet.AvailBufferLength);
                return false;
            }

            Packet.HeaderLength = HeaderLength; //现在这里头部长度，刚好可以解析 Packet Number
            Packet.PayloadLength = (int)LengthVarInt;
            Packet.AvailBufferLength = Packet.HeaderLength + Packet.PayloadLength;
            Packet.ValidatedHeaderVer = true;
            return true;
        }
        
        static bool QuicPacketValidateInitialToken(object Owner, QUIC_RX_PACKET Packet, QUIC_SSBuffer TokenBuffer, ref bool DropPacket)
        {
            bool IsNewToken = BoolOk(TokenBuffer[0] & 0x1);
            if (IsNewToken)
            {
                QuicPacketLogDrop(Owner, Packet, "New Token not supported");
                DropPacket = true;
                return false;
            }

            if (TokenBuffer.Length != QUIC_TOKEN_CONTENTS.sizeof_QUIC_TOKEN_CONTENTS)
            {
                QuicPacketLogDrop(Owner, Packet, "Invalid Token Length");
                DropPacket = true;
                return false;
            }

            QUIC_TOKEN_CONTENTS Token = null;
            if (!QuicRetryTokenDecrypt(Packet, TokenBuffer, out Token))
            {
                QuicPacketLogDrop(Owner, Packet, "Retry Token Decryption Failure");
                DropPacket = true;
                return false;
            }

            if (Token.Encrypted.OrigConnId.Length > Token.Encrypted.OrigConnId.Length)
            {
                QuicPacketLogDrop(Owner, Packet, "Invalid Retry Token OrigConnId Length");
                DropPacket = true;
                return false;
            }

            if (Token.Encrypted.RemoteAddress != Packet.Route.RemoteAddress)
            {
                QuicPacketLogDrop(Owner, Packet, "Retry Token Addr Mismatch");
                DropPacket = true;
                return false;
            }

            return true;
        }

        static void QuicPacketLogDropWithValue(object Owner, QUIC_RX_PACKET Packet, string Reason, long Value)
        {
            if (Packet.AssignedToConnection)
            {
                Interlocked.Increment(ref ((QUIC_CONNECTION)Owner).Stats.Recv.DroppedPackets);
            }
            else
            {
                Interlocked.Increment(ref ((QUIC_BINDING)Owner).Stats.Recv.DroppedPackets);
            }
            QuicPerfCounterIncrement(MsQuicLib.Partitions[Packet.PartitionIndex], QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_PKTS_DROPPED);
            NetLog.LogError(Reason);
        }

        static int MIN_RETRY_HEADER_LENGTH_V1()
        {
            return sizeof_QUIC_RETRY_PACKET_V1 + sizeof(byte);
        }

        static int QuicPacketMaxBufferSizeForRetryV1()
        {
            return MIN_RETRY_HEADER_LENGTH_V1() + 3 * QUIC_MAX_CONNECTION_ID_LENGTH_V1 + QUIC_TOKEN_CONTENTS.sizeof_QUIC_TOKEN_CONTENTS;
        }

        static int QuicPacketEncodeRetryV1(uint Version, QUIC_SSBuffer DestCid, QUIC_SSBuffer SourceCid, QUIC_SSBuffer OrigDestCid, QUIC_SSBuffer Token, QUIC_SSBuffer Buffer)
        {
            int RequiredBufferLength = MIN_RETRY_HEADER_LENGTH_V1() + DestCid.Length + SourceCid.Length + Token.Length + QUIC_RETRY_INTEGRITY_TAG_LENGTH_V1;
            if (Buffer.Length < RequiredBufferLength)
            {
                return 0;
            }

            QUIC_RETRY_PACKET_V1 Header = new QUIC_RETRY_PACKET_V1();
            Header.WriteFrom(Buffer);
                
            byte RandomBits = CxPlatRandom.RandomByte();
            Header.IsLongHeader = 1;
            Header.FixedBit = 1;
            Header.Type = Version == QUIC_VERSION_2 ? (byte)QUIC_LONG_HEADER_TYPE_V2.QUIC_RETRY_V2 : (byte)QUIC_LONG_HEADER_TYPE_V1.QUIC_RETRY_V1;
            Header.UNUSED = RandomBits;
            Header.Version = Version;
            Header.DestCidLength = (byte)DestCid.Length;

            QUIC_SSBuffer HeaderBuffer = Header.DestCid;
            if (DestCid.Length != 0)
            {
                DestCid.GetSpan().CopyTo(HeaderBuffer.GetSpan());
                HeaderBuffer = HeaderBuffer.Slice(DestCid.Length);
            }

            HeaderBuffer[0] = (byte)SourceCid.Length;
            HeaderBuffer = HeaderBuffer.Slice(1);

            if (SourceCid.Length != 0)
            {
                SourceCid.GetSpan().CopyTo(HeaderBuffer.GetSpan());
                HeaderBuffer = HeaderBuffer.Slice(SourceCid.Length);
            }

            if (Token.Length != 0)
            {
                Token.GetSpan().CopyTo(HeaderBuffer.GetSpan());
                HeaderBuffer = HeaderBuffer.Slice(Token.Length);
            }

            QUIC_VERSION_INFO VersionInfo = null;
            for (int i = 0; i < QuicSupportedVersionList.Length; ++i)
            {
                if (QuicSupportedVersionList[i].Number == Version)
                {
                    VersionInfo = QuicSupportedVersionList[i];
                    break;
                }
            }
            NetLog.Assert(VersionInfo != null);

            if (QUIC_FAILED(QuicPacketGenerateRetryIntegrity(VersionInfo, OrigDestCid, new QUIC_SSBuffer(Header.ToBytes(), RequiredBufferLength - QUIC_RETRY_INTEGRITY_TAG_LENGTH_V1, QUIC_RETRY_INTEGRITY_TAG_LENGTH_V1),
                    HeaderBuffer)))
            {
                return 0;
            }

            return RequiredBufferLength;
        }

        static int QuicPacketGenerateRetryIntegrity(QUIC_VERSION_INFO Version, QUIC_SSBuffer OrigDestCid, QUIC_SSBuffer Buffer, QUIC_SSBuffer IntegrityField)
        {
            CXPLAT_SECRET Secret = new CXPLAT_SECRET();
            Secret.Hash = CXPLAT_HASH_TYPE.CXPLAT_HASH_SHA256;
            Secret.Aead = CXPLAT_AEAD_TYPE.CXPLAT_AEAD_AES_128_GCM;

            Version.RetryIntegritySecret.Slice(0, QUIC_VERSION_RETRY_INTEGRITY_SECRET_LENGTH).CopyTo(Secret.Secret);

            byte[] RetryPseudoPacket = null;
            QUIC_PACKET_KEY RetryIntegrityKey = null;
            int Status = QuicPacketKeyDerive(QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL, Version.HkdfLabels, Secret, "RetryIntegrity", false, ref RetryIntegrityKey);
            if (QUIC_FAILED(Status))
            {
                goto Exit;
            }

            int RetryPseudoPacketLength = sizeof(byte) + OrigDestCid.Length + Buffer.Length;
            RetryPseudoPacket = new byte[RetryPseudoPacketLength];
            if (RetryPseudoPacket == null)
            {
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                goto Exit;
            }

            QUIC_SSBuffer RetryPseudoPacketCursor = RetryPseudoPacket;
            RetryPseudoPacketCursor[0] = (byte)OrigDestCid.Length;
            RetryPseudoPacketCursor = RetryPseudoPacketCursor.Slice(1);
            OrigDestCid.GetSpan().CopyTo(RetryPseudoPacketCursor.GetSpan());
            RetryPseudoPacketCursor = RetryPseudoPacketCursor.Slice(OrigDestCid.Length);
            Buffer.GetSpan().CopyTo(RetryPseudoPacketCursor.GetSpan());

            //Status = CxPlatEncrypt(
            //        RetryIntegrityKey.PacketKey,
            //        RetryIntegrityKey.Iv,
            //        RetryPseudoPacket,
            //        IntegrityField);

        Exit:
            if (RetryPseudoPacket != null)
            {
                RetryPseudoPacket = null;
            }
            QuicPacketKeyFree(RetryIntegrityKey);
            return Status;
        }

        static ulong QuicPktNumDecompress(ulong ExpectedPacketNumber, ulong CompressedPacketNumber, int CompressedPacketNumberBytes)
        {
            NetLog.Assert(CompressedPacketNumberBytes < 8);
            ulong Mask = 0xFFFFFFFFFFFFFFFF << (8 * CompressedPacketNumberBytes);
            ulong PacketNumberInc = (~Mask) + 1;
            ulong PacketNumber = (Mask & ExpectedPacketNumber) | CompressedPacketNumber;

            if (PacketNumber < ExpectedPacketNumber)
            {
                ulong High = ExpectedPacketNumber - PacketNumber;
                ulong Low = PacketNumberInc - High;
                if (Low < High)
                {
                    PacketNumber += PacketNumberInc;
                }
            }
            else
            {
                ulong Low = PacketNumber - ExpectedPacketNumber;
                ulong High = PacketNumberInc - Low;
                if (High <= Low && PacketNumber >= PacketNumberInc)
                {
                    PacketNumber -= PacketNumberInc;
                }
            }

            return PacketNumber;
        }

        //这里编码 比较特殊，这里只编码了 64位包号的一部分， 所以后面接收的时候，需要解压缩
        static void QuicPktNumEncode(ulong PacketNumber, int PacketNumberLength, QUIC_SSBuffer Buffer)
        {
            for (int i = 0; i < PacketNumberLength; i++)
            {
                Buffer[i] = (byte)(PacketNumber >> ((PacketNumberLength - i - 1) * 8));
            }
        }

        static void QuicPktNumDecode(int PacketNumberLength, QUIC_SSBuffer Buffer, out ulong PacketNumber)
        {
            PacketNumber = 0;
            for (int i = 0; i < PacketNumberLength; i++)
            {
                PacketNumber |= (ulong)Buffer[i] << ((PacketNumberLength - i - 1) * 8);
            }
        }

        static bool QuicPacketValidateShortHeaderV1(object Owner, QUIC_RX_PACKET Packet, bool IgnoreFixedBit)
        {
            NetLog.Assert(Packet.ValidatedHeaderInv);
            NetLog.Assert(Packet.AvailBufferLength >= Packet.HeaderLength);

            if (IgnoreFixedBit == false && !BoolOk(Packet.SH.FixedBit))
            {
                QuicPacketLogDrop(Owner, Packet, "Invalid SH FixedBit bits values");
                return false;
            }

            Packet.PayloadLength = Packet.AvailBufferLength - Packet.HeaderLength;
            Packet.ValidatedHeaderVer = true;
            return true;
        }

        static void QuicPacketDecodeRetryTokenV1(QUIC_RX_PACKET Packet, ref QUIC_SSBuffer Token)
        {
            NetLog.Assert(Packet.ValidatedHeaderInv);
            NetLog.Assert(Packet.ValidatedHeaderVer);
            NetLog.Assert(BoolOk(Packet.Invariant.IsLongHeader));
            NetLog.Assert((Packet.LH.Version != QUIC_VERSION_2 && Packet.LH.Type == (byte)QUIC_LONG_HEADER_TYPE_V1.QUIC_INITIAL_V1) ||
                (Packet.LH.Version == QUIC_VERSION_2 && Packet.LH.Type == (byte)QUIC_LONG_HEADER_TYPE_V2.QUIC_INITIAL_V2));

            int Offset = QUIC_LONG_HEADER_V1.sizeof_Length + Packet.DestCid.Data.Length + sizeof(byte) + Packet.SourceCid.Data.Length;

            int TokenLengthVarInt = 0;
            bool Success = QuicVarIntDecode2(Packet.AvailBuffer, ref TokenLengthVarInt);
            NetLog.Assert(Success);

            NetLog.Assert(Offset + TokenLengthVarInt <= Packet.AvailBufferLength);
            Token = new QUIC_SSBuffer(Packet.AvailBuffer.Buffer, 0, TokenLengthVarInt);
        }

        static int QuicPacketEncodeShortHeaderV1(QUIC_CID DestCid, ulong PacketNumber, int PacketNumberLength, bool SpinBit, bool KeyPhase, bool FixedBit, QUIC_SSBuffer Buffer)
        {
            NetLog.Assert(PacketNumberLength != 0 && PacketNumberLength <= 4);
            int RequiredBufferLength = QUIC_SHORT_HEADER_V1.sizeof_Length + DestCid.Data.Length + PacketNumberLength;
            if (Buffer.Length < RequiredBufferLength)
            {
                return 0;
            }

            QUIC_SHORT_HEADER_V1 Header = new QUIC_SHORT_HEADER_V1();
            Header.IsLongHeader = 0;
            Header.FixedBit = (byte)(FixedBit ? 1 : 0);
            Header.SpinBit = (byte)(SpinBit ? 1 : 0);
            Header.Reserved = 0;
            Header.KeyPhase = (byte)(KeyPhase ? 1 : 0);
            Header.PnLength = (byte)(PacketNumberLength - 1);

            Buffer[0] = Header.GetFirstByte();

            QUIC_SSBuffer HeaderBuffer = Buffer.Slice(1);
            if (!DestCid.Data.IsEmpty)
            {
                DestCid.Data.CopyTo(HeaderBuffer);
                HeaderBuffer += DestCid.Data.Length;
            }
            
            QuicPktNumEncode(PacketNumber, PacketNumberLength, HeaderBuffer);
            return RequiredBufferLength;
        }

        static int QuicPacketEncodeLongHeaderV1(uint Version, byte PacketType, bool FixedBit, QUIC_CID DestCid, QUIC_CID SourceCid, 
            QUIC_SSBuffer Token, uint PacketNumber, QUIC_SSBuffer Buffer, ref int PayloadLengthOffset, ref int PacketNumberLength
            )
        {
            bool IsInitial =
                (Version != QUIC_VERSION_2 && PacketType == (byte)QUIC_LONG_HEADER_TYPE_V1.QUIC_INITIAL_V1) ||
                (Version == QUIC_VERSION_2 && PacketType == (byte)QUIC_LONG_HEADER_TYPE_V2.QUIC_INITIAL_V2);
            int RequiredBufferLength =
               QUIC_LONG_HEADER_V1.sizeof_Length +
                DestCid.Data.Length +
                sizeof(byte) +
                SourceCid.Data.Length +
                sizeof(ushort) +
                sizeof(uint);

            if (IsInitial)
            {
                RequiredBufferLength += QuicVarIntSize(Token.Length) + Token.Length; // TokenLength
            }

            if (Buffer.Length < RequiredBufferLength)
            {
                return 0;
            }

#if DEBUG
            Buffer.GetSpan().Slice(0, RequiredBufferLength).Clear();
#endif

            QUIC_LONG_HEADER_V1 Header = new QUIC_LONG_HEADER_V1();
            Header.IsLongHeader = 1;
            Header.FixedBit = (byte)(FixedBit ? 1 : 0);
            Header.Type = PacketType;
            Header.Reserved = 0;
            Header.PnLength = sizeof(uint) - 1;
            Header.Version = Version;
            Header.DestCidLength = (byte)DestCid.Data.Length;

            Buffer[0] = Header.GetFirstByte();
            EndianBitConverter.SetBytes(Buffer.GetSpan(), 1, Header.Version);
            Buffer[5] = Header.DestCidLength;

            int nOffset = 6;
            QUIC_SSBuffer HeaderBuffer = Buffer.Slice(nOffset);
            if (DestCid.Data.Length != 0)
            {
                DestCid.Data.GetSpan().CopyTo(HeaderBuffer.GetSpan());
                HeaderBuffer += DestCid.Data.Length;
            }

            HeaderBuffer[0] = (byte)SourceCid.Data.Length;
            HeaderBuffer += 1;
            if (!SourceCid.Data.IsEmpty)
            {
                SourceCid.Data.GetSpan().CopyTo(HeaderBuffer.GetSpan());
                HeaderBuffer += SourceCid.Data.Length;
            }
            if (IsInitial)
            {
                HeaderBuffer = QuicVarIntEncode(Token.Length, HeaderBuffer);
                if (!Token.IsEmpty)
                {
                    Token.GetSpan().CopyTo(HeaderBuffer.GetSpan());
                    HeaderBuffer += Token.Length;
                }
            }
            
            PayloadLengthOffset = (HeaderBuffer.Offset - Buffer.Offset);
            HeaderBuffer += sizeof(ushort); // Skip PayloadLength; 这个数据长度包括 PacketNumber的长度
            EndianBitConverter.SetBytes(HeaderBuffer.GetSpan(), 0, PacketNumber); //包Number的长度是固定的4个字节，那么怎么表示long类型呢
            PacketNumberLength = sizeof(uint);
            return RequiredBufferLength;
        }

    }
}
