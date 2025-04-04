using System;

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
    }
}
