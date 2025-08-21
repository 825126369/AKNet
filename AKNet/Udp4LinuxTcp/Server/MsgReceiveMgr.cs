/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp4LinuxTcp.Common;

namespace AKNet.Udp4LinuxTcp.Server
{
    internal class MsgReceiveMgr
	{
        private UdpServer mNetServer = null;
        private ClientPeer mClientPeer = null;
        private readonly AkCircularManyBuffer mReceiveStreamList = null;
        private readonly msghdr mTcpMsg = null; 

        public MsgReceiveMgr(UdpServer mNetServer, ClientPeer mClientPeer)
        {
			this.mNetServer = mNetServer;
			this.mClientPeer = mClientPeer;
            this.mReceiveStreamList = new AkCircularManyBuffer();
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
            
        }
	}
}