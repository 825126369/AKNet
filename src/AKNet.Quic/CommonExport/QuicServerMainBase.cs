/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:14
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Common
{
    public class QuicServerMainBase : QuicServerInterface
    {
        protected QuicServerInterface mInterface = null;
        public QuicServerInterface GetInstance()
        {
            return mInterface;
        }

        public void SetInstance(QuicServerInterface mInterface)
        {
            this.mInterface = mInterface;
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

        public void addListenClientPeerStateFunc(Action<QuicClientPeerBase, SOCKET_PEER_STATE> mFunc)
        {
            mInterface.addListenClientPeerStateFunc(mFunc);
        }

        public void addListenClientPeerStateFunc(Action<QuicClientPeerBase> mFunc)
        {
            mInterface.addListenClientPeerStateFunc(mFunc);
        }

        public void removeListenClientPeerStateFunc(Action<QuicClientPeerBase, SOCKET_PEER_STATE> mFunc)
        {
            mInterface.removeListenClientPeerStateFunc(mFunc);
        }

        public void removeListenClientPeerStateFunc(Action<QuicClientPeerBase> mFunc)
        {
            mInterface.removeListenClientPeerStateFunc(mFunc);
        }

   

        public void Update(double elapsed)
        {
            mInterface.Update(elapsed);
        }

        public void addNetListenFunc(ushort id, Action<QuicClientPeerBase, QuicStreamBase, QuicNetPackage> mFunc)
        {
            mInterface.addNetListenFunc(id, mFunc);
        }

        public void removeNetListenFunc(ushort id, Action<QuicClientPeerBase, QuicStreamBase, QuicNetPackage> mFunc)
        {
            mInterface.removeNetListenFunc(id, mFunc);
        }

        public void addNetListenFunc(Action<QuicClientPeerBase, QuicStreamBase, QuicNetPackage> mFunc)
        {
            mInterface.addNetListenFunc(mFunc);
        }

        public void removeNetListenFunc(Action<QuicClientPeerBase, QuicStreamBase, QuicNetPackage> mFunc)
        {
            mInterface.removeNetListenFunc(mFunc);
        }
    }
}
