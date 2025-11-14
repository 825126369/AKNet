/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:42
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp4LinuxTcp.Common;

namespace AKNet.Udp4LinuxTcp.Server
{
    internal class MsgReceiveMgr
	{
        private UdpServer mNetServer = null;
        private ClientPeerPrivate mClientPeer = null;
        private readonly NetStreamCircularBuffer mReceiveStreamList = null;
        private readonly msghdr mTcpMsg = null; 

        public MsgReceiveMgr(UdpServer mNetServer, ClientPeerPrivate mClientPeer)
        {
			this.mNetServer = mNetServer;
			this.mClientPeer = mClientPeer;
            this.mReceiveStreamList = new NetStreamCircularBuffer();
            this.mTcpMsg = new msghdr(mReceiveStreamList, 1500);
        }

        public void Update(double elapsed)
        {
            GetReceiveCheckPackage();
            ReceiveTcpStream();
        }

        private void GetReceiveCheckPackage()
        {
            sk_buff mPackage = null;
            do
            {
                mPackage = mClientPeer.mSocketMgr.GetReceivePackage();
                if (mPackage != null)
                {
                    mClientPeer.mUdpCheckPool.ReceiveNetPackage(mPackage);
                }
            }
            while (mPackage != null);
        }

        public void ReceiveTcpStream()
        {
            while (mClientPeer.mUdpCheckPool.ReceiveTcpStream(mTcpMsg))
            {
                while (NetTcpPackageExecute())
                {

                }
            }
        }

        private bool NetTcpPackageExecute()
        {
            var mNetPackage = mNetServer.GetLikeTcpNetPackage();
            bool bSuccess = mNetServer.mCryptoMgr.Decode(mReceiveStreamList, mNetPackage);
            if (bSuccess)
            {
                mClientPeer.NetPackageExecute(mNetPackage);
            }
            return bSuccess;
        }

        public void Reset()
		{
            mReceiveStreamList.Reset();
        }

        public void Release()
        {
            mReceiveStreamList.Dispose();
        }
    }
}