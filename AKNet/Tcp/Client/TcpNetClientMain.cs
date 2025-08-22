/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Tcp.Common;
using System;
using System.Net;

namespace AKNet.Tcp.Client
{
    public class TcpNetClientMain : NetClientInterface, PrivateConfigInterface
    {
        private readonly ClientPeer mInstance;

        public TcpNetClientMain()
        {
            mInstance = new ClientPeer();
        }

        public void ConnectServer(string Ip, int nPort)
        {
            mInstance.ConnectServer(Ip, nPort);
        }

        public bool DisConnectServer()
        {
            return mInstance.DisConnectServer();
        }

        public IPEndPoint GetIPEndPoint()
        {
            return mInstance.GetIPEndPoint();
        }

        public SOCKET_PEER_STATE GetSocketState()
        {
            return mInstance.GetSocketState();
        }

        public void ReConnectServer()
        {
            mInstance.ReConnectServer();
        }

        public void Release()
        {
            mInstance.Release();
        }

        public void SendNetData(ushort nPackageId)
        {
            mInstance.SendNetData(nPackageId);
        }

        public void SendNetData(ushort nPackageId, byte[] buffer)
        {
            mInstance.SendNetData(nPackageId, buffer);
        }

        public void SendNetData(NetPackage mNetPackage)
        {
            mInstance.SendNetData(mNetPackage);
        }

        public void SendNetData(ushort nPackageId, ReadOnlySpan<byte> buffer)
        {
            mInstance.SendNetData(nPackageId, buffer);
        }

        public void Update(double elapsed)
        {
            mInstance.Update(elapsed);
        }

        public void addNetListenFunc(ushort nPackageId, Action<ClientPeerBase, NetPackage> mFunc)
        {
            mInstance.addNetListenFunc(nPackageId, mFunc);
        }

        public void removeNetListenFunc(ushort nPackageId, Action<ClientPeerBase, NetPackage> mFunc)
        {
            mInstance.removeNetListenFunc(nPackageId, mFunc);
        }

        public void addNetListenFunc(Action<ClientPeerBase, NetPackage> mFunc)
        {
            mInstance.addNetListenFunc(mFunc);
        }

        public void removeNetListenFunc(Action<ClientPeerBase, NetPackage> mFunc)
        {
            mInstance.removeNetListenFunc(mFunc);
        }

        public void addListenClientPeerStateFunc(Action<ClientPeerBase> mFunc)
        {
            mInstance.addListenClientPeerStateFunc(mFunc);
        }

        public void removeListenClientPeerStateFunc(Action<ClientPeerBase> mFunc)
        {
            mInstance.removeListenClientPeerStateFunc(mFunc);
        }

        public void addListenClientPeerStateFunc(Action<ClientPeerBase, SOCKET_PEER_STATE> mFunc)
        {
            mInstance.addListenClientPeerStateFunc(mFunc);
        }

        public void removeListenClientPeerStateFunc(Action<ClientPeerBase, SOCKET_PEER_STATE> mFunc)
        {
            mInstance.removeListenClientPeerStateFunc(mFunc);
        }

        public Config GetConfig()
        {
            return mInstance.GetConfig();
        }
    }

}