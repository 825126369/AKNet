/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.QuicNet.Common;
using System.Net;

namespace AKNet.QuicNet.Client
{
    internal class QuicConnectionMgr
    {
        private readonly Memory<byte> mReceiveBuffer = new byte[1024];
        private readonly Memory<byte> mSendBuffer = new byte[1024];

        private readonly AkCircularBuffer mSendStreamList = new AkCircularBuffer();
        private bool bSendIOContextUsed = false;

        private QuicClient mQuicClient = null;
        private string ServerIp = "";
        private int nServerPort = 0;
        private IPEndPoint mIPEndPoint = null;
        private ClientPeer mClientPeer;
        private QuicStream mQuicStream;

        public QuicConnectionMgr(ClientPeer mClientPeer)
        {
            this.mClientPeer = mClientPeer;
            mClientPeer.SetSocketState(SOCKET_PEER_STATE.NONE);
        }

        public void ReConnectServer()
        {
            ConnectServer(this.ServerIp, this.nServerPort);
        }

        public void ConnectServer(string ServerAddr, int ServerPort)
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
                mQuicClient = new QuicClient();
                var mConnection = mQuicClient.Connect(ServerAddr, ServerPort);
                // Create a data stream
                mQuicStream = mConnection.CreateStream(StreamType.ClientBidirectional);
                Task.Run(()=>
                {
                    ReceiveStreamDataFunc(mQuicStream);
                });
            }
            catch (Exception e)
            {
                NetLog.LogError(e.ToString());
                mClientPeer.SetSocketState(SOCKET_PEER_STATE.RECONNECTING);
            }
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
            //mQuicClient.cl();
            NetLog.Log("客户端 主动 断开服务器 Finish......");
        }

        public async void ReceiveStreamDataFunc(QuicStream mQuicStream)
        {
            while(true)
            {
                await Task.CompletedTask;
                if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
                {
                    var mReceiveBuffer = mQuicStream.Receive();
                    if (mReceiveBuffer.Length > 0)
                    {
                       // mClientPeer.mMsgReceiveMgr.MultiThreadingReceiveSocketStream(mReceiveBuffer.Span.Slice(0, nLength));
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        public void Update(double elapsed)
        {

        }

        private void SendFinishFunc(QuicStream mQuicStream)
        {
            //if (mQuicStream == mSendQuicStream)
            //{
            //    SendNetStream2();
            //}
            //else
            //{
            //    NetLog.LogError("SendFinishFunc Error");
            //}
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
                SendNetStream2();
            }
        }

        private void SendNetStream2()
        {
            try
            {
                if (mSendStreamList.Length > 0)
                {
                    int nLength = 0;
                    lock (mSendStreamList)
                    {
                        nLength = mSendStreamList.WriteToMax(0, mSendBuffer.Span);
                    }
                    mQuicStream.Send(mSendBuffer.Slice(0, nLength).ToArray());
                }
                else
                {
                    bSendIOContextUsed = false;
                }
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
            //try
            //{
            //    if (mQuicClient != null && mQuicClient.en != null)
            //    {
            //        mRemoteEndPoint = mQuicClient.RemoteEndPoint as IPEndPoint;
            //    }
            //}
            //catch { }

            return mRemoteEndPoint;
        }

		private async void CloseSocket()
		{
            if (mQuicClient != null)
            {
                QuicClient mQuicConnection2 = mQuicClient;
                mQuicClient = null;
				//mQuicConnection2.();
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
