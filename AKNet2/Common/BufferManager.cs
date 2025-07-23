/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Collections.Generic;
using System.Net.Sockets;

namespace AKNet.Common
{
    internal class BufferManager
	{
		readonly byte[] m_buffer;
		readonly Stack<int> m_freeIndexPool;
		readonly int nBufferSize = 0;
		readonly object lock_buffer_alloc_object = new object();
		
        int nReadIndex = 0;
        public BufferManager(int nBufferSize, int nCount)
		{
			this.nBufferSize = nBufferSize;
			this.m_freeIndexPool = new Stack<int>();
			int Length = nBufferSize * nCount;
			this.m_buffer = new byte[Length];

			this.nReadIndex = 0;
		}

		public bool SetBuffer(SocketAsyncEventArgs args)
		{
			lock (lock_buffer_alloc_object)
			{
				if (m_freeIndexPool.Count > 0)
				{
					args.SetBuffer(m_buffer, m_freeIndexPool.Pop(), nBufferSize);
				}
				else
				{
					if (nReadIndex + nBufferSize <= m_buffer.Length)
					{
						args.SetBuffer(m_buffer, nReadIndex, nBufferSize);
						nReadIndex += nBufferSize;
					}
					else
					{
						NetLog.LogWarning("BufferManager 缓冲区溢出");
						return false;
					}
				}
				return true;
			}
		}

		public void FreeBuffer(SocketAsyncEventArgs args)
		{
			lock (lock_buffer_alloc_object)
			{
				m_freeIndexPool.Push(args.Offset);
				args.SetBuffer(null, 0, 0);
			}
		}
	}
	
}