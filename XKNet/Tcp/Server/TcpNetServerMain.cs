using System;
using XKNet.Common;

namespace XKNet.Tcp.Server
{
    public class TcpNetServerMain : ServerBase
    {
        TcpServer mServer = null;

        public TcpNetServerMain()
        {
            mServer = new TcpServer();
        }

        public void InitNet(string Ip, int nPort)
        {
            mServer.InitNet(Ip, nPort);
        }

        public void Update(double elapsed)
        {
            mServer.Update(elapsed);
        }

        public void addNetListenFun(ushort id, Action<ClientPeerBase, NetPackage> func)
        {
            mServer.addNetListenFun(id, func);
        }

        public void removeNetListenFun(ushort id, Action<ClientPeerBase, NetPackage> func)
        {
            mServer.removeNetListenFun(id, func);
        }

        public SOCKET_SERVER_STATE GetServerState()
        {
            return mServer.GetServerState();
        }

        public void SetNetCommonListenFun(Action<ClientPeerBase, NetPackage> func)
        {
            mServer.SetNetCommonListenFun(func);
        }

        public int GetPort()
        {
            return mServer.GetPort();
        }

        public void InitNet()
        {
            mServer.InitNet();
        }

        public void InitNet(int nPort)
        {
            mServer.InitNet(nPort);
        }
    }
}