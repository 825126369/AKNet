using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("AKNet")]
[assembly: InternalsVisibleTo("AKNet.MSQuic")]
[assembly: InternalsVisibleTo("AKNet2")]
[assembly: InternalsVisibleTo("AKNet.QuicNet")]
namespace AKNet.Common
{
    //��ǰ��ѭ��Buffer �����ڴ涶�������⣬�������Ϊ�˻��������������
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Span<byte> GetCanWriteSpan()
            {
                return mBufferMemory.Span.Slice(nLength + nOffset); 
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Span<byte> GetCanReadSpan()
            {
                return mBufferMemory.Span.Slice(nOffset, nLength);
            }

            public void Reset()
            {
                this.nOffset = 0;
                this.nLength = 0;
            }
        }

        private int nMaxBlockCount = 0;
        private readonly LinkedList<BufferItem> mItemList = new LinkedList<BufferItem>();
        private readonly int BlockSize = 16 * 1024;
        private LinkedListNode<BufferItem> nCurrentWriteBlock;
        private LinkedListNode<BufferItem> nCurrentReadBlock;
        private int nSumByteCount;

        public AkCircularManyBuffer()
        {
            int nInitBlockCount = 10;
            int nBlockSize = 16 * 1024;
            this.BlockSize = nBlockSize;
            for (int i = 0; i < nInitBlockCount; i++)
            {
                BufferItem mItem = new BufferItem(BlockSize);
                mItemList.AddLast(mItem.mEntry);
            }

            nCurrentWriteBlock = mItemList.First;
            nCurrentReadBlock = mItemList.First;
            nSumByteCount = 0;
        }

        public AkCircularManyBuffer(int nInitBlockCount, int nBlockSize)
        {
            this.BlockSize = nBlockSize;
            for (int i = 0; i < nInitBlockCount; i++)
            {
                BufferItem mItem = new BufferItem(BlockSize);
                mItemList.AddLast(mItem.mEntry);
            }
            nCurrentWriteBlock = mItemList.First;
            nCurrentReadBlock = mItemList.First;
            nSumByteCount = 0;
        }

        public void SetMaxBlockCount(int nCount)
        {
            this.nMaxBlockCount = nCount;
        }

        public int Length
        {
            get
            {
                return nSumByteCount;
            }
        }

        public void WriteFrom(ReadOnlySpan<byte> buffer)
        {
            if (buffer.IsEmpty)
            {
                return;
            }

            while (true)
            {
                if(nCurrentWriteBlock == null)
                {
                    BufferItem mItem = new BufferItem(BlockSize);
                    mItemList.AddLast(mItem.mEntry);
                    nCurrentWriteBlock = mItem.mEntry;
                }

                BufferItem mBufferItem = nCurrentWriteBlock.Value;
                Span<byte> mBufferSpan = mBufferItem.GetCanWriteSpan();
                int nCopyLength = Math.Min(mBufferSpan.Length, buffer.Length);
                buffer.Slice(0, nCopyLength).CopyTo(mBufferSpan);
                buffer = buffer.Slice(nCopyLength);
                mBufferItem.nLength += nCopyLength;
                nSumByteCount += nCopyLength;
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

        public int WriteTo(Span<byte> buffer)
        {
            int nReadLength = 0;
            while (true)
            {
                if (nCurrentReadBlock == null)
                {
                    break;
                }

                BufferItem mBufferItem = nCurrentReadBlock.Value;
                ReadOnlySpan<byte> mBufferSpan = mBufferItem.GetCanReadSpan();
                if (mBufferSpan.IsEmpty)
                {
                    break;
                }

                int nCopyLength = Math.Min(mBufferSpan.Length, buffer.Length);
                mBufferSpan.Slice(0, nCopyLength).CopyTo(buffer);
                buffer = buffer.Slice(nCopyLength);
                mBufferItem.nOffset += nCopyLength;
                mBufferItem.nLength -= nCopyLength;
                nReadLength += nCopyLength;
                nSumByteCount -= nCopyLength;

                if (buffer.Length == 0)
                {
                    break;
                }

                if (mBufferItem.GetCanReadSpan().IsEmpty)
                {
                    if (nCurrentReadBlock == nCurrentWriteBlock)
                    {
                        break;
                    }
                    else
                    {
                        nCurrentReadBlock = nCurrentReadBlock.Next;
                        mBufferItem.Reset();
                        mItemList.Remove(mBufferItem.mEntry);
                        if (nMaxBlockCount == 0 || mItemList.Count < nMaxBlockCount)
                        {
                            mItemList.AddLast(mBufferItem.mEntry);
                        }
                    }
                }
            }

            return nReadLength;
        }


    }
}
