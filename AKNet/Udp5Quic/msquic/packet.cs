using AKNet.Common;
using System;
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
        public byte PnLength;
        public byte Reserved;    // Must be 0.
        public byte Type;    // QUIC_LONG_HEADER_TYPE_V1 or _V2
        public byte FixedBit;    // Must be 1, unless grease_quic_bit tp has been sent.
        public byte IsLongHeader;
        public uint Version;
        public byte DestCidLength;
        public byte[] DestCid = new byte[0];

        public void WriteFrom(byte[] buffer)
        {

        }

        public void WriteTo(byte[] buffer)
        {

        }
    }

    internal class QUIC_SHORT_HEADER_V1
    {
        public byte PnLength;
        public bool KeyPhase;
        public byte Reserved;
        public byte SpinBit;
        public byte FixedBit;   
        public bool IsLongHeader;
        public byte[] DestCid = new byte[0];

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

        public void WriteFrom(ReadOnlySpan<byte> buffer)
        {

        }
        public byte[] ToBytes()
        {
            return null
        }

        public void WriteTo(Span<byte> buffer)
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
            public byte[] DestCid = new byte[byte.MaxValue];
        }

        public class SHORT_HDR_Class
        {
            public byte VARIANT;
            public byte IsLongHeader;
            public byte[] DestCid = new byte[byte.MaxValue];
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
        public const int MIN_INV_LONG_HDR_LENGTH = (sizeof(QUIC_HEADER_INVARIANT) + sizeof(byte));
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

        static bool QuicPacketValidateInvariant(QUIC_BINDING Owner, QUIC_RX_PACKET Packet, bool IsBindingShared)
        {
            int DestCidLen, SourceCidLen;
            byte[] DestCid, SourceCid;

            if (Packet.AvailBufferLength == 0 || Packet.AvailBufferLength < QuicMinPacketLengths(Packet.Invariant.IsLongHeader))
            {
                QuicPacketLogDrop(Owner, Packet, "Too small for Packet->Invariant");
                return false;
            }

            if (Packet.Invariant.IsLongHeader)
            {
                Packet.IsShortHeader = false;
                DestCidLen = Packet.Invariant.LONG_HDR.DestCidLength;
                if (Packet.AvailBufferLength < MIN_INV_LONG_HDR_LENGTH + DestCidLen)
                {
                    QuicPacketLogDrop(Owner, Packet, "LH no room for DestCid");
                    return false;
                }

                DestCid = Packet.Invariant.LONG_HDR.DestCid;
                SourceCidLen = DestCid.AsSpan().Slice(DestCidLen)[0];
                Packet.HeaderLength = MIN_INV_LONG_HDR_LENGTH + DestCidLen + SourceCidLen;
                if (Packet.AvailBufferLength < Packet.HeaderLength)
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
                if (Packet.AvailBufferLength < Packet.HeaderLength)
                {
                    QuicPacketLogDrop(Owner, Packet, "SH no room for DestCid");
                    return false;
                }

                DestCid = Packet.Invariant.SHORT_HDR.DestCid;
                SourceCid = null;
            }

            if (Packet.DestCid != null)
            {
                if (Packet.DestCidLen != DestCidLen || !orBufferEqual(Packet.DestCid, DestCid, DestCidLen))
                {
                    QuicPacketLogDrop(Owner, Packet, "DestCid don't match");
                    return false;
                }

                if (!Packet.IsShortHeader)
                {
                    NetLog.Assert(Packet.SourceCid != null);
                    if (Packet.SourceCidLen != SourceCidLen || !orBufferEqual(Packet.SourceCid, SourceCid, SourceCidLen))
                    {
                        QuicPacketLogDrop(Owner, Packet, "SourceCid don't match");
                        return false;
                    }
                }
            }
            else
            {
                Packet.DestCidLen = DestCidLen;
                Packet.SourceCidLen = SourceCidLen;
                Packet.DestCid = DestCid;
                Packet.SourceCid = SourceCid;
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

            switch (Packet.LONG_HDR.Version)
            {
                case QUIC_VERSION_1:
                case QUIC_VERSION_DRAFT_29:
                case QUIC_VERSION_MS_1:
                    return ((QUIC_LONG_HEADER_V1)Packet).Type != QUIC_LONG_HEADER_TYPE_V1.QUIC_0_RTT_PROTECTED_V1;
                case QUIC_VERSION_2:
                    return ((QUIC_LONG_HEADER_V1)Packet).Type != QUIC_LONG_HEADER_TYPE_V2.QUIC_0_RTT_PROTECTED_V2;
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
            QuicPerfCounterIncrement(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_PKTS_DROPPED);
        }

        static uint QuicPacketHash(QUIC_ADDR RemoteAddress, int RemoteCidLength, byte[] RemoteCid)
        {
            uint Key = 0; 
            int Offset = 0;
            CxPlatToeplitzHashComputeAddr(MsQuicLib.ToeplitzHash, RemoteAddress, ref Key, ref Offset);
            if (RemoteCidLength != 0)
            {
                Key ^= CxPlatToeplitzHashCompute(MsQuicLib.ToeplitzHash, RemoteCid, Math.Min(RemoteCidLength, QUIC_MAX_CONNECTION_ID_LENGTH_V1), Offset);
            }
            return Key;
        }

        static bool QuicPacketValidateLongHeaderV1(object Owner, bool IsServer, QUIC_RX_PACKET Packet, bool IgnoreFixedBit, ref byte[] Token, ref int TokenLength)
        {
            NetLog.Assert(Packet.ValidatedHeaderInv);
            NetLog.Assert(Packet.AvailBufferLength >= Packet.HeaderLength);
            NetLog.Assert((Packet.LH.Version != QUIC_VERSION_2 && Packet.LH.Type != (byte)QUIC_LONG_HEADER_TYPE_V1.QUIC_RETRY_V1) ||
            (Packet.LH.Version == QUIC_VERSION_2 && Packet.LH.Type != (byte)QUIC_LONG_HEADER_TYPE_V2.QUIC_RETRY_V2));

            if (Packet.DestCidLen > QUIC_MAX_CONNECTION_ID_LENGTH_V1 || Packet.SourceCidLen > QUIC_MAX_CONNECTION_ID_LENGTH_V1)
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
                QuicPacketLogDrop(Owner, Packet, "Invalid LH FixedBit bits values");
                return false;
            }

            int Offset = Packet.HeaderLength;

            if ((Packet.LH.Version != QUIC_VERSION_2 && Packet.LH.Type == (byte)QUIC_LONG_HEADER_TYPE_V1.QUIC_INITIAL_V1) ||
                (Packet.LH.Version == QUIC_VERSION_2 && Packet.LH.Type == (byte)QUIC_LONG_HEADER_TYPE_V2.QUIC_INITIAL_V2))
            {
                if (IsServer && Packet.AvailBufferLength < QUIC_MIN_INITIAL_PACKET_LENGTH)
                {
                    QuicPacketLogDropWithValue(Owner, Packet, "Client Long header Initial packet too short", Packet.AvailBufferLength);
                    return false;
                }

                ulong TokenLengthVarInt = 0;
                if (!QuicVarIntDecode(Packet.AvailBuffer.AsSpan().Slice(Offset, Packet.AvailBufferLength), ref TokenLengthVarInt))
                {
                    QuicPacketLogDrop(Owner, Packet, "Long header has invalid token length");
                    return false;
                }

                if (Packet.AvailBufferLength < (int)(Offset + (int)TokenLengthVarInt))
                {
                    QuicPacketLogDropWithValue(Owner, Packet, "Long header has token length larger than buffer length", (int)TokenLengthVarInt);
                    return false;
                }

                Token = Packet.AvailBuffer.AsSpan().Slice(Offset).ToArray();
                TokenLength = (ushort)TokenLengthVarInt;
                Offset += (ushort)TokenLengthVarInt;
            }
            else
            {
                Token = null;
                TokenLength = 0;
            }

            ulong LengthVarInt = 0;
            if (!QuicVarIntDecode(Packet.AvailBuffer, ref LengthVarInt))
            {
                QuicPacketLogDrop(Owner, Packet, "Long header has invalid payload length");
                return false;
            }

            if (Packet.AvailBufferLength < Offset + (int)LengthVarInt)
            {
                QuicPacketLogDropWithValue(Owner, Packet, "Long header has length larger than buffer length", (int)LengthVarInt);
                return false;
            }

            if (Packet.AvailBufferLength < Offset + sizeof(uint))
            {
                QuicPacketLogDropWithValue(Owner, Packet, "Long Header doesn't have enough room for packet number", Packet.AvailBufferLength);
                return false;
            }

            Packet.HeaderLength = Offset;
            Packet.PayloadLength = (int)LengthVarInt;
            Packet.AvailBufferLength = Packet.HeaderLength + Packet.PayloadLength;
            Packet.ValidatedHeaderVer = true;
            return true;
        }

        static bool QuicPacketValidateInitialToken(object Owner, QUIC_RX_PACKET Packet, int TokenLength, byte[] TokenBuffer, ref bool DropPacket)
        {
            bool IsNewToken = BoolOk(TokenBuffer[0] & 0x1);
            if (IsNewToken)
            {
                QuicPacketLogDrop(Owner, Packet, "New Token not supported");
                DropPacket = true;
                return false;
            }

            if (TokenLength != QUIC_TOKEN_CONTENTS.sizeof_QUIC_TOKEN_CONTENTS)
            {
                QuicPacketLogDrop(Owner, Packet, "Invalid Token Length");
                DropPacket = true;
                return false;
            }

            QUIC_TOKEN_CONTENTS Token = null;
            if (!QuicRetryTokenDecrypt(Packet, TokenBuffer, Token))
            {
                QuicPacketLogDrop(Owner, Packet, "Retry Token Decryption Failure");
                DropPacket = true;
                return false;
            }

            if (Token.Encrypted.OrigConnIdLength > Token.Encrypted.OrigConnId.Length)
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

        static int QuicPacketEncodeRetryV1(uint Version, ReadOnlySpan<byte> DestCid, ReadOnlySpan<byte> SourceCid, ReadOnlySpan<byte> OrigDestCid, ReadOnlySpan<byte> Token, ReadOnlySpan<byte> Buffer)
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

            Span<byte> HeaderBuffer = Header.DestCid;
            if (DestCid.Length != 0)
            {
                DestCid.CopyTo(HeaderBuffer);
                HeaderBuffer = HeaderBuffer.Slice(DestCid.Length);
            }

            HeaderBuffer[0] = (byte)SourceCid.Length;
            HeaderBuffer = HeaderBuffer.Slice(1);

            if (SourceCid.Length != 0)
            {
                SourceCid.Slice(0, SourceCid.Length).CopyTo(HeaderBuffer);
                HeaderBuffer = HeaderBuffer.Slice(SourceCid.Length);
            }

            if (Token.Length != 0)
            {
                Token.CopyTo(HeaderBuffer);
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

            if (QUIC_FAILED(QuicPacketGenerateRetryIntegrity(VersionInfo, OrigDestCid, Header.ToBytes().AsSpan().Slice(RequiredBufferLength - QUIC_RETRY_INTEGRITY_TAG_LENGTH_V1, QUIC_RETRY_INTEGRITY_TAG_LENGTH_V1),
                    HeaderBuffer)))
            {
                return 0;
            }

            return RequiredBufferLength;
        }

        static ulong QuicPacketGenerateRetryIntegrity(QUIC_VERSION_INFO Version, ReadOnlySpan<byte> OrigDestCid, ReadOnlySpan<byte> Buffer, ReadOnlySpan<byte> IntegrityField)
        {
            CXPLAT_SECRET Secret = new CXPLAT_SECRET();
            Secret.Hash = CXPLAT_HASH_TYPE.CXPLAT_HASH_SHA256;
            Secret.Aead = CXPLAT_AEAD_TYPE.CXPLAT_AEAD_AES_128_GCM;

            Array.Copy(Version.RetryIntegritySecret, Secret.Secret, QUIC_VERSION_RETRY_INTEGRITY_SECRET_LENGTH);

            Byte[] RetryPseudoPacket = null;
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

            Span<byte> RetryPseudoPacketCursor = RetryPseudoPacket;
            RetryPseudoPacketCursor[0] = (byte)OrigDestCid.Length;
            RetryPseudoPacketCursor = RetryPseudoPacketCursor.Slice(1);
            OrigDestCid.Slice(0, OrigDestCid.Length).CopyTo(RetryPseudoPacketCursor);
            RetryPseudoPacketCursor = RetryPseudoPacketCursor.Slice(OrigDestCid.Length);
            Buffer.CopyTo(RetryPseudoPacketCursor);

            Status = CxPlatEncrypt(
                    RetryIntegrityKey.PacketKey,
                    RetryIntegrityKey.Iv,
                    RetryPseudoPacket,
                    IntegrityField);

        Exit:
            if (RetryPseudoPacket != null)
            {
                RetryPseudoPacket = null;
            }
            QuicPacketKeyFree(RetryIntegrityKey);
            return Status;
        }

        static void QuicPktNumDecode(int PacketNumberLength, ReadOnlySpan<byte> Buffer, ulong PacketNumber)
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

    }
}
