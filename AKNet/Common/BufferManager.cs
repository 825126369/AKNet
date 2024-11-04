/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/4 20:04:54
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Collections.Generic;
using System.Net.Sockets;

namespace AKNet.Common
{
    internal class BufferManager
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