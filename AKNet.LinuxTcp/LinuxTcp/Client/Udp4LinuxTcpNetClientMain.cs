/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:39
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.LinuxTcp.Common;
using System;
using System.Net;

namespace AKNet.LinuxTcp.Client
{
    public class Udp4LinuxTcpNetClientMain : NetClientInterface, ClientPeerBase,PrivateConfigInterface
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

        public Config GetConfig()
        {
            return mNetClientPeer.GetConfig();
        }


        public void SetName(string name)
        {
            mNetClientPeer.SetName(name);
        }

        public string GetName()
        {
            return mNetClientPeer.GetName();
        }

        public void SetID(uint id)
        {
            mNetClientPeer.SetID(id);
        }

        public uint GetID()
        {
            return mNetClientPeer.GetID();
        }

    }
}

