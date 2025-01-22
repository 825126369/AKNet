using System;
using System.Collections.Generic;

namespace AKNet.LinuxTcp
{
    internal class AllocBufferManager
    {
        SlabManager mSlabManager;
        public ArraySegment<byte> AllocBuffer()
        {
            var mBuffer = mSlabManager.AllocBuffer();
            if (mBuffer == ArraySegment<byte>.Empty)
            {
                mBuffer = new byte[mSlabManager.nBufferSegmentSize];
            }
            return mBuffer;
        }

        public void FreeBuffer(ArraySegment<byte> mArraySegment)
        {
            mSlabManager.FreeBuffer(mArraySegment);
        }
    }

    internal class SlabManager
    {
        readonly byte[] mBuffer;
        readonly Stack<int> m_freeIndexPool = new Stack<int>();
        public readonly int nBufferSegmentSize = 0;
        int nReadIndex = 0;

        public SlabManager(int nBufferSegmentSize, int nMaxCount)
        {
            this.nBufferSegmentSize = nBufferSegmentSize;
            this.mBuffer = new byte[nBufferSegmentSize * nMaxCount];
            this.nReadIndex = 0;
        }

        public ArraySegment<byte> AllocBuffer()
        {
            if (m_freeIndexPool.Count > 0)
            {
                ArraySegment<byte> mAllocBuffer = new ArraySegment<byte>(mBuffer, m_freeIndexPool.Pop(), nBufferSegmentSize);
                return mAllocBuffer;
            }
            else
            {
                if (nReadIndex + nBufferSegmentSize <= mBuffer.Length)
                {
                    ArraySegment<byte> mAllocBuffer = new ArraySegment<byte>(mBuffer, nReadIndex, nBufferSegmentSize);
                    nReadIndex += nBufferSegmentSize;
                    return mAllocBuffer;
                }
                else
                {
                    return ArraySegment<byte>.Empty;
                }
            }
        }

        public void FreeBuffer(ArraySegment<byte> mArraySegment)
        {
            if (mArraySegment.Array == mBuffer)
            {
                m_freeIndexPool.Push(mArraySegment.Offset);
            }
        }

    }
}
