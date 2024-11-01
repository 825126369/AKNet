﻿/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:AKNet 网络库, 兼容 C#8.0 和 .Net Standard 2.1
*        Author:阿珂
*        CreateTime:2024/10/30 21:55:39
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Text;

namespace AKNet.Common
{
    /// <summary>
    /// 适用于 频繁的修改数组
    /// </summary>
    internal class CircularBuffer<T>
	{
		private T[] Buffer = null;
		private int dataLength;
		private int nBeginReadIndex;
		private int nBeginWriteIndex;

		public CircularBuffer (int Capacity)
		{
			nBeginReadIndex = 0;
			nBeginWriteIndex = 0;
			dataLength = 0;
			Buffer = new T[Capacity];
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
			if (this.Capacity - this.Length >= countT) {      
				return true;
			} else {
				return false;
			}
		}

        public int WriteFrom(ReadOnlySpan<T> readOnlySpan)
        {
            if (readOnlySpan.Length <= 0)
            {
                return readOnlySpan.Length;
            }

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
                NetLog.LogWarning("环形缓冲区 写 溢出 " + this.Capacity + " | " + this.Length + " | " + readOnlySpan.Length);
                NetLog.LogWarning("环形缓冲区 写入失败");
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

		public int WriteFrom (CircularBuffer<T> writeBuffer, int count)
		{
			if (writeBuffer.Length < count) {
				count = writeBuffer.Length;
			}

			if (count <= 0) {
				return 0;
			}

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
				NetLog.LogWarning ("环形缓冲区 写 溢出");
				return  -1;
			}

			return count;
		}

		public int WriteTo (int index, T[] readBuffer, int offset, int count)
		{
			int readCount = CopyTo (index, readBuffer, offset, count);
			this.ClearBuffer (index + count);
			return readCount;
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








