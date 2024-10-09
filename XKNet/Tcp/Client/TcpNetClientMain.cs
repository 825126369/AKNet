using Google.Protobuf;
using XKNet.Common;
using XKNet.Tcp.Common;

namespace XKNet.Tcp.Client
{
    public class TcpNetClientMain : ClientPeerBase
    {
        private ClientPeer mClientPeer;

        public TcpNetClientMain()
        {
            mClientPeer = new ClientPeer();
        }

        public void addNetListenFun(ushort nPackageId, System.Action<ClientPeerBase, NetPackage> fun)
        {
            mClientPeer.addNetListenFun(nPackageId, fun);
        }

        public void ConnectServer(string Ip, ushort nPort)
        {
            mClientPeer.ConnectServer(Ip, nPort);
        }

        public CLIENT_SOCKET_PEER_STATE GetSocketState()
        {
            return mClientPeer.GetSocketState();
        }

        public void ReConnectServer()
        {
            mClientPeer.ReConnectServer();
        }

        public void Release()
        {
            mClientPeer.Release();
        }

        public void removeNetListenFun(ushort nPackageId, System.Action<ClientPeerBase, NetPackage> fun)
        {
            mClientPeer.removeNetListenFun(nPackageId, fun);
        }

        public void Reset()
        {
            mClientPeer.Reset();
        }

        public void SendLuaNetData(ushort nPackageId, byte[] buffer = null)
        {
            mClientPeer.SendLuaNetData(nPackageId, buffer);
        }

        public void SendNetData(ushort nPackageId, IMessage data = null)
        {
            mClientPeer.SendNetData(nPackageId, data);
        }

        public void Update(double elapsed)
        {
            if (elapsed >= 0.3)
            {
                NetLog.LogWarning("帧 时间 太长: " + elapsed);
            }

            mClientPeer.Update(elapsed);
        }
    }
}