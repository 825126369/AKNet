using System;

namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_CID
    {
        public bool IsInitial;
        public bool NeedsToSend;
        public byte Acknowledged;
        public bool UsedLocally;
        public bool UsedByPeer;
        public bool Retired;
        public byte HasResetToken;
        public byte IsInLookupTable;
        public byte Length;
        public ulong SequenceNumber;
        public byte[] Data = null;
    }

    internal class QUIC_CID_HASH_ENTRY
    {
        public CXPLAT_HASHTABLE_ENTRY Entry;
        public CXPLAT_SLIST_ENTRY Link;
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
        public const int QUIC_CID_MAX_LENGTH = (QUIC_MAX_CID_SID_LENGTH + QUIC_CID_PID_LENGTH + QUIC_CID_PAYLOAD_LENGTH);

        static QUIC_CID_HASH_ENTRY QuicCidNewSource(QUIC_CONNECTION Connection, byte Length, byte[] Data)
        {
            QUIC_CID_HASH_ENTRY Entry = new QUIC_CID_HASH_ENTRY();
            if (Entry != null)
            {
                Entry.Connection = Connection;
                Entry.CID.Length = Length;
                if (Length != 0)
                {
                    Array.Copy(Data, 0, Entry.CID.Data, 0, Length);
                }
            }
            return Entry;
        }

        static QUIC_CID_LIST_ENTRY QuicCidNewDestination(byte Length, byte[] Data)
        {
            QUIC_CID_LIST_ENTRY Entry = (QUIC_CID_LIST_ENTRY)CXPLAT_ALLOC_NONPAGED(sizeof(QUIC_CID_LIST_ENTRY) + Length, QUIC_POOL_CIDLIST);
            if (Entry != null)
            {
                Entry.CID.Length = Length;
                if (Length != 0)
                {
                    memcpy(Entry.CID.Data, Data, Length);
                }
            }

            return Entry;
        }

        static QUIC_CID_LIST_ENTRY QuicCidNewRandomDestination()
        {
            QUIC_CID_LIST_ENTRY Entry = (QUIC_CID_LIST_ENTRY) CXPLAT_ALLOC_NONPAGED(
                    sizeof(QUIC_CID_LIST_ENTRY) +
                    QUIC_MIN_INITIAL_CONNECTION_ID_LENGTH,
                    QUIC_POOL_CIDLIST);

            if (Entry != null)
            {
                Entry.CID.Length = QUIC_MIN_INITIAL_CONNECTION_ID_LENGTH;
                CxPlatRandom(QUIC_MIN_INITIAL_CONNECTION_ID_LENGTH, Entry.CID.Data);
            }

            return Entry;
        }
    }
}
