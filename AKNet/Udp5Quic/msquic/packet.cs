using AKNet.Common;
using System;
using static AKNet.Udp5Quic.Common.QUIC_BINDING;
using System.Data;
using System.Threading;
using System.Net;

namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_VERSION_NEGOTIATION_PACKET
    {
        public byte Unused;
        public byte IsLongHeader;
        public uint Version;
        public byte DestCidLength;
        public byte[] DestCid = new byte[0];
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
    }

    internal class QUIC_SHORT_HEADER_V1
    {
        public byte PnLength;
        public byte KeyPhase;
        public byte Reserved;
        public byte SpinBit;
        public byte FixedBit;   
        public byte IsLongHeader;
        public byte[] DestCid = new byte[0];    
    }

    internal class QUIC_RETRY_PACKET_V1
    {
        public byte UNUSED;
        public byte Type;
        public byte FixedBit;
        public byte IsLongHeader;
        public uint Version;
        public byte DestCidLength;
        public byte[] DestCid = new byte[0];
    }

    internal class QUIC_HEADER_INVARIANT
    {
        public class LONG_HDR_Class
        {
            public byte VARIANT;
            public byte IsLongHeader;
            public uint Version;
            public byte DestCidLength;
            public byte[] DestCid = new byte[0];
        }

        public class SHORT_HDR_Class
        {
            public byte VARIANT;
            public byte IsLongHeader;
            public byte[] DestCid = new byte[0];
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

        public const int MIN_INV_LONG_HDR_LENGTH = (sizeof(QUIC_HEADER_INVARIANT) + sizeof(byte));
        public const int MIN_INV_SHORT_HDR_LENGTH = sizeof(byte);
        static int QuicMinPacketLengths(bool IsLongHeader)
        {
            if(IsLongHeader)
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
                SourceCidLen = DestCid + DestCidLen;
                Packet.HeaderLength = MIN_INV_LONG_HDR_LENGTH + DestCidLen + SourceCidLen;
                if (Packet.AvailBufferLength < Packet.HeaderLength)
                {
                    QuicPacketLogDrop(Owner, Packet, "LH no room for SourceCid");
                    return false;
                }
                SourceCid = DestCid + sizeof(byte) + DestCidLen;
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
                if (Packet.DestCidLen != DestCidLen || memcmp(Packet.DestCid, DestCid, DestCidLen) != 0)
                {
                    QuicPacketLogDrop(Owner, Packet, "DestCid don't match");
                    return false;
                }

                if (!Packet.IsShortHeader)
                {
                    NetLog.Assert(Packet.SourceCid != null);
                    if (Packet.SourceCidLen != SourceCidLen || memcmp(Packet.SourceCid, SourceCid, SourceCidLen) != 0)
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

        static uint QuicPacketHash(IPAddress RemoteAddress, int RemoteCidLength, byte[] RemoteCid)
        {
            uint Key = 0, Offset;
            CxPlatToeplitzHashComputeAddr(&MsQuicLib.ToeplitzHash, RemoteAddress, &Key, &Offset);
            if (RemoteCidLength != 0)
            {
                Key ^= CxPlatToeplitzHashCompute(
                        &MsQuicLib.ToeplitzHash,
                        RemoteCid,
                        CXPLAT_MIN(RemoteCidLength, QUIC_MAX_CONNECTION_ID_LENGTH_V1),
                        Offset);
            }
            return Key;
        }

    }
}
