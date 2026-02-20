/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:52
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/

using AKNet.Common;
using AKNet.Udp5Tcp.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace AKNet.Udp5Tcp.Server
{
    internal partial class NetServerMain
    {
		public void InitNet()
		{
			List<int> mPortList = IPAddressHelper.GetAvailableUdpPortList();
			int nTryBindCount = 100;
			while (nTryBindCount-- > 0)
			{
				if (mPortList.Count > 0)
				{
					int nPort = mPortList[RandomTool.RandomArrayIndex(0, mPortList.Count)];
					InitNet(nPort);
					mPortList.Remove(nPort);
					if (GetServerState() == SOCKET_SERVER_STATE.NORMAL)
					{
						break;
					}
				}
			}

			if (GetServerState() != SOCKET_SERVER_STATE.NORMAL)
			{
				NetLog.LogError("Udp Server 自动查找可用端口 失败！！！");
			}
		}

        public void InitNet(int nPort)
        {
            InitNet(IPAddress.Any, nPort);
        }

        public void InitNet(string Ip, int nPort)
        {
            InitNet(IPAddress.Parse(Ip), nPort);
        }

		private void InitNet(IPAddress mIPAddress, int nPort)
		{
            try
            {
                mState = SOCKET_SERVER_STATE.NORMAL;
                this.nPort = nPort;

                EndPoint bindEndPoint = new IPEndPoint(mIPAddress, nPort);
                mListener.Bind(bindEndPoint);

                NetLog.Log($"{NetType.Udp5Tcp.ToString()} 服务器 初始化成功: {bindEndPoint}");
                StartAcceptEventArg();
            }
            catch (SocketException ex)
            {
                mState = SOCKET_SERVER_STATE.EXCEPTION;
                NetLog.LogError(ex.SocketErrorCode + " | " + ex.Message + " | " + ex.StackTrace);
                NetLog.LogError($"{NetType.Udp5Tcp.ToString()} 服务器 初始化失败: {mIPAddress} | {nPort}");
            }
            catch (Exception ex)
            {
                mState = SOCKET_SERVER_STATE.EXCEPTION;
                NetLog.LogError(ex.Message + " | " + ex.StackTrace);
                NetLog.LogError($"{NetType.Udp5Tcp.ToString()} 服务器 初始化失败: {mIPAddress} | {nPort}");
            }
        }

		public int GetPort()
		{
			return this.nPort;
		}

        public SOCKET_SERVER_STATE GetServerState()
        {
            return mState;
        }

        private async void StartAcceptEventArg()
        {
            while (mListener != null)
            {
                try
                {
                    var mAcceptConnection = await mListener.AcceptAsync();
                    if (!MultiThreadingHandleConnectedSocket(mAcceptConnection))
                    {
                        HandleConnectFull(mAcceptConnection);
                    }
                }
                catch (Exception e)
                {
                    if (mListener != null)
                    {
                        NetLog.LogException(e);
                    }
                }
            }
        }

        private void HandleConnectFull(Connection mClientSocket)
        {
            try
            {
                mClientSocket.Dispose();
            }
            catch
            {

            }
        }

        public void CloseSocket()
		{
			if (mListener != null)
			{
				Listener mSocket2 = mListener;
                mListener = null;

				try
				{
					mSocket2.Dispose();
				}
				catch (Exception) { }
			}
		}
	}

}









