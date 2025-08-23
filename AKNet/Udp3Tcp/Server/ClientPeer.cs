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

namespace AKNet.Udp3Tcp.Server
{
    internal class ClientPeer : ClientPeerBase
	{
        private string Name = null;
        private ClientPeerPrivate mInstance = null;
        private UdpServer mNetServer;
        public ClientPeer(UdpServer mNetServer)
		{
            this.mNetServer = mNetServer;
            this.mInstance = mNetServer.mClientPeerPool.Pop();
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
            if (mInstance != null)
            {
                mInstance.Update(elapsed);
            }
        }

		public void SendNetData(ushort nPackageId)
		{
            if (mInstance != null)
            {
                mInstance.SendNetData(nPackageId);
            }
        }

        public void SendNetData(ushort nPackageId, byte[] data)
        {
            if (mInstance != null)
            {
                mInstance.SendNetData(nPackageId, data);
            }
        }

        public void SendNetData(NetPackage data)
        {
            if (mInstance != null)
            {
                mInstance.SendNetData(data);
            }
        }

		public void SendNetData(ushort nPackageId, ReadOnlySpan<byte> data)
		{
            if (mInstance != null)
            {
                mInstance.SendNetData(nPackageId, data);
            }
        }

		public void HandleConnectedSocket(FakeSocket mSocket)
		{
            if (mInstance != null)
            {
                mInstance.HandleConnectedSocket(mSocket);
            }
		}

        public IPEndPoint GetIPEndPoint()
        {
            if (mInstance != null)
            {
                return mInstance.GetIPEndPoint();
            }
            return null;
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
