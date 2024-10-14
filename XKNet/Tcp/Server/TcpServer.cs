using System;
using XKNet.Common;

namespace XKNet.Tcp.Server
{
    internal class TcpServer : ServerBase
    {
        TCPSocket_Server mSocketMgr;
        public TcpServer()
        {
            ServerGlobalVariable.Instance.Init();
            mSocketMgr = new TCPSocket_Server(this);
        }

        public void addNetListenFun(ushort id, Action<ClientPeerBase, NetPackage> func)
        {
           ServerGlobalVariable.Instance.mPackageManager.addNetListenFun(id, func);
        }

        public void InitNet(string Ip, int nPort)
        {
            mSocketMgr.InitNet(Ip, nPort);
        }

        public void removeNetListenFun(ushort id, Action<ClientPeerBase, NetPackage> func)
        {
            ServerGlobalVariable.Instance.mPackageManager.removeNetListenFun(id, func);
        }

        public void Update(double elapsed)
        {
            if (elapsed >= 0.3)
            {
                NetLog.LogWarning("XKNet.Tcp.Server 帧 时间 太长: " + elapsed);
            }

            ServerGlobalVariable.Instance.mClientPeerManager.Update(elapsed);
        }
    }
}
