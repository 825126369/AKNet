/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:15
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/

#if NET9_0_OR_GREATER

using AKNet.Common;
using AKNet.Quic.Common;
using System;
using System.Net;

namespace AKNet.Quic.Client
{
    public class NetClientMain : NetClientInterface
    {
        private ClientPeer mClientPeer;

        public NetClientMain()
        {
            mClientPeer = new ClientPeer();
        }

        public void ConnectServer(string Ip, int nPort)
        {
            mClientPeer.ConnectServer(Ip, nPort);
        }

        public bool DisConnectServer()
        {
            return mClientPeer.DisConnectServer();
        }

        public IPEndPoint GetIPEndPoint()
        {
            return mClientPeer.GetIPEndPoint();
        }

        public SOCKET_PEER_STATE GetSocketState()
        {
            return mClientPeer.GetSocketState();
        }

        public void ReConnectServer()
        {
            mClientPeer.ReConnectServer();
        }

        public void Release()
        {
            mClientPeer.Release();
        }

        public void SendNetData(ushort nPackageId)
        {
            mClientPeer.SendNetData(nPackageId);
        }

        public void SendNetData(ushort nPackageId, byte[] buffer)
        {
            mClientPeer.SendNetData(nPackageId, buffer);
        }

        public void SendNetData(NetPackage mNetPackage)
        {
            mClientPeer.SendNetData(mNetPackage);
        }

        public void SendNetData(ushort nPackageId, ReadOnlySpan<byte> buffer)
        {
            mClientPeer.SendNetData(nPackageId, buffer);
        }

        public void Update(double elapsed)
        {
            mClientPeer.Update(elapsed);
        }

        public void addNetListenFunc(ushort nPackageId, Action<ClientPeerBase, NetPackage> mFunc)
        {
            mClientPeer.addNetListenFunc(nPackageId, mFunc);
        }

        public void removeNetListenFunc(ushort nPackageId, Action<ClientPeerBase, NetPackage> mFunc)
        {
            mClientPeer.removeNetListenFunc(nPackageId, mFunc);
        }

        public void addNetListenFunc(Action<ClientPeerBase, NetPackage> mFunc)
        {
            mClientPeer.addNetListenFunc(mFunc);
        }

        public void removeNetListenFunc(Action<ClientPeerBase, NetPackage> mFunc)
        {
            mClientPeer.removeNetListenFunc(mFunc);
        }

        public void addListenClientPeerStateFunc(Action<ClientPeerBase> mFunc)
        {
            mClientPeer.addListenClientPeerStateFunc(mFunc);
        }

        public void removeListenClientPeerStateFunc(Action<ClientPeerBase> mFunc)
        {
            mClientPeer.removeListenClientPeerStateFunc(mFunc);
        }

        public void addListenClientPeerStateFunc(Action<ClientPeerBase, SOCKET_PEER_STATE> mFunc)
        {
            mClientPeer.addListenClientPeerStateFunc(mFunc);
        }

        public void removeListenClientPeerStateFunc(Action<ClientPeerBase, SOCKET_PEER_STATE> mFunc)
        {
            mClientPeer.removeListenClientPeerStateFunc(mFunc);
        }

        public void SetName(string name)
        {
            mClientPeer.SetName(name);
        }

        public string GetName()
        {
            return mClientPeer.GetName();
        }

        public void SetID(uint id)
        {
            mClientPeer.SetID(id);
        }

        public uint GetID()
        {
            return mClientPeer.GetID();
        }
    }
}

#endif