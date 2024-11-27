/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/23 22:12:36
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
namespace AKNet.Common
{
    public class NetClientMain : NetClientInterface
    {
        NetClientInterface mInterface = null;
        public NetClientMain()
        {
            mInterface = new AKNet.Udp.POINTTOPOINT.Client.UdpNetClientMain();
        }

        public NetClientMain(NetType nNetType = NetType.UDP)
        {
            if (nNetType == NetType.TCP)
            {
                mInterface = new AKNet.Tcp.Client.TcpNetClientMain();
            }
            else if (nNetType == NetType.UDP)
            {
                mInterface = new AKNet.Udp.POINTTOPOINT.Client.UdpNetClientMain();
            }
            else
            {
                NetLog.LogError("Unsupported network type: " + nNetType);
            }
        }

        public NetClientMain(NetConfigInterface IConfig)
        {
            if (IConfig == null)
            {
                NetLog.LogError("IConfig == null");
                return;
            }

            if (IConfig is TcpConfig)
            {
                mInterface = new AKNet.Tcp.Client.TcpNetClientMain();
            }
            else if (IConfig is UdpConfig)
            {
                mInterface = new AKNet.Udp.POINTTOPOINT.Client.UdpNetClientMain();
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

        public void ConnectServer(string Ip, int nPort)
        {
            mInterface.ConnectServer(Ip, nPort);
        }

        public bool DisConnectServer()
        {
            return mInterface.DisConnectServer();
        }

        public void ReConnectServer()
        {
            mInterface.ReConnectServer();
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
