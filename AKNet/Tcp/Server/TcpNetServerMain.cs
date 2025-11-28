/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/29 4:33:38
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using AKNet.Common;
using AKNet.Tcp.Common;

namespace AKNet.Tcp.Server
{
    public class TcpNetServerMain : NetServerInterface
    {
        TcpServer mServer = null;

        public TcpNetServerMain()
        {
            mServer = new TcpServer();
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
    }
}