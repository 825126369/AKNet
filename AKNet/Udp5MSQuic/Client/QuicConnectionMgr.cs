/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp5MSQuic.Common;
using System;
using System.Net;
using System.Net.Security;

namespace AKNet.Udp5MSQuic.Client
{
    internal class QuicConnectionMgr
	{
        private readonly Memory<byte> mReceiveBuffer = new byte[1024];
        private readonly Memory<byte> mSendBuffer = new byte[1024];

        private readonly AkCircularBuffer mSendStreamList = new AkCircularBuffer();
        private bool bSendIOContextUsed = false;

        private QuicConnection mQuicConnection = null;
		private string ServerIp = "";
		private int nServerPort = 0;
		private IPEndPoint mIPEndPoint = null;
        private ClientPeer mClientPeer;
        private QuicStream mSendQuicStream;

        public QuicConnectionMgr(ClientPeer mClientPeer)
		{
			this.mClientPeer = mClientPeer;
            mClientPeer.SetSocketState(SOCKET_PEER_STATE.NONE);
        }

		public void ReConnectServer()
		{
            ConnectServer(this.ServerIp, this.nServerPort);
        }

		public async void ConnectServer(string ServerAddr, int ServerPort)
		{
			this.ServerIp = ServerAddr;
			this.nServerPort = ServerPort;

			mClientPeer.SetSocketState(SOCKET_PEER_STATE.CONNECTING);
			NetLog.Log("Client 正在连接服务器: " + this.ServerIp + " | " + this.nServerPort);

			Reset();
			
			IPAddress mIPAddress = IPAddress.Parse(ServerAddr);
			mIPEndPoint = new IPEndPoint(mIPAddress, ServerPort);
			
            try
            {
                mQuicConnection = QuicConnection.StartConnect(GetQuicClientConnectionOptions(mIPEndPoint));
            }
            catch (Exception e)
            {
                NetLog.LogError(e.ToString());
                mClientPeer.SetSocketState(SOCKET_PEER_STATE.RECONNECTING);
            }
		}

        private QuicConnectionOptions GetQuicClientConnectionOptions(IPEndPoint mIPEndPoint)
        {
            QuicConnectionOptions mOption = new QuicConnectionOptions();
            mOption.RemoteEndPoint = mIPEndPoint;
            mOption.ClientAuthenticationOptions = new SslClientAuthenticationOptions();
            mOption.ClientAuthenticationOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            mOption.ConnectFinishFunc = ConnectFinishFunc;
            mOption.ReceiveStreamDataFunc = ReceiveStreamDataFunc;
            mOption.CloseFinishFunc = CloseFinishFunc;
            return mOption;
        }

        private void ConnectFinishFunc()
        {
            NetLog.Log("Client 连接服务器成功: " + this.ServerIp + " | " + this.nServerPort);
            mClientPeer.SetSocketState(SOCKET_PEER_STATE.CONNECTED);
            mSendQuicStream = mQuicConnection.OpenSendStream(QuicStreamType.Unidirectional);
            mQuicConnection.RequestReceiveStreamData();
        }

        private void CloseFinishFunc()
        {
            NetLog.Log("客户端 主动 断开服务器 Finish......");
        }

        public bool DisConnectServer()
		{
			MainThreadCheck.Check();
            DisConnectServer2();
            return true;
		}

        private void DisConnectServer2()
        {
            NetLog.Log("客户端 主动 断开服务器 Begin......");
            mQuicConnection.StartClose();
            NetLog.Log("客户端 主动 断开服务器 Finish......");
        }

        private void ReceiveStreamDataFunc(QuicStream mQuicStream)
        {
            if (mQuicStream != null)
            {
                int nLength = mQuicStream.Read(mReceiveBuffer);
                if (nLength > 0)
                {
                    mClientPeer.mMsgReceiveMgr.MultiThreadingReceiveSocketStream(mReceiveBuffer.Span.Slice(0, nLength));
                }
            }
        }
        
        public void SendNetStream(ReadOnlyMemory<byte> mBufferSegment)
        {
            lock (mSendStreamList)
            {
                mSendStreamList.WriteFrom(mBufferSegment.Span);
            }

            if (!bSendIOContextUsed)
            {
                bSendIOContextUsed = true;
                SendNetStream2();
            }
        }

        private void SendNetStream2()
        {
            try
            {
                while(mSendStreamList.Length > 0)
                {
                    int nLength = 0;
                    lock (mSendStreamList)
                    {
                        nLength = mSendStreamList.WriteToMax(0, mSendBuffer.Span);
                    }
                    mSendQuicStream.Send(mSendBuffer.Slice(0, nLength));
                }
                bSendIOContextUsed = false;
            }
            catch (Exception e)
            {
                NetLog.LogError(e.ToString());
                mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
            }
        }

		public IPEndPoint GetIPEndPoint()
		{
            IPEndPoint mRemoteEndPoint = null;
            try
            {
                if (mQuicConnection != null && mQuicConnection.RemoteEndPoint != null)
                {
                    mRemoteEndPoint = mQuicConnection.RemoteEndPoint as IPEndPoint;
                }
            }
            catch { }

            return mRemoteEndPoint;
        }

		private async void CloseSocket()
		{
            if (mQuicConnection != null)
            {
                QuicConnection mQuicConnection2 = mQuicConnection;
                mQuicConnection = null;
				mQuicConnection2.StartClose();
            }
        }

		public void Reset()
		{
            CloseSocket();
        }

		public void Release()
		{
            CloseSocket();
        }
    }
}
