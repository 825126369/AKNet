/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp1MSQuic.Common;
using System;

namespace AKNet.Udp1MSQuic.Server
{
    public class QuicNetServerMain : NetServerInterface,PrivateConfigInterface
    {
        QuicServer mServer = null;

        public QuicNetServerMain()
        {
            NetLog.Init();
            mServer = new QuicServer();
        }

        public void InitNet()
        {
            mServer.InitNet();
        }

        public void InitNet(int nPort)
        {
            mServer.InitNet(nPort);
        }

        public void InitNet(string Ip, int nPort)
        {
            mServer.InitNet(Ip, nPort);
        }

        public void Update(double elapsed)
        {
            mServer.Update(elapsed);
        }

        public SOCKET_SERVER_STATE GetServerState()
        {
            return mServer.GetServerState();
        }

        public int GetPort()
        {
            return mServer.GetPort();
        }

        public void Release()
        {
            mServer.Release();
        }

        public void addNetListenFunc(ushort id, Action<ClientPeerBase, NetPackage> func)
        {
            mServer.addNetListenFunc(id, func);
        }

        public void removeNetListenFunc(ushort id, Action<ClientPeerBase, NetPackage> func)
        {
            mServer.removeNetListenFunc(id, func);
        }

        public void addNetListenFunc(Action<ClientPeerBase, NetPackage> func)
        {
            mServer.addNetListenFunc(func);
        }

        public void removeNetListenFunc(Action<ClientPeerBase, NetPackage> func)
        {
            mServer.removeNetListenFunc(func);
        }

        public void addListenClientPeerStateFunc(Action<ClientPeerBase> mFunc)
        {
            mServer.addListenClientPeerStateFunc(mFunc);
        }

        public void removeListenClientPeerStateFunc(Action<ClientPeerBase> mFunc)
        {
            mServer.removeListenClientPeerStateFunc(mFunc);
        }

        public void addListenClientPeerStateFunc(Action<ClientPeerBase, SOCKET_PEER_STATE> mFunc)
        {
            mServer.addListenClientPeerStateFunc(mFunc);
        }

        public void removeListenClientPeerStateFunc(Action<ClientPeerBase, SOCKET_PEER_STATE> mFunc)
        {
            mServer.removeListenClientPeerStateFunc(mFunc);
        }

        public Config GetConfig()
        {
           return mServer.GetConfig();
        }
    }
}