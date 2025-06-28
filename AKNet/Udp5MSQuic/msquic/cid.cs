using System;
using System.Collections.Generic;
using System.Text;

namespace AKNet.Udp5MSQuic.Common
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
        
        public readonly byte[] ResetToken = new byte[MSQuicFunc.QUIC_STATELESS_RESET_TOKEN_LENGTH];
        public readonly CXPLAT_LIST_ENTRY Link;
        public QUIC_CONNECTION Connection;
        public QUIC_ADDR RemoteAddress;
        public int Hash;

        private QUIC_BUFFER m_Data;
        public QUIC_BUFFER Data
        {
            get
            {
                if (m_Data == null)
                {
                    m_Data = new QUIC_BUFFER();
                }
                return m_Data;
            }
        }

        public QUIC_CID()
        {
            Link = new CXPLAT_LIST_ENTRY<QUIC_CID>(this);
        }

        public QUIC_CID(int Length)
        {
            m_Data = new QUIC_BUFFER(Length);
            Link = new CXPLAT_LIST_ENTRY<QUIC_CID>(this);
        }

        public QUIC_CID(QUIC_SSBuffer Buffer, QUIC_ADDR Address = null)
        {
            this.Data.SetData(Buffer);
            this.RemoteAddress = Address;
            Link = new CXPLAT_LIST_ENTRY<QUIC_CID>(this);
        }

        public Span<byte> GetSpan()
        {
            return Data.GetSpan();
        }

        public override string ToString()
        {
            StringBuilder mBuilder = new StringBuilder();
            for (int i = 0; i < Data.Length; i++)
            {
                mBuilder.Append(Data.Buffer[i]);
                mBuilder.Append("-");
            }
            return mBuilder.ToString();
        }

        public void Reset()
        {
            Hash = 0;
            IsInitial = default;
            NeedsToSend = default;
            Acknowledged = default;
            UsedLocally = default;
            UsedByPeer = default;
            Retired = default;
            HasResetToken = default;
            IsInLookupTable = default;
            SequenceNumber = default;

            Connection = null;
            RemoteAddress = null;
            m_Data.Reset();
        }
    }

    internal class QUIC_CID_EqualityComparer : IEqualityComparer<QUIC_CID>
    {
        public bool Equals(QUIC_CID x, QUIC_CID y)
        {
            if (x.RemoteAddress != null && y.RemoteAddress != null)
            {
                return MSQuicFunc.orBufferEqual(x.GetSpan(), y.GetSpan()) && MSQuicFunc.QuicAddrCompare(x.RemoteAddress, y.RemoteAddress);
            }
            else if(x.RemoteAddress != null)
            {
                return false;
            }
            else if (y.RemoteAddress != null)
            {
                return false;
            }
            else
            {
                return MSQuicFunc.orBufferEqual(x.GetSpan(), y.GetSpan());
            }
        }

        public int GetHashCode(QUIC_CID obj)
        {
            if (obj.Hash == 0)
            {
                if (obj.RemoteAddress != null)
                {
                    obj.Hash = GetHash(obj.RemoteAddress, obj.Data);
                }
                else
                {
                    obj.Hash = GetHash(obj.Data);
                }
            }
            return obj.Hash;
        }

        public static int GetHash(QUIC_ADDR RemoteAddress, QUIC_SSBuffer CidData)
        {
            return MSQuicFunc.QuicPacketHash(RemoteAddress, CidData);
        }

        public static int GetHash(QUIC_SSBuffer CidData)
        {
            return MSQuicFunc.CxPlatHashSimple(CidData);
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

        static QUIC_CID QuicCidNewSource(QUIC_CONNECTION Connection, QUIC_SSBuffer Data)
        {
            QUIC_CID Entry = new QUIC_CID(Data.Length);
            if (Entry != null)
            {
                Entry.Connection = Connection;
                Entry.Data.Length = Data.Length;
                if (Data.Length != 0)
                {
                    Data.CopyTo(Entry.Data);
                }
            }
            return Entry;
        }

        static QUIC_CID QuicCidNewDestination(QUIC_SSBuffer Data)
        {
            QUIC_CID Entry = new QUIC_CID(Data.Length);
            if (Entry != null)
            {
                Entry.Data.Length = Data.Length;
                if (Data.Length != 0)
                {
                    Data.CopyTo(Entry.Data);
                }
            }
            return Entry;
        }

        static QUIC_CID QuicCidNewRandomDestination()
        {
            QUIC_CID Entry = new QUIC_CID(QUIC_MIN_INITIAL_CONNECTION_ID_LENGTH);
            if (Entry != null)
            {
                Entry.Data.Length = QUIC_MIN_INITIAL_CONNECTION_ID_LENGTH;
                CxPlatRandom.Random(Entry.Data);
            }
            return Entry;
        }

        static QUIC_CID QuicCidNewNullSource(QUIC_CONNECTION Connection)
        {
            QUIC_CID Entry = new QUIC_CID();
            if (Entry != null)
            {
                Entry.Connection = Connection;
            }
            return Entry;
        }

    }
}
