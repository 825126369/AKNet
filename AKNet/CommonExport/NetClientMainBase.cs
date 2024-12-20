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
    public class NetClientMainBase : NetClientInterface,ClientPeerBase
    {
        protected NetClientInterface mInterface = null;
        public NetClientMainBase()
        {
            mInterface = new AKNet.Udp.POINTTOPOINT.Client.UdpNetClientMain();
        }

        public NetClientMainBase(NetType nNetType = NetType.UDP)
        {
            if (nNetType == NetType.TCP)
            {
                mInterface = new AKNet.Tcp.Client.TcpNetClientMain();
            }
            else if (nNetType == NetType.UDP)
            {
                mInterface = new AKNet.Udp.POINTTOPOINT.Client.UdpNetClientMain();
            }
            else if (nNetType == NetType.Udp2Tcp)
            {
                mInterface = new AKNet.Udp2Tcp.Client.Udp2TcpNetClientMain();
            }
            else if (nNetType == NetType.Udp3Tcp)
            {
                mInterface = new AKNet.Udp3Tcp.Client.Udp3TcpNetClientMain();
            }
            else
            {
                NetLog.LogError("Unsupported network type: " + nNetType);
            }
        }

        public NetClientMainBase(NetConfigInterface IConfig)
        {
            if (IConfig == null)
            {
                NetLog.LogError("IConfig == null");
                return;
            }

            if (IConfig is TcpConfig)
            {
                mInterface = new AKNet.Tcp.Client.TcpNetClientMain(IConfig as TcpConfig);
            }
            else if (IConfig is UdpConfig)
            {
                mInterface = new AKNet.Udp.POINTTOPOINT.Client.UdpNetClientMain(IConfig as UdpConfig);
            }
            else if (IConfig is Udp2TcpConfig)
            {
                mInterface = new AKNet.Udp2Tcp.Client.Udp2TcpNetClientMain(IConfig as Udp2TcpConfig);
            }
            else if (IConfig is Udp3TcpConfig)
            {
                mInterface = new AKNet.Udp3Tcp.Client.Udp3TcpNetClientMain(IConfig as Udp3TcpConfig);
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

        public string GetIPAddress()
        {
            return mInterface.GetIPAddress();
        }

        public SOCKET_PEER_STATE GetSocketState()
        {
            return mInterface.GetSocketState();
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

        public void SendNetData(ushort nPackageId)
        {
            mInterface.SendNetData(nPackageId);
        }

        public void SendNetData(ushort nPackageId, byte[] data)
        {
            mInterface.SendNetData(nPackageId, data);
        }

        public void SendNetData(NetPackage mNetPackage)
        {
            mInterface.SendNetData(mNetPackage);
        }

        public void SendNetData(ushort nPackageId, ReadOnlySpan<byte> buffer)
        {
            mInterface.SendNetData(nPackageId, buffer);
        }

        public void Update(double elapsed)
        {
            mInterface.Update(elapsed);
        }
    }
}
