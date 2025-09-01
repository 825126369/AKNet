using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
[assembly: InternalsVisibleTo("AKNet")]
[assembly: InternalsVisibleTo("AKNet.MSQuic")]
[assembly: InternalsVisibleTo("AKNet2")]
[assembly: InternalsVisibleTo("AKNet.Other")]
[assembly: InternalsVisibleTo("AKNet.Test")]
namespace AKNet.Common
{
    //先前的循环Buffer 存在内存抖动的问题，这里就是为了缓解这个抖动问题
    internal class AkCircularManyBuffer:IDisposable
    {
        public class BufferItem:IDisposable
        {
            public readonly LinkedListNode<BufferItem> mEntry = null;
            private readonly IMemoryOwner<byte> mBufferMemory;
            public int nOffset;
            public int nLength;
            private bool bDispose = false;
            ~BufferItem() 
            {
                Dispose();
            }

            public BufferItem(int nBufferLength)
            {
                this.bDispose = false;
                mEntry = new LinkedListNode<BufferItem>(this);
                mBufferMemory = MemoryPool<byte>.Shared.Rent(nBufferLength);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Span<byte> GetCanWriteSpan()
            {
                return mBufferMemory.Memory.Span.Slice(nOffset + nLength);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Span<byte> GetCanReadSpan()
            {
                return mBufferMemory.Memory.Span.Slice(nOffset, nLength);
            }

            public void Reset()
            {
                this.nOffset = 0;
                this.nLength = 0;
            }

            public void Dispose()
            {
                if (bDispose) return;
                this.nOffset = 0;
                this.nLength = 0;
                this.bDispose = true;
                mBufferMemory.Dispose();
            }
        }

        private readonly int nInitBlockCount = 1;
        private readonly int nMaxBlockCount = 1;
        private readonly LinkedList<BufferItem> mItemList = new LinkedList<BufferItem>();
        private readonly int BlockSize = 1024;
        private LinkedListNode<BufferItem> nCurrentWriteBlock;
        private int nSumByteCount;

        public AkCircularManyBuffer(int nInitBlockCount = 1, int nMaxBlockCount = -1, int nBlockSize = 1024)
        {
            this.BlockSize = nBlockSize;
            this.nInitBlockCount = nInitBlockCount;
            this.nMaxBlockCount = nMaxBlockCount;
            for (int i = 0; i < nInitBlockCount; i++)
            {
                BufferItem mItem = new BufferItem(BlockSize);
                mItemList.AddLast(mItem.mEntry);
            }
            nCurrentWriteBlock = mItemList.First;
            nSumByteCount = 0;
        }

        private LinkedListNode<BufferItem> nCurrentReadBlock => mItemList.First;

        public int Length
        {
            get
            {
                return nSumByteCount;
            }
        }

        public bool IsEmpty => nSumByteCount == 0;

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
                if (mBufferSpan.Length == 0)
                {
                    goto NextBlock;
                }

                int nCopyLength = Math.Min(mBufferSpan.Length, buffer.Length);
                buffer.Slice(0, nCopyLength).CopyTo(mBufferSpan);
                buffer = buffer.Slice(nCopyLength);
                mBufferItem.nLength += nCopyLength;
                nSumByteCount += nCopyLength;

                if (buffer.IsEmpty)
                {
                    break;
                }

            NextBlock:
                if (mBufferItem.GetCanWriteSpan().IsEmpty)
                {
                    nCurrentWriteBlock = nCurrentWriteBlock.Next;
                }
            }
        }

        public int WriteTo(Span<byte> buffer)
        {
            int nReadLength = 0;
            while (nCurrentReadBlock != null)
            {
                BufferItem mBufferItem = nCurrentReadBlock.Value;
                ReadOnlySpan<byte> mBufferSpan = mBufferItem.GetCanReadSpan();
                if (mBufferSpan.IsEmpty)
                {
                    goto NextBlock;
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

            NextBlock:
                if (mBufferItem.GetCanReadSpan().IsEmpty)
                {
                    if (nCurrentReadBlock == nCurrentWriteBlock)
                    {
                        break;
                    }
                    else
                    {
                        RemoveFirstNodeToLast();
                    }
                }
            }

            return nReadLength;
        }

        public int CopyTo(Span<byte> mTempSpan)
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

                    NetLog.Assert(nReadLength <= mTempSpan.Length);
                    if (nReadLength == mTempSpan.Length)
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

        public int CopyTo(Span<byte> mTempSpan, int nOffset, int nCount)
        {
            var mNode = nCurrentReadBlock;
            int nReadLength = 0;
            while (true)
            {
                ReadOnlySpan<byte> mSpan = mNode.Value.GetCanReadSpan();
                if (mSpan.Length > 0)
                {
                    int nBeginIndex = nOffset - nReadLength;
                    if (nBeginIndex < mSpan.Length)
                    {
                        int nCopyLength = Math.Min(nCount, mSpan.Length - nBeginIndex);
                        mSpan.Slice(nBeginIndex, nCopyLength).CopyTo(mTempSpan);
                        mTempSpan = mTempSpan.Slice(nCopyLength);
                        nOffset += nCopyLength;
                        nCount -= nCopyLength;
                        if (nCount == 0)
                        {
                            break;
                        }
                    }
                    nReadLength += mSpan.Length;
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

        public void ClearBuffer(int nLength)
        {
            while (nCurrentReadBlock != null)
            {
                BufferItem mBufferItem = nCurrentReadBlock.Value;
                ReadOnlySpan<byte> mBufferSpan = mBufferItem.GetCanReadSpan();
                if (mBufferSpan.IsEmpty)
                {
                    goto NextBlock;
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

            NextBlock:
                if (nCurrentReadBlock == nCurrentWriteBlock)
                {
                    break;
                }
                else
                {
                    RemoveFirstNodeToLast();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemoveFirstNodeToLast()
        {
            // 回收/销毁这个Item
            var mItem = nCurrentReadBlock.Value;
            mItem.Reset();
            mItemList.Remove(mItem.mEntry);
            if (nMaxBlockCount <= 0 || mItemList.Count < nMaxBlockCount)
            {
                mItemList.AddLast(mItem.mEntry);
            }
            else
            {
                mItem.Dispose();
            }
        }

        public void Reset()
        {
            while (mItemList.Count > nInitBlockCount)
            {
                mItemList.Last.Value.Dispose();
                mItemList.RemoveLast();
            }

            foreach(var v in mItemList)
            {
                v.Reset();
            }

            nCurrentWriteBlock = mItemList.First;
            nSumByteCount = 0;
        }

        public void Dispose()
        {
            foreach (var v in mItemList)
            {
                v.Dispose();
            }
        }

        private static void Test()
        {
            AkCircularManyBuffer mAkCircularManyBuffer = new AkCircularManyBuffer();

            var mTimer = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
            {
                int nLength = 1000000;
                Span<byte> mArray = new byte[nLength];
                RandomNumberGenerator.Fill(mArray);
                mAkCircularManyBuffer.WriteFrom(mArray);
                NetLog.Assert(mAkCircularManyBuffer.Length == nLength);

                Span<byte> mArray2 = new byte[nLength];
                NetLog.Assert(mAkCircularManyBuffer.CopyTo(mArray2) == nLength);

                mAkCircularManyBuffer.ClearBuffer(nLength);
                NetLog.Assert(mAkCircularManyBuffer.Length == 0);

                NetLog.Assert(BufferTool.orBufferEqual(mArray, mArray2));
                //NetLog.Assert(BufferTool.orBufferEqual(mArray, mArray3));
            }
            NetLog.Log($"花费时间: {mTimer.ElapsedMilliseconds}");
        }
    }
}
