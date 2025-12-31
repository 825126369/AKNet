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
using System.Collections.Generic;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("AKNet")]
[assembly: InternalsVisibleTo("AKNet.MSQuic")]
[assembly: InternalsVisibleTo("AKNet.LinuxTcp")]
[assembly: InternalsVisibleTo("AKNet.WebSocket")]
namespace AKNet.Common
{
    /// <summary>
    /// 循环Buffer块，对于 实现 UDP的滑动窗口，TCP的流接受，以及UDP 发送流的吞吐能力，都至关重要
    /// </summary>
    internal class AkCircularSpanBuffer:IDisposable
	{
        private byte[] mBuffer = null;
        private Memory<byte> MemoryBuffer = null;

        private int dataLength;
		private int nBeginReadIndex;
		private int nBeginWriteIndex;
		private int nMaxCapacity = 0;
		private Queue<int> mSegmentLengthQueue = null;

        private bool bIsSpan = true;
		private int nTempSegmentLength = 0;

		public AkCircularSpanBuffer(int initCapacity = 1024 * 10, int nMaxCapacity = 0)
		{
			nBeginReadIndex = 0;
			nBeginWriteIndex = 0;
			dataLength = 0;

			SetMaxCapacity(nMaxCapacity);
			NetLog.Assert(initCapacity % 1024 == 0);
			if (initCapacity > 0)
			{
				mBuffer = new byte[initCapacity];
			}
			else
			{
				mBuffer = new byte[1024];
			}
			MemoryBuffer = mBuffer;
			mSegmentLengthQueue = new Queue<int>();
		}

		public void SetMaxCapacity(int nCapacity)
		{
			this.nMaxCapacity = nCapacity;
		}

		public void Reset()
		{
			dataLength = 0;
			nBeginReadIndex = 0;
			nBeginWriteIndex = 0;
			mSegmentLengthQueue.Clear();
		}

		public void Dispose()
		{
			mBuffer = null;
			MemoryBuffer = null;
			mSegmentLengthQueue = null;
			this.Reset();
		}

		private int Capacity
		{
			get
			{
				return this.mBuffer.Length;
			}
		}

		private int Length
		{
			get
			{
				return this.dataLength;
			}
		}

		public int GetSpanCount()
		{
			return mSegmentLengthQueue.Count;
		}

        private int CurrentSegmentLength
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
            if (!bIsSpan) return;

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

				byte[] newBuffer = new byte[newSize];
				InnerCopyTo(newBuffer, nOriLength);
				this.mBuffer = newBuffer;
				this.MemoryBuffer = this.mBuffer;
				this.nBeginReadIndex = 0;
				this.nBeginWriteIndex = nOriLength;

				this.Check();
#if DEBUG
				//NetLog.LogWarning("EnSureCapacityOk AddTo Size: " + Capacity);
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
						byte[] newBuffer = new byte[newSize];
						InnerCopyTo(newBuffer, nOriLength);
						this.mBuffer = newBuffer;
                        this.MemoryBuffer = this.mBuffer;
                        this.nBeginReadIndex = 0;
						this.nBeginWriteIndex = nOriLength;

						this.Check();
#if DEBUG
						// NetLog.LogWarning("EnSureCapacityOk MinusTo Size: " + Capacity);
#endif
					}
				}
			}
		}
		
		public void BeginSpan()
		{
			this.bIsSpan = false;
			this.nTempSegmentLength = 0;
        }

		public void FinishSpan()
		{
			NetLog.Assert(!bIsSpan);
			this.bIsSpan = true;
            mSegmentLengthQueue.Enqueue(this.nTempSegmentLength);
            this.nTempSegmentLength = 0;
            Check();
        }

        public void WriteOneSpan(AkCircularBuffer mOtherStreamList, int nOffset, int nCount)
		{
			if (nCount <= 0)
			{
				return;
			}

			EnSureCapacityOk(nCount);
			if (isCanWriteFrom(nCount))
			{
				if (nBeginWriteIndex + nCount <= this.Capacity)
				{
					mOtherStreamList.CopyTo(nOffset, MemoryBuffer.Span.Slice(nBeginWriteIndex, nCount));
				}
				else
				{
					int Length1 = this.mBuffer.Length - nBeginWriteIndex;
					int Length2 = nCount - Length1;
					mOtherStreamList.CopyTo(nOffset, MemoryBuffer.Span.Slice(nBeginWriteIndex, Length1));
					mOtherStreamList.CopyTo(nOffset + Length1, MemoryBuffer.Span.Slice(0, Length2));
				}

				dataLength += nCount;
				nBeginWriteIndex += nCount;
				if (nBeginWriteIndex >= this.Capacity)
				{
					nBeginWriteIndex -= this.Capacity;
				}

				if (bIsSpan)
				{
					mSegmentLengthQueue.Enqueue(nCount);
					Check();
				}
				else
				{
                    this.nTempSegmentLength += nCount;
                }
			}
			else
			{
				NetLog.LogError("环形缓冲区 写 溢出 " + this.Capacity + " | " + this.Length + " | " + nCount);
			}
		}

        public void WriteFrom(ReadOnlySpan<byte> readOnlySpan)
		{
			int nCount = readOnlySpan.Length;
			if (nCount <= 0)
			{
				return;
			}

			EnSureCapacityOk(nCount);
			if (isCanWriteFrom(nCount))
			{
                if (nBeginWriteIndex + nCount <= this.Capacity)
				{
					readOnlySpan.CopyTo(MemoryBuffer.Span.Slice(nBeginWriteIndex));
				}
				else
				{
					int Length1 = this.mBuffer.Length - nBeginWriteIndex;
					int Length2 = nCount - Length1;
					readOnlySpan.Slice(0, Length1).CopyTo(MemoryBuffer.Span.Slice(nBeginWriteIndex));
					readOnlySpan.Slice(Length1, Length2).CopyTo(MemoryBuffer.Span);
				}

				dataLength += nCount;
				nBeginWriteIndex += nCount;
				if (nBeginWriteIndex >= this.Capacity)
				{
					nBeginWriteIndex -= this.Capacity;
				}

				if (bIsSpan)
				{
					mSegmentLengthQueue.Enqueue(nCount);
					Check();
				}
                else
                {
                    this.nTempSegmentLength += nCount;
                }
            }
			else
			{
				NetLog.LogError("环形缓冲区 写 溢出 " + this.Capacity + " | " + this.Length + " | " + nCount);
			}
		}

		public int WriteTo(Span<byte> readBuffer)
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

		public int WriteToMax(Span<byte> readBuffer)
		{
			int nLength = CopyToMax(readBuffer);
			ClearBuffer(nLength);
			Check();
			return nLength;
		}

		public int CopyToMax(Span<byte> readBuffer)
		{
			int nMaxLength = 0;
			foreach (int v in mSegmentLengthQueue)
			{
				int nNextLength = nMaxLength + v;
				if (nNextLength <= readBuffer.Length)
				{
					nMaxLength = nNextLength;
				}
				else
				{
					break;
				}
			}
			return InnerCopyTo(readBuffer, nMaxLength);
		}

        public int CopyTo(Span<byte> readBuffer)
		{
			return InnerCopyTo(readBuffer, CurrentSegmentLength);
		}

        private int InnerCopyTo(Span<byte> readBuffer, int nCopyLength)
		{
			int copyLength = nCopyLength;
			if (copyLength <= 0)
			{
				return 0;
			}
			else if (copyLength > readBuffer.Length)
			{
				NetLog.LogError($"InnerCopyTo Error: {copyLength} | {readBuffer.Length}");
				return 0;
			}

			int tempBeginIndex = nBeginReadIndex;
			if (tempBeginIndex + copyLength <= this.Capacity)
			{
                MemoryBuffer.Span.Slice(tempBeginIndex, copyLength).CopyTo(readBuffer);
			}
			else
			{
				int Length1 = this.Capacity - tempBeginIndex;
				int Length2 = copyLength - Length1;
                MemoryBuffer.Span.Slice(tempBeginIndex, Length1).CopyTo(readBuffer);
                MemoryBuffer.Span.Slice(0, Length2).CopyTo(readBuffer.Slice(Length1));
			}
			return copyLength;
		}

        public int ClearFirstBuffer()
		{
			if (CurrentSegmentLength > 0)
			{
                int readLength = mSegmentLengthQueue.Dequeue();
                if (readLength >= this.Length)
				{
					this.Reset();
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
				return readLength;
			}
			return 0;
		}

		public void ClearBuffer(int nClearLength)
		{
			while (nClearLength > 0)
			{
				nClearLength -= ClearFirstBuffer();
			}
			NetLog.Assert(nClearLength == 0);
		}
    }
}








