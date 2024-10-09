using System.Collections.Generic;
using System.Net.Sockets;
using XKNetCommon;

namespace XKNetTcpServer
{
    public class BufferManager
	{
		byte[] m_buffer;
		Stack<int> m_freeIndexPool;
		int nReadIndex = 0;
		int nBufferSize = 0;

		public BufferManager(int nBufferSize, int nCount)
		{
			this.nBufferSize = nBufferSize;
			this.nReadIndex = 0;
			this.m_freeIndexPool = new Stack<int>();

			int Length = nBufferSize * nCount;
			this.m_buffer = new byte[Length];
		}

		public bool SetBuffer(SocketAsyncEventArgs args)
		{
			if (m_freeIndexPool.Count > 0)
			{
				args.SetBuffer(m_buffer, m_freeIndexPool.Pop(), nBufferSize);
			}
			else
			{
				NetLog.Assert(nReadIndex + nBufferSize <= m_buffer.Length, "BufferManager 缓冲区溢出");
				args.SetBuffer(m_buffer, nReadIndex, nBufferSize);
				nReadIndex += nBufferSize;
			}
			return true;
		}

		public void FreeBuffer(SocketAsyncEventArgs args)
		{
			m_freeIndexPool.Push(args.Offset);
			args.SetBuffer(null, 0, 0);
		}
	}
	
}