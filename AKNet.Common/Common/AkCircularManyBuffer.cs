using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace AKNet.Common
{
    //先前的循环Buffer 存在内存抖动的问题，这里就是为了缓解这个抖动问题
    internal class AkCircularManyBuffer
    {
        public class BufferItem
        {
            public readonly LinkedListNode<BufferItem> mEntry = null;
            public readonly byte[] mBuffer;
            public readonly Memory<byte> mBufferMemory;
            public int nOffset;
            public int nLength;

            public BufferItem(int nBufferLength)
            {
                mEntry = new LinkedListNode<BufferItem>(this);
                mBuffer = new byte[nBufferLength];
                mBufferMemory = mBuffer;
            }
            
            public int RemainLength
            {
                get { return mBuffer.Length - nLength - nOffset; }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Span<byte> GetCanWriteSpan()
            {
                return mBufferMemory.Span.Slice(nLength + nOffset); 
            }

            public void Reset()
            {
                this.nOffset = 0;
                this.nLength = 0;
            }
            
            public bool orHaveData()
            {
                return this.nLength > 0;
            }
        }

        private int nSumItemCount = 0;
        private readonly LinkedList<BufferItem> mItemList = new LinkedList<BufferItem>();
        private readonly int BlockSize = 16 * 1024;
        private LinkedListNode<BufferItem> nCurrentWriteBlock;
        private LinkedListNode<BufferItem> nCurrentReadBlock;

        public AkCircularManyBuffer()
        {
            int nInitBlockCount = 10;
            int nBlockSize = 16 * 1024;
            this.BlockSize = nBlockSize;

            for (int i = 0; i < nInitBlockCount; i++)
            {
                BufferItem mItem = new BufferItem(BlockSize);
                mItemList.AddLast(mItem);
            }

            nCurrentWriteBlock = mItemList.First;
            nCurrentReadBlock = mItemList.First;
        }

        public AkCircularManyBuffer(int nInitBlockCount, int nBlockSize)
        {
            this.BlockSize = nBlockSize;
            for (int i = 0; i < nInitBlockCount; i++)
            {
                BufferItem mItem = new BufferItem(BlockSize);
                mItemList.AddLast(mItem);
            }

            nCurrentWriteBlock = mItemList.First;
            nCurrentReadBlock = mItemList.First;
        }

        public void ResizeItemList()
        {

        }

        public void WriteFrom(ReadOnlySpan<byte> buffer)
        {
            while (true)
            {
                BufferItem mBufferItem = nCurrentWriteBlock.Value;
                int nRemainLength = mBufferItem.RemainLength;
                int nCopyLength = Math.Min(mBufferItem.RemainLength, buffer.Length);
                buffer.Slice(0, nCopyLength).CopyTo(mBufferItem.GetCanWriteSpan());
                buffer = buffer.Slice(nCopyLength);
                mBufferItem.nLength += nCopyLength;

                if (buffer.IsEmpty)
                {
                    break;
                }

                if(mBufferItem.GetCanWriteSpan().IsEmpty)
                {
                    nCurrentWriteBlock = nCurrentWriteBlock.Next;
                }
            }
        }

        public void WriteTo(ReadOnlySpan<byte> buffer, int offset, int length)
        {

        }

    }
}
