using System;
using XKNet.Common;
using XKNet.Tcp.Common;

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
            if (elapsed >= 0.3)
            {
                NetLog.LogWarning("XKNet.Tcp.Server 帧 时间 太长: " + elapsed);
            }

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
    }
}