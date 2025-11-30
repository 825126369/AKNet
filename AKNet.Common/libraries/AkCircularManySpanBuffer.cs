/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:14
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("AKNet")]
[assembly: InternalsVisibleTo("AKNet.MSQuic")]
[assembly: InternalsVisibleTo("AKNet.LinuxTcp")]
[assembly: InternalsVisibleTo("AKNet.WebSocket")]
namespace AKNet.Common
{
    /// 存入一个Udp包后，不会考虑是否还有空间，直接开始下一个BufferItem。
    /// 这样每个BufferItem 对应一个Udp 包
    /// Udp包 最大尺寸1500
    internal class AkCircularManySpanBuffer:IDisposable 
    {
        public class BufferItem : IDisposable
        {
            public readonly LinkedListNode<BufferItem> mEntry = null;
            private readonly IMemoryOwner<byte> mBufferMemory;
            public int nSpanLength;
            private bool bDispose = false;

            ~BufferItem()
            {
                Dispose();
            }

            public BufferItem(int nSpanMaxLength)
            {
                this.bDispose = false;
                mEntry = new LinkedListNode<BufferItem>(this);
                mBufferMemory = MemoryPool<byte>.Shared.Rent(nSpanMaxLength);
				NetLog.Assert(mBufferMemory.Memory.Length != nSpanMaxLength);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AddSpan(ReadOnlySpan<byte> mSpan)
            {
				mSpan.CopyTo(GetCanWriteSpan());
				this.nSpanLength += mSpan.Length;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Span<byte> GetCanReadSpan()
            {
                return mBufferMemory.Memory.Span.Slice(0, nSpanLength);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Span<byte> GetCanWriteSpan()
            {
                return mBufferMemory.Memory.Span.Slice(nSpanLength);
            }

            public void Reset()
            {
                this.nSpanLength = 0;
            }

            public void Dispose()
            {
                if (bDispose) return;
                this.nSpanLength = 0;
                this.bDispose = true;
                this.mBufferMemory.Dispose();
            }
        }

        private readonly int nInitBlockCount = 1;
        private readonly int nMaxBlockCount = 1;
        private readonly int nMaxBlockSize = 1024;
        private readonly LinkedList<BufferItem> mItemList = new LinkedList<BufferItem>();
        private LinkedListNode<BufferItem> nCurrentWriteBlock;
		private int nSpanCount;

        public AkCircularManySpanBuffer(int nMaxBlockSize = 1024, int nInitBlockCount = 1, int nMaxBlockCount = -1)
        {
            this.nMaxBlockSize = nMaxBlockSize;
            this.nInitBlockCount = nInitBlockCount;
            this.nMaxBlockCount = nMaxBlockCount;

            for (int i = 0; i < nInitBlockCount; i++)
            {
                BufferItem mItem = new BufferItem(nMaxBlockSize);
                mItemList.AddLast(mItem.mEntry);
            }

            nCurrentWriteBlock = mItemList.First;
			nSpanCount = 0;
        }

        private LinkedListNode<BufferItem> nCurrentReadBlock => mItemList.First;

        public void Reset()
		{
            while (mItemList.Count > nInitBlockCount)
            {
                mItemList.Last.Value.Dispose();
                mItemList.RemoveLast();
            }

            foreach (var v in mItemList)
			{
				v.Reset();
			}

            nCurrentWriteBlock = mItemList.First;
            nSpanCount = 0;
        }

		public void Dispose()
		{
            foreach (var v in mItemList)
            {
                v.Dispose();
            }
        }

		public int GetSpanCount()
		{
			return nSpanCount;
		}

        private int CurrentSegmentLength
		{
			get
			{
				return mItemList.First.Value.nSpanLength;
			}
		}

		public bool isCanWriteTo()
		{
			return CurrentSegmentLength > 0;
        }

        public BufferItem BeginSpan()
        {
            return nCurrentWriteBlock.Value;
        }
        
        public void FinishSpan()
		{
			nCurrentWriteBlock = nCurrentWriteBlock.Next;
			if(nCurrentWriteBlock == null)
			{
                BufferItem mItem = new BufferItem(nMaxBlockSize);
                mItemList.AddLast(mItem.mEntry);
                nCurrentWriteBlock = mItem.mEntry;
            }
        }

        public void WriteFrom(ReadOnlySpan<byte> readOnlySpan)
		{
            nCurrentWriteBlock.Value.AddSpan(readOnlySpan);
        }

        public int WriteTo(Span<byte> readBuffer)
		{
			ReadOnlySpan<byte> mReadSpan = nCurrentReadBlock.Value.GetCanReadSpan();
            mReadSpan.CopyTo(readBuffer);
            RemoveFirstNodeToLast();
            return mReadSpan.Length;
		}

		public int WriteToMax(Span<byte> readBuffer)
		{
            int nReadLength = 0;
            while (true)
            {
                if(nCurrentReadBlock == nCurrentWriteBlock)
                {
                    break;
                }

                BufferItem mItem = nCurrentReadBlock.Value;
                ReadOnlySpan<byte> mReadSpan = mItem.GetCanReadSpan();
                if (mReadSpan.Length > 0)
                {
                    mReadSpan.CopyTo(readBuffer);
                    nReadLength += mReadSpan.Length;
                    readBuffer = readBuffer.Slice(nReadLength);
                }

                //回收销毁这个Item
                RemoveFirstNodeToLast();
                if (readBuffer.Length == 0)
                {
                    break;
                }
            }
            return nReadLength;
        }

		public int CopyToMax(Span<byte> readBuffer)
		{
			int nReadLength = 0;
            var mNode = mItemList.First;
			while(mNode != null)
			{
                ReadOnlySpan<byte> mReadSpan = nCurrentReadBlock.Value.GetCanReadSpan();
                if(mReadSpan.Length > readBuffer.Length)
                {
                    break;
                }

                if (mReadSpan.Length > 0)
                {
                    mReadSpan.CopyTo(readBuffer);
                    nReadLength += mReadSpan.Length;
                    readBuffer = readBuffer.Slice(nReadLength);
                }

                mNode = mNode.Next;
                if (mNode == nCurrentWriteBlock)
                {
                    break;
                }

                if (readBuffer.Length == 0)
				{
                    break;
				}
			}
            return nReadLength;
		}

		public void ClearBuffer(int nClearLength)
		{
            int nReadLength = 0;
            while (true)
            {
                BufferItem mItem = nCurrentReadBlock.Value;
                if(nCurrentReadBlock == nCurrentWriteBlock)
                {
                    break;
                }

                ReadOnlySpan<byte> mReadSpan = mItem.GetCanReadSpan();
                nReadLength += mReadSpan.Length;

                // 回收/销毁这个Item
                RemoveFirstNodeToLast();

                NetLog.Assert(nReadLength <= nClearLength);
                if (nReadLength == nClearLength)
                {
                    break;
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

    }
}








