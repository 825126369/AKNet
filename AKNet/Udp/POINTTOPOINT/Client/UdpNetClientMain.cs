﻿/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:AKNet 网络库, 兼容 C#8.0 和 .Net Standard 2.1
*        Author:阿珂
*        CreateTime:2024/10/30 21:55:41
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using Google.Protobuf;
using System;
using AKNet.Common;

namespace AKNet.Udp.POINTTOPOINT.Client
{
    public class UdpNetClientMain:UdpClientPeerBase, ClientPeerBase
	{
        private ClientPeer mNetClientPeer;

        public UdpNetClientMain()
        {
            this.mNetClientPeer = new ClientPeer();
        }

        public void addListenClientPeerStateFunc(Action<ClientPeerBase> mFunc)
        {
            this.mNetClientPeer.addListenClientPeerStateFunc(mFunc);
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

        public void removeListenClientPeerStateFunc(Action<ClientPeerBase> mFunc)
        {
            mNetClientPeer.removeListenClientPeerStateFunc(mFunc);
        }

        public void removeNetListenFun(ushort nPackageId, Action<ClientPeerBase, NetPackage> fun)
        {
            mNetClientPeer.addNetListenFun(nPackageId, fun);
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

        public void SetName(string name)
        {
            mNetClientPeer.SetName(name);
        }

        public string GetName()
        {
            return mNetClientPeer.GetName();
        }
    }
}

