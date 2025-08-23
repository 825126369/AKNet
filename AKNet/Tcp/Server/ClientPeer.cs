/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;
using System.Net;
using System.Net.Sockets;

namespace AKNet.Tcp.Server
{
    internal class ClientPeer : ServerClientPeerInterface, ClientPeerBase
	{
        private string Name = null;
        private ClientPeer_Private mInstance = null;
        private TcpServer mNetServer;
        public ClientPeer(TcpServer mNetServer)
		{
            mInstance = mNetServer.mClientPeerPool.Pop();
        }

        public void Reset()
        {
            mNetServer.mClientPeerPool.recycle(mInstance);
            mNetServer = null;
            mInstance = null;
            Name = null;
        }

        public SOCKET_PEER_STATE GetSocketState()
		{
			if (mInstance != null)
			{
				return mInstance.GetSocketState();
			}
			else
			{
				return SOCKET_PEER_STATE.DISCONNECTED;
			}
		}

		public void Update(double elapsed)
		{
            mInstance.Update(elapsed);
        }

		public void SendNetData(ushort nPackageId)
		{
            mInstance.SendNetData(nPackageId);
        }

        public void SendNetData(ushort nPackageId, byte[] data)
        {
            mInstance.SendNetData(nPackageId, data);
        }

        public void SendNetData(NetPackage data)
        {
            mInstance.SendNetData(data);
        }

		public void SendNetData(ushort nPackageId, ReadOnlySpan<byte> data)
		{
            mInstance.SendNetData(nPackageId, data);
        }

		public void HandleConnectedSocket(Socket mSocket)
		{
            mInstance.HandleConnectedSocket(mSocket);
		}

        public IPEndPoint GetIPEndPoint()
        {
            return mInstance.GetIPEndPoint();
        }

        public void SetName(string Name)
        {
            this.Name = Name;
        }

        public string GetName()
        {
            return this.Name;
        }
    }

}
