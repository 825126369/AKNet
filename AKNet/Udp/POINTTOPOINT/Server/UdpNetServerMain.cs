/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:AKNet 网络库, 兼容 C#8.0 和 .Net Standard 2.1
*        Author:阿珂
*        CreateTime:2024/10/30 21:55:41
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using AKNet.Common;

namespace AKNet.Udp.POINTTOPOINT.Server
{
    public class UdpNetServerMain : ServerBase
    {
        private UdpServer mNetServer;
        public UdpNetServerMain()
        {
            mNetServer = new UdpServer();
        }

        public void Update(double elapsed)
        {
            mNetServer.Update(elapsed);
        }

        public void InitNet(string Ip, int nPort)
        {
            mNetServer.InitNet(Ip, nPort);
        }

        public void addNetListenFun(ushort id, Action<ClientPeerBase, NetPackage> func)
        {
            mNetServer.addNetListenFun(id, func);
        }

        public void removeNetListenFun(ushort id, Action<ClientPeerBase, NetPackage> func)
        {
            mNetServer.removeNetListenFun(id, func);
        }

        public void Release()
        {
            mNetServer.Release();
        }

        public void SetNetCommonListenFun(Action<ClientPeerBase, NetPackage> func)
        {
            mNetServer.SetNetCommonListenFun(func);
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
    }

}