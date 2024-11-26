/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/23 22:12:35
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace AKNet.Common
{
    /// <summary>
    /// 适用于 频繁的修改数组
    /// </summary>
    internal class AkCircularSpanBuffer<T>
	{
		private Memory<T> Buffer = null;
		private int dataLength;
		private int nBeginReadIndex;
		private int nBeginWriteIndex;
		private int nMaxCapacity = 0;
		private Queue<int> mSegmentLengthQueue = null;

		public AkCircularSpanBuffer(int initCapacity = 1024 * 64, int nMaxCapacity = 0)
		{
			nBeginReadIndex = 0;
			nBeginWriteIndex = 0;
			dataLength = 0;

			SetMaxCapacity(nMaxCapacity);
			NetLog.Assert(initCapacity % 1024 == 0);
			if (initCapacity > 0)
			{
				Buffer = new T[initCapacity];
			}
			else
			{
				Buffer = new T[1024];
			}
			mSegmentLengthQueue = new Queue<int>();
		}

		public void SetMaxCapacity(int nCapacity)
		{
			this.nMaxCapacity = nCapacity;
		}

		public void reset()
		{
			dataLength = 0;
			nBeginReadIndex = 0;
			nBeginWriteIndex = 0;
			mSegmentLengthQueue.Clear();
		}

		public void release()
		{
			Buffer = Memory<T>.Empty;
			mSegmentLengthQueue = null;
			this.reset();
		}

		private int Capacity
		{
			get
			{
				return this.Buffer.Length;
			}
		}

		private int Length
		{
			get
			{
				return this.dataLength;
			}
		}

        public int CurrentSegmentLength
		{
			get
			{
				if (mSegmentLengthQueue.Count > 0)
				{
					return this.mSegmentLengthQueue.Peek();
				}

				return 0;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void Check()
		{
#if DEBUG
			int nSumLength = 0;
			foreach (var v in mSegmentLengthQueue)
			{
				nSumLength += v;
			}

			NetLog.Assert(nSumLength == Length, nSumLength + " | " + Length);
#endif
		}

        public bool isCanWriteFrom(int countT)
		{
			return this.Capacity - this.Length >= countT;
		}

		public bool isCanWriteTo()
		{
			return CurrentSegmentLength > 0;
        }

		private void EnSureCapacityOk(int nCount)
		{
			if (!isCanWriteFrom(nCount))
			{
                int nOriLength = this.Length;
				int nNeedSumLength = nOriLength + nCount;

				int newSize = Capacity * 2;
				while (newSize < nNeedSumLength)
				{
					newSize *= 2;
				}

				Memory<T> newBuffer = new T[newSize];
				CopyToNewArray(newBuffer.Span);
				this.Buffer = newBuffer;
				this.nBeginReadIndex = 0;
				this.nBeginWriteIndex = nOriLength;

				this.Check();
#if DEBUG
				NetLog.LogWarning("EnSureCapacityOk AddTo Size: " + Capacity);
#endif
			}
			else
			{
				if (nMaxCapacity > 0 && Capacity > nMaxCapacity)
				{
					//这里的话，就是释放内存
					int nOriLength = this.Length;
					int nNeedSumLength = nOriLength + nCount;

					int newSize = Capacity;
					while (newSize / 2 >= nMaxCapacity && newSize / 2 > nNeedSumLength)
					{
						newSize /= 2;
					}

					if (newSize != Capacity)
					{
						Memory<T> newBuffer = new T[newSize];
                        CopyToNewArray(newBuffer.Span);
                        this.Buffer = newBuffer;
						this.nBeginReadIndex = 0;
						this.nBeginWriteIndex = nOriLength;

#if DEBUG
                        NetLog.LogWarning("EnSureCapacityOk MinusTo Size: " + Capacity);
#endif
					}
				}
			}
		}

		public void WriteFrom(ReadOnlySpan<T> readOnlySpan)
		{
			if (readOnlySpan.Length <= 0)
			{
				return;
			}

			EnSureCapacityOk(readOnlySpan.Length);
			if (isCanWriteFrom(readOnlySpan.Length))
			{
				var mBufferSpan = this.Buffer.Span;
                if (nBeginWriteIndex + readOnlySpan.Length <= this.Capacity)
				{
					readOnlySpan.CopyTo(mBufferSpan.Slice(nBeginWriteIndex));
				}
				else
				{
					int Length1 = this.Buffer.Length - nBeginWriteIndex;
					int Length2 = readOnlySpan.Length - Length1;
					readOnlySpan.Slice(0, Length1).CopyTo(mBufferSpan.Slice(nBeginWriteIndex));
					readOnlySpan.Slice(Length1, Length2).CopyTo(mBufferSpan);
				}

				dataLength += readOnlySpan.Length;
				nBeginWriteIndex += readOnlySpan.Length;
				if (nBeginWriteIndex >= this.Capacity)
				{
					nBeginWriteIndex -= this.Capacity;
				}

				mSegmentLengthQueue.Enqueue(readOnlySpan.Length);
				Check();
			}
			else
			{
				NetLog.LogError("环形缓冲区 写 溢出 " + this.Capacity + " | " + this.Length + " | " + readOnlySpan.Length);
			}
		}

		public int WriteTo(Span<T> readBuffer)
		{
			if (isCanWriteTo())
			{
				int nLength = CopyTo(readBuffer);
				ClearFirstBuffer();
				Check();
				return nLength;
			}
			return 0;
		}

		public int CopyTo(Span<T> readBuffer)
		{
			int copyLength = CurrentSegmentLength;
			if (copyLength <= 0)
			{
				return 0;
			}
			else if (copyLength > readBuffer.Length)
			{
				NetLog.LogError($"readBuffer 长度不足: {copyLength} | {readBuffer.Length}");
				return 0;
			}

			var mBufferSpan = this.Buffer.Span;
			int tempBeginIndex = nBeginReadIndex;

			if (tempBeginIndex + copyLength <= this.Capacity)
			{
				mBufferSpan.Slice(tempBeginIndex, copyLength).CopyTo(readBuffer);
			}
			else
			{
				int Length1 = this.Capacity - tempBeginIndex;
				int Length2 = copyLength - Length1;
				mBufferSpan.Slice(tempBeginIndex, Length1).CopyTo(readBuffer);
				mBufferSpan.Slice(0, Length2).CopyTo(readBuffer.Slice(Length1));
			}
			return copyLength;
		}

		private void CopyToNewArray(Span<T> readBuffer)
		{
			int copyLength = dataLength;
			if (copyLength <= 0)
			{
				return;
			}
			else if (copyLength > readBuffer.Length)
			{
				NetLog.LogError($"readBuffer 长度不足: {copyLength} | {readBuffer.Length}");
				return;
			}

			var mBufferSpan = this.Buffer.Span;
			int tempBeginIndex = nBeginReadIndex;

			if (tempBeginIndex + copyLength <= this.Capacity)
			{
				mBufferSpan.Slice(tempBeginIndex, copyLength).CopyTo(readBuffer);
			}
			else
			{
				int Length1 = this.Capacity - tempBeginIndex;
				int Length2 = copyLength - Length1;
				mBufferSpan.Slice(tempBeginIndex, Length1).CopyTo(readBuffer);
				mBufferSpan.Slice(0, Length2).CopyTo(readBuffer.Slice(Length1));
			}
		}

        public void ClearFirstBuffer()
		{
			if (CurrentSegmentLength > 0)
			{
                int readLength = mSegmentLengthQueue.Dequeue();
                if (readLength >= this.Length)
				{
					this.reset();
				}
				else
				{
					dataLength -= readLength;
					nBeginReadIndex += readLength;
					if (nBeginReadIndex >= this.Capacity)
					{
						nBeginReadIndex -= this.Capacity;
					}
				}
			}
		}
    }
}








