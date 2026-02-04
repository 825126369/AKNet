/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:47
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;
using System.Net;
using System.Net.Sockets;

namespace AKNet.Tcp.Server
{
	internal partial class ClientPeer
	{
		public void HandleConnectedSocket(Socket otherSocket)
		{
			MainThreadCheck.Check();

			this.mSocket = otherSocket;
			SetSocketState(SOCKET_PEER_STATE.CONNECTED);
			bSendIOContextUsed = false;

			StartReceiveEventArg();
		}

		private void StartReceiveEventArg()
		{
			bool bIOSyncCompleted = false;

			if (mSocket != null)
			{
				try
				{
					bIOSyncCompleted = !mSocket.ReceiveAsync(mReceiveIOContex);
				}
				catch (Exception e)
				{
					DisConnectedWithException(e);
				}
			}
			
			if (bIOSyncCompleted)
			{
				this.ProcessReceive(mReceiveIOContex);
			}

		}

		private void StartSendEventArg()
		{
			bool bIOSyncCompleted = false;
			if (mSocket != null)
			{
				try
				{
					bIOSyncCompleted = !mSocket.SendAsync(mSendIOContex);
				}
				catch (Exception e)
				{
					bSendIOContextUsed = false;
					DisConnectedWithException(e);
				}
			}
			else
			{
				bSendIOContextUsed = false;
			}
			
			if (bIOSyncCompleted)
			{
				this.ProcessSend(mSendIOContex);
			}
		}

		public IPEndPoint GetIPEndPoint()
		{
			IPEndPoint mRemoteEndPoint = null;
			try
			{
				if (mSocket != null && mSocket.RemoteEndPoint != null)
				{
					mRemoteEndPoint = mSocket.RemoteEndPoint as IPEndPoint;
				}
			}
			catch { }
			return mRemoteEndPoint;
		}

		private void OnIOCompleted(object sender, SocketAsyncEventArgs e)
		{
			switch (e.LastOperation)
			{
				case SocketAsyncOperation.Receive:
					this.ProcessReceive(e);
					break;
				case SocketAsyncOperation.Send:
					this.ProcessSend(e);
					break;
				default:
					throw new ArgumentException("The last operation completed on the socket was not a receive or send");
			}
		}

		private void ProcessReceive(SocketAsyncEventArgs e)
		{
			if (e.SocketError == SocketError.Success)
			{
				if (e.BytesTransferred > 0)
				{
					MultiThreadingReceiveSocketStream(e);
					StartReceiveEventArg();
				}
				else
				{
					DisConnectedWithNormal();
				}
			}
			else
			{
				DisConnectedWithSocketError(e.SocketError);
			}
		}

		private void ProcessSend(SocketAsyncEventArgs e)
		{
			if (e.SocketError == SocketError.Success)
			{
				if (e.BytesTransferred > 0)
				{
					SendNetStream1(e.BytesTransferred);
				}
				else
				{
					DisConnectedWithNormal();
					bSendIOContextUsed = false;
				}
			}
			else
			{
				DisConnectedWithSocketError(e.SocketError);
				bSendIOContextUsed = false;
			}
		}

		public void SendNetStream(ReadOnlySpan<byte> mBufferSegment)
		{
			lock (mSendStreamList)
			{
				mSendStreamList.WriteFrom(mBufferSegment);
			}

			if (!bSendIOContextUsed)
			{
				bSendIOContextUsed = true;
				SendNetStream1();
			}
			else
			{
                if (!bSendIOContextUsed && mSendStreamList.Length > 0)
                {
                    throw new Exception("SendNetStream 有数据, 但发送不了啊");
                }
            }
		}

		private void SendNetStream1(int BytesTransferred = 0)
		{
			if (BytesTransferred > 0)
			{
				lock (mSendStreamList)
				{
					mSendStreamList.ClearBuffer(BytesTransferred);
				}
			}

			int nLength = mSendStreamList.Length;
			if (nLength > 0)
			{
				nLength = Math.Min(mSendIOContex.MemoryBuffer.Length, nLength);
				lock (mSendStreamList)
				{
					mSendStreamList.CopyTo(mSendIOContex.MemoryBuffer.Span.Slice(0, nLength));
				}
				mSendIOContex.SetBuffer(0, nLength);
				StartSendEventArg();
			}
			else
			{
				bSendIOContextUsed = false;
			}
		}

		private void DisConnectedWithNormal()
		{
			SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
		}

		private void DisConnectedWithException(Exception e)
		{
			SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
		}

		private void DisConnectedWithSocketError(SocketError mError)
		{
			SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
		}

		void CloseSocket()
		{
			if (mSocket != null)
			{
				Socket mSocket2 = mSocket;
				mSocket = null;

				try
				{
					mSocket2.Shutdown(SocketShutdown.Both);
				}
				catch { }
				finally
				{
					mSocket2.Close();
				}
			}
		}

	}

}