/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/29 4:33:45
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
#if NET9_0_OR_GREATER

using AKNet.Common;
using AKNet.Quic.Common;
using System;

namespace AKNet.Quic.Server
{
    public class QuicNetServerMain : NetServerInterface,PrivateConfigInterface
    {
        QuicServer mServer = null;

        public QuicNetServerMain()
        {
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

#endif