/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/23 22:12:37
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using Google.Protobuf;
using System;
using AKNet.Common;

namespace AKNet.Udp.POINTTOPOINT.Client
{
    public class UdpNetClientMain:NetClientInterface, ClientPeerBase
	{
        private ClientPeer mNetClientPeer;

        public UdpNetClientMain(UdpConfig mUserConfig = null)
        {
            this.mNetClientPeer = new ClientPeer(mUserConfig);
        }

        public void ConnectServer(string Ip, int nPort)
        {
            this.mNetClientPeer.ConnectServer(Ip, nPort);
        }

        public bool DisConnectServer()
        {
            return this.mNetClientPeer.DisConnectServer();
        }

        public string GetIPAddress()
        {
            return mNetClientPeer.GetIPAddress();
        }

        public SOCKET_PEER_STATE GetSocketState()
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

        public void SendNetData(ushort nPackageId)
        {
            this.mNetClientPeer.SendNetData(nPackageId);
        }
        
        public void SendNetData(ushort nPackageId, byte[] buffer = null)
        {
            this.mNetClientPeer.SendNetData(nPackageId, buffer);
        }

        public void SendNetData(ushort nPackageId, IMessage data = null)
        {
            this.mNetClientPeer.SendNetData(nPackageId, data);
        }

        public void SendNetData(NetPackage mNetPackage)
        {
            this.mNetClientPeer.SendNetData(mNetPackage);
        }

        public void SendNetData(ushort nPackageId, ReadOnlySpan<byte> buffer)
        {
            this.mNetClientPeer.SendNetData(nPackageId, buffer);
        }

        public void Update(double elapsed)
        {
            mNetClientPeer.Update(elapsed);
        }

        public void addNetListenFunc(ushort nPackageId, Action<ClientPeerBase, NetPackage> mFunc)
        {
            mNetClientPeer.addNetListenFunc(nPackageId, mFunc);
        }

        public void removeNetListenFunc(ushort nPackageId, Action<ClientPeerBase, NetPackage> mFunc)
        {
            mNetClientPeer.removeNetListenFunc(nPackageId, mFunc);
        }

        public void addNetListenFunc(Action<ClientPeerBase, NetPackage> mFunc)
        {
            mNetClientPeer.addNetListenFunc(mFunc);
        }

        public void removeNetListenFunc(Action<ClientPeerBase, NetPackage> mFunc)
        {
            mNetClientPeer.removeNetListenFunc(mFunc);
        }

        public void addListenClientPeerStateFunc(Action<ClientPeerBase, SOCKET_PEER_STATE> mFunc)
        {
            mNetClientPeer.addListenClientPeerStateFunc(mFunc);
        }

        public void removeListenClientPeerStateFunc(Action<ClientPeerBase, SOCKET_PEER_STATE> mFunc)
        {
            mNetClientPeer.removeListenClientPeerStateFunc(mFunc);
        }

        public void addListenClientPeerStateFunc(Action<ClientPeerBase> mFunc)
        {
            mNetClientPeer.addListenClientPeerStateFunc(mFunc);
        }

        public void removeListenClientPeerStateFunc(Action<ClientPeerBase> mFunc)
        {
            mNetClientPeer.removeListenClientPeerStateFunc(mFunc);
        }
    }
}

