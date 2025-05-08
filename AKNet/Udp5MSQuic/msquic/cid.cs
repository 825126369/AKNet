using System.Collections.Generic;
using System;
using System.Net;

namespace AKNet.Udp5MSQuic.Common
{
    internal interface QUIC_CID_DIC_KEY
    {
        Span<byte> GetSpan();
        uint GetDicHash();
        QUIC_ADDR GetAddress();
    }

    internal class QUIC_CID: QUIC_CID_DIC_KEY
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
        public uint Hash;
        public readonly QUIC_BUFFER Data = new QUIC_BUFFER(MSQuicFunc.QUIC_MAX_CONNECTION_ID_LENGTH_V1);

        public QUIC_CID()
        {
            Link = new CXPLAT_LIST_ENTRY<QUIC_CID>(this);
        }

        public QUIC_CID(QUIC_SSBuffer Buffer, QUIC_ADDR Address)
        {
            this.Data = Buffer;
            this.RemoteAddress = Address;
        }

        public QUIC_CID(QUIC_SSBuffer Buffer)
        {
            this.Data = Buffer;
            this.RemoteAddress = null;
        }

        public Span<byte> GetSpan()
        {
            return Data.GetSpan();
        }

        public uint GetDicHash()
        {
            if(Hash == 0)
            {
                if(RemoteAddress != null)
                {
                    this.Hash = MSQuicFunc.QuicPacketHash(RemoteAddress, Data);
                }
                else
                {
                    this.Hash = MSQuicFunc.CxPlatHashSimple(Data);
                }
            }
            return Hash;
        }

        public QUIC_ADDR GetAddress()
        {
            return RemoteAddress;
        }
    }

    internal readonly struct QUIC_CID_BUFFER_KEY : QUIC_CID_DIC_KEY
    {
        public readonly int Offset;
        public readonly int Length;
        public readonly byte[] Buffer;
        public readonly uint Hash;
        public readonly QUIC_ADDR RemoteAddress;

        public QUIC_CID_BUFFER_KEY(QUIC_SSBuffer Buffer, QUIC_ADDR Address)
        {
            this.Buffer = Buffer.Buffer;
            this.Offset = Buffer.Offset;
            this.Length = Buffer.Length;
            this.Hash = MSQuicFunc.QuicPacketHash(Address, Buffer);
            this.RemoteAddress = Address;
        }

        public QUIC_CID_BUFFER_KEY(QUIC_SSBuffer Buffer)
        {
            this.Buffer = Buffer.Buffer;
            this.Offset = Buffer.Offset;
            this.Length = Buffer.Length;
            this.Hash = MSQuicFunc.CxPlatHashSimple(Buffer);
            this.RemoteAddress = null;
        }

        public Span<byte> GetSpan()
        {
            return Buffer.AsSpan().Slice(Offset, Length);
        }

        public uint GetDicHash()
        {
            return Hash;
        }

        public QUIC_ADDR GetAddress()
        {
            return RemoteAddress;
        }
    }

    internal class QUIC_CID_DIC_KEY_EqualityComparer : IEqualityComparer<QUIC_CID_DIC_KEY>
    {
        public bool Equals(QUIC_CID_DIC_KEY x, QUIC_CID_DIC_KEY y)
        {
            return MSQuicFunc.orBufferEqual(x.GetSpan(), y.GetSpan()) && x.GetAddress() == y.GetAddress();
        }

        public int GetHashCode(QUIC_CID_DIC_KEY obj)
        {
            return (int)obj.GetDicHash();
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
            QUIC_CID Entry = new QUIC_CID();
            if (Entry != null)
            {
                Entry.Connection = Connection;
                Entry.Data.Length = Data.Length;
                if (Data.Length != 0)
                {
                    Data.GetSpan().CopyTo(Entry.Data.GetSpan());
                }
            }
            return Entry;
        }

        static QUIC_CID QuicCidNewDestination(QUIC_SSBuffer Data)
        {
            QUIC_CID Entry = new QUIC_CID();
            if (Entry != null)
            {
                Entry.Data.Length = Data.Length;
                if (Data.Length != 0)
                {
                    Entry.Data.GetSpan().CopyTo(Data.GetSpan());
                }
            }
            return Entry;
        }

        static QUIC_CID QuicCidNewRandomDestination()
        {
            QUIC_CID Entry = new QUIC_CID();
            if (Entry != null)
            {
                Entry.Data.Length = QUIC_MIN_INITIAL_CONNECTION_ID_LENGTH;
                CxPlatRandom.Random(Entry.Data.GetSpan().Slice(0, QUIC_MIN_INITIAL_CONNECTION_ID_LENGTH));
            }
            return Entry;
        }

        static QUIC_CID QuicCidNewNullSource(QUIC_CONNECTION Connection)
        {
            QUIC_CID Entry = new QUIC_CID();
            if (Entry != null)
            {
                Entry.Connection = Connection;
                Entry.Data.GetSpan().Clear();
            }
            return Entry;
        }

    }
}
