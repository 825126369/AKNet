/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:45
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("AKNet")]
[assembly: InternalsVisibleTo("AKNet.MSQuic")]
[assembly: InternalsVisibleTo("AKNet.LinuxTcp")]
[assembly: InternalsVisibleTo("AKNet.WebSocket")]
[assembly: InternalsVisibleTo("AKNet.MSTest")]
namespace AKNet.Common
{
    internal class AkCircularManyBuffer : IDisposable
    {
        public class BufferItem : IDisposable
        {
            public readonly LinkedListNode<BufferItem> mEntry = null;
            private readonly byte[] mBufferMemory;
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
                mBufferMemory = new byte[nBufferLength];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Span<byte> GetCanWriteSpan()
            {
                return mBufferMemory.AsSpan().Slice(nOffset + nLength);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Span<byte> GetCanReadSpan()
            {
                return mBufferMemory.AsSpan().Slice(nOffset, nLength);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset()
            {
                this.nOffset = 0;
                this.nLength = 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                if (bDispose) return;
                this.nOffset = 0;
                this.nLength = 0;
                this.bDispose = true;
            }
        }

        private const bool bNeedCheck = false;
        private readonly int nInitBlockCount = 0;
        private readonly int nMaxBlockCount = 0;
        private readonly LinkedList<BufferItem> mItemList = new LinkedList<BufferItem>();
        private readonly int BlockSize = 0;
        private LinkedListNode<BufferItem> nCurrentWriteBlock;
        private LinkedListNode<BufferItem> nCurrentReadBlock => mItemList.First;
        private long nSumByteCount;

        public AkCircularManyBuffer(int nBlockSize = 1024, int nInitBlockCount = 1, int nMaxBlockCount = -1)
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

        public int Length
        {
            get
            {
                if (nSumByteCount > int.MaxValue)
                {
                    throw new OverflowException($"nSumByteCount: {nSumByteCount}");
                }
                return (int)nSumByteCount;
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

            nSumByteCount += buffer.Length;
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

#if DEBUG
            Check_Length_Ok();
#endif
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

            nSumByteCount -= nReadLength;

#if DEBUG
            Check_Length_Ok();
#endif
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

#if DEBUG
            Check_Length_Ok();
#endif
            return nReadLength;
        }

        public void CopyTo(Span<byte> mTempSpan, int nOffset, int nCount)
        {
#if DEBUG
            if (nOffset < 0 || nOffset >= Length)
            {
                throw new ArgumentException();
            }

            if (nCount > mTempSpan.Length)
            {
                throw new ArgumentException();
            }

            if (nOffset + nCount > Length)
            {
                throw new ArgumentException();
            }
#endif
            var mNode = nCurrentReadBlock;
            int nSumCopyLength = 0;
            int nRemainCopyLength = nCount;

            while (true)
            {
                ReadOnlySpan<byte> mSpan = mNode.Value.GetCanReadSpan();
                if (mSpan.Length > 0)
                {
                    int nBeginIndex = nOffset;
                    if (nBeginIndex < mSpan.Length)
                    {
                        nOffset = 0;

                        int nCopyLength = Math.Min(nRemainCopyLength, mSpan.Length - nBeginIndex);
                        nCopyLength = Math.Min(mTempSpan.Length, nCopyLength);
                        mSpan.Slice(nBeginIndex, nCopyLength).CopyTo(mTempSpan);
                        mTempSpan = mTempSpan.Slice(nCopyLength);
                        nRemainCopyLength -= nCopyLength;
                        nSumCopyLength += nCopyLength;
                        if (nRemainCopyLength == 0)
                        {
                            break;
                        }
                    }
                    else
                    {
                        nOffset -= mSpan.Length;
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

#if DEBUG
            if (nSumCopyLength != nCount)
            {
                throw new Exception($"nSumCopyLength: {nSumCopyLength}, nCount: {nCount}");
            }
            Check_Length_Ok();
#endif
        }

        public void ClearBuffer(int nLength)
        {
#if DEBUG
            if (nSumByteCount < nLength || nLength <= 0)
            {
                throw new Exception($"ClearBuffer: {nLength}");
            }
#endif

            nSumByteCount -= nLength;
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
                nLength -= nClearLength;;

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

#if DEBUG
            Check_Length_Ok();
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemoveFirstNodeToLast()
        {
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
            mItemList.Clear();
        }

        [Conditional("DEBUG")]
        private void Check_Length_Ok()
        {
            if (bNeedCheck)
            {
                int nSumLength = 0;
                var mNode = nCurrentReadBlock;
                while (true)
                {
                    nSumLength += mNode.Value.GetCanReadSpan().Length;
                    if (mNode == nCurrentWriteBlock)
                    {
                        break;
                    }
                    mNode = mNode.Next;
                }

                if (nSumLength != nSumByteCount)
                {
                    throw new Exception($"nSumByteCount: {nSumByteCount}, nSumLength: {nSumLength}");
                }
            }
        }
    }
}
