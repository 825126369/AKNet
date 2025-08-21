using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("AKNet")]
[assembly: InternalsVisibleTo("AKNet.MSQuic")]
[assembly: InternalsVisibleTo("AKNet2")]
[assembly: InternalsVisibleTo("AKNet.QuicNet")]
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

        public AkCircularManyBuffer(int nInitBlockCount = 10, int nBlockSize = 1024)
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

        public int Peek(Span<byte> mTempSpan)
        {
            var mNode = nCurrentReadBlock;
            int nReadLength = 0;
            while (mNode != null)
            {
                ReadOnlySpan<byte> mSpan = mNode.Value.GetCanReadSpan();
                if (mSpan.Length > 0)
                {
                    int nCopyLength = Math.Min(mTempSpan.Length - nReadLength, mSpan.Length);
                    mSpan.Slice(0, nCopyLength).CopyTo(mTempSpan.Slice(nReadLength));
                    nReadLength += nCopyLength;
                    if (nReadLength >= mTempSpan.Length)
                    {
                        break;
                    }
                }

                if (mNode == nCurrentWriteBlock)
                {
                    break;
                }
                else
                {
                    mNode = mNode.Next;
                }
            }
            return nReadLength;
        }

        public bool isCanWriteTo(int countT)
        {
            return this.Length >= countT;
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

        public void ClearBuffer(int nLength)
        {
            while (nCurrentReadBlock != null)
            {
                BufferItem mBufferItem = nCurrentReadBlock.Value;
                ReadOnlySpan<byte> mBufferSpan = mBufferItem.GetCanReadSpan();
                if (mBufferSpan.IsEmpty)
                {
                    break;
                }

                int nClearLength = Math.Min(mBufferSpan.Length, nLength);
                mBufferItem.nOffset += nClearLength;
                mBufferItem.nLength -= nClearLength;
                nLength -= nClearLength;
                nSumByteCount -= nClearLength;

                NetLog.Assert(nLength >= 0);
                if (nLength == 0)
                {
                    break;
                }
                
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

        public void Reset()
        {
            nCurrentWriteBlock = mItemList.First;
            nCurrentReadBlock = mItemList.First;
            nSumByteCount = 0;
            nCurrentWriteBlock.Value.Reset();
            nCurrentReadBlock.Value.Reset();
        }
    }
}
