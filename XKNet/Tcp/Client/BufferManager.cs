using System;
using System.Collections.Generic;
using System.Net.Sockets;
using XKNet.Common;


namespace XKNet.Tcp.Client
{
    internal class BufferManager
	{
		byte[] m_buffer;
		Stack<int> m_freeIndexPool;
		int nReadIndex = 0;
		int nBufferSize = 0;

		public BufferManager(int nBufferSize, int nCount)
		{
			int Length = nBufferSize * nCount;
			this.m_buffer = new byte[Length];

			this.nReadIndex = 0;
			this.m_freeIndexPool = new Stack<int>();
			this.nBufferSize = nBufferSize;
		}

		public bool SetBuffer(SocketAsyncEventArgs args)
		{
			if (m_freeIndexPool.Count > 0)
			{
				args.SetBuffer(m_buffer, m_freeIndexPool.Pop(), nBufferSize);
			}
			else
			{
				NetLog.Assert(nReadIndex + nBufferSize <= m_buffer.Length, "缓冲区溢出");
				args.SetBuffer(m_buffer, nReadIndex, nBufferSize);
				nReadIndex += nBufferSize;
			}
			return true;
		}

		public bool SetBuffer(ref ArraySegment<byte> args)
		{
			if (m_freeIndexPool.Count > 0)
			{
				args = new ArraySegment<byte>(m_buffer, m_freeIndexPool.Pop(), nBufferSize);
			}
			else
			{
				NetLog.Assert(nReadIndex + nBufferSize <= m_buffer.Length, "缓冲区溢出");
				args = new ArraySegment<byte>(m_buffer, nReadIndex, nBufferSize);
				nReadIndex += nBufferSize;
			}
			return true;
		}
	}
}