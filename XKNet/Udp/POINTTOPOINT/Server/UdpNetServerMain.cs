using System;
using XKNet.Common;

namespace XKNet.Udp.POINTTOPOINT.Server
{
    public class UdpNetServerMain : ServerBase
    {
        private NetServer mNetServer;
        public UdpNetServerMain()
        {
            mNetServer = new NetServer();
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
    }

}