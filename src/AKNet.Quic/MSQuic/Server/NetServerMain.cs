/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:20
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;

namespace AKNet.MSQuic.Server
{
    public class NetServerMain : QuicServerInterface
    {
        QuicServer mServer = null;

        public NetServerMain()
        {
            NetLog.Init();
            mServer = new QuicServer();
        }

        public void InitNet()
        {
            mServer.InitNet();
        }

        public void InitNet(int nPort)
        {
            mServer.InitNet(nPort);
        }

        public void InitNet(string Ip, int nPort)
        {
            mServer.InitNet(Ip, nPort);
        }

        public void Update(double elapsed)
        {
            mServer.Update(elapsed);
        }

        public SOCKET_SERVER_STATE GetServerState()
        {
            return mServer.GetServerState();
        }

        public int GetPort()
        {
            return mServer.GetPort();
        }

        public void Release()
        {
            mServer.Release();
        }

        public void addNetListenFunc(ushort id, Action<QuicClientPeerBase, NetPackage> func)
        {
            mServer.addNetListenFunc(id, func);
        }

        public void removeNetListenFunc(ushort id, Action<QuicClientPeerBase, NetPackage> func)
        {
            mServer.removeNetListenFunc(id, func);
        }

        public void addNetListenFunc(Action<QuicClientPeerBase, NetPackage> func)
        {
            mServer.addNetListenFunc(func);
        }

        public void removeNetListenFunc(Action<QuicClientPeerBase, NetPackage> func)
        {
            mServer.removeNetListenFunc(func);
        }

        public void addListenClientPeerStateFunc(Action<QuicClientPeerBase> mFunc)
        {
            mServer.addListenClientPeerStateFunc(mFunc);
        }

        public void removeListenClientPeerStateFunc(Action<QuicClientPeerBase> mFunc)
        {
            mServer.removeListenClientPeerStateFunc(mFunc);
        }

        public void addListenClientPeerStateFunc(Action<QuicClientPeerBase, SOCKET_PEER_STATE> mFunc)
        {
            mServer.addListenClientPeerStateFunc(mFunc);
        }

        public void removeListenClientPeerStateFunc(Action<QuicClientPeerBase, SOCKET_PEER_STATE> mFunc)
        {
            mServer.removeListenClientPeerStateFunc(mFunc);
        }
    }
}