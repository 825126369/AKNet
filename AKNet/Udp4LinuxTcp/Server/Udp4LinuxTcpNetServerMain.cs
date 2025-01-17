/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/28 7:14:06
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using AKNet.Common;

namespace AKNet.Udp4LinuxTcp.Server
{
    public class Udp4LinuxTcpNetServerMain : NetServerInterface
    {
        private UdpServer mNetServer;
        public Udp4LinuxTcpNetServerMain(Udp3TcpConfig mUserConfig = null)
        {
            mNetServer = new UdpServer(mUserConfig);
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