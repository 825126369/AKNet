/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/29 4:33:49
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Collections.Generic;
using System.Text;

namespace MSQuic1
{
    internal class QUIC_ALPN_BUFFER : QUIC_BUFFER
    {
        public readonly List<string> mAlpnList = new List<string>();

        public QUIC_ALPN_BUFFER() : base()
        {

        }

        public QUIC_ALPN_BUFFER(int nInitSize):base(nInitSize)
        {
           
        }

        public QUIC_ALPN_BUFFER(QUIC_SSBuffer ssBuffer) : base(ssBuffer.Buffer,ssBuffer.Offset, ssBuffer.Length)
        {

        }

        public static implicit operator QUIC_ALPN_BUFFER(QUIC_SSBuffer ssBuffer)
        {
            if (ssBuffer.Buffer == null)
            {
                return null;
            }
            else
            {
                return new QUIC_ALPN_BUFFER(ssBuffer);
            }
        }

        public override string ToString()
        {
            ReadOnlySpan<byte> mSpan = GetSpan();
            List<string> alpnList = new List<string>();
            while (mSpan.Length > 0)
            {
                int nLength = mSpan[0];
                string alpn = Encoding.ASCII.GetString(mSpan.Slice(1, nLength));
                mSpan = mSpan.Slice(1 + nLength);
                alpnList.Add(alpn);
            }
            return string.Join(",", alpnList);
        }
    }

    internal class QUIC_Pool_BUFFER : QUIC_BUFFER, CXPLAT_POOL_Interface<QUIC_Pool_BUFFER>
    {
        public CXPLAT_POOL<QUIC_Pool_BUFFER> mPool = null;
        public readonly CXPLAT_POOL_ENTRY<QUIC_Pool_BUFFER> POOL_ENTRY = null;

        public QUIC_Pool_BUFFER() : base()
        {
            POOL_ENTRY = new CXPLAT_POOL_ENTRY<QUIC_Pool_BUFFER>(this);
        }

        public QUIC_Pool_BUFFER(int nInitSize):base(nInitSize)
        {
            POOL_ENTRY = new CXPLAT_POOL_ENTRY<QUIC_Pool_BUFFER>(this);
        }

        public CXPLAT_POOL_ENTRY<QUIC_Pool_BUFFER> GetEntry()
        {
            return POOL_ENTRY;
        }

        public CXPLAT_POOL<QUIC_Pool_BUFFER> GetPool()
        {
            return mPool;
        }

        public void SetPool(CXPLAT_POOL<QUIC_Pool_BUFFER> mPool)
        {
            this.mPool = mPool;
        }
    }

    internal unsafe class QUIC_BUFFER
    {
        public int Offset;
        public int Length;
        public byte[] Buffer;
        
        public QUIC_BUFFER()
        {
            
        }

        public QUIC_BUFFER(int nInitSize)
        {
            Buffer = new byte[nInitSize];
            Offset = 0;
            Length = Buffer.Length;
        }

        public QUIC_BUFFER(byte[]? Buffer)
        {
            this.Offset = 0;
            this.Length = Buffer.Length;
            this.Buffer = Buffer;
        }

        public QUIC_BUFFER(byte[]? Buffer, int Offset, int Length)
        {
            this.Offset = Offset;
            this.Length = Length;
            this.Buffer = Buffer;
        }

        public Span<byte> GetSpan()
        {
            return Buffer.AsSpan().Slice(Offset, Length);
        }

        public void Reset()
        {
            Buffer = null;
            Offset = 0;
            Length = 0;
        }

        public void CopyTo(QUIC_BUFFER Buffer)
        {
            GetSpan().CopyTo(Buffer.GetSpan());
        }

        public void CopyTo(QUIC_SSBuffer Buffer)
        {
            GetSpan().CopyTo(Buffer.GetSpan());
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

        public void SetData(QUIC_SSBuffer otherBuffer)
        {
            this.Buffer = otherBuffer.Buffer;
            this.Offset = otherBuffer.Offset;
            this.Length = otherBuffer.Length;
        }

        public void SetData(byte[] otherBuffer, int nOffset, int nLength)
        {
            this.Buffer = otherBuffer;
            this.Offset = nOffset;
            this.Length = nLength;
        }

        public void Clear()
        {
            Array.Clear(this.Buffer, 0, this.Buffer.Length);
        }

        public QUIC_SSBuffer Slice(int Offset)
        {
            return new QUIC_SSBuffer(Buffer, this.Offset + Offset, Length - Offset);
        }

        public QUIC_SSBuffer Slice(int Offset, int Length)
        {
            return new QUIC_SSBuffer(Buffer, this.Offset + Offset, Length);
        }

        public static QUIC_SSBuffer operator +(QUIC_BUFFER Buffer, int Offset)
        {
            return Buffer.Slice(Offset);
        }

        public static QUIC_SSBuffer operator -(QUIC_BUFFER Buffer, int Offset)
        {
            return Buffer.Slice(-Offset);
        }

        public static int operator -(QUIC_BUFFER Buffer1, QUIC_BUFFER Buffer2)
        {
            return Buffer1.Offset - Buffer2.Offset;
        }

        public static implicit operator QUIC_BUFFER(QUIC_SSBuffer ssBuffer)
        {
            if (ssBuffer.Buffer == null)
            {
                return null;
            }
            else
            {
                return new QUIC_BUFFER(ssBuffer.Buffer, ssBuffer.Offset, ssBuffer.Length);
            }
        }

        public static implicit operator QUIC_BUFFER(byte[] ssBuffer)
        {
            if (ssBuffer == null)
            {
                return null;
            }
            else
            {
                return new QUIC_BUFFER(ssBuffer, 0, ssBuffer.Length);
            }
        }

        public bool IsEmpty
        {
            get => Length == 0;
        }

        public void* GetBufferPtr()
        {
            fixed (void* ptr = Buffer)
            {
                return ptr;
            }
        }
    }

    internal ref struct QUIC_SSBuffer
    {
        public int Offset;
        public int Length;
        public byte[] Buffer;

        public QUIC_SSBuffer(byte[] Buffer)
        {
            this.Offset = 0;
            this.Length = Buffer.Length;
            this.Buffer = Buffer;
        }

        public QUIC_SSBuffer(byte[] Buffer, int Length)
        {
            this.Buffer = Buffer;
            this.Offset = 0;
            this.Length = Length;
        }

        public QUIC_SSBuffer(byte[] Buffer, int Offset, int Length)
        {
            this.Buffer = Buffer;
            this.Offset = Offset;
            this.Length = Length;
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

        public void Clear()
        {
            Array.Clear(this.Buffer, 0, this.Buffer.Length);
        }

        public QUIC_SSBuffer Slice(int Offset)
        {
            return new QUIC_SSBuffer(Buffer, this.Offset + Offset, Length - Offset);
        }

        public QUIC_SSBuffer Slice(int Offset, int Length)
        {
            return new QUIC_SSBuffer(Buffer, this.Offset + Offset, Length);
        }

        public Span<byte> GetSpan()
        {
            return Buffer.AsSpan().Slice(Offset, Length);
        }

        public void CopyTo(QUIC_BUFFER Buffer)
        {
            GetSpan().CopyTo(Buffer.GetSpan());
        }

        public void CopyTo(QUIC_SSBuffer Buffer)
        {
            GetSpan().CopyTo(Buffer.GetSpan());
        }

        public void CopyTo(byte[] Buffer)
        {
            GetSpan().CopyTo(Buffer);
        }

        public void CopyTo(Span<byte> Buffer)
        {
            GetSpan().CopyTo(Buffer);
        }

        public static QUIC_SSBuffer operator +(QUIC_SSBuffer Buffer, int Offset)
        {
            return Buffer.Slice(Offset);
        }

        public static QUIC_SSBuffer operator -(QUIC_SSBuffer Buffer, int Offset)
        {
            return Buffer.Slice(-Offset);
        }

        public static int operator -(QUIC_SSBuffer Buffer1, QUIC_SSBuffer Buffer2)
        {
            return Buffer1.Offset - Buffer2.Offset;
        }

        public static implicit operator QUIC_SSBuffer(byte[]? amount)
        {
            if (amount == null)
            {
                return Empty;
            }
            else
            {
                return new QUIC_SSBuffer(amount);
            }
        }

        public static implicit operator QUIC_SSBuffer(QUIC_BUFFER? amount)
        {
            if (amount == null)
            {
                return default;
            }
            else
            {
                return new QUIC_SSBuffer(amount.Buffer, amount.Offset, amount.Length);
            }
        }

        public static bool operator !=(QUIC_SSBuffer left, QUIC_SSBuffer right)
        {
            return !(left == right);
        }
        public static bool operator ==(QUIC_SSBuffer left, QUIC_SSBuffer right)
        {
            return left.Buffer == right.Buffer && left.Offset == right.Offset && left.Length == right.Length;
        }

        public static QUIC_SSBuffer Empty => default;

        public bool IsEmpty
        {
            get => Length == 0;
        }
    }
}
