/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:16
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using AKNet.Common;
using AKNet.Udp2Tcp.Common;

namespace AKNet.Udp2Tcp.Server
{
    public class NetServerMain : NetServerInterface
    {
        private ServerMgr mNetServer;
        public NetServerMain()
        {
            mNetServer = new ServerMgr();
        }

        public void Update(double elapsed)
        {
            mNetServer.Update(elapsed);
        }

        public void InitNet(string Ip, int nPort)
        {
            mNetServer.InitNet(Ip, nPort);
        }

        public void Release()
        {
            mNetServer.Release();
        }

        public SOCKET_SERVER_STATE GetServerState()
        {
            return mNetServer.GetServerState();
        }

        public int GetPort()
        {
            return mNetServer.GetPort();
        }

        public void InitNet()
        {
            mNetServer.InitNet();
        }

        public void InitNet(int nPort)
        {
            mNetServer.InitNet(nPort);
        }

        public void addListenClientPeerStateFunc(Action<ClientPeerBase> mFunc)
        {
            mNetServer.addListenClientPeerStateFunc(mFunc);
        }

        public void removeListenClientPeerStateFunc(Action<ClientPeerBase> mFunc)
        {
            mNetServer.removeListenClientPeerStateFunc(mFunc);
        }

        public void addNetListenFunc(ushort id, Action<ClientPeerBase, NetPackage> mFunc)
        {
            mNetServer.addNetListenFunc(id, mFunc);
        }

        public void removeNetListenFunc(ushort id, Action<ClientPeerBase, NetPackage> mFunc)
        {
            mNetServer.removeNetListenFunc(id, mFunc);
        }

        public void addNetListenFunc(Action<ClientPeerBase, NetPackage> mFunc)
        {
            mNetServer.addNetListenFunc(mFunc);
        }

        public void removeNetListenFunc(Action<ClientPeerBase, NetPackage> mFunc)
        {
            mNetServer.removeNetListenFunc(mFunc);
        }

        public void addListenClientPeerStateFunc(Action<ClientPeerBase, SOCKET_PEER_STATE> mFunc)
        {
            mNetServer.addListenClientPeerStateFunc(mFunc);
        }

        public void removeListenClientPeerStateFunc(Action<ClientPeerBase, SOCKET_PEER_STATE> mFunc)
        {
            mNetServer.removeListenClientPeerStateFunc(mFunc);
        }
    }

}