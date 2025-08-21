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

namespace AKNet.Tcp.Server
{
    public class TcpNetServerMain : NetServerInterface, PrivateInterface
    {
        readonly TcpServer mInstance = null;

        public TcpNetServerMain()
        {
            mInstance = new TcpServer();
        }

        public void InitNet()
        {
            mInstance.InitNet();
        }

        public void InitNet(int nPort)
        {
            mInstance.InitNet(nPort);
        }

        public void InitNet(string Ip, int nPort)
        {
            mInstance.InitNet(Ip, nPort);
        }

        public void Update(double elapsed)
        {
            mInstance.Update(elapsed);
        }

        public SOCKET_SERVER_STATE GetServerState()
        {
            return mInstance.GetServerState();
        }

        public int GetPort()
        {
            return mInstance.GetPort();
        }

        public void Release()
        {
            mInstance.Release();
        }

        public void addNetListenFunc(ushort id, Action<ClientPeerBase, NetPackage> func)
        {
            mInstance.addNetListenFunc(id, func);
        }

        public void removeNetListenFunc(ushort id, Action<ClientPeerBase, NetPackage> func)
        {
            mInstance.removeNetListenFunc(id, func);
        }

        public void addNetListenFunc(Action<ClientPeerBase, NetPackage> func)
        {
            mInstance.addNetListenFunc(func);
        }

        public void removeNetListenFunc(Action<ClientPeerBase, NetPackage> func)
        {
            mInstance.removeNetListenFunc(func);
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
            return mInstance.mConfig;
        }
    }
}