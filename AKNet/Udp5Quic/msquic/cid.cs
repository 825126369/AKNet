namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_CID
    {
        public bool IsInitial;
        public bool NeedsToSend;
        public bool Acknowledged;
        public bool UsedLocally;
        public bool UsedByPeer;
        public bool Retired;
        public bool HasResetToken;
        public bool IsInLookupTable;
        public ulong SequenceNumber;
        public readonly QUIC_BUFFER Data = new QUIC_BUFFER();
    }

    internal class QUIC_CID_HASH_ENTRY
    {
        public CXPLAT_SLIST_ENTRY Link;
        public QUIC_CONNECTION Connection;
        public QUIC_CID CID;
    }

    internal class QUIC_CID_LIST_ENTRY
    {
        public readonly CXPLAT_LIST_ENTRY Link;
        public readonly byte[] ResetToken = new byte[MSQuicFunc.QUIC_STATELESS_RESET_TOKEN_LENGTH];
        public readonly QUIC_CID CID = new QUIC_CID();
        public QUIC_CID_LIST_ENTRY()
        {
            Link = new CXPLAT_LIST_ENTRY<QUIC_CID_LIST_ENTRY>(this);
        }
    }

    internal static partial class MSQuicFunc
    {
        public const int QUIC_MAX_CID_SID_LENGTH = 5;
        public const int QUIC_CID_PID_LENGTH = 2;
        public const int QUIC_CID_PAYLOAD_LENGTH = 7;
        public const int QUIC_CID_MIN_RANDOM_BYTES = 4;
        public const int QUIC_MAX_CIBIR_LENGTH = 6;
        public const int QUIC_CID_MAX_LENGTH = QUIC_MAX_CID_SID_LENGTH + QUIC_CID_PID_LENGTH + QUIC_CID_PAYLOAD_LENGTH;
        public const int QUIC_CID_MAX_COLLISION_RETRY = 8;

        static QUIC_CID_HASH_ENTRY QuicCidNewSource(QUIC_CONNECTION Connection, int Length, byte[] Data)
        {
            QUIC_CID_HASH_ENTRY Entry = new QUIC_CID_HASH_ENTRY();
            if (Entry != null)
            {
                Entry.Connection = Connection;
                Entry.CID.Data.Length = Length;
                if (Length != 0)
                {
                    Entry.CID.Data.GetSpan().CopyTo(Data);
                }
            }
            return Entry;
        }

        static QUIC_CID_LIST_ENTRY QuicCidNewDestination(int Length, byte[] Data)
        {
            QUIC_CID_LIST_ENTRY Entry = new QUIC_CID_LIST_ENTRY();
            if (Entry != null)
            {
                Entry.CID.Data.Length = Length;
                if (Length != 0)
                {
                    Entry.CID.Data.GetSpan().CopyTo(Data);
                }
            }

            return Entry;
        }

        static QUIC_CID_LIST_ENTRY QuicCidNewRandomDestination()
        {
            QUIC_CID_LIST_ENTRY Entry = new QUIC_CID_LIST_ENTRY();
            if (Entry != null)
            {
                Entry.CID.Data.Length = QUIC_MIN_INITIAL_CONNECTION_ID_LENGTH;
                CxPlatRandom.Random(Entry.CID.Data.GetSpan().Slice(0, QUIC_MIN_INITIAL_CONNECTION_ID_LENGTH));
            }
            return Entry;
        }

        static QUIC_CID_HASH_ENTRY QuicCidNewNullSource(QUIC_CONNECTION Connection)
        {
            QUIC_CID_HASH_ENTRY Entry = new QUIC_CID_HASH_ENTRY();
            if (Entry != null)
            {
                Entry.Connection = Connection;
                Entry.CID.Data.GetSpan().Clear();
            }
            return Entry;
        }

    }
}
