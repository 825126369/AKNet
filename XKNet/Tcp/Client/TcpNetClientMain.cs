using Google.Protobuf;
using XKNet.Common;

namespace XKNet.Tcp.Client
{
    public class TcpNetClientMain : TcpClientPeerBase, ClientPeerBase
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

        public void ConnectServer(string Ip, int nPort)
        {
            mClientPeer.ConnectServer(Ip, nPort);
        }

        public bool DisConnectServer()
        {
            return mClientPeer.DisConnectServer();
        }

        public SOCKET_PEER_STATE GetSocketState()
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

        public void SendNetData(ushort nPackageId)
        {
            mClientPeer.SendNetData(nPackageId);
        }

        public void SendNetData(ushort nPackageId, byte[] buffer)
        {
            mClientPeer.SendNetData(nPackageId, buffer);
        }

        public void SendNetData(ushort nPackageId, IMessage data)
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