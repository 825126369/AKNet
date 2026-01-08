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
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
[assembly: InternalsVisibleTo("AKNet")]
[assembly: InternalsVisibleTo("AKNet.MSQuic")]
[assembly: InternalsVisibleTo("AKNet.LinuxTcp")]
[assembly: InternalsVisibleTo("AKNet.WebSocket")]
namespace AKNet.Common
{
	/// <summary>
	/// 循环Buffer，对于实现UDP的滑动窗口，TCP的流接受，以及UDP发送流的吞吐能力，都至关重要
	/// </summary>
    internal class AkCircularBuffer :IDisposable
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

        public void Reset()
		{
			dataLength = 0;
			nBeginReadIndex = 0;
			nBeginWriteIndex = 0;
		}

		public void Dispose()
		{
            MemoryBuffer = null;
            mBuffer = null;
            Reset();
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
				if (index >= this.Length) 
				{
					throw new Exception ("环形缓冲区异常，索引溢出");
				}

				if (nBeginReadIndex + index < this.Capacity)
				{
					return this.mBuffer [nBeginReadIndex + index];
				}
				else 
				{
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
				CopyTo(newBuffer);
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
						CopyTo(newBuffer);
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

        public int WriteToMax(int index, Span<byte> readBuffer)
		{
			int nReadLength = CopyToMax(index, readBuffer);
			this.ClearBuffer(index + nReadLength);
			return nReadLength;
		}

		public int CopyToMax(int index, Span<byte> readBuffer)
		{
			return CopyTo(index, readBuffer);
		}

        public int CopyTo(Span<byte> readBuffer)
		{
			return CopyTo(0, readBuffer);
		}

        public int CopyTo(int index, Span<byte> readBuffer)
		{
            if (index < 0)
            {
                throw new ArgumentException();
            }

            if (readBuffer.Length == 0 || dataLength == 0)
			{
				return 0;
			}

            int copyLength = readBuffer.Length;
            if (index + copyLength > dataLength)
            {
                copyLength = dataLength - index;
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

		public void ClearBuffer(int readLength)
		{
			if (readLength >= this.Length) 
			{
				this.Reset();
			}
			else 
			{
				dataLength -= readLength;
				nBeginReadIndex += readLength;
				if (nBeginReadIndex >= this.Capacity) {
					nBeginReadIndex -= this.Capacity;
				}
			}
		}
    }
}








