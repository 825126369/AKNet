/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/28 7:14:04
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;

namespace AKNet.Common
{
    /// <summary>
    /// 适用于 频繁的修改数组
    /// </summary>
    internal class AkCircularBuffer
	{
		private byte[] mBuffer = null;
		private Memory<byte> MemoryBuffer = null;
		private int dataLength;
		private int nBeginReadIndex;
		private int nBeginWriteIndex;
		private int nMaxCapacity = 0;

		public AkCircularBuffer(int initCapacity = 1024 * 8, int nMaxCapacity = 0)
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
		}

		public void release()
		{
			MemoryBuffer = null;
			mBuffer = null;
			this.reset ();
		}

		public int Capacity
		{
			get {
				return this.mBuffer.Length;
			}
		}

		public int Length
		{
			get {
				return this.dataLength;
			}
		}

		public byte this [int index] {
			get {
				if (index >= this.Length) {
					throw new Exception ("环形缓冲区异常，索引溢出");
				}
				if (nBeginReadIndex + index < this.Capacity) {
					return this.mBuffer [nBeginReadIndex + index];
				} else {
					return this.mBuffer [nBeginReadIndex + index - this.Capacity];
				}
			}
		}

		public bool isCanWriteFrom(int countT)
		{
			return this.Capacity - this.Length >= countT;
        }

		public bool isCanWriteTo(int countT)
		{
			return this.Length >= countT;
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
				CopyTo(0, newBuffer, 0, nOriLength);
				this.mBuffer = newBuffer;
				this.MemoryBuffer = this.mBuffer;
                this.nBeginReadIndex = 0;
				this.nBeginWriteIndex = nOriLength;
				this.dataLength = nOriLength;

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
						CopyTo(0, newBuffer, 0, nOriLength);
						this.mBuffer = newBuffer;
                        this.MemoryBuffer = this.mBuffer;
                        this.nBeginReadIndex = 0;
						this.nBeginWriteIndex = nOriLength;
						this.dataLength = nOriLength;

#if DEBUG
                        //NetLog.LogWarning("EnSureCapacityOk MinusTo Size: " + Capacity);
#endif
					}
				}
			}
		}

		public int WriteFrom(ReadOnlySpan<byte> readOnlySpan)
		{
			if (readOnlySpan.Length <= 0)
			{
				return readOnlySpan.Length;
			}

			EnSureCapacityOk(readOnlySpan.Length);
			if (isCanWriteFrom(readOnlySpan.Length))
			{
				if (nBeginWriteIndex + readOnlySpan.Length <= this.Capacity)
				{
					readOnlySpan.CopyTo(this.MemoryBuffer.Span.Slice(nBeginWriteIndex));
				}
				else
				{
					int Length1 = this.mBuffer.Length - nBeginWriteIndex;
					readOnlySpan.Slice(0, Length1).CopyTo(this.MemoryBuffer.Span.Slice(nBeginWriteIndex));
					readOnlySpan.Slice(Length1).CopyTo(this.MemoryBuffer.Span);
				}

				dataLength += readOnlySpan.Length;
				nBeginWriteIndex += readOnlySpan.Length;
				if (nBeginWriteIndex >= this.Capacity)
				{
					nBeginWriteIndex -= this.Capacity;
				}
			}
			else
			{
				NetLog.LogError("环形缓冲区 写 溢出 " + this.Capacity + " | " + this.Length + " | " + readOnlySpan.Length);
				return -1;
			}
			return readOnlySpan.Length;
		}

        public int WriteFrom(byte[] writeBuffer, int offset, int count)
		{
			if (writeBuffer.Length < count)
			{
                NetLog.LogError($"WriteFrom Error： {writeBuffer.Length}-{count}");
				return 0;
            }
			else if (count <= 0)
			{
				return 0;
			}

            EnSureCapacityOk(count);
            if (isCanWriteFrom(count))
			{
				if (nBeginWriteIndex + count <= this.Capacity)
				{
                    Buffer.BlockCopy(writeBuffer, offset, this.mBuffer, nBeginWriteIndex, count);
				}
				else
				{
					int Length1 = this.mBuffer.Length - nBeginWriteIndex;
					int Length2 = count - Length1;
					Buffer.BlockCopy(writeBuffer, offset, this.mBuffer, nBeginWriteIndex, Length1);
                    Buffer.BlockCopy(writeBuffer, offset + Length1, this.mBuffer, 0, Length2);
				}

				dataLength += count;
				nBeginWriteIndex += count;
				if (nBeginWriteIndex >= this.Capacity)
				{
					nBeginWriteIndex -= this.Capacity;
				}
			}
			else
			{
				NetLog.LogError("环形缓冲区 写 溢出 " + this.Capacity + " | " + this.Length + " | " + count);
				return -1;
			}

			return count;
		}

		public void WriteTo(int index, Span<byte> readBuffer)
		{
			int count = readBuffer.Length;
			if (isCanWriteTo(count))
			{
				CopyTo(index, readBuffer);
				this.ClearBuffer(index + count);
			}
			else
			{
				NetLog.LogError("WriteTo Failed : " + count);
			}
		}

        public void WriteTo(int index, byte[] readBuffer, int offset, int count)
        {
            if (isCanWriteTo(count))
            {
                CopyTo(index, readBuffer, offset, count);
                this.ClearBuffer(index + count);
            }
            else
            {
                NetLog.LogError("WriteTo Failed : " + count);
            }
        }

        public int WriteToMax(int index, Span<byte> readBuffer)
		{
			int nReadLength = CopyToMax(index, readBuffer);
			this.ClearBuffer(index + nReadLength);
			return nReadLength;
		}

        public int WriteToMax(int index, byte[] readBuffer, int offset, int count)
		{
			int nReadLength = CopyToMax(index, readBuffer, offset, count);
			this.ClearBuffer(index + nReadLength);
			return nReadLength;
		}

		public int CopyToMax(int index, Span<byte> readBuffer)
		{
			int copyLength = readBuffer.Length;
			if (index + copyLength > dataLength)
			{
                copyLength = dataLength - index;
			}
			return CopyTo(index, readBuffer, copyLength);
		}

        public int CopyToMax(int index, byte[] readBuffer, int offset, int count)
        {
            if (index + count > dataLength)
            {
                count = dataLength - index;
            }
            return CopyTo(index, readBuffer, offset, count);
        }

        public int CopyTo(int index, Span<byte> readBuffer, int copyLength = 0)
		{
            if (copyLength == 0)
            {
                copyLength = readBuffer.Length;
            }

			if (copyLength <= 0 || dataLength <= 0)
			{
				return 0;
			}
			else if (copyLength > dataLength)
			{
				NetLog.LogError($"CopyTo Error: {copyLength}-{Length}");
				return 0;
			}

			int tempBeginIndex = nBeginReadIndex + index;
			if (tempBeginIndex >= Capacity)
			{
				tempBeginIndex = tempBeginIndex - Capacity;
			}

			if (tempBeginIndex + copyLength <= this.Capacity)
			{
				this.MemoryBuffer.Span.Slice(tempBeginIndex, copyLength).CopyTo(readBuffer);
			}
			else
			{
				int Length1 = this.Capacity - tempBeginIndex;
				int Length2 = copyLength - Length1;
				this.MemoryBuffer.Span.Slice(tempBeginIndex, Length1).CopyTo(readBuffer);
				this.MemoryBuffer.Span.Slice(0, Length2).CopyTo(readBuffer.Slice(Length1));
			}
			return copyLength;
		}

		public int CopyTo(int index, byte[] readBuffer, int offset, int copyLength)
		{
			if (copyLength <= 0)
			{
				return 0;
			}
            else if (copyLength > Length)
            {
                NetLog.LogError($"CopyTo Error: {copyLength}-{Length}");
                return 0;
            }

            int tempBeginIndex = nBeginReadIndex + index;
			if (tempBeginIndex >= Capacity)
			{
				tempBeginIndex = tempBeginIndex - Capacity;
			}

			if (tempBeginIndex + copyLength <= this.Capacity)
			{
				Buffer.BlockCopy(this.mBuffer, tempBeginIndex, readBuffer, offset, copyLength);
			}
			else
			{
				int Length1 = this.Capacity - tempBeginIndex;
				int Length2 = copyLength - Length1;
				Buffer.BlockCopy(this.mBuffer, tempBeginIndex, readBuffer, offset, Length1);
				Buffer.BlockCopy(this.mBuffer, 0, readBuffer, offset + Length1, Length2);
			}

			return copyLength;
		}

		public void ClearBuffer (int readLength)
		{
			if (readLength >= this.Length) {
				this.reset ();
			} else {
				dataLength -= readLength;
				nBeginReadIndex += readLength;
				if (nBeginReadIndex >= this.Capacity) {
					nBeginReadIndex -= this.Capacity;
				}
			}
		}
	}
}








