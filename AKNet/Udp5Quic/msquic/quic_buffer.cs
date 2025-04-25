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

    public struct SimpleStructBuffer
    {
        public int Offset;
        public int Length;
        public byte[] Buffer;

        public SimpleStructBuffer(byte[] Buffer, int Length)
        {
            this.Offset = 0;
            this.Length = Length;
            this.Buffer = Buffer;
        }

        public SimpleStructBuffer(byte[] Buffer, int Offset, int Length)
        {
            this.Offset = Offset;
            this.Length = Length;
            this.Buffer = Buffer;
        }

        public Span<byte> GetSpan()
        {
            return Buffer.AsSpan().Slice(Offset, Length);
        }
    }

    public struct SimpleClassBuffer
    {
        public int Offset;
        public int Length;
        public byte[] Buffer;

        public SimpleClassBuffer(byte[] Buffer, int Length)
        {
            this.Offset = 0;
            this.Length = Length;
            this.Buffer = Buffer;
        }

        public SimpleClassBuffer(byte[] Buffer, int Offset, int Length)
        {
            this.Offset = Offset;
            this.Length = Length;
            this.Buffer = Buffer;
        }

        public Span<byte> GetSpan()
        {
            return Buffer.AsSpan().Slice(Offset, Length);
        }
    }
}
