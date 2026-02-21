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

namespace AKNet.Tcp.Client
{
    internal partial class NetClientMain
    {
		public void ReConnectServer()
		{
			bool Connected = false;

			try
			{
				Connected = mSocket != null && mSocket.Connected;
			}
			catch { }
			
			if (Connected)
			{
				SetSocketState(SOCKET_PEER_STATE.CONNECTED);
			}
			else
			{
				ConnectServer(this.ServerIp, this.nServerPort);
			}
		}

		public void ConnectServer(string ServerAddr, int ServerPort)
		{
            Reset();
            this.ServerIp = ServerAddr;
			this.nServerPort = ServerPort;

			SetSocketState(SOCKET_PEER_STATE.CONNECTING);
            mSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            mSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            mSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, int.MaxValue);
            mSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, int.MaxValue);

            if (mIPEndPoint == null)
			{
				IPAddress mIPAddress = IPAddress.Parse(ServerAddr);
				mIPEndPoint = new IPEndPoint(mIPAddress, ServerPort);
			}

			if (!bConnectIOContexUsed)
			{
				bConnectIOContexUsed = true;
				mConnectIOContex.RemoteEndPoint = mIPEndPoint;

                NetLog.Log($"{NetType.TCP.ToString()} 客户端 正在连接服务器: {mIPEndPoint}");
                StartConnectEventArg();
			}
		}

		public bool DisConnectServer()
		{
			NetLog.Log("客户端 主动 断开服务器 Begin......");
			MainThreadCheck.Check();

			bool Connected = false;
			try
			{
				Connected = mSocket != null && mSocket.Connected;
			}
			catch { }

			if (Connected)
			{
				SetSocketState(SOCKET_PEER_STATE.DISCONNECTING);
				mDisConnectIOContex.RemoteEndPoint = mIPEndPoint;
				if (!bDisConnectIOContexUsed)
				{
					bDisConnectIOContexUsed = true;
					StartDisconnectEventArg();
				}
			}
			else
			{
				NetLog.Log("客户端 主动 断开服务器 Finish......");
				SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
				bDisConnectIOContexUsed = false;

			}
			
			return GetSocketState() == SOCKET_PEER_STATE.DISCONNECTED;
		}

		private void StartConnectEventArg()
		{
			bool bIOSyncCompleted = false;

			if (mSocket != null)
			{
				try
				{
					bIOSyncCompleted = !mSocket.ConnectAsync(mConnectIOContex);
				}
				catch (Exception e)
				{
					bConnectIOContexUsed = false;
					DisConnectedWithException(e);
				}
			}
			else
			{
				bConnectIOContexUsed = false;
			}
			
			if (bIOSyncCompleted)
			{
				this.ProcessConnect(mConnectIOContex);
			}
		}

		private void StartDisconnectEventArg()
		{
			bool bIOSyncCompleted = false;
			if (mSocket != null)
			{
				try
				{
					bIOSyncCompleted = !mSocket.DisconnectAsync(mDisConnectIOContex);
				}
				catch (Exception e)
				{
					bDisConnectIOContexUsed = false;
					DisConnectedWithException(e);
				}
			}
			else
			{
				bDisConnectIOContexUsed = false;
			}
			
			if (bIOSyncCompleted)
			{
				this.ProcessDisconnect(mDisConnectIOContex);
			}
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
					bReceiveIOContextUsed = false;
					DisConnectedWithException(e);
				}
			}
			else
			{
				bReceiveIOContextUsed = false;
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

        private void OnIOCompleted(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Connect:
                    ProcessConnect(e);
                    break;
                case SocketAsyncOperation.Disconnect:
                    ProcessDisconnect(e);
                    break;
                case SocketAsyncOperation.Receive:
                    this.ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    this.ProcessSend(e);
                    break;
                default:
                    NetLog.LogError("The last operation completed on the socket was not a receive or send");
                    break;
            }
        }

		private void ProcessConnect(SocketAsyncEventArgs e)
		{
			if (e.SocketError == SocketError.Success)
			{
				NetLog.Log($"{NetType.TCP.ToString()} 客户端 连接服务器: {mIPEndPoint} 成功");
				SetSocketState(SOCKET_PEER_STATE.CONNECTED);

				if (!bReceiveIOContextUsed)
				{
					bReceiveIOContextUsed = true;
					StartReceiveEventArg();
				}
			}
			else
			{
                if (mConfigInstance.bAutoReConnect)
                {
                    SetSocketState(SOCKET_PEER_STATE.RECONNECTING);
                }
                else
                {
                    SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
                }
                NetLog.LogError($"{NetType.TCP.ToString()} 客户端 连接服务器: {mIPEndPoint} 失败：{e.SocketError}");
			}

			e.RemoteEndPoint = null;
			bConnectIOContexUsed = false;
		}

        private void ProcessDisconnect(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
                NetLog.Log("客户端 主动 断开服务器 Finish");
            }
            else
            {
                DisConnectedWithSocketError(e.SocketError);
            }

            e.RemoteEndPoint = null;
            bDisConnectIOContexUsed = false;
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
					bReceiveIOContextUsed = false;
					DisConnectedWithNormal();
				}
			}
			else
			{
                bReceiveIOContextUsed = false;
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
            ResetSendHeartBeatTime();

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
#if DEBUG
			NetLog.Log("客户端 正常 断开服务器 ");
#endif
			DisConnectedWithError();
        }

		private void DisConnectedWithException(Exception e)
		{
#if DEBUG
            if (mSocket != null)
			{
				NetLog.LogException(e);
			}
#endif
			DisConnectedWithError();
		}

        private void DisConnectedWithSocketError(SocketError mError)
		{
#if DEBUG
            NetLog.LogError(mError);
#endif
			DisConnectedWithError();
        }

        private void DisConnectedWithError()
        {
            var mSocketPeerState = GetSocketState();
            if (mSocketPeerState == SOCKET_PEER_STATE.DISCONNECTING)
            {
                SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
            }
            else if (mSocketPeerState == SOCKET_PEER_STATE.CONNECTED)
            {
                if (mConfigInstance.bAutoReConnect)
                {
                    SetSocketState(SOCKET_PEER_STATE.RECONNECTING);
                }
                else
                {
                    SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
                }
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

		private void CloseSocket()
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
