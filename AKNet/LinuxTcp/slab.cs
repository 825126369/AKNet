using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace AKNet.LinuxTcp
{
    internal class SlabManager
    {
        readonly byte[] m_buffer;
        readonly Memory<byte> m_memory;
        readonly Stack<int> m_freeIndexPool;
        readonly int nBufferSize = 0;
        int nReadIndex = 0;

        public SlabManager(int nBufferSize, int nCount)
        {
            this.nBufferSize = nBufferSize;
            this.m_freeIndexPool = new Stack<int>();
            int Length = nBufferSize * nCount;
            this.m_buffer = new byte[Length];
            this.m_memory = m_buffer;

            this.nReadIndex = 0;
        }

        public Span<byte> AllocBuffer()
        {
            Span<byte> mAllocBuffer = m_memory.Span;
            if (m_freeIndexPool.Count > 0)
            {
                mAllocBuffer = mAllocBuffer.Slice(m_freeIndexPool.Pop(), nBufferSize);
            }
            else
            {
                if (nReadIndex + nBufferSize <= m_buffer.Length)
                {
                    mAllocBuffer = mAllocBuffer.Slice(nReadIndex, nBufferSize);
                    nReadIndex += nBufferSize;
                }
                else
                {
                    return Span<byte>.Empty;
                }
            }
            return mAllocBuffer;
        }

        public void FreeBuffer()
        {
            //m_freeIndexPool.Push(args.Offset);
            //args.(null);
        }

    }
}
