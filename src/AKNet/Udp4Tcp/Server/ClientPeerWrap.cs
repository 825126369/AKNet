/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:16
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;
using System.Net;
using System.Net.Sockets;

namespace AKNet.Udp4Tcp.Server
{
    internal class ClientPeerWrap : ClientPeerBase
	{
        private ClientPeer mInstance = null;
        private ServerMgr mNetServer;
        public ClientPeerWrap(ServerMgr mNetServer)
		{
            this.mNetServer = mNetServer;
            this.mInstance = mNetServer.GetClientPeerPool().Pop();
        }

        public void Reset()
        {
            mNetServer.GetClientPeerPool().recycle(mInstance);
            mNetServer = null;
            mInstance = null;
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

        public void CloseSocket()
        {
            if (mInstance != null)
            {
                mInstance.CloseSocket();
            }
        }


        public void SetName(string name)
        {
            mInstance.SetName(name);
        }

        public string GetName()
        {
            return mInstance.GetName();
        }

        public void SetID(uint id)
        {
            mInstance.SetID(id);
        }

        public uint GetID()
        {
            return mInstance.GetID();
        }
    }

}
