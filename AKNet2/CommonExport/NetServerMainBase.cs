/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
namespace AKNet.Common
{
    public class NetServerMainBase : NetServerInterface
    {
        protected NetServerInterface mInterface = null;
        public NetServerMainBase()
        {
            mInterface = new AKNet.Quic.Server.QuicNetServerMain();
        }

        public NetServerMainBase(NetType nNetType = NetType.Quic)
        {
            if (nNetType == NetType.TCP)
            {
                mInterface = new AKNet.Tcp.Server.TcpNetServerMain();
            }
            else if (nNetType == NetType.Quic)
            {
                mInterface = new AKNet.Quic.Server.QuicNetServerMain();
            }
            else
            {
                NetLog.LogError("Unsupported network type: " + nNetType);
            }
        }

        public void addListenClientPeerStateFunc(Action<ClientPeerBase, SOCKET_PEER_STATE> mFunc)
        {
            mInterface.addListenClientPeerStateFunc(mFunc);
        }

        public void addListenClientPeerStateFunc(Action<ClientPeerBase> mFunc)
        {
            mInterface.addListenClientPeerStateFunc(mFunc);
        }

        public void addNetListenFunc(ushort nPackageId, Action<ClientPeerBase, NetPackage> mFunc)
        {
            mInterface.addNetListenFunc(nPackageId, mFunc);
        }

        public void addNetListenFunc(Action<ClientPeerBase, NetPackage> mFunc)
        {
            mInterface.addNetListenFunc(mFunc);
        }

        public int GetPort()
        {
            return mInterface.GetPort();
        }

        public SOCKET_SERVER_STATE GetServerState()
        {
            return mInterface.GetServerState();
        }

        public void InitNet()
        {
            mInterface.InitNet();
        }

        public void InitNet(int nPort)
        {
            mInterface.InitNet(nPort);
        }

        public void InitNet(string Ip, int nPort)
        {
            mInterface.InitNet(Ip, nPort);
        }

        public void Release()
        {
            mInterface.Release();
        }

        public void removeListenClientPeerStateFunc(Action<ClientPeerBase, SOCKET_PEER_STATE> mFunc)
        {
            mInterface.removeListenClientPeerStateFunc(mFunc);
        }

        public void removeListenClientPeerStateFunc(Action<ClientPeerBase> mFunc)
        {
            mInterface.removeListenClientPeerStateFunc(mFunc);
        }

        public void removeNetListenFunc(ushort nPackageId, Action<ClientPeerBase, NetPackage> mFunc)
        {
            mInterface.removeNetListenFunc(nPackageId, mFunc);
        }

        public void removeNetListenFunc(Action<ClientPeerBase, NetPackage> mFunc)
        {
            mInterface.removeNetListenFunc(mFunc);
        }

        public void Update(double elapsed)
        {
            mInterface.Update(elapsed);
        }
    }
}
