/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Net;

namespace AKNet.Common
{
    public class NetClientMainBase : NetClientInterface,ClientPeerBase
    {
        protected NetClientInterface mInterface = null;
        public NetClientInterface GetInstance()
        {
            return mInterface;
        }

        public NetClientMainBase(NetType nNetType = NetType.Quic)
        {
            if (nNetType == NetType.TCP)
            {
                mInterface = new AKNet.Tcp.Client.TcpNetClientMain();
            }
            else if (nNetType == NetType.Quic)
            {
                mInterface = new AKNet.Quic.Client.QuicNetClientMain();
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

        public void ConnectServer(string Ip, int nPort)
        {
            mInterface.ConnectServer(Ip, nPort);
        }

        public bool DisConnectServer()
        {
            return mInterface.DisConnectServer();
        }

        public IPEndPoint GetIPEndPoint()
        {
            return mInterface.GetIPEndPoint();
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

        public void SetName(string name)
        {
            mInterface.SetName(name);
        }

        public string GetName()
        {
            return mInterface.GetName();
        }

        public void SetID(uint id)
        {
            mInterface.SetID(id);
        }

        public uint GetID()
        {
            return mInterface.GetID();
        }
    }
}
