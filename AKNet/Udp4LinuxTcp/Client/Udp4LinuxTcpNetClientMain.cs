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

namespace AKNet.Udp4LinuxTcp.Client
{
    public class Udp4LinuxTcpNetClientMain : NetClientInterface, ClientPeerBase
	{
        private ClientPeer mNetClientPeer;

        public Udp4LinuxTcpNetClientMain()
        {
            this.mNetClientPeer = new ClientPeer();
        }

        public void ConnectServer(string Ip, int nPort)
        {
            this.mNetClientPeer.ConnectServer(Ip, nPort);
        }

        public bool DisConnectServer()
        {
            return this.mNetClientPeer.DisConnectServer();
        }

        public IPEndPoint GetIPEndPoint()
        {
            return mNetClientPeer.GetIPEndPoint();
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

