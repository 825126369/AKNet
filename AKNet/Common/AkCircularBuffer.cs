﻿/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/7 21:38:41
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Text;

namespace AKNet.Common
{
    /// <summary>
    /// 适用于 频繁的修改数组
    /// </summary>
    public class AkCircularBuffer<T>
	{
		private T[] Buffer = null;
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
		}

		public void release()
		{
			Buffer = null;
			this.reset ();
		}

		public int Capacity
		{
			get {
				return this.Buffer.Length;
			}
		}

		public int Length
		{
			get {
				return this.dataLength;
			}
		}

		public T this [int index] {
			get {
				if (index >= this.Length) {
					throw new Exception ("环形缓冲区异常，索引溢出");
				}
				if (nBeginReadIndex + index < this.Capacity) {
					return this.Buffer [nBeginReadIndex + index];
				} else {
					return this.Buffer [nBeginReadIndex + index - this.Capacity];
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

				T[] newBuffer = new T[newSize];
				WriteTo(0, newBuffer, 0, nOriLength);
				this.Buffer = newBuffer;
				this.nBeginReadIndex = 0;
				this.nBeginWriteIndex = nOriLength;
				this.dataLength = nOriLength;

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
						WriteTo(0, newBuffer, 0, nOriLength);
						this.Buffer = newBuffer;
						this.nBeginReadIndex = 0;
						this.nBeginWriteIndex = nOriLength;
						this.dataLength = nOriLength;

						NetLog.LogWarning("EnSureCapacityOk MinusTo Size: " + Capacity);
					}
				}
			}
		}

        public int WriteFrom(ReadOnlySpan<T> readOnlySpan)
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
            }
            else
            {
                NetLog.LogError("环形缓冲区 写 溢出 " + this.Capacity + " | " + this.Length + " | " + readOnlySpan.Length);
                return -1;
            }

            return readOnlySpan.Length;
        }

        public int WriteFrom(T[] writeBuffer, int offset, int count)
		{
			if (writeBuffer.Length < count)
			{
				count = writeBuffer.Length;
			}

			if (count <= 0)
			{
				return count;
			}

            EnSureCapacityOk(count);
            if (isCanWriteFrom(count))
			{
				if (nBeginWriteIndex + count <= this.Capacity)
				{
					Array.Copy(writeBuffer, offset, this.Buffer, nBeginWriteIndex, count);
				}
				else
				{
					int Length1 = this.Buffer.Length - nBeginWriteIndex;
					int Length2 = count - Length1;
					Array.Copy(writeBuffer, offset, this.Buffer, nBeginWriteIndex, Length1);
					Array.Copy(writeBuffer, offset + Length1, this.Buffer, 0, Length2);
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

		public int WriteFrom (AkCircularBuffer<T> writeBuffer, int count)
		{
			if (writeBuffer.Length < count) {
				count = writeBuffer.Length;
			}

			if (count <= 0) {
				return 0;
			}

            EnSureCapacityOk(count);
            if (isCanWriteFrom (count)) {                          
				if (nBeginWriteIndex + count <= this.Capacity) {
					for (int i = 0; i < count; i++) {
						this.Buffer [nBeginWriteIndex + i] = writeBuffer [i];
					}
				} else {    
					int Length1 = this.Capacity - nBeginWriteIndex;
					int Length2 = count - Length1;

					for (int i = 0; i < Length1; i++) {
						this.Buffer [nBeginWriteIndex + i] = writeBuffer [i];
					}
						
					for (int i = 0; i < Length2; i++) {
						this.Buffer [i] = writeBuffer [Length1 + i];
					}
				}

				dataLength += count;
				nBeginWriteIndex += count;
				if (nBeginWriteIndex >= this.Capacity) {
					nBeginWriteIndex -= this.Capacity;
				}

				writeBuffer.ClearBuffer (count);
			} else {
                NetLog.LogError("环形缓冲区 写 溢出 " + this.Capacity + " | " + this.Length + " | " + count);
                return -1;
			}

			return count;
		}

		public int WriteTo(int index, T[] readBuffer, int offset, int count)
		{
			if (isCanWriteTo(count))
			{
				int readCount = CopyTo(index, readBuffer, offset, count);
				this.ClearBuffer(index + count);
				return readCount;
			}
			else
			{
				NetLog.LogError("WriteTo Failed : " + count);
				return 0;
			}
		}

		public int CopyTo(int index, T[] readBuffer, int offset, int copyLength)
		{
			if (copyLength > this.Length - index)
			{
				copyLength = this.Length - index;
			}

			if (copyLength <= 0)
			{
				return 0;
			}

			int tempBeginIndex = nBeginReadIndex + index;
			if (tempBeginIndex >= Capacity)
			{
				tempBeginIndex = tempBeginIndex - Capacity;
			}

			if (tempBeginIndex + copyLength <= this.Capacity)
			{
				Array.Copy(this.Buffer, tempBeginIndex, readBuffer, offset, copyLength);
			}
			else
			{
				int Length1 = this.Capacity - tempBeginIndex;
				int Length2 = copyLength - Length1;
				Array.Copy(this.Buffer, tempBeginIndex, readBuffer, offset, Length1);
				Array.Copy(this.Buffer, 0, readBuffer, offset + Length1, Length2);
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

		public void PrintBasicInfo()
		{
			NetLog.Log(this.ToString());
		}

		public void PrintBuffer()
		{
			NetLog.Log (this.ToString ());
		}

		public override string ToString ()
		{
			StringBuilder aaStr = new StringBuilder ();
			aaStr.Append ("<color=red>");
			aaStr.Append (this.GetType ().Name + ": ");
			aaStr.Append ("</color>");
			aaStr.Append ("<color=yellow>");
			for (int i = 0; i < Length; i++) {
				aaStr.Append (this [i] + " | ");
			}
			aaStr.Append ("</color>");
			return aaStr.ToString ();
		}
	}
}







