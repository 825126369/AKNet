using Google.Protobuf;
using System;
using XKNet.Common;
using XKNet.Udp.POINTTOPOINT.Common;

namespace XKNet.Udp.POINTTOPOINT.Client
{
	public class UdpNetClientMain:ClientPeerBase
	{
        private ClientPeer mNetClientPeer;

        public UdpNetClientMain()
        {
            this.mNetClientPeer = new ClientPeer();
        }

        public void addNetListenFun(ushort nPackageId, Action<ClientPeerBase, NetPackage> fun)
        {
            this.mNetClientPeer.addNetListenFun(nPackageId, fun);
        }

        public void ConnectServer(string Ip, ushort nPort)
        {
            this.mNetClientPeer.ConnectServer(Ip, nPort);
        }

        public bool DisConnectServer()
        {
            return this.mNetClientPeer.DisConnectServer();
        }

        public CLIENT_SOCKET_PEER_STATE GetSocketState()
        {
            return this.mNetClientPeer.GetSocketState();
        }

        public void ReConnectServer()
        {
             mNetClientPeer.ReConnectServer();
        }

        public void Release()
        {
            mNetClientPeer.Release();
        }

        public void removeNetListenFun(ushort nPackageId, Action<ClientPeerBase, NetPackage> fun)
        {
            mNetClientPeer.addNetListenFun(nPackageId, fun);
        }

        public void SendLuaNetData(ushort nPackageId, byte[] buffer = null)
        {
            this.mNetClientPeer.SendLuaNetData(nPackageId, buffer);
        }

        public void SendNetData(ushort nPackageId, IMessage data = null)
        {
            this.mNetClientPeer.SendNetData(nPackageId, data);
        }

        public void Update(double elapsed)
        {
            if (elapsed >= 0.3)
            {
                NetLog.LogWarning("NetClient 帧 时间 太长: " + elapsed);
            }

            mNetClientPeer.Update(elapsed);
        }
	}
}

