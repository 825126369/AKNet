/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:15
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/

using AKNet.Common;
using System.Net;
using System.Net.Quic;

namespace AKNet.Quic.Server
{
    internal class ClientPeerWrap : QuicClientPeerBase
	{
        private ClientPeer mInstance = null;
        private ServerMgr mServerMgr;

        public ClientPeerWrap(ServerMgr mNetServer)
		{
            this.mServerMgr = mNetServer;
            this.mInstance = mNetServer.mClientPeerPool.Pop();
        }

        public void Reset()
        {
            mServerMgr.mClientPeerPool.recycle(mInstance);
            mServerMgr = null;
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

		public void HandleConnectedSocket(QuicConnection mSocket)
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

        public void SendNetData(int nStreamIndex, ushort nPackageId)
        {
            mInstance.SendNetData(nStreamIndex, nPackageId);
        }

        public void SendNetData(int nStreamIndex, ushort nPackageId, byte[] data)
        {
            mInstance.SendNetData(nStreamIndex, nPackageId, data);
        }

        public void SendNetData(int nStreamIndex, ushort nPackageId, ReadOnlySpan<byte> buffer)
        {
            mInstance.SendNetData(nStreamIndex, nPackageId, buffer);
        }

        public void SendNetData(int nStreamIndex, NetPackage mNetPackage)
        {
            mInstance.SendNetData(nStreamIndex, mNetPackage);
        }
    }
}
