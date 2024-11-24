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
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Text;

namespace AKNet.Common
{
	/// <summary>
	/// 适用于 频繁的修改数组
	/// </summary>
	internal class AkCircularSpanBuffer<T>
	{
		private T[] Buffer = null;
		private int dataLength;
		private int nBeginReadIndex;
		private int nBeginWriteIndex;
		private int nMaxCapacity = 0;
		private Queue<int> mSegmentLengthQueue = new Queue<int>();

		public AkCircularSpanBuffer(int initCapacity = 1024 * 8, int nMaxCapacity = 0)
		{
			mSegmentLengthQueue.Clear();

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
			Buffer = null;
			this.reset();
		}

		public int Capacity
		{
			get
			{
				return this.Buffer.Length;
			}
		}

		public int Length
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
			return mSegmentLengthQueue.Count > 0;
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

				T[] newBuffer = new T[newSize];
				Queue<int> newSegmentLengthQueue = new Queue<int>(mSegmentLengthQueue);

				int nLength = 0;
				while (isCanWriteTo())
				{
					int nLength2 = mSegmentLengthQueue.Peek();
					WriteTo(newBuffer.AsSpan().Slice(nLength));
					nLength += nLength2;
				}

				this.Buffer = newBuffer;
				this.nBeginReadIndex = 0;
				this.nBeginWriteIndex = nOriLength;
				this.dataLength = nOriLength;
				this.mSegmentLengthQueue = newSegmentLengthQueue;

				NetLog.LogWarning("EnSureCapacityOk AddTo Size: " + Capacity);
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
						T[] newBuffer = new T[newSize];
						Queue<int> newSegmentLengthQueue = new Queue<int>(mSegmentLengthQueue);

						int nLength = 0;
						while (isCanWriteTo())
						{
							int nLength2 = mSegmentLengthQueue.Peek();
							WriteTo(newBuffer.AsSpan().Slice(nLength));
							nLength += nLength2;
						}

						this.Buffer = newBuffer;
						this.nBeginReadIndex = 0;
						this.nBeginWriteIndex = nOriLength;
						this.dataLength = nOriLength;
						this.mSegmentLengthQueue = newSegmentLengthQueue;

						NetLog.LogWarning("EnSureCapacityOk MinusTo Size: " + Capacity);
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
				if (nBeginWriteIndex + readOnlySpan.Length <= this.Capacity)
				{
					for (int i = 0; i < readOnlySpan.Length; i++)
					{
						this.Buffer[nBeginWriteIndex + i] = readOnlySpan[i];
					}
				}
				else
				{
					int Length1 = this.Buffer.Length - nBeginWriteIndex;
					int Length2 = readOnlySpan.Length - Length1;

					for (int i = 0; i < Length1; i++)
					{
						this.Buffer[nBeginWriteIndex + i] = readOnlySpan[i];
					}

					for (int i = 0; i < Length2; i++)
					{
						this.Buffer[i] = readOnlySpan[i + Length1];
					}
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

		public void WriteTo(Span<T> readBuffer)
		{
			if (isCanWriteTo())
			{
				CopyTo(readBuffer);
				ClearFirstBuffer();
                Check();
            }
			else
			{
                NetLog.LogError("WriteTo Failed : " + CurrentSegmentLength);
            }
		}

		public void CopyTo(Span<T> readBuffer)
		{
			int copyLength = mSegmentLengthQueue.Peek();
            if (copyLength <= 0)
			{
				return;
			}
			else if (copyLength > readBuffer.Length)
			{
				NetLog.LogError($"readBuffer 长度不足: {copyLength} | {readBuffer.Length}");
				return;
			}

			var mSpanBuffer = this.Buffer.AsSpan();
			int tempBeginIndex = nBeginReadIndex;

			if (tempBeginIndex + copyLength <= this.Capacity)
			{
				mSpanBuffer.Slice(tempBeginIndex, copyLength).CopyTo(readBuffer);
			}
			else
			{
				int Length1 = this.Capacity - tempBeginIndex;
				int Length2 = copyLength - Length1;
				mSpanBuffer.Slice(tempBeginIndex, Length1).CopyTo(readBuffer);
				mSpanBuffer.Slice(0, Length2).CopyTo(readBuffer.Slice(Length1));
			}
		}

		public void ClearFirstBuffer()
		{
			int readLength = 0;
			if (mSegmentLengthQueue.TryDequeue(out readLength))
			{
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








