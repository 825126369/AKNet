/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/20 10:55:52
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
            mInterface = new AKNet.Udp.POINTTOPOINT.Server.UdpNetServerMain();
        }

        public NetServerMainBase(NetType nNetType = NetType.UDP)
        {
            if (nNetType == NetType.TCP)
            {
                mInterface = new AKNet.Tcp.Server.TcpNetServerMain();
            }
            else if (nNetType == NetType.UDP)
            {
                mInterface = new AKNet.Udp.POINTTOPOINT.Server.UdpNetServerMain();
            }
            else if (nNetType == NetType.Udp2Tcp)
            {
                mInterface = new AKNet.Udp2Tcp.Server.Udp2TcpNetServerMain();
            }
            else if (nNetType == NetType.Udp3Tcp)
            {
                mInterface = new AKNet.Udp3Tcp.Server.Udp3TcpNetServerMain();
            }
            else
            {
                NetLog.LogError("Unsupported network type: " + nNetType);
            }
        }

        public NetServerMainBase(NetConfigInterface IConfig)
        {
            if (IConfig == null)
            {
                NetLog.LogError("IConfig == null");
                return;
            }

            if (IConfig is TcpConfig)
            {
                mInterface = new AKNet.Tcp.Server.TcpNetServerMain();
            }
            else if (IConfig is UdpConfig)
            {
                mInterface = new AKNet.Udp.POINTTOPOINT.Server.UdpNetServerMain();
            }
            else if (IConfig is Udp2TcpConfig)
            {
                mInterface = new AKNet.Udp2Tcp.Server.Udp2TcpNetServerMain();
            }
            else if (IConfig is Udp3TcpConfig)
            {
                mInterface = new AKNet.Udp3Tcp.Server.Udp3TcpNetServerMain();
            }
            else
            {
                NetLog.LogError("Unsupported network type: " + IConfig.GetType().Name);
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
