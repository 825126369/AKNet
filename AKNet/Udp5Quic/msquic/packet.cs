using System;

namespace AKNet.Udp5Quic.msquic
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
        public class LONG_HDR
        {
            public byte VARIANT : 7;
            public byte IsLongHeader : 1;
            public uint Version;
            public byte DestCidLength;
            public byte DestCid[0];
        }

        public class SHORT_HDR
        {
            public byte VARIANT;
            public byte IsLongHeader;
            public byte[] DestCid = new byte[0];
        }

        public byte VARIANT;
        public bool IsLongHeader;
        public uint Version;
    }
}
