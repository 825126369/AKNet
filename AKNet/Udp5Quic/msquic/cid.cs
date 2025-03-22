namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_CID
    {
        public byte IsInitial;
        public byte NeedsToSend;
        public byte Acknowledged;
        public byte UsedLocally;
        public byte UsedByPeer;
        public byte Retired;
        public byte HasResetToken;
        public byte IsInLookupTable;
        public byte Length;
        public ulong SequenceNumber;
        public byte[] Data = new byte[0];
    }

    internal class QUIC_CID_HASH_ENTRY
    {
        public CXPLAT_HASHTABLE_ENTRY Entry;
        public quic_platform_cxplat_slist_entry Link;
        public QUIC_CONNECTION Connection;
        public QUIC_CID CID;
    }

    internal class QUIC_CID_LIST_ENTRY
    {
        public CXPLAT_LIST_ENTRY Link;
        public readonly byte[] ResetToken = new byte[MSQuicFunc.QUIC_STATELESS_RESET_TOKEN_LENGTH];
        public QUIC_PATH AssignedPath;
        public QUIC_CID CID;
    }

    internal static partial class MSQuicFunc
    {
        public const int QUIC_MAX_CID_SID_LENGTH = 5;
        public const int QUIC_CID_PID_LENGTH = 2;
        public const int QUIC_CID_PAYLOAD_LENGTH = 7;
        public const int QUIC_CID_MIN_RANDOM_BYTES = 4;
        public const int QUIC_MAX_CIBIR_LENGTH = 6;
    }
}
