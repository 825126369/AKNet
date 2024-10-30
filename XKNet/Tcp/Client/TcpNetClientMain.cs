/************************************Copyright*****************************************
*        ProjectName:XKNet
*        Web:https://github.com/825126369/XKNet
*        Description:XKNet 网络库, 兼容 C#8.0 和 .Net Standard 2.1
*        Author:阿珂
*        CreateTime:2024/10/30 12:14:19
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using Google.Protobuf;
using System;
using XKNet.Common;

namespace XKNet.Tcp.Client
{
    public class TcpNetClientMain : TcpClientPeerBase, ClientPeerBase
    {
        private ClientPeer mClientPeer;

        public TcpNetClientMain()
        {
            mClientPeer = new ClientPeer();
        }

        public void addListenClientPeerStateFunc(Action<ClientPeerBase> mFunc)
        {
            mClientPeer.addListenClientPeerStateFunc(mFunc);
        }

        public void addNetListenFun(ushort nPackageId, System.Action<ClientPeerBase, NetPackage> fun)
        {
            mClientPeer.addNetListenFun(nPackageId, fun);
        }

        public void ConnectServer(string Ip, int nPort)
        {
            mClientPeer.ConnectServer(Ip, nPort);
        }

        public bool DisConnectServer()
        {
            return mClientPeer.DisConnectServer();
        }

        public string GetIPAddress()
        {
            return mClientPeer.GetIPAddress();
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

        public void removeListenClientPeerStateFunc(Action<ClientPeerBase> mFunc)
        {
            mClientPeer.removeListenClientPeerStateFunc(mFunc);
        }

        public void removeNetListenFun(ushort nPackageId, System.Action<ClientPeerBase, NetPackage> fun)
        {
            mClientPeer.removeNetListenFun(nPackageId, fun);
        }

        public void Reset()
        {
            mClientPeer.Reset();
        }

        public void SendNetData(ushort nPackageId)
        {
            mClientPeer.SendNetData(nPackageId);
        }

        public void SendNetData(ushort nPackageId, byte[] buffer)
        {
            mClientPeer.SendNetData(nPackageId, buffer);
        }

        public void SendNetData(ushort nPackageId, IMessage data)
        {
            mClientPeer.SendNetData(nPackageId, data);
        }

        public void SendNetData(NetPackage mNetPackage)
        {
            mClientPeer.SendNetData(mNetPackage);
        }

        public void SendNetData(ushort nPackageId, ReadOnlySpan<byte> buffer)
        {
            mClientPeer.SendNetData(nPackageId, buffer);
        }

        public void SetNetCommonListenFun(Action<ClientPeerBase, NetPackage> func)
        {
            mClientPeer.SetNetCommonListenFun(func);
        }

        public void Update(double elapsed)
        { 
            mClientPeer.Update(elapsed);
        }

        public void SetName(string name)
        {
            mClientPeer.SetName(name);
        }

        public string GetName()
        {
            return mClientPeer.GetName();
        }
    }
}