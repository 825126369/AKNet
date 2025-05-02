using AKNet.Common;
using AKNet.Udp5Quic.Common;
using System;
using System.Net.Sockets;
using System.Threading;

namespace AKNet.Udp5Quic.Common
{
    //QUIC 版本协商数据包
    internal class QUIC_VERSION_NEGOTIATION_PACKET
    {
        public byte Unused;
        public bool IsLongHeader;
        public uint Version;
        public byte DestCidLength;
        public byte[] DestCid = new byte[0];

        public void WriteFrom(byte[] buffer)
        {
            Unused = buffer[0];
            IsLongHeader = buffer[1] != 0;
            Version = EndianBitConverter.ToUInt32(buffer, 2);
            DestCidLength = buffer[0];
        }

        public void WriteTo(byte[] buffer)
        {

        }
    }

    internal class QUIC_LONG_HEADER_V1
    {
        public const int sizeof_Length = 8;

        public byte PnLength;
        public byte Reserved;    // Must be 0.
        public byte Type;    // QUIC_LONG_HEADER_TYPE_V1 or _V2
        public bool FixedBit;    // Must be 1, unless grease_quic_bit tp has been sent.
        public bool IsLongHeader;
        public uint Version;
        public QUIC_BUFFER DestCid = new QUIC_BUFFER(0);

        public void WriteFrom(ReadOnlySpan<byte> buffer)
        {

        }

        public void WriteTo(Span<byte> buffer)
        {

        }
    }

    //短头部主要用于在[连接建立][之后]传输数据。
    internal class QUIC_SHORT_HEADER_V1
    {
        public byte PnLength; //2位，表示数据包编号（Packet Number）的长度，单位为字节。
        public bool KeyPhase; //1位，用于标识当前使用的密钥阶段，在 QUIC 中，密钥阶段用于区分不同的加密密钥。当密钥更新时，该位会切换
        public byte Reserved; //2位, 一定是0
        public bool SpinBit; //1位，用于测量往返时间（RTT）。客户端和服务器会交替翻转该位，以帮助检测网络延迟
        public bool FixedBit;   // 固定位（1位，必须为1, 用于标识这是一个有效的 QUIC 数据包
        public bool IsLongHeader;// 是否为长头部（1位，短头部为0）


        public byte[] DestCid = new byte[0]; // 目标连接ID，
        // uint8_t PacketNumber[PnLength]; // 数据包编号（长度由PnLength决定）
        // uint8_t Payload[0];             // 数据包有效载荷

        public void WriteFrom(byte[] buffer)
        {

        }

        public void WriteTo(byte[] buffer)
        {

        }
    }

    internal class QUIC_RETRY_PACKET_V1
    {
        public byte UNUSED;
        public byte Type;
        public byte FixedBit;
        public bool IsLongHeader;
        public uint Version;

        public int DestCidLength;
        public byte[] DestCid = new byte[byte.MaxValue];

        public void WriteFrom(QUIC_SSBuffer buffer)
        {

        }

        public byte[] ToBytes()
        {
            return null;
        }

        public void WriteTo(QUIC_SSBuffer buffer)
        {

        }
    }

    internal class QUIC_HEADER_INVARIANT
    {
        public class LONG_HDR_Class
        {
            public byte VARIANT;
            public byte IsLongHeader;
            public uint Version;
            public byte DestCidLength;
            public readonly byte[] DestCid = new byte[byte.MaxValue];
        }

        public class SHORT_HDR_Class
        {
            public byte VARIANT;
            public byte IsLongHeader;
            public readonly byte[] DestCid = new byte[byte.MaxValue];
        }

        public byte VARIANT;
        public bool IsLongHeader;
        public uint Version;
        public LONG_HDR_Class LONG_HDR;
        public SHORT_HDR_Class SHORT_HDR;
    }

    internal class QUIC_VERSION_INFO
    {
        public uint Number;
        public byte[] Salt = new byte[MSQuicFunc.CXPLAT_VERSION_SALT_LENGTH];
        public byte[] RetryIntegritySecret = new byte[MSQuicFunc.QUIC_VERSION_RETRY_INTEGRITY_SECRET_LENGTH];
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
        public const int MIN_INV_LONG_HDR_LENGTH = sizeof_QUIC_HEADER_INVARIANT + sizeof(byte);
        public const int MIN_INV_SHORT_HDR_LENGTH = sizeof(byte);
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
             },
            new QUIC_VERSION_INFO()
            {
                Number = QUIC_VERSION_DRAFT_29,
                Salt = new byte[]{ 0xaf, 0xbf, 0xec, 0x28, 0x99, 0x93, 0xd2, 0x4c, 0x9e, 0x97, 0x86, 0xf1, 0x9c, 0x61, 0x11, 0xe0, 0x43, 0x90, 0xa8, 0x99 },
                RetryIntegritySecret = new byte[]{ 0x8b, 0x0d, 0x37, 0xeb, 0x85, 0x35, 0x02, 0x2e, 0xbc, 0x8d, 0x76, 0xa2, 0x07, 0xd8, 0x0d, 0xf2,0x26, 0x46, 0xec, 0x06, 0xdc, 0x80, 0x96, 0x42, 0xc3, 0x0a, 0x8b, 0xaa, 0x2b, 0xaa, 0xff, 0x4c },
                HkdfLabels = new QUIC_HKDF_LABELS()
                {
                    KeyLabel = "quic key",
                    IvLabel = "quic iv",
                    HpLabel = "quic hp",
                    KuLabel = "quic ku"
                }
            },
            new QUIC_VERSION_INFO()
            {
               Number = QUIC_VERSION_MS_1,
              Salt = new byte[]{ 0xaf, 0xbf, 0xec, 0x28, 0x99, 0x93, 0xd2, 0x4c, 0x9e, 0x97, 0x86, 0xf1, 0x9c, 0x61, 0x11, 0xe0, 0x43, 0x90, 0xa8, 0x99 },
              RetryIntegritySecret = new byte[]{ 0x8b, 0x0d, 0x37, 0xeb, 0x85, 0x35, 0x02, 0x2e, 0xbc, 0x8d, 0x76, 0xa2, 0x07, 0xd8, 0x0d, 0xf2, 0x26, 0x46, 0xec, 0x06, 0xdc, 0x80, 0x96, 0x42, 0xc3, 0x0a, 0x8b, 0xaa, 0x2b, 0xaa, 0xff, 0x4c },
              HkdfLabels = new QUIC_HKDF_LABELS()
              {
                  KeyLabel = "quic key",
                  IvLabel = "quic iv",
                  HpLabel = "quic hp",
                  KuLabel = "quic ku"
              }
            }
        };

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

        static int QuicMinPacketLengths(bool IsLongHeader)
        {
            if (IsLongHeader)
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
            byte[] DestCid, SourceCid;

            if (Packet.AvailBuffer.Length == 0 || Packet.AvailBuffer.Length < QuicMinPacketLengths(Packet.Invariant.IsLongHeader))
            {
                QuicPacketLogDrop(Owner, Packet, "Too small for Packet->Invariant");
                return false;
            }

            if (Packet.Invariant.IsLongHeader)
            {
                Packet.IsShortHeader = false;
                DestCidLen = Packet.Invariant.LONG_HDR.DestCidLength;
                if (Packet.AvailBuffer.Length < MIN_INV_LONG_HDR_LENGTH + DestCidLen)
                {
                    QuicPacketLogDrop(Owner, Packet, "LH no room for DestCid");
                    return false;
                }

                DestCid = Packet.Invariant.LONG_HDR.DestCid;
                SourceCidLen = DestCid.AsSpan().Slice(DestCidLen)[0];
                Packet.HeaderLength = MIN_INV_LONG_HDR_LENGTH + DestCidLen + SourceCidLen;
                if (Packet.AvailBuffer.Length < Packet.HeaderLength)
                {
                    QuicPacketLogDrop(Owner, Packet, "LH no room for SourceCid");
                    return false;
                }
                SourceCid = DestCid.AsSpan().Slice(sizeof(byte) + DestCidLen).ToArray();
            }
            else
            {

                Packet.IsShortHeader = true;
                DestCidLen = IsBindingShared ? MsQuicLib.CidTotalLength : 0;
                SourceCidLen = 0;

                Packet.HeaderLength = sizeof(byte) + DestCidLen;
                if (Packet.AvailBuffer.Length < Packet.HeaderLength)
                {
                    QuicPacketLogDrop(Owner, Packet, "SH no room for DestCid");
                    return false;
                }

                DestCid = Packet.Invariant.SHORT_HDR.DestCid;
                SourceCid = null;
            }

            if (Packet.DestCid != null)
            {
                if (!orBufferEqual(Packet.DestCid, DestCid))
                {
                    QuicPacketLogDrop(Owner, Packet, "DestCid don't match");
                    return false;
                }

                if (!Packet.IsShortHeader)
                {
                    NetLog.Assert(Packet.SourceCid != null);
                    if (!orBufferEqual(Packet.SourceCid, SourceCid))
                    {
                        QuicPacketLogDrop(Owner, Packet, "SourceCid don't match");
                        return false;
                    }
                }
            }
            else
            {
                Packet.DestCid.Length = DestCidLen;
                Packet.SourceCid.Length = SourceCidLen;
                Packet.DestCid.Buffer = DestCid;
                Packet.SourceCid.Buffer = SourceCid;
            }

            Packet.ValidatedHeaderInv = true;
            return true;
        }

        static bool QuicPacketIsHandshake(QUIC_HEADER_INVARIANT Packet)
        {
            if (!Packet.IsLongHeader)
            {
                return false;
            }

            //switch (Packet.LONG_HDR.Version)
            //{
            //    case QUIC_VERSION_1:
            //    case QUIC_VERSION_DRAFT_29:
            //    case QUIC_VERSION_MS_1:
            //        return ((QUIC_LONG_HEADER_V1)Packet).Type != QUIC_LONG_HEADER_TYPE_V1.QUIC_0_RTT_PROTECTED_V1;
            //    case QUIC_VERSION_2:
            //        return ((QUIC_LONG_HEADER_V1)Packet).Type != QUIC_LONG_HEADER_TYPE_V2.QUIC_0_RTT_PROTECTED_V2;
            //    default:
            //        return true;
            //}
            return true;
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
            QuicPerfCounterIncrement(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_PKTS_DROPPED);
        }

        static uint QuicPacketHash(QUIC_ADDR RemoteAddress, QUIC_SSBuffer RemoteCid)
        {
            uint Key = 0; 
            //int Offset = 0;
            //CxPlatToeplitzHashComputeAddr(MsQuicLib.ToeplitzHash, RemoteAddress, ref Key, ref Offset);
            //if (RemoteCid.Length != 0)
            //{
            //    Key ^= CxPlatToeplitzHashCompute(MsQuicLib.ToeplitzHash, RemoteCid, Math.Min(RemoteCid.Length, QUIC_MAX_CONNECTION_ID_LENGTH_V1), Offset);
            //}
            return Key;
        }

        static bool QuicPacketValidateLongHeaderV1(object Owner, bool IsServer, QUIC_RX_PACKET Packet, ref QUIC_SSBuffer Token, bool IgnoreFixedBit)
        {
            NetLog.Assert(Packet.ValidatedHeaderInv);
            NetLog.Assert(Packet.AvailBuffer.Length >= Packet.HeaderLength);
            NetLog.Assert((Packet.LH.Version != QUIC_VERSION_2 && Packet.LH.Type != (byte)QUIC_LONG_HEADER_TYPE_V1.QUIC_RETRY_V1) ||
            (Packet.LH.Version == QUIC_VERSION_2 && Packet.LH.Type != (byte)QUIC_LONG_HEADER_TYPE_V2.QUIC_RETRY_V2));

            if (Packet.DestCid.Length > QUIC_MAX_CONNECTION_ID_LENGTH_V1 || Packet.SourceCid.Length > QUIC_MAX_CONNECTION_ID_LENGTH_V1)
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

            if (IgnoreFixedBit == false && !Packet.LH.FixedBit)
            {
                QuicPacketLogDrop(Owner, Packet, "Invalid LH FixedBit bits values");
                return false;
            }

            int Offset = Packet.HeaderLength;

            if ((Packet.LH.Version != QUIC_VERSION_2 && Packet.LH.Type == (byte)QUIC_LONG_HEADER_TYPE_V1.QUIC_INITIAL_V1) ||
                (Packet.LH.Version == QUIC_VERSION_2 && Packet.LH.Type == (byte)QUIC_LONG_HEADER_TYPE_V2.QUIC_INITIAL_V2))
            {

                QUIC_SSBuffer AvailBuffer = new QUIC_SSBuffer(Packet.AvailBuffer.Buffer, Offset, Packet.AvailBuffer.Length);
                if (IsServer && Packet.AvailBuffer.Length < QUIC_MIN_INITIAL_PACKET_LENGTH)
                {
                    QuicPacketLogDropWithValue(Owner, Packet, "Client Long header Initial packet too short", Packet.AvailBuffer.Length);
                    return false;
                }

                ulong TokenLengthVarInt = 0;
                if (!QuicVarIntDecode(ref AvailBuffer, ref TokenLengthVarInt))
                {
                    QuicPacketLogDrop(Owner, Packet, "Long header has invalid token length");
                    return false;
                }

                if (Packet.AvailBuffer.Length < (int)(Offset + (int)TokenLengthVarInt))
                {
                    QuicPacketLogDropWithValue(Owner, Packet, "Long header has token length larger than buffer length", (int)TokenLengthVarInt);
                    return false;
                }

                Token = AvailBuffer.Slice(Offset);
                Token.Length = (ushort)TokenLengthVarInt;
                Offset += (ushort)TokenLengthVarInt;
            }
            else
            {
                Token = QUIC_SSBuffer.Empty;
                Token.Length = 0;
            }

            ulong LengthVarInt = 0;

            QUIC_SSBuffer mSpan = Packet.AvailBuffer;
            if (!QuicVarIntDecode(ref mSpan, ref LengthVarInt))
            {
                QuicPacketLogDrop(Owner, Packet, "Long header has invalid payload length");
                return false;
            }

            if (Packet.AvailBuffer.Length < Offset + (int)LengthVarInt)
            {
                QuicPacketLogDropWithValue(Owner, Packet, "Long header has length larger than buffer length", (int)LengthVarInt);
                return false;
            }

            if (Packet.AvailBuffer.Length < Offset + sizeof(uint))
            {
                QuicPacketLogDropWithValue(Owner, Packet, "Long Header doesn't have enough room for packet number", Packet.AvailBuffer.Length);
                return false;
            }

            Packet.HeaderLength = Offset;
            Packet.PayloadLength = (int)LengthVarInt;
            Packet.AvailBuffer.Length = Packet.HeaderLength + Packet.PayloadLength;
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
            if (!QuicRetryTokenDecrypt(Packet, TokenBuffer, ref Token))
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
            QuicPerfCounterIncrement(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_PKTS_DROPPED);
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
            Header.IsLongHeader = true;
            Header.FixedBit = 1;
            Header.Type = Version == QUIC_VERSION_2 ? (byte)QUIC_LONG_HEADER_TYPE_V2.QUIC_RETRY_V2 : (byte)QUIC_LONG_HEADER_TYPE_V1.QUIC_RETRY_V1;
            Header.UNUSED = RandomBits;
            Header.Version = Version;
            Header.DestCidLength = DestCid.Length;

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

        static ulong QuicPacketGenerateRetryIntegrity(QUIC_VERSION_INFO Version, QUIC_SSBuffer OrigDestCid, QUIC_SSBuffer Buffer, QUIC_SSBuffer IntegrityField)
        {
            CXPLAT_SECRET Secret = new CXPLAT_SECRET();
            Secret.Hash = CXPLAT_HASH_TYPE.CXPLAT_HASH_SHA256;
            Secret.Aead = CXPLAT_AEAD_TYPE.CXPLAT_AEAD_AES_128_GCM;

            Array.Copy(Version.RetryIntegritySecret, Secret.Secret, QUIC_VERSION_RETRY_INTEGRITY_SECRET_LENGTH);

            byte[] RetryPseudoPacket = null;
            QUIC_PACKET_KEY RetryIntegrityKey = null;
            ulong Status = QuicPacketKeyDerive(QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL, Version.HkdfLabels, Secret, "RetryIntegrity", false, ref RetryIntegrityKey);
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

        static void QuicPktNumDecode(int PacketNumberLength, QUIC_SSBuffer Buffer, ulong PacketNumber)
        {
            PacketNumber = 0;
            for (int i = 0; i < PacketNumberLength; i++)
            {
                PacketNumber |= Buffer[PacketNumberLength - i - 1];
            }
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

        static int QuicPacketEncodeShortHeaderV1(QUIC_CID DestCid, ulong PacketNumber, int PacketNumberLength, bool SpinBit, bool KeyPhase, bool FixedBit, QUIC_SSBuffer Buffer)
        {
            NetLog.Assert(PacketNumberLength != 0 && PacketNumberLength <= 4);
            int RequiredBufferLength = sizeof_QUIC_SHORT_HEADER_V1 + DestCid.Data.Length + PacketNumberLength;
            if (Buffer.Length < RequiredBufferLength)
            {
                return 0;
            }

            QUIC_SHORT_HEADER_V1 Header = new QUIC_SHORT_HEADER_V1();
            Header.WriteFrom(Buffer);

            Header.IsLongHeader = false;
            Header.FixedBit = FixedBit;
            Header.SpinBit = SpinBit;
            Header.Reserved = 0;
            Header.KeyPhase = KeyPhase;
            Header.PnLength = (byte)(PacketNumberLength - 1);

            QUIC_SSBuffer HeaderBuffer = Header.DestCid;
            if (DestCid.Data.Length != 0)
            {
                DestCid.Data.GetSpan().CopyTo(HeaderBuffer.GetSpan());
                HeaderBuffer += DestCid.Data.Length;
            }

            QuicPktNumEncode(PacketNumber, PacketNumberLength, HeaderBuffer);
            return RequiredBufferLength;
        }

        static void QuicPktNumEncode(ulong PacketNumber, int PacketNumberLength, QUIC_SSBuffer Buffer)
        {
            for (int i = 0; i < PacketNumberLength; i++)
            {
                Buffer[PacketNumberLength - i - 1] = (byte)(PacketNumber >> (56 - i * 8));
            }
        }

        static bool QuicPacketValidateShortHeaderV1(object Owner, QUIC_RX_PACKET Packet, bool IgnoreFixedBit)
        {
            NetLog.Assert(Packet.ValidatedHeaderInv);
            NetLog.Assert(Packet.AvailBuffer.Length >= Packet.HeaderLength);

            if (IgnoreFixedBit == false && !Packet.SH.FixedBit)
            {
                QuicPacketLogDrop(Owner, Packet, "Invalid SH FixedBit bits values");
                return false;
            }

            Packet.PayloadLength = Packet.AvailBuffer.Length - Packet.HeaderLength;
            Packet.ValidatedHeaderVer = true;
            return true;
        }

        static void QuicPacketDecodeRetryTokenV1(QUIC_RX_PACKET Packet, ref QUIC_SSBuffer Token)
        {
            NetLog.Assert(Packet.ValidatedHeaderInv);
            NetLog.Assert(Packet.ValidatedHeaderVer);
            NetLog.Assert(Packet.Invariant.IsLongHeader);
            NetLog.Assert((Packet.LH.Version != QUIC_VERSION_2 && Packet.LH.Type == (byte)QUIC_LONG_HEADER_TYPE_V1.QUIC_INITIAL_V1) ||
                (Packet.LH.Version == QUIC_VERSION_2 && Packet.LH.Type == (byte)QUIC_LONG_HEADER_TYPE_V2.QUIC_INITIAL_V2));

            int Offset = sizeof_QUIC_LONG_HEADER_V1 + Packet.DestCid.Length + sizeof(byte) + Packet.SourceCid.Length;

            int TokenLengthVarInt = 0;
            bool Success = QuicVarIntDecode2(Packet.AvailBuffer, ref TokenLengthVarInt);
            NetLog.Assert(Success);

            NetLog.Assert(Offset + TokenLengthVarInt <= Packet.AvailBuffer.Length);
            Token = new QUIC_SSBuffer(Packet.AvailBuffer.Buffer, 0, TokenLengthVarInt);
        }

        static int QuicPacketEncodeLongHeaderV1(uint Version, byte PacketType, bool FixedBit, QUIC_CID DestCid, QUIC_CID SourceCid, QUIC_SSBuffer Token, uint PacketNumber, QUIC_SSBuffer Buffer,
            ref int PayloadLengthOffset, ref int PacketNumberLength
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

            QUIC_LONG_HEADER_V1 Header = new QUIC_LONG_HEADER_V1();
            Header.WriteFrom(Buffer.GetSpan());

            Header.IsLongHeader = true;
            Header.FixedBit = FixedBit;
            Header.Type = PacketType;
            Header.Reserved = 0;
            Header.PnLength = sizeof(uint) - 1;
            Header.Version = Version;
            Header.DestCid.Length = (byte)DestCid.Data.Length;

            QUIC_SSBuffer HeaderBuffer = Header.DestCid;
            if (DestCid.Data.Length != 0)
            {
                DestCid.Data.GetSpan().CopyTo(HeaderBuffer.GetSpan());
                HeaderBuffer += DestCid.Data.Length;
            }

            HeaderBuffer[0] = (byte)SourceCid.Data.Length;
            HeaderBuffer += 1;
            if (SourceCid.Data.Length != 0)
            {
                SourceCid.Data.GetSpan().CopyTo(HeaderBuffer.GetSpan());
                HeaderBuffer += SourceCid.Data.Length;
            }
            if (IsInitial)
            {
                HeaderBuffer = QuicVarIntEncode(Token.Length, HeaderBuffer);
                if (Token.Length != 0)
                {
                    Token.GetSpan().CopyTo(HeaderBuffer.GetSpan());
                    HeaderBuffer += Token.Length;
                }
            }

            PayloadLengthOffset = (HeaderBuffer.Offset - Buffer.Offset);
            HeaderBuffer += sizeof(ushort); // Skip PayloadLength;
            EndianBitConverter.SetBytes(HeaderBuffer.GetSpan(), 0, PacketNumber);
            PacketNumberLength = sizeof(uint);

            return RequiredBufferLength;
        }

    }
}
