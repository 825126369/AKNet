using System;

namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_BUFFER : CXPLAT_POOL_Interface<QUIC_BUFFER>
    {
        public int Offset;
        public int Length;
        public byte[] Buffer;

        public readonly CXPLAT_POOL_ENTRY<QUIC_BUFFER> POOL_ENTRY = null;
        public QUIC_BUFFER()
        {
            POOL_ENTRY = new CXPLAT_POOL_ENTRY<QUIC_BUFFER>(this);
        }

        public QUIC_BUFFER(int nInitSize)
        {
            POOL_ENTRY = new CXPLAT_POOL_ENTRY<QUIC_BUFFER>(this);
            Buffer = new byte[nInitSize];
            Offset = 0;
            Length = Buffer.Length;
        }

        public Span<byte> GetSpan()
        {
            return Buffer.AsSpan().Slice(Offset, Length);
        }

        public CXPLAT_POOL_ENTRY<QUIC_BUFFER> GetEntry()
        {
            return POOL_ENTRY;
        }

        public void Reset()
        {
            Offset = 0;
            Length = 0;
        }

        public void CopyFrom(byte[] Buffer, int Offset, int nLength)
        {

        }

        public void CopyFrom(byte[] Buffer, int nLength)
        {

        }

        public void CopyTo(byte[] Buffer, int nLength)
        {

        }
    }

    public ref struct QUIC_SSBuffer
    {
        public int Offset;
        public int Length;
        public byte[] Buffer;

        public QUIC_SSBuffer(byte[] Buffer, int Length)
        {
            this.Offset = 0;
            this.Length = Length;
            this.Buffer = Buffer;
        }

        public QUIC_SSBuffer(byte[] Buffer, int Offset, int Length)
        {
            this.Offset = Offset;
            this.Length = Length;
            this.Buffer = Buffer;
        }

        public int Capacity
        {
            get { return Buffer.Length; }
        }

        public byte this[int index]
        {
            get
            {
                return Buffer[index + Offset];
            }

            set
            {
                Buffer[index + Offset] = value;
            }
        }

        public Span<byte> GetSpan()
        {
            return Buffer.AsSpan().Slice(Offset, Length);
        }
        
        public static QUIC_SSBuffer operator +(QUIC_SSBuffer Buffer, int Offset)
        {
            return new QUIC_SSBuffer(Buffer.Buffer, Buffer.Offset + Offset, Buffer.Buffer.Length - Offset);
        }
    }
}
